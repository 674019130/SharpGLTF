﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Linq;

using SharpGLTF.Schema2;

namespace SharpGLTF
{
    /// <summary>
    /// Extensions used internally.
    /// </summary>
    static class _Extensions
    {
        #region private numerics extensions

        internal static bool IsMultipleOf(this int value, int mult)
        {
            return (value % mult) == 0;
        }

        internal static int PaddingSize(this int size, int mult)
        {
            var rest = size % mult;

            return rest == 0 ? 0 : mult - rest;
        }

        internal static int WordPadded(this int length)
        {
            var padding = length & 3;

            return length + (padding == 0 ? 0 : 4 - padding);
        }

        internal static bool _IsReal(this float value)
        {
            return !(float.IsNaN(value) | float.IsInfinity(value));
        }

        internal static bool _IsReal(this Vector2 v)
        {
            return v.X._IsReal() & v.Y._IsReal();
        }

        internal static bool _IsReal(this Vector3 v)
        {
            return v.X._IsReal() & v.Y._IsReal() & v.Z._IsReal();
        }

        internal static bool _IsReal(this Vector4 v)
        {
            return v.X._IsReal() & v.Y._IsReal() & v.Z._IsReal() & v.W._IsReal();
        }

        internal static bool _IsReal(this Matrix4x4 v)
        {
            if (!(v.M11._IsReal() & v.M12._IsReal() & v.M13._IsReal() & v.M14._IsReal())) return false;
            if (!(v.M21._IsReal() & v.M22._IsReal() & v.M23._IsReal() & v.M24._IsReal())) return false;
            if (!(v.M31._IsReal() & v.M32._IsReal() & v.M33._IsReal() & v.M34._IsReal())) return false;
            if (!(v.M41._IsReal() & v.M42._IsReal() & v.M43._IsReal() & v.M44._IsReal())) return false;
            return true;
        }

        internal static bool _IsReal(this Quaternion v)
        {
            return v.X._IsReal() & v.Y._IsReal() & v.Z._IsReal() & v.W._IsReal();
        }

        internal static Vector3 WithLength(this Vector3 v, float len)
        {
            return Vector3.Normalize(v) * len;
        }

        internal static Quaternion AsQuaternion(this Vector4 v)
        {
            return new Quaternion(v.X, v.Y, v.Z, v.W);
        }

        internal static Boolean IsNormalized(this Quaternion q)
        {
            // As per: https://github.com/KhronosGroup/glTF-Validator/issues/33 , quaternions need to be normalized.

            return Math.Abs(1.0 - q.Length()) > 0.000005;
        }

        internal static Quaternion Sanitized(this Quaternion q)
        {
            return q.IsNormalized() ? q : Quaternion.Normalize(q);
        }

        internal static bool IsInRange(this Vector3 value, Vector3 min, Vector3 max)
        {
            if (value.X < min.X || value.X > max.X) return false;
            if (value.Y < min.Y || value.Y > max.Y) return false;
            if (value.Z < min.Z || value.Z > max.Z) return false;
            return true;
        }

        internal static bool IsInRange(this Vector4 value, Vector4 min, Vector4 max)
        {
            if (value.X < min.X || value.X > max.X) return false;
            if (value.Y < min.Y || value.Y > max.Y) return false;
            if (value.Z < min.Z || value.Z > max.Z) return false;
            if (value.W < min.W || value.W > max.W) return false;
            return true;
        }

        internal static bool IsRound(this Vector4 value)
        {
            var r = new Vector4((int)value.X, (int)value.Y, (int)value.Z, (int)value.W);

            return (value - r) == Vector4.Zero;
        }

        internal static void Validate(this Vector3 vector, string msg)
        {
            if (!vector._IsReal()) throw new NotFiniteNumberException($"{msg} is invalid.");
        }

        internal static void ValidateNormal(this Vector3 normal, string msg)
        {
            if (!normal._IsReal()) throw new NotFiniteNumberException($"{msg} is invalid.");

            var len = normal.Length();

            if (len < 0.99f || len > 1.01f) throw new ArithmeticException($"{msg} is not unit length.");
        }

        internal static void ValidateTangent(this Vector4 tangent, string msg)
        {
            if (tangent.W != 1 && tangent.W != -1) throw new ArithmeticException(msg);

            new Vector3(tangent.X, tangent.Y, tangent.Z).ValidateNormal(msg);
        }

        internal static bool IsValidNormal(this Vector3 normal)
        {
            if (!normal._IsReal()) return false;

            var len = normal.Length();

            if (len < 0.99f || len > 1.01f) return false;

            return true;
        }

