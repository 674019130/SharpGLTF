﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2.LoadAndSave
{
    [TestFixture]
    public class LoadModelTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            TestFiles.CheckoutDataDirectories();
        }

        #endregion

        #region testing models of https://github.com/bghgary/glTF-Asset-Generator.git

        [Test]
        public void TestLoadReferenceModels()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var files = TestFiles.GetGeneratedFilePaths();

            foreach (var f in files)
            {
                var errors = _LoadNumErrorsForModel(f);

                if (errors > 0) continue;

                try
                {
                    var model = ModelRoot.Load(f);
                    model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".obj"));
                }
                catch (IO.UnsupportedExtensionException eex)
                {
                    TestContext.WriteLine($"{f.ToShortDisplayPath()} ERROR: {eex.Message}");
                }
            }
        }

        // right now this test does not make much sense...
        // [Test]
        public void TestLoadInvalidModels()
        {
            TestContext.CurrentContext.AttachShowDirLink();

            var files = TestFiles.GetGeneratedFilePaths();

            bool passed = true;

            foreach (var f in files)
            {
                var errors = _LoadNumErrorsForModel(f);

                if (errors == 0) continue;
                
                try
                {
                    var model = GltfUtils.LoadModel(f);
                    passed = false;

                    TestContext.WriteLine($"FAILED {f.ToShortDisplayPath()}");
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"PASSED {f.ToShortDisplayPath()}");
                }                
            }

            Assert.IsTrue(passed);
        }

        private static int _LoadNumErrorsForModel(string gltfPath)
        {
            var dir = System.IO.Path.GetDirectoryName(gltfPath);
            var fn = System.IO.Path.GetFileNameWithoutExtension(gltfPath);

            var jsonPath = System.IO.Path.Combine(dir, "ValidatorResults", System.IO.Path.ChangeExtension(fn, "json"));

            var content = System.IO.File.ReadAllText(jsonPath);
            var doc = Newtonsoft.Json.Linq.JObject.Parse(content);

            var token = doc.SelectToken("issues").SelectToken("numErrors");

            return (int)token;
        }
        
        #endregion

        #region testing models of https://github.com/KhronosGroup/glTF-Sample-Models.git

        [TestCase("\\glTF\\")]
        // [TestCase("\\glTF-Draco\\")] // Not supported
        [TestCase("\\glTF-IBL\\")]
        [TestCase("\\glTF-Binary\\")]
        [TestCase("\\glTF-Embedded\\")]
        [TestCase("\\glTF-pbrSpecularGlossiness\\")]
        public void TestLoadSampleModels(string section)
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            foreach (var f in TestFiles.GetSampleFilePaths())
            {
                if (!f.Contains(section)) continue;

                var model = GltfUtils.LoadModel(f);
                Assert.NotNull(model);

                // evaluate and save all the triangles to a Wavefront Object
                model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".obj"));
                model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".glb"));
                
                // do a model clone and compare it
                _AssertAreEqual(model, model.DeepClone());

                // check extensions used
                if (!model.ExtensionsUsed.Contains("EXT_lights_image_based"))
                {
                    var detectedExtensions = model.RetrieveUsedExtensions().ToArray();
                    CollectionAssert.AreEquivalent(model.ExtensionsUsed, detectedExtensions);
                }
            }
        }

        private static void _AssertAreEqual(ModelRoot a, ModelRoot b)
        {
            var aa = a.GetLogicalChildrenFlattened().ToList();
            var bb = b.GetLogicalChildrenFlattened().ToList();

            Assert.AreEqual(aa.Count,bb.Count);

            CollectionAssert.AreEqual
                (
                aa.Select(item => item.GetType()),
                bb.Select(item => item.GetType())
                );
        }

        [TestCase("SpecGlossVsMetalRough.gltf")]
        [TestCase(@"UnlitTest\glTF-Binary\UnlitTest.glb")]
        public void TestLoadSpecialCaseModels(string filePath)
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var f = TestFiles.GetSampleFilePaths()
                .FirstOrDefault(item => item.EndsWith(filePath));

            var model = GltfUtils.LoadModel(f);
            Assert.NotNull(model);

            // evaluate and save all the triangles to a Wavefront Object
            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".obj"));
            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".glb"));
            model.AttachToCurrentTest(System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(f), ".gltf"));

            // do a model roundtrip
            var bytes = model.WriteGLB();
            var modelBis = ModelRoot.ParseGLB(bytes);

            // clone
            var cloned = model.DeepClone();
        }

        [Test]
        public void TestLoadUnlitModel()
        {
            var f = TestFiles.GetSampleFilePaths()
                .FirstOrDefault(item => item.EndsWith(@"UnlitTest\glTF-Binary\UnlitTest.glb"));

            var model = GltfUtils.LoadModel(f);
            Assert.NotNull(model);

            Assert.IsTrue(model.LogicalMaterials[0].Unlit);

            // do a model roundtrip
            var modelBis = ModelRoot.ParseGLB(model.WriteGLB());
            Assert.NotNull(modelBis);

            Assert.IsTrue(modelBis.LogicalMaterials[0].Unlit);
        }

        [Test]
        public void TestLoadLightsModel()
        {
            var f = TestFiles.GetSchemaFilePaths()
                .FirstOrDefault(item => item.EndsWith("lights.gltf"));

            var model = GltfUtils.LoadModel(f);
            Assert.NotNull(model);

            Assert.AreEqual(3, model.LogicalPunctualLights.Count);

            Assert.AreEqual(1, model.DefaultScene.VisualChildren.ElementAt(0).PunctualLight.LogicalIndex);
            Assert.AreEqual(0, model.DefaultScene.VisualChildren.ElementAt(1).PunctualLight.LogicalIndex);
        }

        #endregion

        #region testing polly model

        [Test(Description ="Example of traversing the visual tree all the way to individual vertices and indices")]
        public void TestLoadPolly()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            
            // load Polly model
            var model = GltfUtils.LoadModel( TestFiles.GetPollyFilePath() );

            Assert.NotNull(model);            

            // Save as GLB, and also evaluate all triangles and save as Wavefront OBJ            
            model.AttachToCurrentTest("polly_out.glb");
            model.AttachToCurrentTest("polly_out.obj");

            // hierarchically browse some elements of the model:

            var scene = model.DefaultScene;

            var pollyNode = scene.FindNode("Polly_Display");            

            var pollyPrimitive = pollyNode.Mesh.Primitives[0];

            var pollyIndices = pollyPrimitive.GetIndices();
            var pollyPositions = pollyPrimitive.GetVertices("POSITION").AsVector3Array();
            var pollyNormals = pollyPrimitive.GetVertices("NORMAL").AsVector3Array();

            for (int i=0; i < pollyIndices.Count; i+=3)
            {
                var a = (int)pollyIndices[i + 0];
                var b = (int)pollyIndices[i + 1];
                var c = (int)pollyIndices[i + 2];

                var ap = pollyPositions[a];
                var bp = pollyPositions[b];
                var cp = pollyPositions[c];

                var an = pollyNormals[a];
                var bn = pollyNormals[b];
                var cn = pollyNormals[c];

                TestContext.WriteLine($"Triangle {ap} {an} {bp} {bn} {cp} {cn}");
            }
        }

        #endregion        
    }
}
