﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    public interface IMeshBuilder<TMaterial>
    {
        string Name { get; set; }

        IEnumerable<TMaterial> Materials { get; }

        IReadOnlyCollection<IPrimitiveReader<TMaterial>> Primitives { get; }

        IPrimitiveBuilder UsePrimitive(TMaterial material, int primitiveVertexCount = 3);

        void Validate();
    }

    static class MeshBuilderToolkit
    {
        public static IMeshBuilder<TMaterial> CreateMeshBuilderFromVertexAttributes<TMaterial>(params string[] vertexAttributes)
        {
            Type meshType = GetMeshBuilderType(typeof(TMaterial), vertexAttributes);

            var mesh = Activator.CreateInstance(meshType, string.Empty);

            return mesh as IMeshBuilder<TMaterial>;
        }

        public static Type GetMeshBuilderType(Type materialType, string[] vertexAttributes)
        {
            var tvg = VertexUtils.GetVertexGeometryType(vertexAttributes);
            var tvm = VertexUtils.GetVertexMaterialType(vertexAttributes);
            var tvs = VertexUtils.GetVertexSkinningType(vertexAttributes);

            var meshType = typeof(MeshBuilder<,,,>);

            meshType = meshType.MakeGenericType(materialType, tvg, tvm, tvs);
            return meshType;
        }

        public static IReadOnlyDictionary<Vector3, Vector3> CalculateSmoothNormals<TMaterial>(this IMeshBuilder<TMaterial> srcMesh)
        {
            var posnrm = new Dictionary<Vector3, Vector3>();

            void addDirection(Dictionary<Vector3, Vector3> dict, Vector3 pos, Vector3 dir)
            {
                if (!dir._IsFinite()) return;
                if (!dict.TryGetValue(pos, out Vector3 n)) n = Vector3.Zero;
                dict[pos] = n + dir;
            }

            foreach (var prim in srcMesh.Primitives)
            {
                foreach (var tri in prim.Triangles)
                {
                    var a = prim.Vertices[tri.Item1].GetGeometry().GetPosition();
                    var b = prim.Vertices[tri.Item1].GetGeometry().GetPosition();
                    var c = prim.Vertices[tri.Item1].GetGeometry().GetPosition();
                    var d = Vector3.Cross(b - a, c - a);
                    addDirection(posnrm, a, d);
                    addDirection(posnrm, b, d);
                    addDirection(posnrm, c, d);
                }
            }

            foreach (var pos in posnrm.Keys.ToList())
            {
                posnrm[pos] = Vector3.Normalize(posnrm[pos]);
            }

            return posnrm;
        }

        public static bool IsEmpty<TMaterial>(this IPrimitiveReader<TMaterial> primitive)
        {
            if (primitive.Points.Count > 0) return false;
            if (primitive.Lines.Count > 0) return false;
            if (primitive.Triangles.Count > 0) return false;
            return true;
        }

        public static bool IsEmpty<TMaterial>(this IMeshBuilder<TMaterial> mesh)
        {
            return mesh.Primitives.All(prim => prim.IsEmpty());
        }

        /// <summary>
        /// Given a set of 4 points defining a quadrangle, it determines which
        /// is the optimal diagonal to choose to reprensent the quadrangle as two triangles.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static bool GetQuadrangleDiagonal(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var area1 = Vector3.Cross(a - b, c - b).Length() + Vector3.Cross(a - d, c - d).Length();
            var area2 = Vector3.Cross(b - a, d - a).Length() + Vector3.Cross(b - c, d - c).Length();

            return area1 <= area2;
        }
    }
}
