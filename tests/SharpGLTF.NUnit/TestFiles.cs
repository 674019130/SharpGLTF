﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    /// <summary>
    /// Encapsulates the access to test files.
    /// </summary>
    public static class TestFiles
    {
        #region lifecycle

        static TestFiles()
        {
            var wdir = TestContext.CurrentContext.WorkDirectory;

            _ExamplesFound = false;

            while (wdir.Length > 3)
            {
                _RootDir = System.IO.Path.Combine(wdir, "TestFiles");

                if (wdir.ToLower().EndsWith("tests") && System.IO.Directory.Exists(_RootDir))
                {
                    _ExamplesFound = true;
                    break;
                }

                wdir = System.IO.Path.GetDirectoryName(wdir);
            }

            _Check();

            _SchemaDir = System.IO.Path.Combine(_RootDir, "glTF-Schema");
            _ValidationDir = System.IO.Path.Combine(_RootDir, "glTF-Validator");
            _SampleModelsDir = System.IO.Path.Combine(_RootDir, "glTF-Sample-Models");

            _PollyModelsDir = System.IO.Path.Combine(_RootDir, "glTF-Blender-Exporter");
            _UniVRMModelsDir = System.IO.Path.Combine(_RootDir, "UniVRM");

            _BabylonJsMeshesDir = System.IO.Path.Combine(_RootDir, "BabylonJS-MeshesLibrary");
            _BabylonJsPlaygroundDir = System.IO.Path.Combine(_RootDir, "BabylonJS-PlaygroundScenes");

            _GeneratedModelsDir = System.IO.Path.Combine(_RootDir, "GeneratedReferenceModels", "v_0_6_1");
        }       

        #endregion

        #region data

        private static Boolean _ExamplesFound = false;

        private static readonly string _RootDir;

        private static readonly string _SchemaDir;
        private static readonly string _ValidationDir;
        private static readonly string _SampleModelsDir;

        private static readonly string _PollyModelsDir;
        private static readonly string _UniVRMModelsDir;
        private static readonly string _BabylonJsMeshesDir;
        private static readonly string _BabylonJsPlaygroundDir;
        private static readonly string _GeneratedModelsDir;

        private static readonly string[] _BabylonJsInvalidFiles = { };        

        #endregion

        #region properties

        public static string RootDirectory { get { _Check(); return _RootDir; } }

        #endregion

        #region API

        private static void _Check()
        {
            Assert.IsTrue(_ExamplesFound, "TestFiles directory not found; please, run '1_DownloadTestFiles.cmd' before running the tests.");            
        }

        public static IReadOnlyList<string> GetSchemaExtensionsModelsPaths()
        {
            _Check();

            return GetModelPathsInDirectory(_SchemaDir, "extensions", "2.0");         
        }

        public static IEnumerable<(string Path, bool ShouldLoad)> GetReferenceModelPaths(bool useNegative = false)
        {
            _Check();

            var dirPath = _GeneratedModelsDir;
            if (dirPath.EndsWith(".zip")) dirPath = dirPath.Substring(0, dirPath.Length - 4);

            var manifestsPath = System.IO.Path.Combine(dirPath, useNegative? "Negative" : "Positive");

            var manifests = System.IO.Directory.GetFiles(manifestsPath, "Manifest.json", System.IO.SearchOption.AllDirectories)
                .Skip(1)
                .ToArray();

            foreach (var m in manifests)
            {
                var d = System.IO.Path.GetDirectoryName(m);

                var content = System.IO.File.ReadAllText(m);
                var doc = Newtonsoft.Json.Linq.JObject.Parse(content);

                var models = doc.SelectToken("models");
                
                foreach(var model in models)
                {
                    var mdlPath = (String)model.SelectToken("fileName");

                    var loadable = !useNegative;

                    if (loadable) loadable = (Boolean)model.SelectToken("loadable");

                    mdlPath = System.IO.Path.Combine(d, mdlPath);

                    yield return (mdlPath, loadable);
                }
            }

            yield break;
        }

        public static IReadOnlyList<string> GetSampleModelsPaths()
        {
            _Check();

            var files = GetModelPathsInDirectory(_SampleModelsDir, "2.0");

            return files
                .OrderBy(item => item)
                .Where(item => !item.Contains("\\glTF-Draco\\"))
                .ToList();
        }

        public static IReadOnlyList<string> GetKhronosValidationPaths()
        {
            _Check();

            var skip = new string[] { "misplaced_bin_chunk.glb", "valid_placeholder.glb" };

            var files = GetModelPathsInDirectory(_ValidationDir, "test")
                .Where(item => skip.All(f=>!item.EndsWith(f)));
            
            return files
                .OrderBy(item => item)                
                .ToList();
        }

        public static IReadOnlyList<string> GetBabylonJSValidModelsPaths()
        {
            _Check();

            var files = GetModelPathsInDirectory(_BabylonJsMeshesDir);

            return files
                .OrderBy(item => item)
                .Where(item => !item.Contains("\\AssetGenerator\\0.6\\"))
                .Where(item => !item.Contains("\\Sheen\\"))
                .Where(item => !_BabylonJsInvalidFiles.Any(ff => item.EndsWith(ff)))                
                .ToList();
        }

        public static IReadOnlyList<string> GetBabylonJSInvalidModelsPaths()
        {
            _Check();

            var files = GetModelPathsInDirectory(_BabylonJsMeshesDir);

            return files
                .OrderBy(item => item)
                .Where(item => !item.Contains("\\AssetGenerator\\0.6\\"))
                .Where(item => _BabylonJsInvalidFiles.Any(ff => item.EndsWith(ff)))
                .ToList();
        }

        public static string GetPollyFileModelPath()
        {
            _Check();

            return System.IO.Path.Combine(_PollyModelsDir, "polly", "project_polly.glb");
        }

        public static string GetUniVRMModelPath()
        {
            _Check();

            return System.IO.Path.Combine(_UniVRMModelsDir, "AliciaSolid_vrm-0.40.vrm");
        }

        private static IReadOnlyList<string> GetModelPathsInDirectory(params string[] paths)
        {
            _Check();

            var dirPath = System.IO.Path.Combine(paths);

            if (dirPath.EndsWith(".zip")) dirPath = dirPath.Substring(0, dirPath.Length-4);

            // if (!System.IO.Path.IsPathFullyQualified(dirPath)) throw new ArgumentException(nameof(dirPath));
            if (!System.IO.Path.IsPathRooted(dirPath)) throw new ArgumentException(nameof(dirPath));

            var gltf = System.IO.Directory.GetFiles(dirPath, "*.gltf", System.IO.SearchOption.AllDirectories);
            var glbb = System.IO.Directory.GetFiles(dirPath, "*.glb", System.IO.SearchOption.AllDirectories);

            return gltf.Concat(glbb).ToList();
        }        

        #endregion
    }
}
