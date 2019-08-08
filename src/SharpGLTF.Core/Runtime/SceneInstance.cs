﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpGLTF.Transforms;

using XFORM = System.Numerics.Matrix4x4;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Defines a node of a scene graph in <see cref="SceneInstance"/>
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public class NodeInstance
    {
        #region lifecycle

        internal NodeInstance(NodeTemplate template, NodeInstance parent)
        {
            _Template = template;
            _Parent = parent;
        }

        #endregion

        #region data

        private readonly NodeTemplate _Template;
        private readonly NodeInstance _Parent;

        private XFORM _LocalMatrix;
        private XFORM? _WorldMatrix;

        private SparseWeight8 _MorphWeights;

        #endregion

        #region properties

        public String Name => _Template.Name;

        public NodeInstance VisualParent => _Parent;

        public SparseWeight8 MorphWeights
        {
            get => _MorphWeights;
            set => _MorphWeights = value;
        }

        public XFORM LocalMatrix
        {
            get => _LocalMatrix;
            set
            {
                _LocalMatrix = value;
                _WorldMatrix = null;
            }
        }

        public XFORM WorldMatrix
        {
            get => _GetWorldMatrix();
            set => _SetWorldMatrix(value);
        }

        /// <summary>
        /// Gets a value indicating whether any of the transforms down the scene tree has been modified.
        /// </summary>
        private bool TransformChainIsDirty
        {
            get
            {
                if (!_WorldMatrix.HasValue) return true;

                return _Parent == null ? false : _Parent.TransformChainIsDirty;
            }
        }

        #endregion

        #region API

        private XFORM _GetWorldMatrix()
        {
            if (!TransformChainIsDirty) return _WorldMatrix.Value;

            _WorldMatrix = _Parent == null ? _LocalMatrix : XFORM.Multiply(_LocalMatrix, _Parent.WorldMatrix);

            return _WorldMatrix.Value;
        }

        private void _SetWorldMatrix(XFORM xform)
        {
            if (_Parent == null) { LocalMatrix = xform; return; }

            XFORM.Invert(_Parent._GetWorldMatrix(), out XFORM ipwm);

            LocalMatrix = XFORM.Multiply(xform, ipwm);
        }

        public void SetPoseTransform() { SetAnimationFrame(null, 0); }

        public void SetAnimationFrame(string trackName, float time)
        {
            this.MorphWeights = _Template.GetMorphWeights(trackName, time);
            this.LocalMatrix = _Template.GetLocalMatrix(trackName, time);
        }

        #endregion
    }

    /// <summary>
    /// Represents a specific and independent state of a <see cref="SceneTemplate"/>.
    /// </summary>
    public class SceneInstance
    {
        #region lifecycle

        internal SceneInstance(NodeTemplate[] nodeTemplates, DrawableReference[] drawables, IReadOnlyDictionary<string, float> tracks)
        {
            AnimationTracks = tracks;

            _NodeTemplates = nodeTemplates;
            _NodeInstances = new NodeInstance[_NodeTemplates.Length];

            for (var i = 0; i < _NodeInstances.Length; ++i)
            {
                var n = _NodeTemplates[i];
                var pidx = _NodeTemplates[i].ParentIndex;

                if (pidx >= i) throw new ArgumentException("invalid parent index", nameof(nodeTemplates));

                var p = pidx < 0 ? null : _NodeInstances[pidx];

                _NodeInstances[i] = new NodeInstance(n, p);
            }

            _DrawableReferences = drawables;
            _DrawableTransforms = new IGeometryTransform[_DrawableReferences.Length];

            for (int i = 0; i < _DrawableTransforms.Length; ++i)
            {
                _DrawableTransforms[i] = _DrawableReferences[i].CreateGeometryTransform();
            }
        }

        #endregion

        #region data

        private readonly NodeTemplate[] _NodeTemplates;
        private readonly NodeInstance[] _NodeInstances;

        private readonly DrawableReference[] _DrawableReferences;
        private readonly IGeometryTransform[] _DrawableTransforms;

        #endregion

        #region properties

        public IReadOnlyList<NodeInstance> LogicalNodes => _NodeInstances;

        public IEnumerable<NodeInstance> VisualNodes => _NodeInstances.Where(item => item.VisualParent == null);

        public IReadOnlyDictionary<string, float> AnimationTracks { get; private set; }

        /// <summary>
        /// Gets the number of drawable references.
        /// </summary>
        public int DrawableReferencesCount => _DrawableTransforms.Length;

        /// <summary>
        /// Gets a collection of drawable references, where Item1 is the logical Index
        /// of a <see cref="Schema2.Mesh"/> in <see cref="Schema2.ModelRoot.LogicalMeshes"/>
        /// and Item2 is an <see cref="IGeometryTransform"/> that can be used to transform
        /// the <see cref="Schema2.Mesh"/> into world space.
        /// </summary>
        public IEnumerable<(int, IGeometryTransform)> DrawableReferences
        {
            get
            {
                for (int i = 0; i < _DrawableTransforms.Length; ++i)
                {
                    yield return GetDrawableReference(i);
                }
            }
        }

        #endregion

        #region API

        public void SetPoseTransforms()
        {
            foreach (var n in _NodeInstances) n.SetPoseTransform();
        }

        public void SetAnimationFrame(string trackName, float time)
        {
            foreach (var n in _NodeInstances) n.SetAnimationFrame(trackName, time);
        }

        /// <summary>
        /// Gets a drawable reference pair, where Item1 is the logical Index
        /// of a <see cref="Schema2.Mesh"/> in <see cref="Schema2.ModelRoot.LogicalMeshes"/>
        /// and Item2 is an <see cref="IGeometryTransform"/> that can be used to transform
        /// the <see cref="Schema2.Mesh"/> into world space.
        /// </summary>
        /// <param name="index">The index of the drawable reference, from 0 to <see cref="DrawableReferencesCount"/></param>
        /// <returns>A drawable reference</returns>
        public (int, IGeometryTransform) GetDrawableReference(int index)
        {
            var dref = _DrawableReferences[index];

            dref.UpdateGeometryTransform(_DrawableTransforms[index], _NodeInstances);

            return (dref.LogicalMeshIndex, _DrawableTransforms[index]);
        }

        #endregion
    }
}
