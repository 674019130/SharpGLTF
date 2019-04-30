﻿using System.Numerics;

using NUnit.Framework;

namespace SharpGLTF.Schema2.Authoring
{
    using VPOS = Geometry.VertexTypes.VertexPosition;
    using VTEX = Geometry.VertexTypes.VertexTexture1;

    [TestFixture]
    [Category("Model Authoring")]
    public class ExtensionsCreationTests
    {
        [Test(Description = "Creates a scene with lights")]
        public void CreateSceneWithWithLightsExtension()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var root = ModelRoot.CreateModel();
            var scene = root.UseScene("Empty Scene");

            scene.CreateNode()
                .PunctualLight = root.CreatePunctualLight(PunctualLightType.Directional)
                .WithColor(Vector3.UnitX, 2);

            var node2 = scene.CreateNode()
                .PunctualLight = root.CreatePunctualLight(PunctualLightType.Spot)
                .WithColor(Vector3.UnitY, 3, 10)
                .WithSpotCone(0.2f, 0.3f);

            root.AttachToCurrentTest("sceneWithLight.gltf");
            root.AttachToCurrentTest("sceneWithLight.glb");
        }

        [Test(Description = "Creates a quad mesh with a complex material")]
        public void CreateSceneWithSpecularGlossinessExtension()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var basePath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "glTF-Sample-Models", "2.0", "SpecGlossVsMetalRough", "glTF");

            // first, create a default material
            var material = new Materials.MaterialBuilder("material1 fallback")
                .WithMetallicRoughnessShader()
                .WithChannelImage(Materials.KnownChannels.Normal, System.IO.Path.Combine(basePath, "WaterBottle_normal.png"))
                .WithChannelImage(Materials.KnownChannels.Emissive, System.IO.Path.Combine(basePath, "WaterBottle_emissive.png"))
                .WithChannelImage(Materials.KnownChannels.Occlusion, System.IO.Path.Combine(basePath, "WaterBottle_occlusion.png"))
                .WithChannelImage(Materials.KnownChannels.BaseColor, System.IO.Path.Combine(basePath, "WaterBottle_baseColor.png"))
                .WithChannelImage(Materials.KnownChannels.MetallicRoughness, System.IO.Path.Combine(basePath, "WaterBottle_roughnessMetallic.png"));

            // wrap the fallback material with a PBR Specular Glossiness material.
            material = new Materials.MaterialBuilder("material1")
                .WithFallback(material)
                .WithSpecularGlossinessShader()
                .WithChannelImage(Materials.KnownChannels.Normal, System.IO.Path.Combine(basePath, "WaterBottle_normal.png"))
                .WithChannelImage(Materials.KnownChannels.Emissive, System.IO.Path.Combine(basePath, "WaterBottle_emissive.png"))
                .WithChannelImage(Materials.KnownChannels.Occlusion, System.IO.Path.Combine(basePath, "WaterBottle_occlusion.png"))
                .WithChannelImage(Materials.KnownChannels.Diffuse, System.IO.Path.Combine(basePath, "WaterBottle_diffuse.png"))
                .WithChannelImage(Materials.KnownChannels.SpecularGlossiness, System.IO.Path.Combine(basePath, "WaterBottle_specularGlossiness.png"));

            var mesh = new Geometry.MeshBuilder<VPOS, VTEX>("mesh1");
            mesh.UsePrimitive(material).AddPolygon
                ((new Vector3(-10, 10, 0), new Vector2(1, 0))
                , (new Vector3(10, 10, 0), new Vector2(0, 0))
                , (new Vector3(10, -10, 0), new Vector2(0, 1))
                , (new Vector3(-10, -10, 0), new Vector2(1, 1))
                );

            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");
            var rnode = scene.CreateNode("RootNode").WithMesh(model.CreateMesh(mesh));

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        [TestCase("shannon-dxt5.dds")]
        [TestCase("shannon.webp")]
        public void CreateSceneWithTextureImageExtension(string textureFileName)
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var basePath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "Assets");

            // first, create a default material
            var material = new Materials.MaterialBuilder("material1")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelImage
                (
                    Materials.KnownChannels.BaseColor,
                    System.IO.Path.Combine(basePath, textureFileName)
                );                

            var mesh = new Geometry.MeshBuilder<VPOS, VTEX>("mesh1");

            mesh
                .UsePrimitive(material)
                .AddPolygon
                ((new Vector3(-10, 10, 0), new Vector2(1, 0))
                , (new Vector3(10, 10, 0), new Vector2(0, 0))
                , (new Vector3(10, -10, 0), new Vector2(0, 1))
                , (new Vector3(-10, -10, 0), new Vector2(1, 1))
                );

            var model = ModelRoot.CreateModel();

            model.CreateMeshes(mesh);

            model.UseScene("Default")
                .CreateNode("RootNode")
                .WithMesh(model.LogicalMeshes[0]);

            model.AttachToCurrentTest("result_wf.obj");
            model.AttachToCurrentTest("result_glb.glb");
            model.AttachToCurrentTest("result_gltf.gltf");            
        }        

        [Test]
        public void CrateSceneWithTextureTransformExtension()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var basePath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "Assets");

            // first, create a default material
            var material = new Materials.MaterialBuilder("material1")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelImage(Materials.KnownChannels.BaseColor, System.IO.Path.Combine(basePath, "shannon.jpg"));

            material.GetChannel(Materials.KnownChannels.BaseColor).UseTexture().WithTransform(0.40f,0.25f, 0.5f,0.5f);

            var mesh = new Geometry.MeshBuilder<VPOS, VTEX>("mesh1");

            mesh
                .UsePrimitive(material)
                .AddPolygon
                ((new Vector3(-10, 10, 0), new Vector2(1, 0))
                , (new Vector3(10, 10, 0), new Vector2(0, 0))
                , (new Vector3(10, -10, 0), new Vector2(0, 1))
                , (new Vector3(-10, -10, 0), new Vector2(1, 1))
                );

            var model = ModelRoot.CreateModel();

            model.CreateMeshes(mesh);

            model.UseScene("Default")
                .CreateNode("RootNode")
                .WithMesh(model.LogicalMeshes[0]);
            
            model.AttachToCurrentTest("result_glb.glb");
            model.AttachToCurrentTest("result_gltf.gltf");
        }
    }
}
