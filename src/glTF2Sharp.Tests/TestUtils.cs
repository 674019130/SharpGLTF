﻿using System;
using System.Collections.Generic;
using System.Text;

namespace glTF2Sharp
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

        public static string GetAttachmentPath(this NUnit.Framework.TestContext context, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (System.IO.Path.IsPathRooted(fileName)) throw new ArgumentException(nameof(fileName), "path must be a relative path");

            return System.IO.Path.Combine(context.TestDirectory, $"{context.Test.ID}.{fileName}");
        }

        public static void AttachToCurrentTest(this Schema2.ModelRoot model, string fileName)
        {
            fileName = NUnit.Framework.TestContext.CurrentContext.GetAttachmentPath(fileName);

            if (fileName.ToLower().EndsWith(".gltf")) model.SaveGLTF(fileName, Newtonsoft.Json.Formatting.Indented);
            if (fileName.ToLower().EndsWith(".glb"))
            {
                model.MergeBuffers();
                model.SaveGLB(fileName);
            }

            NUnit.Framework.TestContext.AddTestAttachment(fileName);
        }

        public static void SyncronizeGitRepository(string remoteUrl, string localDirectory)
        {
            if (LibGit2Sharp.Repository.Discover(localDirectory) == null)
            {
                NUnit.Framework.TestContext.Progress.WriteLine($"Cloning {remoteUrl} can take several minutes; Please wait...");

                LibGit2Sharp.Repository.Clone(remoteUrl, localDirectory);

                NUnit.Framework.TestContext.Progress.WriteLine($"... Clone Completed");

                return;
            }

            using (var repo = new LibGit2Sharp.Repository(localDirectory))
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
}
