﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace MeshBuffers
{
    [TestFixture]
    public class MeshBuffersTests
    {
        [Test]
        public void CreateVertexBufferTest()
        {
            var vbdecl = new VertexDeclaration()
                .WithVector3("POSITION")
                .WithVector3("NORMAL");

            Assert.AreEqual(6, vbdecl.Stride);            

            var vertex1 = vbdecl.CreateVertex();
            var vertex2 = vbdecl.CreateVertex();
            var vertex3 = vbdecl.CreateVertex();

            var vbuffer = new TriangleBufferBuilder(vbdecl);

            vertex1.Position = new Vector3(1, 2, 3);
            vertex1.Normal = Vector3.UnitX;
            vertex2.Position = new Vector3(4, 2, 3);
            vertex2.Normal = Vector3.UnitY;
            vertex3.Position = new Vector3(1, 5, 3);
            vertex3.Normal = Vector3.UnitZ;

            vbuffer.AddTriangle(vertex1, vertex2, vertex3);
            vbuffer.AddTriangle(vertex1, vertex2, vertex3);            

        }

    }
}
