﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using SharpGLTF.Transforms;

namespace SharpGLTF.Geometry.VertexTypes
{
    /// <summary>
    /// Represents a a Node Joint index and its weight in a skinning system.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{Joint} = {Weight}")]
    public struct JointBinding
    {
        #region constructors

        public JointBinding(int joint, float weight)
        {
            this.Joint = joint;
            this.Weight = weight;
            if (Weight == 0) Joint = 0;
        }

        public static implicit operator JointBinding((int, float) jw)
        {
            return new JointBinding(jw.Item1, jw.Item2);
        }

        #endregion

        #region data

        public int Joint;
        public float Weight;

        private static readonly _WeightComparer _DefaultWeightComparer = new _WeightComparer();

        #endregion

        #region properties

        public static IComparer<JointBinding> WeightComparer => _DefaultWeightComparer;

        #endregion

        #region API

        public static IEnumerable<JointBinding> GetBindings(IVertexSkinning vs)
        {
            for (int i = 0; i < vs.MaxBindings; ++i)
            {
                var jw = vs.GetJointBinding(i);
                if (jw.Weight != 0) yield return jw;
            }
        }

        #endregion

        #region types

        private sealed class _WeightComparer : IComparer<JointBinding>
        {
            public int Compare(JointBinding x, JointBinding y)
            {
                var a = x.Weight.CompareTo(y.Weight);
                if (a != 0) return a;

                return x.Joint.CompareTo(y.Joint);
            }
        }

        #endregion
    }

    public interface IVertexSkinning
    {
        int MaxBindings { get; }

        void Validate();

        JointBinding GetJointBinding(int index);

        void SetJointBinding(int index, int joint, float weight);

        void SetWeights(in SparseWeight8 weights);

        IEnumerable<JointBinding> JointBindings { get; }

        SparseWeight8 SparseWeights { get; }

        Vector4 JointsLow { get; }
        Vector4 JointsHigh { get; }

        Vector4 WeightsLow { get; }
        Vector4 Weightshigh { get; }
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 256 bone joints and 4 weights.
    /// </summary>
    public struct VertexJoints8x4 : IVertexSkinning
    {
        #region constructors

        public VertexJoints8x4(int jointIndex)
        {
            Joints = new Vector4(jointIndex);
            Weights = Vector4.UnitX;
        }

        public VertexJoints8x4(JointBinding a, JointBinding b)
        {
            Joints = new Vector4(a.Joint, b.Joint, 0, 0);
            Weights = new Vector4(a.Weight, b.Weight, 0, 0);

            InPlaceSort();
        }

        public VertexJoints8x4(JointBinding a, JointBinding b, JointBinding c)
        {
            Joints = new Vector4(a.Joint, b.Joint, c.Joint, 0);
            Weights = new Vector4(a.Weight, b.Weight, c.Weight, 0);

            InPlaceSort();
        }

        public VertexJoints8x4(JointBinding a, JointBinding b, JointBinding c, JointBinding d)
        {
            Joints = new Vector4(a.Joint, b.Joint, c.Joint, d.Joint);
            Weights = new Vector4(a.Weight, b.Weight, c.Weight, d.Weight);

            InPlaceSort();
        }

        public VertexJoints8x4(params (int, float)[] bindings)
        {
            // var sparse = new Transforms.SparseWeight8(bindings);

            Guard.NotNull(bindings, nameof(bindings));
            Guard.MustBeBetweenOrEqualTo(bindings.Length, 1, 4, nameof(bindings));

            Joints = Vector4.Zero;
            Weights = Vector4.Zero;

            for (int i = 0; i < bindings.Length; ++i)
            {
                this.SetJointBinding(i, bindings[i].Item1, bindings[i].Item2);
            }
        }

        public VertexJoints8x4(in Transforms.SparseWeight8 weights)
        {
            var w4 = Transforms.SparseWeight8.OrderedByWeight(weights);

            Joints = new Vector4
                (
                w4.Index0,
                w4.Index1,
                w4.Index2,
                w4.Index3
                );

            Weights = new Vector4
                (
                w4.Weight0,
                w4.Weight1,
                w4.Weight2,
                w4.Weight3
                );

            // renormalize
            var w = Vector4.Dot(Weights, Vector4.One);
            if (w != 0) Weights /= w;
        }

        #endregion

