﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    public struct SkinnedPosition
    {
        public SkinnedPosition(float px, float py, float pz, int jointIndex)
        {
            Position = new Vector3(px, py, pz);
            Joints_0 = new Vector4(jointIndex);
            Weights_0 = Vector4.UnitX;
        }

        public SkinnedPosition(float px, float py, float pz, int jointIndex1, int jointIndex2)
        {
            Position = new Vector3(px, py, pz);
            Joints_0 = new Vector4(jointIndex1, jointIndex2, 0, 0);
            Weights_0 = new Vector4(0.5f, 0.5f, 0, 0);
        }

        [VertexAttribute("POSITION")]
        public Vector3 Position;

        [VertexAttribute("JOINTS_0", Schema2.ComponentType.UNSIGNED_BYTE, false)]
        public Vector4 Joints_0;

        [VertexAttribute("WEIGHTS_0", Schema2.ComponentType.UNSIGNED_BYTE, true)]
        public Vector4 Weights_0;
    }
}
