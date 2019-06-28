﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using SharpGLTF.Memory;

using JOINTWEIGHT = System.Collections.Generic.KeyValuePair<int, float>;

namespace SharpGLTF.Geometry.VertexTypes
{
    static class VertexUtils
    {
        public static bool SanitizeVertex<TvG>(this TvG inVertex, out TvG outVertex)
            where TvG : struct, IVertexGeometry
        {
            outVertex = inVertex;

            var p = inVertex.GetPosition();

            if (!p._IsReal()) return false;

            if (inVertex.TryGetNormal(out Vector3 n))
            {
                if (!n._IsReal()) return false;
                if (n == Vector3.Zero) n = p;
                if (n == Vector3.Zero) return false;

                var l = n.Length();
                if (l < 0.99f || l > 0.01f) outVertex.SetNormal(Vector3.Normalize(n));
            }

            if (inVertex.TryGetTangent(out Vector4 tw))
            {
                if (!tw._IsReal()) return false;

                var t = new Vector3(tw.X, tw.Y, tw.Z);
                if (t == Vector3.Zero) return false;

                if (tw.W > 0) tw.W = 1;
                if (tw.W < 0) tw.W = -1;

                var l = t.Length();
                if (l < 0.99f || l > 0.01f) t = Vector3.Normalize(t);

                outVertex.SetTangent(new Vector4(t, tw.W));
            }

            return true;
        }