        internal static bool IsValidTangent(this Vector4 tangent)
        {
            if (tangent.W != 1 && tangent.W != -1) return false;

            return new Vector3(tangent.X, tangent.Y, tangent.Z).IsValidNormal();
        }

        internal static Matrix4x4 Inverse(this Matrix4x4 src)
        {
            if (!Matrix4x4.Invert(src, out Matrix4x4 dst)) Guard.IsTrue(false, nameof(src), "Matrix cannot be inverted.");

            return dst;
        }

        internal static (T, T, float) GetSample<T>(this IEnumerable<(float, T)> sequence, float offset)
        {
            (float, T)? left = null;
            (float, T)? right = null;
            (float, T)? prev = null;

            if (offset < 0) offset = 0;

            foreach (var item in sequence)
            {
                if (item.Item1 == offset)
                {
                    left = item; continue;
                }

                if (item.Item1 > offset)
                {
                    if (left == null) left = prev;
                    right = item;
                    break;
                }

                prev = item;
            }

            if (left == null && right == null) return (default(T), default(T), 0);
            if (left == null) return (right.Value.Item2, right.Value.Item2, 0);
            if (right == null) return (left.Value.Item2, left.Value.Item2, 0);

            System.Diagnostics.Debug.Assert(left.Value.Item1 < right.Value.Item1);

            var amount = (offset - left.Value.Item1) / (right.Value.Item1 - left.Value.Item1);

            System.Diagnostics.Debug.Assert(amount >= 0 && amount <= 1);

            return (left.Value.Item2, right.Value.Item2, amount);
        }

        internal static Func<float, Vector3> GetLinearSamplerFunc(this IEnumerable<(float, Vector3)> collection)
        {
            if (collection == null) return null;

            Vector3 _sampler(float offset)
            {
                var sample = collection.GetSample(offset);
                return Vector3.Lerp(sample.Item1, sample.Item2, sample.Item3);
            }

            return _sampler;
        }

        internal static Func<float, Quaternion> GetLinearSamplerFunc(this IEnumerable<(float, Quaternion)> collection)
        {
            if (collection == null) return null;

            Quaternion _sampler(float offset)
            {
                var sample = collection.GetSample(offset);
                return Quaternion.Slerp(sample.Item1, sample.Item2, sample.Item3);
            }

            return _sampler;
        }

        internal static Func<float, float[]> GetLinearSamplerFunc(this IEnumerable<(float, float[])> collection)
        {
            if (collection == null) return null;

            float[] _sampler(float offset)
            {
                var sample = collection.GetSample(offset);
                var result = new float[sample.Item1.Length];

                for (int i = 0; i < result.Length; ++i)
                {
                    result[i] = sample.Item1[i] * (1 - sample.Item3) + sample.Item2[i] * sample.Item3;
                }

                return result;
            }

            return _sampler;
        }

        #endregion

        #region linq

        internal static ArraySegment<T> Slice<T>(this T[] array, int offset)
        {
            return new ArraySegment<T>(array, offset, array.Length - offset);
        }

        internal static ArraySegment<T> Slice<T>(this ArraySegment<T> array, int offset)
        {
            return new ArraySegment<T>(array.Array, array.Offset + offset, array.Count - offset);
        }

        internal static ArraySegment<T> Slice<T>(this ArraySegment<T> array, int offset, int count)
        {
            return new ArraySegment<T>(array.Array, array.Offset + offset, count);
        }

        internal static T[] CloneArray<T>(this T[] srcArray)
        {
            if (srcArray == null) return null;

            var dstArray = new T[srcArray.Length];
            srcArray.CopyTo(dstArray, 0);

            return dstArray;
        }

        internal static void Fill<T>(this IList<T> collection, T value)
        {
            for (int i = 0; i < collection.Count; ++i)
            {
                collection[i] = value;
            }
        }

