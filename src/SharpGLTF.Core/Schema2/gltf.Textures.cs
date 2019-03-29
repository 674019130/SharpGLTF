﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Texture[{LogicalIndex}] {Name}")]
    public sealed partial class Texture
    {
        #region lifecycle

        internal Texture() { }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Texture"/> at <see cref="ModelRoot.LogicalTextures"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalTextures.IndexOfReference(this);

        public TextureSampler Sampler
        {
            get => _sampler.HasValue ? LogicalParent.LogicalTextureSamplers[_sampler.Value] : null;
            set
            {
                if (value != null) Guard.MustShareLogicalParent(this, value, nameof(value));
                _sampler = value?.LogicalIndex;
            }
        }

        public Image Image
        {
            get => _source.HasValue ? LogicalParent.LogicalImages[_source.Value] : null;
            set
            {
                if (value != null) Guard.MustShareLogicalParent(this, value, nameof(value));
                _source = value?.LogicalIndex;
            }
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("TextureSampler[{LogicalIndex}] {Name}")]
    public sealed partial class TextureSampler
    {
        #region lifecycle

        internal TextureSampler() { }

        internal TextureSampler(TextureInterpolationMode mag, TextureMipMapMode min, TextureWrapMode ws, TextureWrapMode wt)
        {
            _magFilter = mag;
            _minFilter = min;
            _wrapS = ws;
            _wrapT = wt;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="TextureSampler"/> at <see cref="ModelRoot.LogicalTextureSamplers"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalTextureSamplers.IndexOfReference(this);

        public TextureInterpolationMode MagFilter => _magFilter.AsValue(TextureInterpolationMode.LINEAR);

        public TextureMipMapMode MinFilter => _minFilter.AsValue(TextureMipMapMode.LINEAR);

        public TextureWrapMode WrapS => _wrapS.AsValue(_wrapSDefault);

        public TextureWrapMode WrapT => _wrapT.AsValue(_wrapTDefault);

        #endregion
    }

    

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates or reuses a <see cref="TextureSampler"/> instance
        /// at <see cref="ModelRoot.LogicalTextureSamplers"/>.
        /// </summary>
        /// <param name="mag">A value of <see cref="TextureInterpolationMode"/>.</param>
        /// <param name="min">A value of <see cref="TextureMipMapMode"/>.</param>
        /// <param name="ws">The <see cref="TextureWrapMode"/> in the S axis.</param>
        /// <param name="wt">The <see cref="TextureWrapMode"/> in the T axis.</param>
        /// <returns>A <see cref="TextureSampler"/> instance.</returns>
        public TextureSampler UseSampler(TextureInterpolationMode mag, TextureMipMapMode min, TextureWrapMode ws, TextureWrapMode wt)
        {
            foreach (var s in this._samplers)
            {
                if (s.MagFilter == mag && s.MinFilter == min && s.WrapS == ws && s.WrapT == wt) return s;
            }

            var ss = new TextureSampler(mag, min, ws, wt);

            this._samplers.Add(ss);

            return ss;
        }

        /// <summary>
        /// Creates or reuses a <see cref="Texture"/> instance
        /// at <see cref="ModelRoot.LogicalTextures"/>.
        /// </summary>
        /// <param name="image">The source <see cref="Image"/>.</param>
        /// <param name="sampler">The source <see cref="TextureSampler"/>.</param>
        /// <returns>A <see cref="Texture"/> instance.</returns>
        public Texture UseTexture(Image image, TextureSampler sampler)
        {
            if (image == null) return null;

            if (image != null) Guard.MustShareLogicalParent(this, image, nameof(image));
            if (sampler != null) Guard.MustShareLogicalParent(this, sampler, nameof(sampler));

            var tex = _textures.FirstOrDefault(item => item.Image == image && item.Sampler == sampler);
            if (tex != null) return tex;

            tex = new Texture();
            _textures.Add(tex);

            tex.Image = image;
            tex.Sampler = sampler;

            return tex;
        }

        internal T _UseTextureInfo<T>(Image image, TextureSampler sampler, int textureSet)
            where T : TextureInfo, new()
        {
            var tex = UseTexture(image, sampler);
            if (tex == null) return null;

            return new T
            {
                _LogicalTextureIndex = tex.LogicalIndex,
                TextureCoordinate = textureSet
            };
        }
    }
}
