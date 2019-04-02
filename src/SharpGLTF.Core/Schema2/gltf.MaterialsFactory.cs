﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public partial class Material
    {
        #region API

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Metallic Roughness attributes.
        /// </summary>
        public void InitializePBRMetallicRoughness()
        {
            if (this._pbrMetallicRoughness == null) this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();

            this.RemoveExtensions<MaterialPBRSpecularGlossiness>();
            this.RemoveExtensions<MaterialUnlit>();
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with PBR Specular Glossiness attributes.
        /// </summary>
        /// <param name="useFallback">true to add a PBRMetallicRoughness fallback material.</param>
        public void InitializePBRSpecularGlossiness(bool useFallback = false)
        {
            if (useFallback)
            {
                if (this._pbrMetallicRoughness == null) this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();
            }
            else
            {
                this._pbrMetallicRoughness = null;
            }

            this.RemoveExtensions<MaterialUnlit>();
            this.SetExtension(new MaterialPBRSpecularGlossiness(this));
        }

        /// <summary>
        /// Initializes this <see cref="Material"/> instance with Unlit attributes.
        /// </summary>
        public void InitializeUnlit()
        {
            if (this._pbrMetallicRoughness == null) this._pbrMetallicRoughness = new MaterialPBRMetallicRoughness();

            this.RemoveExtensions<MaterialPBRSpecularGlossiness>();
            this.SetExtension(new MaterialUnlit(this));
        }

        private IEnumerable<MaterialChannel> _GetChannels()
        {
            if (_pbrMetallicRoughness != null)
            {
                var channels = _pbrMetallicRoughness.GetChannels(this);
                foreach (var c in channels) yield return c;
            }

            var pbrSpecGloss = this.GetExtension<MaterialPBRSpecularGlossiness>();
            if (pbrSpecGloss != null)
            {
                var channels = pbrSpecGloss.GetChannels(this);
                foreach (var c in channels) yield return c;
            }

            yield return new MaterialChannel(this, "Normal", _GetNormalTexture, () => _GetNormalTexture(false)?.Scale ?? 0, value => _GetNormalTexture(true).Scale = value);

            yield return new MaterialChannel(this, "Occlusion", _GetOcclusionTexture, () => _GetOcclusionTexture(false)?.Strength ?? 0, value => _GetOcclusionTexture(true).Strength = value);

            yield return new MaterialChannel(this, "Emissive", _GetEmissiveTexture, () => this._EmissiveColor, value => this._EmissiveColor = value );
        }

        private Vector4 _EmissiveColor
        {
            get => new Vector4(_emissiveFactor.AsValue(_emissiveFactorDefault), 1);
            set => _emissiveFactor = new Vector3(value.X, value.Y, value.Z).AsNullable(_emissiveFactorDefault, Vector3.Zero, Vector3.One);
        }

        private MaterialNormalTextureInfo _GetNormalTexture(bool create)
        {
            if (create && _normalTexture == null) _normalTexture = new MaterialNormalTextureInfo();
            return _normalTexture;
        }

        private MaterialOcclusionTextureInfo _GetOcclusionTexture(bool create)
        {
            if (create && _occlusionTexture == null) _occlusionTexture = new MaterialOcclusionTextureInfo();
            return _occlusionTexture;
        }

        private TextureInfo _GetEmissiveTexture(bool create)
        {
            if (create && _emissiveTexture == null) _emissiveTexture = new TextureInfo();
            return _emissiveTexture;
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Material"/> instance and adds it to <see cref="ModelRoot.LogicalMaterials"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Material"/> instance.</returns>
        public Material CreateMaterial(string name = null)
        {
            var mat = new Material();
            mat.Name = name;

            _materials.Add(mat);

            return mat;
        }
    }

    internal sealed partial class MaterialPBRMetallicRoughness
    {
        /// <inheritdoc />
        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatItems(_baseColorTexture, _metallicRoughnessTexture);
        }

        private TextureInfo _GetBaseTexture(bool create)
        {
            if (create && _baseColorTexture == null) _baseColorTexture = new TextureInfo();
            return _baseColorTexture;
        }

        private TextureInfo _GetMetallicTexture(bool create)
        {
            if (create && _metallicRoughnessTexture == null) _metallicRoughnessTexture = new TextureInfo();
            return _metallicRoughnessTexture;
        }

        public Vector4 Color
        {
            get => _baseColorFactor.AsValue(_baseColorFactorDefault);
            set => _baseColorFactor = value.AsNullable(_baseColorFactorDefault);
        }

        public Vector4 Parameter
        {
            get
            {
                return new Vector4
                    (
                    (float) _metallicFactor.AsValue( _metallicFactorDefault),
                    (float)_roughnessFactor.AsValue(_roughnessFactorDefault),
                    0,
                    0
                    );
            }
            set
            {
                _metallicFactor  = ((double)value.X).AsNullable( _metallicFactorDefault,  _metallicFactorMinimum,  _metallicFactorMaximum);
                _roughnessFactor = ((double)value.Y).AsNullable(_roughnessFactorDefault, _roughnessFactorMinimum, _roughnessFactorMaximum);
            }
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            yield return new MaterialChannel(material, "BaseColor", _GetBaseTexture, () => this.Color, value => this.Color = value);

            yield return new MaterialChannel(material, "MetallicRoughness", _GetMetallicTexture, () => this.Parameter, value => this.Parameter = value);
        }
    }

    internal sealed partial class MaterialPBRSpecularGlossiness
    {
        internal MaterialPBRSpecularGlossiness(Material material) { }

        /// <inheritdoc />
        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().ConcatItems(_diffuseTexture, _specularGlossinessTexture);
        }

        private TextureInfo _GetDiffuseTexture(bool create)
        {
            if (create && _diffuseTexture == null) _diffuseTexture = new TextureInfo();
            return _diffuseTexture;
        }

        private TextureInfo _GetGlossinessTexture(bool create)
        {
            if (create && _specularGlossinessTexture == null) _specularGlossinessTexture = new TextureInfo();
            return _specularGlossinessTexture;
        }

        public Vector4 Color
        {
            get => _diffuseFactor.AsValue(_diffuseFactorDefault);
            set => _diffuseFactor = value.AsNullable(_diffuseFactorDefault);
        }

        public Vector4 Parameter
        {
            get
            {
                return new Vector4
                    (
                    _specularFactor.AsValue(_specularFactorDefault),
                    (float)_glossinessFactor.AsValue(_glossinessFactorDefault)
                    );
            }
            set
            {
                _specularFactor = new Vector3(value.X, value.Y, value.Z).AsNullable(_specularFactorDefault);
                _glossinessFactor = ((double)value.W).AsNullable(_glossinessFactorDefault, _glossinessFactorMinimum, _glossinessFactorMaximum);
            }
        }

        public IEnumerable<MaterialChannel> GetChannels(Material material)
        {
            yield return new MaterialChannel(material, "Diffuse", _GetDiffuseTexture, () => this.Color, value => this.Color = value);

            yield return new MaterialChannel(material, "SpecularGlossiness", _GetGlossinessTexture, () => this.Parameter, value => this.Parameter = value);
        }
    }

    internal sealed partial class MaterialUnlit
    {
        internal MaterialUnlit(Material material) { }
    }
}