        internal static void Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = value;
            }
        }

        internal static int IndexOf<T>(this IReadOnlyList<T> collection, T value)
        {
            var l = collection.Count;

            for (int i = 0; i < l; ++i)
            {
                if (Object.Equals(collection[i], value)) return i;
            }

            return -1;
        }

        internal static int IndexOf<T>(this IReadOnlyList<T> collection, Predicate<T> predicate)
        {
            var l = collection.Count;

            for (int i = 0; i < l; ++i)
            {
                if (predicate(collection[i])) return i;
            }

            return -1;
        }

        internal static int IndexOfReference<T>(this IReadOnlyList<T> collection, T value)
            where T : class
        {
            var l = collection.Count;

            for (int i = 0; i < l; ++i)
            {
                if (Object.ReferenceEquals(collection[i], value)) return i;
            }

            return -1;
        }

        internal static int IndexOf<T>(this IReadOnlyList<T> collection, T[] subset)
            where T : IEquatable<T>
        {
            var l = collection.Count - subset.Length;

            for (int i = 0; i < l; ++i)
            {
                bool r = false;

                for (int j = 0; j < subset.Length; ++j)
                {
                    if (!collection[i + j].Equals(subset[j])) break;
                    r = true;
                }

                if (r) return i;
            }

            return -1;
        }

        internal static void CopyTo<T>(this T[] src, int srcOffset, IList<T> dst, int dstOffset, int count)
        {
            var srcArray = new ArraySegment<T>(src);

            srcArray.CopyTo(srcOffset, dst, dstOffset, count);
        }

        internal static void CopyTo<T>(this ArraySegment<T> src, int srcOffset, IList<T> dst, int dstOffset, int count)
        {
            if (dst is T[] dstArray)
            {
                Array.Copy(src.Array, src.Offset + srcOffset, dstArray, dstOffset, count);
                return;
            }

            for (int i = 0; i < count; ++i)
            {
                dst[dstOffset + i] = src.Array[src.Offset + srcOffset + i];
            }
        }

        internal static void AddRange<Tin, Tout>(this IList<Tout> dst, IEnumerable<Tin> src, Func<Tin, Tout> cvt)
        {
            foreach (var item in src)
            {
                dst.Add(cvt(item));
            }
        }

        internal static IEnumerable<T> ConcatItems<T>(this IEnumerable<T> collection, params T[] instances)
        {
            return collection.Concat(instances.Where(item => item != null));
        }

        #endregion

        #region images

        internal static bool _IsPngImage(this IReadOnlyList<Byte> data)
        {
            if (data[0] != 0x89) return false;
            if (data[1] != 0x50) return false;
            if (data[2] != 0x4e) return false;
            if (data[3] != 0x47) return false;

            return true;
        }

        internal static bool _IsJpgImage(this IReadOnlyList<Byte> data)
        {
            if (data[0] != 0xff) return false;
            if (data[1] != 0xd8) return false;

            return true;
        }

        internal static bool _IsDdsImage(this IReadOnlyList<Byte> data)
        {
            if (data[0] != 0x44) return false;
            if (data[1] != 0x44) return false;
            if (data[2] != 0x53) return false;
            if (data[3] != 0x20) return false;
            return true;
        }

        internal static bool _IsWebpImage(this IReadOnlyList<Byte> data)
        {
            // RIFF
            if (data[0] != 0x52) return false;
            if (data[1] != 0x49) return false;
            if (data[2] != 0x46) return false;
            if (data[3] != 0x46) return false;

            // WEBP
            if (data[8] != 0x57) return false;
            if (data[9] != 0x45) return false;
            if (data[10] != 0x42) return false;
            if (data[11] != 0x50) return false;

            return true;
        }

        #endregion

        #region vertex & index accessors

        public static int ByteLength(this IndexEncodingType encoding)
        {
            switch (encoding)
            {
                case IndexEncodingType.UNSIGNED_BYTE: return 1;
                case IndexEncodingType.UNSIGNED_SHORT: return 2;
                case IndexEncodingType.UNSIGNED_INT: return 4;

                default: throw new NotImplementedException();
            }
        }

        public static int ByteLength(this EncodingType encoding)
        {
            switch (encoding)
            {
                case EncodingType.BYTE: return 1;
                case EncodingType.SHORT: return 2;
                case EncodingType.FLOAT: return 4;

                case EncodingType.UNSIGNED_BYTE: return 1;
                case EncodingType.UNSIGNED_SHORT: return 2;
                case EncodingType.UNSIGNED_INT: return 4;

                default: throw new NotImplementedException();
            }
        }

        public static EncodingType ToComponent(this IndexEncodingType t)
        {
            switch (t)
            {
                case IndexEncodingType.UNSIGNED_BYTE: return EncodingType.UNSIGNED_BYTE;
                case IndexEncodingType.UNSIGNED_SHORT: return EncodingType.UNSIGNED_SHORT;
                case IndexEncodingType.UNSIGNED_INT: return EncodingType.UNSIGNED_INT;

                default: throw new NotImplementedException();
            }
        }

        public static IndexEncodingType ToIndex(this EncodingType t)
        {
            switch (t)
            {
                case EncodingType.UNSIGNED_BYTE: return IndexEncodingType.UNSIGNED_BYTE;
                case EncodingType.UNSIGNED_SHORT: return IndexEncodingType.UNSIGNED_SHORT;
                case EncodingType.UNSIGNED_INT: return IndexEncodingType.UNSIGNED_INT;

                default: throw new NotImplementedException();
            }
        }

        public static int DimCount(this DimensionType dimension)
        {
            switch (dimension)
            {
                case DimensionType.SCALAR: return 1;
                case DimensionType.VEC2: return 2;
                case DimensionType.VEC3: return 3;
                case DimensionType.VEC4: return 4;
                case DimensionType.MAT2: return 4;
                case DimensionType.MAT3: return 9;
                case DimensionType.MAT4: return 16;
                default: throw new NotImplementedException();
            }
        }

        internal static DimensionType ToDimension(this int l)
        {
            switch (l)
            {
                case 1: return DimensionType.SCALAR;
                case 2: return DimensionType.VEC2;
                case 3: return DimensionType.VEC3;
                case 4: return DimensionType.VEC4;
                // case 4: return ElementType.MAT2;
                case 9: return DimensionType.MAT3;
                case 16: return DimensionType.MAT4;
                default: throw new NotImplementedException();
            }
        }

        public static IEnumerable<(int, int)> GetLinesIndices(this PrimitiveType ptype, int count)
        {
            return ptype.GetLinesIndices(Enumerable.Range(0, count).Select(item => (UInt32)item));
        }

        public static IEnumerable<(int, int)> GetLinesIndices(this PrimitiveType ptype, IEnumerable<UInt32> sourceIndices)
        {
            switch (ptype)
            {
                case PrimitiveType.LINES:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var a = ptr.Current;
                                if (!ptr.MoveNext()) break;
                                var b = ptr.Current;

                                if (!_IsDegenerated(a, b)) yield return ((int)a, (int)b);
                            }
                        }

                        break;
                    }
            }
        }

        public static IEnumerable<(int, int, int)> GetTrianglesIndices(this PrimitiveType ptype, int count)
        {
            return ptype.GetTrianglesIndices(Enumerable.Range(0, count).Select(item => (UInt32)item));
        }

        public static IEnumerable<(int, int, int)> GetTrianglesIndices(this PrimitiveType ptype, IEnumerable<UInt32> sourceIndices)
        {
            switch (ptype)
            {
                case PrimitiveType.TRIANGLES:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var a = ptr.Current;
                                if (!ptr.MoveNext()) break;
                                var b = ptr.Current;
                                if (!ptr.MoveNext()) break;
                                var c = ptr.Current;

                                if (!_IsDegenerated(a, b, c)) yield return ((int)a, (int)b, (int)c);
                            }
                        }

                        break;
                    }

                case PrimitiveType.TRIANGLE_FAN:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            if (!ptr.MoveNext()) break;
                            var a = ptr.Current;
                            if (!ptr.MoveNext()) break;
                            var b = ptr.Current;

                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var c = ptr.Current;

                                if (!_IsDegenerated(a, b, c)) yield return ((int)a, (int)b, (int)c);

                                b = c;
                            }
                        }

                        break;
                    }

                case PrimitiveType.TRIANGLE_STRIP:
                    {
                        using (var ptr = sourceIndices.GetEnumerator())
                        {
                            if (!ptr.MoveNext()) break;
                            var a = ptr.Current;
                            if (!ptr.MoveNext()) break;
                            var b = ptr.Current;

                            bool reversed = false;

                            while (true)
                            {
                                if (!ptr.MoveNext()) break;
                                var c = ptr.Current;

                                if (!_IsDegenerated(a, b, c))
                                {
                                    if (reversed) yield return ((int)b, (int)a, (int)c);
                                    else yield return ((int)a, (int)b, (int)c);
                                }

                                a = b;
                                b = c;
                                reversed = !reversed;
                            }
                        }

                        break;
                    }
            }
        }

        private static bool _IsDegenerated(uint a, uint b)
        {
            return a == b;
        }

        private static bool _IsDegenerated(uint a, uint b, uint c)
        {
            if (a == b) return true;
            if (a == c) return true;
            if (b == c) return true;
            return false;
        }

        #endregion

        #region serialization

        public static ArraySegment<Byte> ToArraySegment(this System.IO.MemoryStream m)
        {
            if (m.TryGetBuffer(out ArraySegment<Byte> data)) return data;
            return new ArraySegment<byte>(m.ToArray());
        }

        public static Byte[] GetPaddedContent(this Byte[] content)
        {
            if (content == null) return null;

            if (content.Length.IsMultipleOf(4)) return content;

            var paddedContent = new Byte[content.Length + content.Length.PaddingSize(4)];
            content.CopyTo(paddedContent, 0);
            return paddedContent;
        }

        #endregion
    }
}