        public static IEnumerable<MemoryAccessor[]> CreateVertexMemoryAccessors(this IEnumerable<IReadOnlyList<IVertexBuilder>> vertexBlocks)
        {
            // total number of vertices
            var totalCount = vertexBlocks.Sum(item => item.Count);

            var firstVertex = vertexBlocks.First().First();

            var tvg = firstVertex.GetGeometry().GetType();
            var tvm = firstVertex.GetMaterial().GetType();
            var tvs = firstVertex.GetSkinning().GetType();

            // vertex attributes
            var attributes = GetVertexAttributes(tvg, tvm, tvs, totalCount);

            // create master vertex buffer
            int byteStride = attributes[0].ByteStride;
            var vbuffer = new ArraySegment<byte>(new Byte[byteStride * totalCount]);

            var baseVertexIndex = 0;

            foreach (var block in vertexBlocks)
            {
                var accessors = MemoryAccessInfo
                    .Slice(attributes, baseVertexIndex, block.Count)
                    .Select(item => new MemoryAccessor(vbuffer, item))
                    .ToArray();

                foreach (var accessor in accessors)
                {
                    var columnFunc = GetVertexBuilderAttributeFunc(accessor.Attribute.Name);

                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.SCALAR) accessor.AsScalarArray().Fill(block.GetColumn<float>(columnFunc));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC2) accessor.AsVector2Array().Fill(block.GetColumn<Vector2>(columnFunc));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC3) accessor.AsVector3Array().Fill(block.GetColumn<Vector3>(columnFunc));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC4) accessor.AsVector4Array().Fill(block.GetColumn<Vector4>(columnFunc));
                }

                yield return accessors;

                baseVertexIndex += block.Count;
            }
        }

        public static IEnumerable<MemoryAccessor[]> CreateVertexMemoryAccessors<TvG, TvM, TvS>(this IEnumerable<IReadOnlyList<VertexBuilder<TvG, TvM, TvS>>> vertexBlocks)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            // total number of vertices
            var totalCount = vertexBlocks.Sum(item => item.Count);

            // vertex attributes
            var attributes = GetVertexAttributes(typeof(TvG), typeof(TvM), typeof(TvS), totalCount);

            // create master vertex buffer
            int byteStride = attributes[0].ByteStride;
            var vbuffer = new ArraySegment<byte>( new Byte[byteStride * totalCount] );

            var baseVertexIndex = 0;

            foreach (var block in vertexBlocks)
            {
                var accessors = MemoryAccessInfo
                    .Slice(attributes, baseVertexIndex, block.Count)
                    .Select(item => new MemoryAccessor(vbuffer, item))
                    .ToArray();

                foreach (var accessor in accessors)
                {
                    var columnFunc = GetVertexBuilderAttributeFunc<TvG, TvM, TvS>(accessor.Attribute.Name);

                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.SCALAR) accessor.AsScalarArray().Fill(block.GetScalarColumn(columnFunc));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC2) accessor.AsVector2Array().Fill(block.GetVector2Column(columnFunc));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC3) accessor.AsVector3Array().Fill(block.GetVector3Column(columnFunc));
                    if (accessor.Attribute.Dimensions == Schema2.DimensionType.VEC4) accessor.AsVector4Array().Fill(block.GetVector4Column(columnFunc));
                }

                yield return accessors;

                baseVertexIndex += block.Count;
            }
        }

        public static IEnumerable<MemoryAccessor> CreateIndexMemoryAccessors(this IEnumerable<IReadOnlyList<Int32>> indexBlocks)
        {
            // get attributes
            var totalCount = indexBlocks.Sum(item => item.Count);
            var attribute = new MemoryAccessInfo("INDEX", 0, totalCount, 0, Schema2.DimensionType.SCALAR, Schema2.EncodingType.UNSIGNED_INT);

            // create master index buffer
            var ibytes = new Byte[4 * totalCount];
            var ibuffer = new ArraySegment<byte>(ibytes);

            var baseIndicesIndex = 0;

            foreach (var block in indexBlocks)
            {
                var accessor = new MemoryAccessor(ibuffer, attribute.Slice(baseIndicesIndex, block.Count));

                accessor.AsIntegerArray().Fill(block);

                yield return accessor;

                baseIndicesIndex += block.Count;
            }
        }

        private static System.Reflection.FieldInfo GetVertexField(Type vertexType, string attributeName)
        {
            foreach (var finfo in vertexType.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);

                if (attribute.HasValue)
                {
                    if (attribute.Value.Name == attributeName) return finfo;
                }
            }

            return null;
        }

        private static MemoryAccessInfo[] GetVertexAttributes(Type vertexType, Type valuesType, Type jointsType, int itemsCount)
        {
            var attributes = new List<MemoryAccessInfo>();

            foreach (var finfo in vertexType.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);
                if (attribute.HasValue) attributes.Add(attribute.Value);
            }

            foreach (var finfo in valuesType.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);
                if (attribute.HasValue) attributes.Add(attribute.Value);
            }

            foreach (var finfo in jointsType.GetFields())
            {
                var attribute = _GetMemoryAccessInfo(finfo);
                if (attribute.HasValue) attributes.Add(attribute.Value);
            }

            var array = attributes.ToArray();

            MemoryAccessInfo.SetInterleavedInfo(array, 0, itemsCount);

            return array;
        }

        private static MemoryAccessInfo? _GetMemoryAccessInfo(System.Reflection.FieldInfo finfo)
        {
            var attribute = finfo.GetCustomAttributes(true)
                    .OfType<VertexAttributeAttribute>()
                    .FirstOrDefault();

            if (attribute == null) return null;

            var dimensions = (Schema2.DimensionType?)null;

            if (finfo.FieldType == typeof(Single)) dimensions = Schema2.DimensionType.SCALAR;
            if (finfo.FieldType == typeof(Vector2)) dimensions = Schema2.DimensionType.VEC2;
            if (finfo.FieldType == typeof(Vector3)) dimensions = Schema2.DimensionType.VEC3;
            if (finfo.FieldType == typeof(Vector4)) dimensions = Schema2.DimensionType.VEC4;
            if (finfo.FieldType == typeof(Quaternion)) dimensions = Schema2.DimensionType.VEC4;
            if (finfo.FieldType == typeof(Matrix4x4)) dimensions = Schema2.DimensionType.MAT4;

            if (dimensions == null) throw new ArgumentException($"invalid type {finfo.FieldType}");

            return new MemoryAccessInfo(attribute.Name, 0, 0, 0, dimensions.Value, attribute.Encoding, attribute.Normalized);
        }

        private static Func<VertexBuilder<TvG, TvM, TvS>, Object> GetVertexBuilderAttributeFunc<TvG, TvM, TvS>(string attributeName)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var finfo = GetVertexField(typeof(TvG), attributeName);
            if (finfo != null) return vertex => finfo.GetValue(vertex.Geometry);

            finfo = GetVertexField(typeof(TvM), attributeName);
            if (finfo != null) return vertex => finfo.GetValue(vertex.Material);

            finfo = GetVertexField(typeof(TvS), attributeName);
            if (finfo != null) return vertex => finfo.GetValue(vertex.Skinning);

            throw new NotImplementedException();
        }

        private static Func<IVertexBuilder, Object> GetVertexBuilderAttributeFunc(string attributeName)
        {
            if (attributeName == "POSITION") return v => v.GetGeometry().GetPosition();
            if (attributeName == "NORMAL") return v => { return v.GetGeometry().TryGetNormal(out Vector3 n) ? n : Vector3.Zero; };
            if (attributeName == "TANGENT") return v => { return v.GetGeometry().TryGetTangent(out Vector4 n) ? n : Vector4.Zero; };

            if (attributeName == "COLOR_0") return v => { var m = v.GetMaterial(); return m.MaxColors <= 0 ? Vector4.One : m.GetColor(0); };
            if (attributeName == "COLOR_1") return v => { var m = v.GetMaterial(); return m.MaxColors <= 1 ? Vector4.One : m.GetColor(1); };
            if (attributeName == "COLOR_2") return v => { var m = v.GetMaterial(); return m.MaxColors <= 2 ? Vector4.One : m.GetColor(2); };
            if (attributeName == "COLOR_3") return v => { var m = v.GetMaterial(); return m.MaxColors <= 3 ? Vector4.One : m.GetColor(3); };

            if (attributeName == "TEXCOORD_0") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 0 ? Vector2.Zero : m.GetTexCoord(0); };
            if (attributeName == "TEXCOORD_1") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 1 ? Vector2.Zero : m.GetTexCoord(1); };
            if (attributeName == "TEXCOORD_2") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 2 ? Vector2.Zero : m.GetTexCoord(2); };
            if (attributeName == "TEXCOORD_3") return v => { var m = v.GetMaterial(); return m.MaxTextCoords <= 3 ? Vector2.Zero : m.GetTexCoord(3); };

            if (attributeName == "JOINTS_0") return v => v.GetSkinning().JointsLow;
            if (attributeName == "JOINTS_1") return v => v.GetSkinning().JointsHigh;

            if (attributeName == "WEIGHTS_0") return v => v.GetSkinning().WeightsLow;
            if (attributeName == "WEIGHTS_1") return v => v.GetSkinning().Weightshigh;

            throw new NotImplementedException();
        }

        private static Single[] GetScalarColumn<TvG, TvM, TvS>(this IReadOnlyList<VertexBuilder<TvG, TvM, TvS>> vertices, Func<VertexBuilder<TvG, TvM, TvS>, Object> func)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            return GetColumn<TvG, TvM, TvS, Single>(vertices, func);
        }

        private static Vector2[] GetVector2Column<TvG, TvM, TvS>(this IReadOnlyList<VertexBuilder<TvG, TvM, TvS>> vertices, Func<VertexBuilder<TvG, TvM, TvS>, Object> func)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            return GetColumn<TvG, TvM, TvS, Vector2>(vertices, func);
        }

        private static Vector3[] GetVector3Column<TvG, TvM, TvS>(this IReadOnlyList<VertexBuilder<TvG, TvM, TvS>> vertices, Func<VertexBuilder<TvG, TvM, TvS>, Object> func)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            return GetColumn<TvG, TvM, TvS, Vector3>(vertices, func);
        }

        private static Vector4[] GetVector4Column<TvG, TvM, TvS>(this IReadOnlyList<VertexBuilder<TvG, TvM, TvS>> vertices, Func<VertexBuilder<TvG, TvM, TvS>, Object> func)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            return GetColumn<TvG, TvM, TvS, Vector4>(vertices, func);
        }

        private static TColumn[] GetColumn<TvG, TvM, TvS, TColumn>(this IReadOnlyList<VertexBuilder<TvG, TvM, TvS>> vertices, Func<VertexBuilder<TvG, TvM, TvS>, Object> func)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var dst = new TColumn[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = (TColumn)func(v);
            }

            return dst;
        }

        private static TColumn[] GetColumn<TColumn>(this IReadOnlyList<IVertexBuilder> vertices, Func<IVertexBuilder, Object> func)
        {
            var dst = new TColumn[vertices.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                var v = vertices[i];

                dst[i] = (TColumn)func(v);
            }

            return dst;
        }

        public static TvP ConvertTo<TvP>(this IVertexGeometry src)
            where TvP : struct, IVertexGeometry
        {
            if (src.GetType() == typeof(TvP)) return (TvP)src;

            var dst = default(TvP);

            dst.SetPosition(src.GetPosition());
            if (src.TryGetNormal(out Vector3 nrm)) dst.SetNormal(nrm);
            if (src.TryGetTangent(out Vector4 tgt)) dst.SetTangent(tgt);

            return dst;
        }

        public static TvM ConvertTo<TvM>(this IVertexMaterial src)
            where TvM : struct, IVertexMaterial
        {
            if (src.GetType() == typeof(TvM)) return (TvM)src;

            var dst = default(TvM);

            int i = 0;

            while (i < Math.Min(src.MaxColors, dst.MaxColors))
            {
                dst.SetColor(i, src.GetColor(i));
                ++i;
            }

            while (i < dst.MaxColors)
            {
                dst.SetColor(i, Vector4.One);
                ++i;
            }

            i = 0;

            while (i < Math.Min(src.MaxTextCoords, dst.MaxTextCoords))
            {
                dst.SetTexCoord(i, src.GetTexCoord(i));
                ++i;
            }

            while (i < dst.MaxColors)
            {
                dst.SetTexCoord(i, Vector2.Zero);
                ++i;
            }

            return dst;
        }

        public static unsafe TvS ConvertTo<TvS>(this IVertexSkinning src)
            where TvS : struct, IVertexSkinning
        {
            if (src.GetType() == typeof(TvS)) return (TvS)src;

            // create copy

            var dst = default(TvS);

            if (dst.MaxBindings >= src.MaxBindings)
            {
                for (int i = 0; i < src.MaxBindings; ++i)
                {
                    var jw = src.GetJointBinding(i);

                    dst.SetJointBinding(i, jw.Joint, jw.Weight);
                }

                return dst;
            }

            // if there's more source joints than destination joints, transfer with scale

            Span<JointBinding> srcjw = stackalloc JointBinding[src.MaxBindings];

            for (int i = 0; i < src.MaxBindings; ++i)
            {
                srcjw[i] = src.GetJointBinding(i);
            }

            JointBinding.InPlaceReverseBubbleSort(srcjw);

            var w = JointBinding.CalculateScaleFor(srcjw, dst.MaxBindings);

            for (int i = 0; i < dst.MaxBindings; ++i)
            {
                dst.SetJointBinding(i, srcjw[i].Joint, srcjw[i].Weight * w);
            }

            return dst;
        }        
    }
}
