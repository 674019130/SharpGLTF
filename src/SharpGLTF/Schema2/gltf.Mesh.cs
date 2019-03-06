﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    using Collections;

    [System.Diagnostics.DebuggerDisplay("Mesh[{LogicalIndex}] {Name}")]
    public sealed partial class Mesh
    {
        #region lifecycle

        internal Mesh()
        {
            _primitives = new ChildrenCollection<MeshPrimitive, Mesh>(this);
            _weights = new List<double>();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Mesh"/> at <see cref="ModelRoot.LogicalMeshes"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalMeshes.IndexOfReference(this);

        public IEnumerable<Node> VisualParents => Node.FindNodesUsingMesh(this);

        public IReadOnlyList<MeshPrimitive> Primitives => _primitives;

        public IReadOnlyList<Single> MorphWeights => _weights.Select(item => (Single)item).ToArray();

        public Transforms.BoundingBox3? LocalBounds3 => Transforms.BoundingBox3.UnionOf(Primitives.Select(item => item.LocalBounds3));

        #endregion

        #region API

        /// <summary>
        /// Creates a new <see cref="MeshPrimitive"/> instance
        /// and adds it to the current <see cref="Mesh"/>.
        /// </summary>
        /// <returns>A <see cref="MeshPrimitive"/> instance.</returns>
        public MeshPrimitive CreatePrimitive()
        {
            var mp = new MeshPrimitive();

            _primitives.Add(mp);

            return mp;
        }

        public override IEnumerable<Exception> Validate()
        {
            var exx = base.Validate().ToList();

            foreach (var p in this.Primitives)
            {
                exx.AddRange(p.Validate());
            }

            return exx;
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Mesh"/> instance
        /// and adds it to <see cref="ModelRoot.LogicalMeshes"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Mesh"/> instance.</returns>
        public Mesh CreateMesh(string name = null)
        {
            var mesh = new Mesh();
            mesh.Name = name;

            this._meshes.Add(mesh);

            return mesh;
        }
    }
}
