﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Defines a node object within an armature.
    /// </summary>
    public class NodeBuilder
    {
        #region lifecycle

        public NodeBuilder() { }

        public NodeBuilder(string name) { Name = name; }

        private NodeBuilder(NodeBuilder parent)
        {
            _Parent = parent;
        }

        #endregion

        #region data

        private readonly NodeBuilder _Parent;

        private readonly List<NodeBuilder> _Children = new List<NodeBuilder>();

        private Matrix4x4? _Matrix;
        private Animations.AnimatableProperty<Vector3> _Scale;
        private Animations.AnimatableProperty<Quaternion> _Rotation;
        private Animations.AnimatableProperty<Vector3> _Translation;

        #endregion

        #region properties - hierarchy

        public String Name { get; set; }

        public NodeBuilder Parent => _Parent;

        public NodeBuilder Root => _Parent == null ? this : _Parent.Root;

        public IReadOnlyList<NodeBuilder> Children => _Children;

        #endregion

        #region properties - transform

        /// <summary>
        /// Gets a value indicating whether this <see cref="NodeBuilder"/> has animations.
        /// </summary>
        public bool HasAnimations => (_Scale?.IsAnimated ?? false) || (_Rotation?.IsAnimated ?? false) || (_Translation?.IsAnimated ?? false);

        public Animations.AnimatableProperty<Vector3> Scale => _Scale;

        public Animations.AnimatableProperty<Quaternion> Rotation => _Rotation;

        public Animations.AnimatableProperty<Vector3> Translation => _Translation;

        /// <summary>
        /// Gets or sets the local transform <see cref="Matrix4x4"/> of this <see cref="NodeBuilder"/>.
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get => Transforms.AffineTransform.Evaluate(_Matrix, Scale?.Value, Rotation?.Value, Translation?.Value);
            set
            {
                if (HasAnimations) { _DecomposeMatrix(value); return; }

                _Matrix = value != Matrix4x4.Identity ? value : (Matrix4x4?)null;
                _Scale = null;
                _Rotation = null;
                _Translation = null;
            }
        }

        /// <summary>
        /// Gets or sets the local Scale, Rotation and Translation of this <see cref="NodeBuilder"/>.
        /// </summary>
        public Transforms.AffineTransform LocalTransform
        {
            get => _Matrix.HasValue
                ?
                Transforms.AffineTransform.Create(_Matrix.Value)
                :
                Transforms.AffineTransform.Create(Translation?.Value, Rotation?.Value, Scale?.Value);
            set
            {
                Guard.IsTrue(value.IsValid, nameof(value));

                _Matrix = null;

                if (value.Scale != Vector3.One) UseScale().Value = value.Scale;
                if (value.Rotation != Quaternion.Identity) UseRotation().Value = value.Rotation;
                if (value.Translation != Vector3.Zero) UseTranslation().Value = value.Translation;
            }
        }

        /// <summary>
        /// Gets or sets the world transform <see cref="Matrix4x4"/> of this <see cref="NodeBuilder"/>.
        /// </summary>
        public Matrix4x4 WorldMatrix
        {
            get
            {
                var vs = this.Parent;
                return vs == null ? LocalMatrix : Transforms.AffineTransform.LocalToWorld(vs.WorldMatrix, LocalMatrix);
            }
            set
            {
                var vs = this.Parent;
                LocalMatrix = vs == null ? value : Transforms.AffineTransform.WorldToLocal(vs.WorldMatrix, value);
            }
        }

        #endregion

        #region API - hierarchy

        public NodeBuilder CreateNode(string name = null)
        {
            var c = new NodeBuilder(this);
            _Children.Add(c);
            c.Name = name;
            return c;
        }

        /// <summary>
        /// Checks if the collection of joints can be used for skinning a mesh.
        /// </summary>
        /// <param name="joints">A collection of joints.</param>
        /// <returns>True if the joints can be used for skinning.</returns>
        public static bool IsValidArmature(IEnumerable<NodeBuilder> joints)
        {
            if (joints == null) return false;
            if (!joints.Any()) return false;
            if (joints.Any(item => item == null)) return false;

            var root = joints.First().Root;

            return joints.All(item => Object.ReferenceEquals(item.Root, root));
        }

        #endregion

        #region API - transform

        private void _DecomposeMatrix()
        {
            if (!_Matrix.HasValue) return;
            if (_Matrix.Value == Matrix4x4.Identity) return;
            _DecomposeMatrix(_Matrix.Value);
            _Matrix = null;
        }

        private void _DecomposeMatrix(Matrix4x4 matrix)
        {
            var affine = Transforms.AffineTransform.Create(matrix);

            UseScale().Value = affine.Scale;
            UseRotation().Value = affine.Rotation;
            UseTranslation().Value = affine.Translation;
        }

        public Animations.AnimatableProperty<Vector3> UseScale()
        {
            _DecomposeMatrix();

            if (_Scale == null)
            {
                _Scale = new Animations.AnimatableProperty<Vector3>();
                _Scale.Value = Vector3.One;
            }

            return _Scale;
        }

        public Animations.CurveBuilder<Vector3> UseScale(string animationTrack)
        {
            return UseScale().UseTrackBuilder(animationTrack);
        }

        public Animations.AnimatableProperty<Quaternion> UseRotation()
        {
            _DecomposeMatrix();

            if (_Rotation == null)
            {
                _Rotation = new Animations.AnimatableProperty<Quaternion>();
                _Rotation.Value = Quaternion.Identity;
            }

            return _Rotation;
        }

        public Animations.CurveBuilder<Quaternion> UseRotation(string animationTrack)
        {
            return UseRotation().UseTrackBuilder(animationTrack);
        }

        public Animations.AnimatableProperty<Vector3> UseTranslation()
        {
            _DecomposeMatrix();

            if (_Translation == null)
            {
                _Translation = new Animations.AnimatableProperty<Vector3>();
                _Translation.Value = Vector3.Zero;
            }

            return _Translation;
        }

        public Animations.CurveBuilder<Vector3> UseTranslation(string animationTrack)
        {
            return UseTranslation().UseTrackBuilder(animationTrack);
        }

        public void SetScaleTrack(string track, Animations.ICurveSampler<Vector3> curve) { UseScale().SetTrack(track, curve); }

        public void SetTranslationTrack(string track, Animations.ICurveSampler<Vector3> curve) { UseTranslation().SetTrack(track, curve); }

        public void SetRotationTrack(string track, Animations.ICurveSampler<Quaternion> curve) { UseRotation().SetTrack(track, curve); }

        public Transforms.AffineTransform GetLocalTransform(string animationTrack, float time)
        {
            if (animationTrack == null) return this.LocalTransform;

            var scale = Scale?.GetValueAt(animationTrack, time);
            var rotation = Rotation?.GetValueAt(animationTrack, time);
            var translation = Translation?.GetValueAt(animationTrack, time);

            return Transforms.AffineTransform.Create(translation, rotation, scale);
        }

        public Matrix4x4 GetWorldMatrix(string animationTrack, float time)
        {
            if (animationTrack == null) return this.WorldMatrix;

            var vs = Parent;
            var lm = GetLocalTransform(animationTrack, time).Matrix;
            return vs == null ? lm : Transforms.AffineTransform.LocalToWorld(vs.GetWorldMatrix(animationTrack, time), lm);
        }

        #endregion

        #region With* API

        public NodeBuilder WithLocalTranslation(Vector3 translation)
        {
            this.UseTranslation().Value = translation;
            return this;
        }

        public NodeBuilder WithLocalScale(Vector3 scale)
        {
            this.UseScale().Value = scale;
            return this;
        }

        public NodeBuilder WithLocalRotation(Quaternion rotation)
        {
            this.UseRotation().Value = rotation;
            return this;
        }

        #endregion
    }
}
