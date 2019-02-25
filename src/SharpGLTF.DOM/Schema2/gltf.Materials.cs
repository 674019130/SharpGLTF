﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Material[{LogicalIndex}] {Name}")]
    public partial class Material
    {
        #region lifecycle

        internal static Material CreateBestChoice(IReadOnlyList<String> channelKeys)
        {
            if (channelKeys.Contains("Diffuse") && channelKeys.Contains("Glossiness")) return CreateMaterialPBRSpecularGlossiness();

                return CreatePBRMetallicRoughness();
        }

        internal static Material CreatePBRMetallicRoughness()
        {
            var m = new Material
            {
                _pbrMetallicRoughness = new MaterialPBRMetallicRoughness()
            };

            return m;
        }

        internal static Material CreateMaterialPBRSpecularGlossiness()
        {
            var m = new Material();

            m.SetExtension(new MaterialPBRSpecularGlossiness_KHR());

            return m;
        }

        internal static Material CreateMaterialUnlit()
        {
            var m = new Material();

            m.SetExtension(new MaterialUnlit_KHR());

            return m;
        }

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent.LogicalMaterials.IndexOfReference(this);

        public AlphaMode Alpha
        {
            get => _alphaMode.AsValue(_alphaModeDefault);
            set => _alphaMode = value.AsNullable(_alphaModeDefault);
        }

        public Double AlphaCutoff
        {
            get => _alphaCutoff.AsValue(_alphaCutoffDefault);
            set => _alphaCutoff = value.AsNullable(_alphaCutoffDefault, _alphaCutoffMinimum, double.MaxValue);
        }

        public Boolean DoubleSided
        {
            get => _doubleSided.AsValue(_doubleSidedDefault);
            set => _doubleSided = value.AsNullable(_doubleSidedDefault);
        }

        public Boolean Unlit => this.GetExtension<MaterialUnlit_KHR>() != null;

        /// <summary>
        /// returns a collection of channel views available for this material.
        /// </summary>
        public IEnumerable<MaterialChannelView> Channels
        {
            get
            {
                if (_pbrMetallicRoughness != null)
                {
                    var channels = _pbrMetallicRoughness.GetChannels(this);
                    foreach (var c in channels) yield return c;
                }

                var pbrSpecGloss = this.GetExtension<MaterialPBRSpecularGlossiness_KHR>();
                if (pbrSpecGloss != null)
                {
                    var channels = pbrSpecGloss.GetChannels(this);
                    foreach (var c in channels) yield return c;
                }

                yield return new MaterialChannelView
                    (
                    this,
                    "Normal",
                    _GetNormalTexture,
                    () => _GetNormalTexture(false) == null ? Vector4.Zero : new Vector4((float)_GetNormalTexture(false).Scale),
                    value => _GetNormalTexture(true).Scale = (double)value.X
                    );

                yield return new MaterialChannelView
                    (
                    this,
                    "Occlusion",
                    _GetOcclusionTexture,
                    () => _GetOcclusionTexture(false) == null ? Vector4.Zero : new Vector4((float)_GetOcclusionTexture(false).Strength),
                    value => _GetOcclusionTexture(true).Strength = (double)value.X
                    );

                yield return new MaterialChannelView
                    (
                    this,
                    "Emissive",
                    _GetEmissiveTexture,
                    () => { var rgb = _emissiveFactor.AsValue(_emissiveFactorDefault); return new Vector4(rgb, 1); },
                    value => _emissiveFactor = new Vector3(value.X, value.Y, value.Z).AsNullable(_emissiveFactorDefault)
                    );
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Returns an object that allows to read and write information of a given channel of the material.
        /// </summary>
        /// <param name="key">the channel key</param>
        /// <returns>the channel view</returns>
        public MaterialChannelView GetChannel(string key)
        {
            return Channels.FirstOrDefault(item => item.Key == key);
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

    [System.Diagnostics.DebuggerDisplay("Channel {_Key}")]
    public struct MaterialChannelView
    {
        #region lifecycle

        internal MaterialChannelView(Material m, String key, Func<Boolean, TextureInfo> texInfo, Func<Vector4> fg, Action<Vector4> fs)
        {
            _Key = key;
            _Material = m;
            _TextureInfoGetter = texInfo;
            _FactorGetter = fg;
            _FactorSetter = fs;
        }

        #endregion

        #region data

        private readonly String _Key;
        private readonly Material _Material;

        private readonly Func<Boolean, TextureInfo> _TextureInfoGetter;

        private readonly Func<Vector4> _FactorGetter;
        private readonly Action<Vector4> _FactorSetter;

        #endregion

        #region properties

        public String Key => _Key;

        public Texture Texture => _TextureInfoGetter?.Invoke(false) == null ? null : _Material.LogicalParent.LogicalTextures[_TextureInfoGetter(false)._LogicalTextureIndex];

        public int Set => _TextureInfoGetter?.Invoke(false) == null ? 0 : _TextureInfoGetter(false).TextureSet;

        public Image Image => Texture?.Source;

        public Sampler Sampler => Texture?.Sampler;

        public Vector4 Factor => _FactorGetter();

        #endregion

        #region fluent API

        public void SetFactor(Vector4 value) { _FactorSetter?.Invoke(value); }

        public void SetTexture(
            int texSet,
            Image texImg,
            TextureInterpolationMode mag = TextureInterpolationMode.LINEAR,
            TextureMipMapMode min = TextureMipMapMode.LINEAR_MIPMAP_LINEAR,
            TextureWrapMode ws = TextureWrapMode.REPEAT,
            TextureWrapMode wt = TextureWrapMode.REPEAT)
        {
            if (texImg == null) return; // in theory, we should completely remove the TextureInfo

            var sampler = _Material.LogicalParent.UseLogicalSampler(mag, min, ws, wt);
            var texture = _Material.LogicalParent.UseLogicalTexture(texImg, sampler);

            SetTexture(texSet, texture);
        }

        public void SetTexture(int texSet, Texture tex)
        {
            Guard.NotNull(tex, nameof(tex));
            Guard.MustShareLogicalParent(_Material, tex, nameof(tex));

            var texInfo = _TextureInfoGetter(true);

            texInfo.TextureSet = texSet;
            texInfo._LogicalTextureIndex = tex.LogicalIndex;
        }

        #endregion
    }

    internal partial class MaterialPBRMetallicRoughness
    {
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

        public IEnumerable<MaterialChannelView> GetChannels(Material material)
        {
            yield return new MaterialChannelView
                (
                material,
                "BaseColor",
                _GetBaseTexture,
                () => _baseColorFactor.AsValue(_baseColorFactorDefault),
                value => _baseColorFactor = value.AsNullable(_baseColorFactorDefault)
                );

            yield return new MaterialChannelView
                (
                material,
                "Metallic",
                _GetMetallicTexture,
                () => new Vector4((float)(_metallicFactor ?? _metallicFactorDefault)),
                value => _metallicFactor = ((double)value.X).AsNullable(_metallicFactorDefault, _metallicFactorMaximum, _metallicFactorMaximum)
                );

            yield return new MaterialChannelView
                (
                material,
                "Roughness",
                null,
                () => new Vector4((float)(_roughnessFactor ?? _roughnessFactorDefault)),
                value => _roughnessFactor = ((double)value.X).AsNullable(_roughnessFactorDefault, _roughnessFactorMinimum, _roughnessFactorMaximum)
                );
        }
    }

    internal partial class MaterialPBRSpecularGlossiness_KHR
    {
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

        public IEnumerable<MaterialChannelView> GetChannels(Material material)
        {
            yield return new MaterialChannelView
                (
                material,
                "Diffuse",
                _GetDiffuseTexture,
                () => _diffuseFactor.AsValue(_diffuseFactorDefault),
                value => _diffuseFactor = value.AsNullable(_diffuseFactorDefault)
                );

            yield return new MaterialChannelView
                (
                material,
                "Glossiness",
                _GetGlossinessTexture,
                () => new Vector4((float)(_glossinessFactor ?? _glossinessFactorDefault)),
                value => _glossinessFactor = ((double)value.X).AsNullable(_glossinessFactorDefault, _glossinessFactorMinimum, _glossinessFactorMaximum)
                );

            yield return new MaterialChannelView
                (
                material,
                "Specular",
                null,
                () => { var rgb = _specularFactor.AsValue(_specularFactorDefault); return new Vector4(rgb, 1); },
                value => _specularFactor = new Vector3(value.X, value.Y, value.Z).AsNullable(_specularFactorDefault)
                );
        }
    }

    public partial class ModelRoot
    {
        public Material AddLogicalMaterial()
        {
            var mat = new Material();

            _materials.Add(mat);

            return mat;
        }

        public Material AddLogicalMaterial(IReadOnlyList<string> channelKeys)
        {
            var mat = Material.CreateBestChoice(channelKeys);

            _materials.Add(mat);

            return mat;
        }

        public Material AddLogicalMaterial(Type mtype)
        {
            Material mat = null;

            if (mtype == typeof(MaterialPBRMetallicRoughness)) mat = Material.CreatePBRMetallicRoughness();
            if (mtype == typeof(MaterialPBRSpecularGlossiness_KHR)) mat = Material.CreateMaterialPBRSpecularGlossiness();
            if (mtype == typeof(MaterialUnlit_KHR)) mat = Material.CreateMaterialUnlit();

            if (mat == null) throw new ArgumentException(nameof(mtype));

            _materials.Add(mat);

            return mat;
        }
    }
}
