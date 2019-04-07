﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public interface IVertexPosition
    {
        void SetPosition(Vector3 position);
        void SetNormal(Vector3 normal);
        void SetTangent(Vector4 tangent);

        Vector3 GetPosition();
        Boolean TryGetNormal(out Vector3 normal);
        Boolean TryGetTangent(out Vector4 tangent);

        void AssignFrom(IVertexPosition vertex);

        void Transform(Matrix4x4 xform);

        void Validate();
    }

    /// <summary>
    /// Defines a Vertex attribute with a Position.
    /// </summary>
    public struct VertexPosition : IVertexPosition
    {
        public VertexPosition(Vector3 position)
        {
            this.Position = position;
        }

        public VertexPosition(float px, float py, float pz)
        {
            this.Position = new Vector3(px, py, pz);
        }

        public static implicit operator VertexPosition(Vector3 position)
        {
            return new VertexPosition(position);
        }

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        void IVertexPosition.SetPosition(Vector3 position) { this.Position = position; }

        void IVertexPosition.SetNormal(Vector3 normal) { }

        void IVertexPosition.SetTangent(Vector4 tangent) { }

        public Vector3 GetPosition() { return this.Position; }

        public bool TryGetNormal(out Vector3 normal) { normal = default; return false; }

        public bool TryGetTangent(out Vector4 tangent) { tangent = default; return false; }

        public void AssignFrom(IVertexPosition vertex) { this.Position = vertex.GetPosition(); }

        public void Transform(Matrix4x4 xform)
        {
            Position = Vector3.Transform(Position, xform);
        }

        public void Validate()
        {
            if (!Position._IsReal()) throw new NotFiniteNumberException(nameof(Position));
        }
    }

    /// <summary>
    /// Defines a Vertex attribute with a Position and a Normal.
    /// </summary>
    public struct VertexPositionNormal : IVertexPosition
    {
        public VertexPositionNormal(Vector3 p, Vector3 n)
        {
            Position = p;
            Normal = Vector3.Normalize(n);
        }

        public VertexPositionNormal(float px, float py, float pz, float nx, float ny, float nz)
        {
            Position = new Vector3(px, py, pz);
            Normal = Vector3.Normalize(new Vector3(nx, ny, nz));
        }

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("NORMAL")]
        public Vector3 Normal;

        void IVertexPosition.SetPosition(Vector3 position) { this.Position = position; }

        void IVertexPosition.SetNormal(Vector3 normal) { this.Normal = normal; }

        void IVertexPosition.SetTangent(Vector4 tangent) { }

        public Vector3 GetPosition() { return this.Position; }

        public bool TryGetNormal(out Vector3 normal) { normal = this.Normal; return true; }

        public bool TryGetTangent(out Vector4 tangent) { tangent = default; return false; }

        public void AssignFrom(IVertexPosition vertex)
        {
            this.Position = vertex.GetPosition();
            if (vertex.TryGetNormal(out Vector3 nrm)) this.Normal = nrm;
        }

        public void Transform(Matrix4x4 xform)
        {
            Position = Vector3.Transform(Position, xform);
            Normal = Vector3.Normalize(Vector3.TransformNormal(Normal, xform));
        }

        public void Validate()
        {
            if (!Position._IsReal()) throw new NotFiniteNumberException(nameof(Position));
            if (!Normal._IsReal()) throw new NotFiniteNumberException(nameof(Normal));
        }
    }

    /// <summary>
    /// Defines a Vertex attribute with a Position, a Normal and a Tangent.
    /// </summary>
    public struct VertexPositionNormalTangent : IVertexPosition
    {
        public VertexPositionNormalTangent(Vector3 p, Vector3 n, Vector4 t)
        {
            Position = p;
            Normal = Vector3.Normalize(n);
            Tangent = t;
        }

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("NORMAL")]
        public Vector3 Normal;

        [VertexAttribute("TANGENT")]
        public Vector4 Tangent;

        void IVertexPosition.SetPosition(Vector3 position) { this.Position = position; }

        void IVertexPosition.SetNormal(Vector3 normal) { this.Normal = normal; }

        void IVertexPosition.SetTangent(Vector4 tangent) { this.Tangent = tangent; }

        public Vector3 GetPosition() { return this.Position; }

        public bool TryGetNormal(out Vector3 normal) { normal = this.Normal; return true; }

        public bool TryGetTangent(out Vector4 tangent) { tangent = this.Tangent; return true; }

        public void AssignFrom(IVertexPosition vertex)
        {
            this.Position = vertex.GetPosition();
            if (vertex.TryGetNormal(out Vector3 nrm)) this.Normal = nrm;
            if (vertex.TryGetTangent(out Vector4 tgt)) this.Tangent = tgt;
        }

        public void Transform(Matrix4x4 xform)
        {
            Position = Vector3.Transform(Position, xform);
            Normal = Vector3.Normalize(Vector3.TransformNormal(Normal, xform));

            // TODO: not sure if this is correct, must be checked. Most probably, if the xform handedness if negative, Tangent.W must be reversed.
            var txyz = Vector3.Normalize(Vector3.TransformNormal(new Vector3(Tangent.X, Tangent.Y, Tangent.Z), xform));
            Tangent = new Vector4(txyz, Tangent.W);
        }

        public void Validate()
        {
            if (!Position._IsReal()) throw new NotFiniteNumberException(nameof(Position));
            if (!Normal._IsReal()) throw new NotFiniteNumberException(nameof(Normal));
            if (!Tangent._IsReal()) throw new NotFiniteNumberException(nameof(Tangent));
        }
    }
}