        #region data

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights;

        public int MaxBindings => 4;

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 JointsLow => this.Joints;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 JointsHigh => Vector4.Zero;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 WeightsLow => this.Weights;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 Weightshigh => Vector4.Zero;

        public Transforms.SparseWeight8 SparseWeights => new Transforms.SparseWeight8(this.Joints, this.Weights);

        #endregion

        #region API

        public void Validate() { FragmentPreprocessors.ValidateVertexSkinning(this); }

        public JointBinding GetJointBinding(int index)
        {
            switch (index)
            {
                case 0: return new JointBinding((int)this.Joints.X, this.Weights.X);
                case 1: return new JointBinding((int)this.Joints.Y, this.Weights.Y);
                case 2: return new JointBinding((int)this.Joints.Z, this.Weights.Z);
                case 3: return new JointBinding((int)this.Joints.W, this.Weights.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJointBinding(int index, int joint, float weight)
        {
            switch (index)
            {
                case 0: { this.Joints.X = joint; this.Weights.X = weight; return; }
                case 1: { this.Joints.Y = joint; this.Weights.Y = weight; return; }
                case 2: { this.Joints.Z = joint; this.Weights.Z = weight; return; }
                case 3: { this.Joints.W = joint; this.Weights.W = weight; return; }
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void InPlaceSort()
        {
            var sparse = new Transforms.SparseWeight8(this.Joints, this.Weights);
            this = new VertexJoints8x4(sparse);
        }

        public void SetWeights(in SparseWeight8 weights) { this = new VertexJoints8x4(weights); }

        public IEnumerable<JointBinding> JointBindings => JointBinding.GetBindings(this);

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 65535 bone joints and 4 weights.
    /// </summary>
    public struct VertexJoints16x4 : IVertexSkinning
    {
        #region constructors

        public VertexJoints16x4(int jointIndex)
        {
            Joints = new Vector4(jointIndex);
            Weights = Vector4.UnitX;
        }

        public VertexJoints16x4(JointBinding a, JointBinding b)
        {
            Joints = new Vector4(a.Joint, b.Joint, 0, 0);
            Weights = new Vector4(a.Weight, b.Weight, 0, 0);

            InPlaceSort();
        }

        public VertexJoints16x4(JointBinding a, JointBinding b, JointBinding c)
        {
            Joints = new Vector4(a.Joint, b.Joint, c.Joint, 0);
            Weights = new Vector4(a.Weight, b.Weight, c.Weight, 0);

            InPlaceSort();
        }

        public VertexJoints16x4(JointBinding a, JointBinding b, JointBinding c, JointBinding d)
        {
            Joints = new Vector4(a.Joint, b.Joint, c.Joint, d.Joint);
            Weights = new Vector4(a.Weight, b.Weight, c.Weight, d.Weight);

            InPlaceSort();
        }

        public VertexJoints16x4(in Transforms.SparseWeight8 weights)
        {
            var w4 = Transforms.SparseWeight8.OrderedByWeight(weights);

            Joints = new Vector4
                (
                w4.Index0,
                w4.Index1,
                w4.Index2,
                w4.Index3
                );

            Weights = new Vector4
                (
                w4.Weight0,
                w4.Weight1,
                w4.Weight2,
                w4.Weight3
                );

            // renormalize
            var w = Vector4.Dot(Weights, Vector4.One);
            if (w != 0) Weights /= w;
        }

        #endregion

        #region data

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_SHORT, false)]
        public Vector4 Joints;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights;

        public int MaxBindings => 4;

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 JointsLow => this.Joints;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 JointsHigh => Vector4.Zero;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 WeightsLow => this.Weights;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 Weightshigh => Vector4.Zero;

        public SparseWeight8 SparseWeights => new SparseWeight8(this.Joints, this.Weights);

        #endregion

        #region API

        public void Validate() { FragmentPreprocessors.ValidateVertexSkinning(this); }

        public JointBinding GetJointBinding(int index)
        {
            switch (index)
            {
                case 0: return new JointBinding((int)this.Joints.X, this.Weights.X);
                case 1: return new JointBinding((int)this.Joints.Y, this.Weights.Y);
                case 2: return new JointBinding((int)this.Joints.Z, this.Weights.Z);
                case 3: return new JointBinding((int)this.Joints.W, this.Weights.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJointBinding(int index, int joint, float weight)
        {
            switch (index)
            {
                case 0: { this.Joints.X = joint; this.Weights.X = weight; return; }
                case 1: { this.Joints.Y = joint; this.Weights.Y = weight; return; }
                case 2: { this.Joints.Z = joint; this.Weights.Z = weight; return; }
                case 3: { this.Joints.W = joint; this.Weights.W = weight; return; }
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetWeights(in SparseWeight8 weights) { this = new VertexJoints16x4(weights); }

        public void InPlaceSort()
        {
            var sparse = new Transforms.SparseWeight8(this.Joints, this.Weights);
            this = new VertexJoints16x4(sparse);
        }

        public IEnumerable<JointBinding> JointBindings => JointBinding.GetBindings(this);

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 256 bone joints and 8 weights.
    /// </summary>
    public struct VertexJoints8x8 : IVertexSkinning
    {
        #region constructors

        public VertexJoints8x8(int jointIndex)
        {
            Joints0 = new Vector4(jointIndex);
            Joints1 = new Vector4(jointIndex);
            Weights0 = Vector4.UnitX;
            Weights1 = Vector4.Zero;
        }

        public VertexJoints8x8(int jointIndex1, int jointIndex2)
        {
            Joints0 = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Joints1 = Vector4.Zero;
            Weights0 = new Vector4(0.5f, 0.5f, 0, 0);
            Weights1 = Vector4.Zero;
        }

        public VertexJoints8x8(in SparseWeight8 weights)
        {
            Joints0 = new Vector4
                (
                weights.Index0,
                weights.Index1,
                weights.Index2,
                weights.Index3
                );

            Joints1 = new Vector4
                (
                weights.Index4,
                weights.Index5,
                weights.Index6,
                weights.Index7
                );

            Weights0 = new Vector4
                (
                weights.Weight0,
                weights.Weight1,
                weights.Weight2,
                weights.Weight3
                );

            Weights1 = new Vector4
                (
                weights.Weight4,
                weights.Weight5,
                weights.Weight6,
                weights.Weight7
                );
        }

        #endregion

        #region data

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints0;

        [VertexAttribute("JOINTS_1", Schema2.EncodingType.UNSIGNED_BYTE, false)]
        public Vector4 Joints1;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights0;

        [VertexAttribute("WEIGHTS_1", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights1;

        public int MaxBindings => 8;

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 JointsLow => this.Joints0;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 JointsHigh => this.Joints1;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 WeightsLow => this.Weights0;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 Weightshigh => this.Joints1;

        public SparseWeight8 SparseWeights => new SparseWeight8(this.Joints0, this.Joints1, this.Weights0, this.Weights1);

        #endregion

        #region API

        public void Validate() { FragmentPreprocessors.ValidateVertexSkinning(this); }

        public void SetWeights(in SparseWeight8 weights) { this = new VertexJoints8x8(weights); }

        public JointBinding GetJointBinding(int index)
        {
            switch (index)
            {
                case 0: return new JointBinding((int)this.Joints0.X, this.Weights0.X);
                case 1: return new JointBinding((int)this.Joints0.Y, this.Weights0.Y);
                case 2: return new JointBinding((int)this.Joints0.Z, this.Weights0.Z);
                case 3: return new JointBinding((int)this.Joints0.W, this.Weights0.W);
                case 4: return new JointBinding((int)this.Joints1.X, this.Weights1.X);
                case 5: return new JointBinding((int)this.Joints1.Y, this.Weights1.Y);
                case 6: return new JointBinding((int)this.Joints1.Z, this.Weights1.Z);
                case 7: return new JointBinding((int)this.Joints1.W, this.Weights1.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJointBinding(int index, int joint, float weight)
        {
            switch (index)
            {
                case 0: { this.Joints0.X = joint; this.Weights0.X = weight; return; }
                case 1: { this.Joints0.Y = joint; this.Weights0.Y = weight; return; }
                case 2: { this.Joints0.Z = joint; this.Weights0.Z = weight; return; }
                case 3: { this.Joints0.W = joint; this.Weights0.W = weight; return; }
                case 4: { this.Joints1.X = joint; this.Weights1.X = weight; return; }
                case 5: { this.Joints1.Y = joint; this.Weights1.Y = weight; return; }
                case 6: { this.Joints1.Z = joint; this.Weights1.Z = weight; return; }
                case 7: { this.Joints1.W = joint; this.Weights1.W = weight; return; }
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public IEnumerable<JointBinding> JointBindings => JointBinding.GetBindings(this);

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with up to 65535 bone joints and 8 weights.
    /// </summary>
    public struct VertexJoints16x8 : IVertexSkinning
    {
        #region constructors

        public VertexJoints16x8(int jointIndex)
        {
            Joints0 = new Vector4(jointIndex);
            Joints1 = new Vector4(jointIndex);
            Weights0 = Vector4.UnitX;
            Weights1 = Vector4.Zero;
        }

        public VertexJoints16x8(int jointIndex1, int jointIndex2)
        {
            Joints0 = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Joints1 = Vector4.Zero;
            Weights0 = new Vector4(0.5f, 0.5f, 0, 0);
            Weights1 = Vector4.Zero;
        }

        public VertexJoints16x8(in Transforms.SparseWeight8 weights)
        {
            Joints0 = new Vector4
                (
                weights.Index0,
                weights.Index1,
                weights.Index2,
                weights.Index3
                );

            Joints1 = new Vector4
                (
                weights.Index4,
                weights.Index5,
                weights.Index6,
                weights.Index7
                );

            Weights0 = new Vector4
                (
                weights.Weight0,
                weights.Weight1,
                weights.Weight2,
                weights.Weight3
                );

            Weights1 = new Vector4
                (
                weights.Weight4,
                weights.Weight5,
                weights.Weight6,
                weights.Weight7
                );
        }

        #endregion

        #region data

        [VertexAttribute("JOINTS_0", Schema2.EncodingType.UNSIGNED_SHORT, false)]
        public Vector4 Joints0;

        [VertexAttribute("JOINTS_1", Schema2.EncodingType.UNSIGNED_SHORT, false)]
        public Vector4 Joints1;

        [VertexAttribute("WEIGHTS_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights0;

        [VertexAttribute("WEIGHTS_1", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Weights1;

        public int MaxBindings => 8;

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 JointsLow => this.Joints0;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 JointsHigh => this.Joints1;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 WeightsLow => this.Weights0;
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 Weightshigh => this.Joints1;

        public SparseWeight8 SparseWeights => new SparseWeight8(this.Joints0, this.Joints1, this.Weights0, this.Weights1);

        #endregion

        #region API

        public void Validate() { FragmentPreprocessors.ValidateVertexSkinning(this); }

        public void SetWeights(in SparseWeight8 weights) { this = new VertexJoints16x8(weights); }

        public JointBinding GetJointBinding(int index)
        {
            switch (index)
            {
                case 0: return new JointBinding((int)this.Joints0.X, this.Weights0.X);
                case 1: return new JointBinding((int)this.Joints0.Y, this.Weights0.Y);
                case 2: return new JointBinding((int)this.Joints0.Z, this.Weights0.Z);
                case 3: return new JointBinding((int)this.Joints0.W, this.Weights0.W);
                case 4: return new JointBinding((int)this.Joints1.X, this.Weights1.X);
                case 5: return new JointBinding((int)this.Joints1.Y, this.Weights1.Y);
                case 6: return new JointBinding((int)this.Joints1.Z, this.Weights1.Z);
                case 7: return new JointBinding((int)this.Joints1.W, this.Weights1.W);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetJointBinding(int index, int joint, float weight)
        {
            switch (index)
            {
                case 0: { this.Joints0.X = joint; this.Weights0.X = weight; return; }
                case 1: { this.Joints0.Y = joint; this.Weights0.Y = weight; return; }
                case 2: { this.Joints0.Z = joint; this.Weights0.Z = weight; return; }
                case 3: { this.Joints0.W = joint; this.Weights0.W = weight; return; }
                case 4: { this.Joints1.X = joint; this.Weights1.X = weight; return; }
                case 5: { this.Joints1.Y = joint; this.Weights1.Y = weight; return; }
                case 6: { this.Joints1.Z = joint; this.Weights1.Z = weight; return; }
                case 7: { this.Joints1.W = joint; this.Weights1.W = weight; return; }
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public IEnumerable<JointBinding> JointBindings => JointBinding.GetBindings(this);

        #endregion
    }
}
