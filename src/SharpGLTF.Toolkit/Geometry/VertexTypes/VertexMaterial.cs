﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertexMaterial
    {
        int MaxColors { get; }

        int MaxTextCoords { get; }

        void Validate();

        Vector4 GetColor(int index);
        Vector2 GetTexCoord(int index);

        void SetColor(int setIndex, Vector4 color);
        void SetTexCoord(int setIndex, Vector2 coord);
    }

    /// <summary>
    /// Defines a Vertex attribute with a material Color.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("𝐂:{Color}")]
    public struct VertexColor1 : IVertexMaterial
    {
        #region constructors

        public VertexColor1(Vector4 color)
        {
            Color = color;
        }

        public VertexColor1(IVertexMaterial src)
        {
            this.Color = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
        }

        public static implicit operator VertexColor1(Vector4 color)
        {
            return new VertexColor1(color);
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        public int MaxColors => 1;

        public int MaxTextCoords => 0;

        #endregion

        #region API

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { }

        public Vector4 GetColor(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return Color;
        }

        public Vector2 GetTexCoord(int index)
        {
            throw new NotSupportedException();
        }

        public void Validate() { FragmentPreprocessors.ValidateVertexMaterial(this); }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a two material Colors.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("𝐂₀:{Color0} 𝐂₁:{Color1}")]
    public struct VertexColor2 : IVertexMaterial
    {
        #region constructors

        public VertexColor2(Vector4 color0, Vector4 color1)
        {
            Color0 = color0;
            Color1 = color1;
        }

        public VertexColor2(IVertexMaterial src)
        {
            this.Color0 = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.Color1 = src.MaxColors > 1 ? src.GetColor(1) : Vector4.One;
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color0;

        [VertexAttribute("COLOR_1", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color1;

        public int MaxColors => 2;

        public int MaxTextCoords => 0;

        #endregion

        #region API

        void IVertexMaterial.SetColor(int setIndex, Vector4 color)
        {
            if (setIndex == 0) this.Color0 = color;
            if (setIndex == 1) this.Color1 = color;
        }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { }

        public Vector4 GetColor(int index)
        {
            if (index == 0) return Color0;
            if (index == 1) return Color1;
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public Vector2 GetTexCoord(int index) { throw new NotSupportedException(); }

        public void Validate() { FragmentPreprocessors.ValidateVertexMaterial(this); }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a Texture Coordinate.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("𝐔𝐕:{TexCoord}")]
    public struct VertexTexture1 : IVertexMaterial
    {
        #region constructors

        public VertexTexture1(Vector2 uv)
        {
            TexCoord = uv;
        }

        public VertexTexture1(IVertexMaterial src)
        {
            this.TexCoord = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
        }

        public static implicit operator VertexTexture1(Vector2 uv)
        {
            return new VertexTexture1(uv);
        }

        #endregion

        #region data

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord;

        public int MaxColors => 0;

        public int MaxTextCoords => 1;

        #endregion

        #region API

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { if (setIndex == 0) this.TexCoord = coord; }

        public Vector4 GetColor(int index)
        {
            throw new NotSupportedException();
        }

        public Vector2 GetTexCoord(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return TexCoord;
        }

        public void Validate() { FragmentPreprocessors.ValidateVertexMaterial(this); }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with two Texture Coordinates.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("𝐔𝐕₀:{TexCoord0} 𝐔𝐕₁:{TexCoord1}")]
    public struct VertexTexture2 : IVertexMaterial
    {
        #region constructors

        public VertexTexture2(Vector2 uv0, Vector2 uv1)
        {
            TexCoord0 = uv0;
            TexCoord1 = uv1;
        }

        public VertexTexture2(IVertexMaterial src)
        {
            this.TexCoord0 = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
            this.TexCoord1 = src.MaxTextCoords > 1 ? src.GetTexCoord(1) : Vector2.Zero;
        }

        #endregion

        #region data

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord0;

        [VertexAttribute("TEXCOORD_1")]
        public Vector2 TexCoord1;

        public int MaxColors => 0;

        public int MaxTextCoords => 2;

        #endregion

        #region API

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            if (setIndex == 0) this.TexCoord0 = coord;
            if (setIndex == 1) this.TexCoord1 = coord;
        }

        public Vector4 GetColor(int index)
        {
            throw new NotSupportedException();
        }

        public Vector2 GetTexCoord(int index)
        {
            if (index == 0) return TexCoord0;
            if (index == 1) return TexCoord1;
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public void Validate() { FragmentPreprocessors.ValidateVertexMaterial(this); }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a Color material and a Texture Coordinate.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("𝐂:{Color} 𝐔𝐕:{TexCoord}")]
    public struct VertexColor1Texture1 : IVertexMaterial
    {
        #region constructors

        public VertexColor1Texture1(Vector4 color, Vector2 tex)
        {
            Color = color;
            TexCoord = tex;
        }

        public VertexColor1Texture1(IVertexMaterial src)
        {
            this.Color = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.TexCoord = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
        }

        public static implicit operator VertexColor1Texture1((Vector4,Vector2) coloruv)
        {
            return new VertexColor1Texture1(coloruv.Item1, coloruv.Item2);
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord;

        public int MaxColors => 1;

        public int MaxTextCoords => 1;

        #endregion

        #region API

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { if (setIndex == 0) this.TexCoord = coord; }

        public Vector4 GetColor(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return Color;
        }

        public Vector2 GetTexCoord(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return TexCoord;
        }

        public void Validate() { FragmentPreprocessors.ValidateVertexMaterial(this); }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a material Colors and two Texture Coordinates.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("𝐂:{Color} 𝐔𝐕₀:{TexCoord0} 𝐔𝐕₁:{TexCoord1}")]
    public struct VertexColor1Texture2 : IVertexMaterial
    {
        #region constructors

        public VertexColor1Texture2(Vector4 color, Vector2 tex0, Vector2 tex1)
        {
            Color = color;
            TexCoord0 = tex0;
            TexCoord1 = tex1;
        }

        public VertexColor1Texture2(IVertexMaterial src)
        {
            this.Color = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.TexCoord0 = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
            this.TexCoord1 = src.MaxTextCoords > 1 ? src.GetTexCoord(1) : Vector2.Zero;
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord0;

        [VertexAttribute("TEXCOORD_1")]
        public Vector2 TexCoord1;

        public int MaxColors => 1;

        public int MaxTextCoords => 2;

        #endregion

        #region API

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            if (setIndex == 0) this.TexCoord0 = coord;
            if (setIndex == 1) this.TexCoord1 = coord;
        }

        public Vector4 GetColor(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return Color;
        }

        public Vector2 GetTexCoord(int index)
        {
            switch (index)
            {
                case 0: return this.TexCoord0;
                case 1: return this.TexCoord1;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void Validate() { FragmentPreprocessors.ValidateVertexMaterial(this); }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with two material Colors and two Texture Coordinates.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("𝐂₀:{Color0} 𝐂₁:{Color1} 𝐔𝐕₀:{TexCoord0} 𝐔𝐕₁:{TexCoord1}")]
    public struct VertexColor2Texture2 : IVertexMaterial
    {
        #region constructors

        public VertexColor2Texture2(Vector4 color0, Vector4 color1, Vector2 tex0, Vector2 tex1)
        {
            Color0 = color0;
            Color1 = color1;
            TexCoord0 = tex0;
            TexCoord1 = tex1;
        }

        public VertexColor2Texture2(IVertexMaterial src)
        {
            this.Color0 = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.Color1 = src.MaxColors > 1 ? src.GetColor(1) : Vector4.One;
            this.TexCoord0 = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
            this.TexCoord1 = src.MaxTextCoords > 1 ? src.GetTexCoord(1) : Vector2.Zero;
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color0;

        [VertexAttribute("COLOR_1", Schema2.EncodingType.UNSIGNED_BYTE, true)]
        public Vector4 Color1;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord0;

        [VertexAttribute("TEXCOORD_1")]
        public Vector2 TexCoord1;

        public int MaxColors => 2;

        public int MaxTextCoords => 2;

        #endregion

        #region API

        void IVertexMaterial.SetColor(int setIndex, Vector4 color)
        {
            if (setIndex == 0) this.Color0 = color;
            if (setIndex == 1) this.Color1 = color;
        }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            if (setIndex == 0) this.TexCoord0 = coord;
            if (setIndex == 1) this.TexCoord1 = coord;
        }

        public Vector4 GetColor(int index)
        {
            switch (index)
            {
                case 0: return this.Color0;
                case 1: return this.Color1;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public Vector2 GetTexCoord(int index)
        {
            switch (index)
            {
                case 0: return this.TexCoord0;
                case 1: return this.TexCoord1;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void Validate() { FragmentPreprocessors.ValidateVertexMaterial(this); }

        #endregion
    }
}
