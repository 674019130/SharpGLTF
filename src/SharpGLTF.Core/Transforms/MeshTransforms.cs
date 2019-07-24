﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using TRANSFORM = System.Numerics.Matrix4x4;
using V3 = System.Numerics.Vector3;
using V4 = System.Numerics.Vector4;

namespace SharpGLTF.Transforms
{
    /// <summary>
    /// Interface for a mesh transform object
    /// </summary>
    public interface ITransform
    {
        /// <summary>
        /// Gets a value indicating whether the current <see cref="ITransform"/> will render visible geometry.
        /// </summary>
        bool Visible { get; }

        /// <summary>
        /// Gets a value indicating whether the triangles need to be flipped to render correctly.
        /// </summary>
        bool FlipFaces { get; }

        V3 TransformPosition(V3 position, V3[] morphTargets, (int, float)[] skinWeights);
        V3 TransformNormal(V3 normal, V3[] morphTargets, (int, float)[] skinWeights);
        V4 TransformTangent(V4 tangent, V3[] morphTargets, (int, float)[] skinWeights);

        V4 MorphColors(V4 color, V4[] morphTargets);
    }

    public abstract class MorphTransform
    {
        #region constructor

        protected MorphTransform()
        {
            Update(new SparseWeight8((0, 1)), false);
        }

        protected MorphTransform(SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Update(morphWeights, useAbsoluteMorphTargets);
        }

        #endregion

        #region data

        /// <summary>
        /// Represents a normalized sparse collection of weights where:
        /// - Indices with value zero point to the master mesh
        /// - Indices with value over zero point to MorphTarget[index-1].
        /// </summary>
        private SparseWeight8 _Weights;

        /// <summary>
        /// True if morph targets represent absolute values.
        /// False if morph targets represent values relative to master value.
        /// </summary>
        private bool _AbsoluteMorphTargets;

        #endregion

        #region API

        public void Update(SparseWeight8 morphWeights, bool useAbsoluteMorphTargets = false)
        {
            _AbsoluteMorphTargets = useAbsoluteMorphTargets;

            if (morphWeights.IsZero)
            {
                _Weights = new SparseWeight8((0, 1));
                return;
            }

            _Weights = Normalize(morphWeights);
        }

        /// <summary>
        /// Increments all indices and adds a new Index[0] with a weight that makes the sum of all weights equal to 1
        /// </summary>
        /// <param name="r">A <see cref="SparseWeight8"/> object representing the weights of the morph targets.</param>
        /// <returns>A <see cref="SparseWeight8"/> representing the morph target weights, compensated with the weight of the master values.</returns>
        internal static SparseWeight8 Normalize(SparseWeight8 r)
        {
            int i = -1;
            float w = float.MaxValue;
            float ww = 0;

            ww += r.Weight0; if (r.Weight0 < w) { i = 0; w = r.Weight0; }
            ww += r.Weight1; if (r.Weight1 < w) { i = 1; w = r.Weight1; }
            ww += r.Weight2; if (r.Weight2 < w) { i = 2; w = r.Weight2; }
            ww += r.Weight3; if (r.Weight3 < w) { i = 3; w = r.Weight3; }
            ww += r.Weight4; if (r.Weight4 < w) { i = 4; w = r.Weight4; }
            ww += r.Weight5; if (r.Weight5 < w) { i = 5; w = r.Weight5; }
            ww += r.Weight6; if (r.Weight6 < w) { i = 6; w = r.Weight6; }
            ww += r.Weight7; if (r.Weight7 < w) { i = 7; w = r.Weight7; }

            r.Index0 += 1;
            r.Index1 += 1;
            r.Index2 += 1;
            r.Index3 += 1;
            r.Index4 += 1;
            r.Index5 += 1;
            r.Index6 += 1;
            r.Index7 += 1;

            ww -= w;
            var iw = 1 - ww;

            switch (i)
            {
                case 0: r.Index0 = 0; r.Weight0 = iw; break;
                case 1: r.Index1 = 0; r.Weight1 = iw; break;
                case 2: r.Index2 = 0; r.Weight2 = iw; break;
                case 3: r.Index3 = 0; r.Weight3 = iw; break;
                case 4: r.Index4 = 0; r.Weight4 = iw; break;
                case 5: r.Index5 = 0; r.Weight5 = iw; break;
                case 6: r.Index6 = 0; r.Weight6 = iw; break;
                case 7: r.Index7 = 0; r.Weight7 = iw; break;
            }

            return r;
        }

        protected V3 MorphVectors(V3 value, V3[] morphTargets)
        {
            if (morphTargets == null) return value;

            if (_Weights.Index0 == 0 && _Weights.Weight0 == 1) return value;

            var p = V3.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var pair in _Weights.GetPairs())
                {
                    var val = pair.Item1 == 0 ? value : morphTargets[pair.Item1 - 1];
                    p += val * pair.Item2;
                }
            }
            else
            {
                foreach (var pair in _Weights.GetPairs())
                {
                    var val = pair.Item1 == 0 ? value : value + morphTargets[pair.Item1 - 1];
                    p += val * pair.Item2;
                }
            }

