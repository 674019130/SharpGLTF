﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGLTF.Schema2
{
    using IO;

    static class ExtensionsFactory
    {
        // extensions design inconsistencies:
        // https://github.com/KhronosGroup/glTF/issues/1491

        #region supported extensions must be registered here

        static ExtensionsFactory()
        {
            RegisterExtension<Material, MaterialPBRSpecularGlossiness>("KHR_materials_pbrSpecularGlossiness");

            RegisterExtension<Material, MaterialUnlit>("KHR_materials_unlit");

            RegisterExtension<ModelRoot, KHR_lights_punctualglTFextension>("KHR_lights_punctual");
            RegisterExtension<Node, KHR_lights_punctualnodeextension>("KHR_lights_punctual");

            RegisterExtension<TextureInfo, TextureTransform>("KHR_texture_transform");
        }

        #endregion

        #region data

        private static readonly List<(string, Type, Type)> _Extensions = new List<(string, Type, Type)>();

        #endregion

        #region API

        public static IEnumerable<string> SupportedExtensions => _Extensions.Select(item => item.Item1);

        public static void RegisterExtension<TParent, TExtension>(string persistentName)
            where TParent : JsonSerializable
            where TExtension : JsonSerializable
        {
            _Extensions.Add( (persistentName, typeof(TParent), typeof(TExtension)) );
        }

        internal static JsonSerializable Create(JsonSerializable parent, string key)
        {
            var ptype = parent.GetType();

            var entry = _Extensions.FirstOrDefault(item => item.Item1 == key && item.Item2 == ptype);

            if (entry.Item1 == null) return null;

            var instance = Activator.CreateInstance
                (
                entry.Item3,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new Object[] { parent },
                null
                );

            return instance as JsonSerializable;
        }

        internal static string Identify(Type parentType, Type extensionType)
        {
            foreach (var entry in _Extensions)
            {
                if (entry.Item2 == parentType && entry.Item3 == extensionType) return entry.Item1;
            }

            return null;
        }

        #endregion
    }

    partial class ModelRoot
    {
        private IEnumerable<glTFProperty> _GetAllExtensionContainers()
        {
            IEnumerable<glTFProperty> containers = new glTFProperty[] { this, this.Asset };

            containers = containers.Concat(this.LogicalAccessors);
            containers = containers.Concat(this.LogicalAnimations);
            containers = containers.Concat(this.LogicalBuffers);
            containers = containers.Concat(this.LogicalBufferViews);
            containers = containers.Concat(this.LogicalCameras);
            containers = containers.Concat(this.LogicalImages);
            containers = containers.Concat(this.LogicalMaterials);
            containers = containers.Concat(this.LogicalMeshes);
            containers = containers.Concat(this.LogicalNodes);
            containers = containers.Concat(this.LogicalPunctualLights);
            containers = containers.Concat(this.LogicalSamplers);
            containers = containers.Concat(this.LogicalScenes);
            containers = containers.Concat(this.LogicalSkins);
            containers = containers.Concat(this.LogicalTextures);
            return containers;
        }

        internal void UpdateExtensionsSupport()
        {
            var usedExtensions = new HashSet<string>();

            foreach (var c in _GetAllExtensionContainers())
            {
                var ids = c.Extensions.Select(item => ExtensionsFactory.Identify(c.GetType(), item.GetType()));

                foreach (var id in ids) usedExtensions.Add(id);
            }
        }

        internal void UsingExtension(Type parentType, Type extensionType)
        {
            var id = ExtensionsFactory.Identify(parentType, extensionType);
            if (string.IsNullOrWhiteSpace(id)) return;

            if (this._extensionsUsed.Contains(id)) return;

            this._extensionsUsed.Add(id);
        }
    }
}
