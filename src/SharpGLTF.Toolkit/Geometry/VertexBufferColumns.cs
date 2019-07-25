﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    /// <summary>
    /// Represents a vertex buffer, where every vertex attribute is represented as a vector column.
    /// </summary>
    /// <remarks>
    /// One of the use cases of <see cref="VertexBufferColumns"/> is to bind the different attribute
    /// columns directly to the <see cref="Schema2.Accessor"/> source feed, which means that
    /// if you modify the contents of a column that is binded directly to a model, you're
    /// modifying the model's internal data.
    /// </remarks>
    public class VertexBufferColumns
    {
        #region Data Columns

        #pragma warning disable CA2227 // Collection properties should be read only

        public IList<Vector3> Positions { get; set; }
        public IList<Vector3> Normals { get; set; }
        public IList<Vector4> Tangents { get; set; }

        public IList<Vector4> Colors0 { get; set; }
        public IList<Vector4> Colors1 { get; set; }

        public IList<Vector2> TexCoords0 { get; set; }
        public IList<Vector2> TexCoords1 { get; set; }

        public IList<Vector4> Joints0 { get; set; }
        public IList<Vector4> Joints1 { get; set; }

        public IList<Vector4> Weights0 { get; set; }
        public IList<Vector4> Weights1 { get; set; }

        #pragma warning restore CA2227 // Collection properties should be read only

        private List<MorphTargetColumns> _MorphTargets;

        public IReadOnlyList<MorphTargetColumns> MorphTargets => _MorphTargets == null ? (IReadOnlyList<MorphTargetColumns>)Array.Empty<MorphTargetColumns>() : _MorphTargets;

        #endregion

        #region API

        private static IList<T> _IsolateColumn<T>(IList<T> column)
        {
            if (column == null) return null;

            var newColumn = new T[column.Count];

            column.CopyTo(newColumn, 0);

            return newColumn;
        }

        /// <summary>
        /// Performs an in-place copy of the contents of every column,
        /// which guarantees that the columns of this <see cref="VertexBufferColumns"/>
        /// are not shared by any other object and can be modified safely.
        /// </summary>
        public void IsolateColumns()
        {
            this.Positions = _IsolateColumn(this.Positions);
            this.Normals = _IsolateColumn(this.Normals);
            this.Tangents = _IsolateColumn(this.Tangents);

            this.Colors0 = _IsolateColumn(this.Colors0);
            this.Colors1 = _IsolateColumn(this.Colors1);

            this.TexCoords0 = _IsolateColumn(this.TexCoords0);
            this.TexCoords1 = _IsolateColumn(this.TexCoords1);

            this.Joints0 = _IsolateColumn(this.Joints0);
            this.Joints1 = _IsolateColumn(this.Joints1);

            this.Weights0 = _IsolateColumn(this.Weights0);
            this.Weights1 = _IsolateColumn(this.Weights1);

            if (_MorphTargets == null) return;

            foreach (var mt in _MorphTargets) mt.IsolateColumns();
        }

        /// <summary>
        /// Applies a transform to the columns of this <see cref="VertexBufferColumns"/>
        /// </summary>
        /// <param name="transform">A Mesh transformer object</param>
        /// <remarks>
        /// This is a one time operation.
        /// Once it's applied, skinning and morphing columns are removed, since they're baked
        /// into the position, normal and tangent columns.
        /// </remarks>
        public void ApplyTransform(Transforms.ITransform transform)
        {
            Guard.NotNull(this.Positions, nameof(this.Positions), "Missing Positions column");
            if (this.Normals != null) Guard.IsTrue(this.Positions.Count == this.Normals.Count, nameof(this.Normals), "Column length mismatch.");
            if (this.Tangents != null) Guard.IsTrue(this.Positions.Count == this.Tangents.Count, nameof(this.Tangents), "Column length mismatch.");
            if (this.Colors0 != null) Guard.IsTrue(this.Positions.Count == this.Colors0.Count, nameof(this.Colors0), "Column length mismatch.");
            if (this.Colors1 != null) Guard.IsTrue(this.Positions.Count == this.Colors1.Count, nameof(this.Colors1), "Column length mismatch.");
            if (this.TexCoords0 != null) Guard.IsTrue(this.Positions.Count == this.TexCoords0.Count, nameof(this.TexCoords0), "Column length mismatch.");
            if (this.TexCoords1 != null) Guard.IsTrue(this.Positions.Count == this.TexCoords1.Count, nameof(this.TexCoords1), "Column length mismatch.");
            if (this.Joints0 != null) Guard.IsTrue(this.Positions.Count == this.Joints0.Count, nameof(this.Joints0), "Column length mismatch.");
            if (this.Joints1 != null) Guard.IsTrue(this.Positions.Count == this.Joints1.Count, nameof(this.Joints1), "Column length mismatch.");
            if (this.Weights0 != null) Guard.IsTrue(this.Positions.Count == this.Weights0.Count, nameof(this.Weights0), "Column length mismatch.");
            if (this.Weights1 != null) Guard.IsTrue(this.Positions.Count == this.Weights1.Count, nameof(this.Weights1), "Column length mismatch.");

            // since the attributes we want to overwrite might be binded directly to the model's buffer
            // data, and we don't want to modify the source data, we isolate the columns to be overwritten.

            this.Positions = _IsolateColumn(this.Positions);
            this.Normals = _IsolateColumn(this.Normals);
            this.Tangents = _IsolateColumn(this.Tangents);
            this.Colors0 = _IsolateColumn(this.Colors0);

            // prepare morph data, if available

            Vector3[] morphPositions = null;
            Vector3[] morphNormals = null;
            Vector3[] morphTangents = null;
            Vector4[] morphColors0 = null;

            if (_MorphTargets != null)
            {
                if (_MorphTargets.All(item => item.Positions != null)) morphPositions = new Vector3[this.MorphTargets.Count];
                if (_MorphTargets.All(item => item.Normals != null)) morphNormals = new Vector3[this.MorphTargets.Count];
                if (_MorphTargets.All(item => item.Tangents != null)) morphTangents = new Vector3[this.MorphTargets.Count];
                if (_MorphTargets.All(item => item.Colors0 != null)) morphColors0 = new Vector4[this.MorphTargets.Count];
            }

            // prepare skinning data, if available

            var jw0 = Joints0 != null && Weights0 != null;
            var jw1 = Joints1 != null && Weights1 != null;

            var skinning = new (int, float)[(jw0 ? 4 : 0) + (jw1 ? 4 : 0)];

            // loop over every vertex

            int vcount = Positions.Count;

            for (int i = 0; i < vcount; ++i)
            {
                if (jw0)
                {
                    var j = Joints0[i];
                    var w = Weights0[i];
                    skinning[0] = ((int)j.X, w.X);
                    skinning[1] = ((int)j.Y, w.Y);
                    skinning[2] = ((int)j.Z, w.Z);
                    skinning[3] = ((int)j.W, w.W);
                }

                if (jw1)
                {
                    var j = Joints1[i];
                    var w = Weights1[i];
                    skinning[4] = ((int)j.X, w.X);
                    skinning[5] = ((int)j.Y, w.Y);
                    skinning[6] = ((int)j.Z, w.Z);
                    skinning[7] = ((int)j.W, w.W);
                }

                if (Positions != null)
                {
                    _FillMorphData(morphPositions, vc => vc.Positions[i]);
                    Positions[i] = transform.TransformPosition(Positions[i], morphPositions, skinning);
                }

                if (Normals != null)
                {
                    _FillMorphData(morphNormals, vc => vc.Normals[i]);
                    Normals[i] = transform.TransformNormal(Normals[i], morphNormals, skinning);
                }

                if (Tangents != null)
                {
                    _FillMorphData(morphTangents, vc => vc.Tangents[i]);
                    Tangents[i] = transform.TransformTangent(Tangents[i], morphTangents, skinning);
                }

                if (Colors0 != null)
                {
                    _FillMorphData(morphColors0, vc => vc.Colors0[i]);
                    Colors0[i] = transform.MorphColors(Colors0[i], morphColors0);
                }
            }

            // we've just applied the transform,
            // so we no longer need these columns.

            _MorphTargets = null;

            Joints0 = null;
            Joints1 = null;
            Weights0 = null;
            Weights1 = null;
        }

        private void _FillMorphData(Vector3[] array, Func<MorphTargetColumns, Vector3> selector)
        {
            if (array == null) return;

            for (int i = 0; i < this._MorphTargets.Count; ++i)
            {
                array[i] = selector(this._MorphTargets[i]);
            }
        }

        private void _FillMorphData(Vector4[] array, Func<MorphTargetColumns, Vector4> selector)
        {
            if (array == null) return;

            for (int i = 0; i < this._MorphTargets.Count; ++i)
            {
                array[i] = selector(this._MorphTargets[i]);
            }
        }

        public MorphTargetColumns AddMorphTarget()
        {
            if (_MorphTargets == null) _MorphTargets = new List<MorphTargetColumns>();
            var mt = new MorphTargetColumns();
            _MorphTargets.Add(mt);

            return mt;
        }
        
        #endregion

        #region API - Vertex indexing

        private void CopyTo(int index, IVertexGeometry v)
        {
            if (Positions != null) v.SetPosition(Positions[index]);
            if (Normals != null) v.SetNormal(Normals[index]);
            if (Tangents != null) v.SetTangent(Tangents[index]);
        }

        private void CopyTo(int index, IVertexMaterial v)
        {
            if (Colors0 != null && v.MaxColors > 0) v.SetColor(0, Colors0[index]);
            if (Colors1 != null && v.MaxColors > 1) v.SetColor(1, Colors1[index]);

            if (TexCoords0 != null && v.MaxTextCoords > 0) v.SetTexCoord(0, TexCoords0[index]);
            if (TexCoords1 != null && v.MaxTextCoords > 1) v.SetTexCoord(1, TexCoords1[index]);
        }

        private void CopyTo(int index, IVertexSkinning v)
        {
            if (Joints0 != null && Weights0 != null)
            {
                var j = Joints0[index];
                var w = Weights0[index];

                v.SetJointBinding(0, (int)j.X, w.X);
                v.SetJointBinding(1, (int)j.Y, w.Y);
                v.SetJointBinding(2, (int)j.Z, w.Z);
                v.SetJointBinding(3, (int)j.W, w.W);
            }

            if (Joints1 != null && Weights1 != null)
            {
                var j = Joints1[index];
                var w = Weights1[index];

                v.SetJointBinding(4, (int)j.X, w.X);
                v.SetJointBinding(5, (int)j.Y, w.Y);
                v.SetJointBinding(6, (int)j.Z, w.Z);
                v.SetJointBinding(7, (int)j.W, w.W);
            }
        }

        public IVertexBuilder GetVertex(Type vertexType, int index)
        {
            var v = (IVertexBuilder)Activator.CreateInstance(vertexType);

            var g = v.GetGeometry();
            CopyTo(index, g);
            v.SetGeometry(g);

            var m = v.GetMaterial();
            CopyTo(index, m);
            v.SetMaterial(m);

            var s = v.GetSkinning();
            CopyTo(index, s);
            v.SetSkinning(s);

            return v;
        }

        public VertexBuilder<TvG, TvM, VertexEmpty> GetVertex<TvG, TvM>(int index)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            var g = default(TvG); CopyTo(index, g);
            var m = default(TvM); CopyTo(index, m);

            return new VertexBuilder<TvG, TvM, VertexEmpty>(g, m);
        }

        public VertexBuilder<TvG, TvM, TvS> GetVertex<TvG, TvM, TvS>(int index)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var g = default(TvG); CopyTo(index, g);
            var m = default(TvM); CopyTo(index, m);
            var s = default(TvS); CopyTo(index, s);

            return new VertexBuilder<TvG, TvM, TvS>(g, m, s);
        }

        #endregion
    }
}