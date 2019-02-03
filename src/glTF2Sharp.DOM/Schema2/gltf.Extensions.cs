﻿using System;
using System.Collections.Generic;
using System.Text;

namespace glTF2Sharp.Schema2
{
    static class ExtensionsFactory
    {
        static ExtensionsFactory()
        {
            RegisterExtension("KHR_materials_pbrSpecularGlossiness", ()=> new MaterialPBRSpecularGlossiness_KHR());
            RegisterExtension("KHR_materials_unlit", () => new MaterialUnlit_KHR());
            // RegisterExtension("MSFT_lod", () => new MSFT_lodglTFextension());
            // RegisterExtension("KHR_draco_mesh_compression", () => new KHR_draco_mesh_compressionextension());
        }

        private static readonly Dictionary<string, Func<JsonSerializable>> _Extensions = new Dictionary<string, Func<JsonSerializable>>();

        public static IEnumerable<string> SupportedExtensions => _Extensions.Keys;

        public static void RegisterExtension(string persistentName, Func<JsonSerializable> activator)
        {
            _Extensions[persistentName] = activator;
        }        

        public static JsonSerializable Create(string key)
        {
            if (!_Extensions.TryGetValue(key, out Func<JsonSerializable> activator)) return null;

            return activator.Invoke();
        }
    }
}