            return p;
        }

        protected V4 MorphVectors(V4 value, V4[] morphTargets)
        {
            if (morphTargets == null) return value;

            if (_Weights.Index0 == 0 && _Weights.Weight0 == 1) return value;

            var p = V4.Zero;

            if (_AbsoluteMorphTargets)
            {
                foreach (var pair in _Weights.GetPairs())
                {
                    var val = pair.Item1 == 0 ? value : morphTargets[pair.Item1 - 1];
                    p += val * pair.Item2;
                }
            }
            else
            {
                foreach (var pair in _Weights.GetPairs())
                {
                    var val = pair.Item1 == 0 ? value : value + morphTargets[pair.Item1 - 1];
                    p += val * pair.Item2;
                }
            }

            return p;
        }

        public V4 MorphColors(V4 color, V4[] morphTargets)
        {
            return MorphVectors(color, morphTargets);
        }

        #endregion
    }

    public class StaticTransform : MorphTransform, ITransform
    {
        #region constructor

        public StaticTransform(TRANSFORM xform, SparseWeight8 morphWeights, bool useAbsoluteMorphs)
        {
            Update(xform, morphWeights, useAbsoluteMorphs);
        }

        #endregion

        #region data

        private TRANSFORM _Transform;
        private Boolean _Visible;
        private Boolean _FlipFaces;

        #endregion

        #region properties

        public Boolean Visible => _Visible;

        public Boolean FlipFaces => _FlipFaces;

        #endregion

        #region API

        public void Update(TRANSFORM xform, SparseWeight8 morphWeights, bool useAbsoluteMorphs)
        {
            Update(morphWeights, useAbsoluteMorphs);

            _Transform = xform;

            // http://m-hikari.com/ija/ija-password-2009/ija-password5-8-2009/hajrizajIJA5-8-2009.pdf

            float determinant3x3 =
                +(xform.M13 * xform.M21 * xform.M32)
                + (xform.M11 * xform.M22 * xform.M33)
                + (xform.M12 * xform.M23 * xform.M31)
                - (xform.M12 * xform.M21 * xform.M33)
                - (xform.M13 * xform.M22 * xform.M31)
                - (xform.M11 * xform.M23 * xform.M32);

            _Visible = Math.Abs(determinant3x3) > float.Epsilon;
            _FlipFaces = determinant3x3 < 0;
        }

        public V3 TransformPosition(V3 position, V3[] morphTargets, (int, float)[] skinWeights)
        {
            position = MorphVectors(position, morphTargets);

            return V3.Transform(position, _Transform);
        }

        public V3 TransformNormal(V3 normal, V3[] morphTargets, (int, float)[] skinWeights)
        {
            normal = MorphVectors(normal, morphTargets);

            return V3.Normalize(V3.Transform(normal, _Transform));
        }

        public V4 TransformTangent(V4 tangent, V3[] morphTargets, (int, float)[] skinWeights)
        {
            var n = MorphVectors(new V3(tangent.X, tangent.Y, tangent.Z), morphTargets);

            n = V3.Normalize(V3.Transform(n, _Transform));

            return new V4(n, tangent.W);
        }

        #endregion
    }

    public class SkinTransform : MorphTransform, ITransform
    {
        #region constructor

        public SkinTransform(TRANSFORM[] invBindings, TRANSFORM[] xforms, SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Update(invBindings, xforms, morphWeights, useAbsoluteMorphTargets);
        }

        #endregion

        #region data

        private TRANSFORM[] _JointTransforms;

        #endregion

        #region API

        public void Update(TRANSFORM[] invBindings, TRANSFORM[] xforms, SparseWeight8 morphWeights, bool useAbsoluteMorphTargets)
        {
            Guard.NotNull(invBindings, nameof(invBindings));
            Guard.NotNull(xforms, nameof(xforms));
            Guard.IsTrue(invBindings.Length == xforms.Length, nameof(xforms), $"{invBindings} and {xforms} length mismatch.");

            Update(morphWeights, useAbsoluteMorphTargets);

            if (_JointTransforms == null || _JointTransforms.Length != invBindings.Length) _JointTransforms = new TRANSFORM[invBindings.Length];

            for (int i = 0; i < _JointTransforms.Length; ++i)
            {
                _JointTransforms[i] = invBindings[i] * xforms[i];
            }
        }

        public bool Visible => true;

        public bool FlipFaces => false;

        public V3 TransformPosition(V3 localPosition, V3[] morphTargets, (int, float)[] skinWeights)
        {
            localPosition = MorphVectors(localPosition, morphTargets);

            var worldPosition = V3.Zero;

            var wnrm = 1.0f / skinWeights.Sum(item => item.Item2);

            foreach (var jw in skinWeights)
            {
                worldPosition += V3.Transform(localPosition, _JointTransforms[jw.Item1]) * jw.Item2 * wnrm;
            }

            return worldPosition;
        }

        public V3 TransformNormal(V3 localNormal, V3[] morphTargets, (int, float)[] skinWeights)
        {
            localNormal = MorphVectors(localNormal, morphTargets);

            var worldNormal = V3.Zero;

            foreach (var jw in skinWeights)
            {
                worldNormal += V3.TransformNormal(localNormal, _JointTransforms[jw.Item1]) * jw.Item2;
            }

            return V3.Normalize(localNormal);
        }

        public V4 TransformTangent(V4 localTangent, V3[] morphTargets, (int, float)[] skinWeights)
        {
            var localTangentV = MorphVectors(new V3(localTangent.X, localTangent.Y, localTangent.Z), morphTargets);

            var worldTangent = V3.Zero;

            foreach (var jw in skinWeights)
            {
                worldTangent += V3.TransformNormal(localTangentV, _JointTransforms[jw.Item1]) * jw.Item2;
            }

            worldTangent = V3.Normalize(worldTangent);

            return new V4(worldTangent, localTangent.W);
        }

        #endregion
    }
}
