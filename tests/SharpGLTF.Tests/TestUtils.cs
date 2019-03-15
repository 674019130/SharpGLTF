﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    static class TestUtils
    {
        public static string ToShortDisplayPath(this string path)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            var fxt = System.IO.Path.GetFileName(path);

            const int maxdir = 12;

            if (dir.Length > maxdir)
            {
                dir = "..." + dir.Substring(dir.Length - maxdir);
            }

            return System.IO.Path.Combine(dir, fxt);
        }        

        public static string GetAttachmentPath(this NUnit.Framework.TestContext context, string fileName, bool ensureDirectoryExists = false)
        {
            var path = System.IO.Path.Combine(context.TestDirectory, "TestResults", $"{context.Test.ID}");
            var dir = path;

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                if (System.IO.Path.IsPathRooted(fileName)) throw new ArgumentException(nameof(fileName), "path must be a relative path");
                path = System.IO.Path.Combine(path, fileName);

                dir = System.IO.Path.GetDirectoryName(path);
            }

            System.IO.Directory.CreateDirectory(dir);

            return path;
        }

        public static void AttachToCurrentTest(this Schema2.ModelRoot model, string fileName)
        {
            // find the output path for the current test
            fileName = NUnit.Framework.TestContext.CurrentContext.GetAttachmentPath(fileName, true);
            
            if (fileName.ToLower().EndsWith(".glb"))
            {
                model.SaveGLB(fileName);
            }
            else if (fileName.ToLower().EndsWith(".gltf"))
            {
                model.SaveGLTF(fileName, Newtonsoft.Json.Formatting.Indented);
            }
            else if (fileName.ToLower().EndsWith(".obj"))
            {
                // evaluate all triangles of the model
                var wavefront = Schema2.ModelDumpUtils.ToWavefrontWriter(model).ToString();
                System.IO.File.WriteAllText(fileName, wavefront);                
            }

            // Attach the saved file to the current test
            NUnit.Framework.TestContext.AddTestAttachment(fileName);
        }

        public static void AttachShowDirLink(this NUnit.Framework.TestContext context)
        {
            context.AttachFileLink("📂 Show Directory", context.GetAttachmentPath(string.Empty));
        }

        public static void AttachGltfValidatorLink(this NUnit.Framework.TestContext context)
        {
            context.AttachUrlLink("🌍 glTF Validator", "http://github.khronos.org/glTF-Validator/");
        }

        public static void AttachFileLink(this NUnit.Framework.TestContext context, string linkPath, string targetPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[InternetShortcut]");
            sb.AppendLine("URL=file:///" + targetPath);
            sb.AppendLine("IconIndex=0");
            string icon = targetPath.Replace('\\', '/');
            sb.AppendLine("IconFile=" + icon);

            linkPath = System.IO.Path.ChangeExtension(linkPath, ".url");
            linkPath = context.GetAttachmentPath(linkPath, true);

            System.IO.File.WriteAllText(linkPath, sb.ToString());

            NUnit.Framework.TestContext.AddTestAttachment(linkPath);
        }

        public static void AttachUrlLink(this NUnit.Framework.TestContext context, string linkPath, string url)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[InternetShortcut]");
            sb.AppendLine("URL=" + url);            

            linkPath = System.IO.Path.ChangeExtension(linkPath, ".url");
            linkPath = context.GetAttachmentPath(linkPath, true);

            System.IO.File.WriteAllText(linkPath, sb.ToString());

            NUnit.Framework.TestContext.AddTestAttachment(linkPath);
        }
    }


    static class DownloadUtils
    {
        private static readonly Object _DownloadMutex = new object();

        public static void SyncronizeGitRepository(string remoteUrl, string localDirectoryPath)
        {
            if (!System.IO.Path.IsPathRooted(localDirectoryPath)) throw new ArgumentException(nameof(localDirectoryPath));

            lock (_DownloadMutex)
            {
                if (LibGit2Sharp.Repository.Discover(localDirectoryPath) == null)
                {
                    NUnit.Framework.TestContext.Progress.WriteLine($"Cloning {remoteUrl} can take several minutes; Please wait...");

                    LibGit2Sharp.Repository.Clone(remoteUrl, localDirectoryPath);

                    NUnit.Framework.TestContext.Progress.WriteLine($"... Clone Completed");

                    return;
                }

                using (var repo = new LibGit2Sharp.Repository(localDirectoryPath))
                {
                    var options = new LibGit2Sharp.PullOptions
                    {
                        FetchOptions = new LibGit2Sharp.FetchOptions()
                    };

                    var r = LibGit2Sharp.Commands.Pull(repo, new LibGit2Sharp.Signature("Anonymous", "anon@anon.com", new DateTimeOffset(DateTime.Now)), options);

                    NUnit.Framework.TestContext.Progress.WriteLine($"{remoteUrl} is {r.Status}");
                }
            }
        }

        public static string DownloadFile(string remoteUri, string localFilePath)
        {
            if (!System.IO.Path.IsPathRooted(localFilePath)) throw new ArgumentException(nameof(localFilePath));

            lock (_DownloadMutex)
            {
                if (System.IO.File.Exists(localFilePath)) return localFilePath; // we check again because we could have downloaded the file while waiting.

                NUnit.Framework.TestContext.Progress.WriteLine($"Downloading {remoteUri}... Please Wait...");

                var dir = System.IO.Path.GetDirectoryName(localFilePath);
                System.IO.Directory.CreateDirectory(dir);

                using (var wc = new System.Net.WebClient())
                {
                    wc.DownloadFile(remoteUri, localFilePath);
                }

                if (localFilePath.ToLower().EndsWith(".zip"))
                {
                    NUnit.Framework.TestContext.Progress.WriteLine($"Extracting {localFilePath}...");

                    var extractPath = System.IO.Path.Combine(dir, System.IO.Path.GetFileNameWithoutExtension(localFilePath));

                    System.IO.Compression.ZipFile.ExtractToDirectory(localFilePath, extractPath);
                }

                return localFilePath;
            }
        }

    }
}
