﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace glTF2Sharp.Schema2
{
    using ROOT = ModelRoot;

    static class glb
    {
        #region constants

        public const uint GLTFHEADER = 0x46546C67;
        public const uint GLTFVERSION2 = 2;
        public const uint CHUNKJSON = 0x4E4F534A;
        public const uint CHUNKBIN = 0x004E4942;

        #endregion

        #region read

        internal static bool _Identify(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanSeek, nameof(stream), "A seekable stream is required for glTF/GLB format identification");

            var currPos = stream.Position;

            uint magic = 0;
            magic |= (uint)stream.ReadByte();
            magic |= (uint)stream.ReadByte() << 8;
            magic |= (uint)stream.ReadByte() << 16;
            magic |= (uint)stream.ReadByte() << 24;

            stream.Position = currPos; // restart read position

            return magic == GLTFHEADER;
        }

        public static IReadOnlyDictionary<UInt32, Byte[]> ReadBinaryFile(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            // WARNING: BinaryReader requires Encoding.ASCII because
            // the binaryReader.PeekChar() must read single bytes
            // in some cases, trying to read the end of the file will throw
            // an exception if encoding is UTF8 and there's just 1 byte left to read.

            using (var binaryReader = new BinaryReader(stream, Encoding.ASCII))
            {
                _ReadBinaryHeader(binaryReader);

                var chunks = new Dictionary<uint, Byte[]>();

                // keep reading until EndOfFile exception
                while (true)
                {
                    if (binaryReader.PeekChar() < 0) break;

                    uint chunkLength = binaryReader.ReadUInt32();

                    if ((chunkLength & 3) != 0)
                    {
                        throw new InvalidDataException($"The chunk must be padded to 4 bytes: {chunkLength}");
                    }

                    uint chunkId = binaryReader.ReadUInt32();

                    var data = binaryReader.ReadBytes((int)chunkLength);

                    chunks[chunkId] = data;
                }

                return chunks;
            }
        }

        private static void _ReadBinaryHeader(BinaryReader binaryReader)
        {
            Guard.NotNull(binaryReader, nameof(binaryReader));

            uint magic = binaryReader.ReadUInt32();

            Guard.IsTrue(magic == GLTFHEADER, nameof(magic), $"Unexpected magic number: {magic}");

            uint version = binaryReader.ReadUInt32();

            Guard.IsTrue(version == GLTFVERSION2, nameof(version), $"Unknown version number: {version}");

            uint length = binaryReader.ReadUInt32();
            long fileLength = binaryReader.BaseStream.Length;

            Guard.IsTrue(length == fileLength, nameof(length), $"The specified length of the file ({length}) is not equal to the actual length of the file ({fileLength}).");
        }

        #endregion

        #region write

        /// <summary>
        /// Tells if a given model can be stored as Binary format.
        /// </summary>
        /// <param name="model">the model to test</param>
        /// <returns>null if it can be stored as binary, or an exception object if it can't</returns>
        /// <remarks>
        /// Due to the limitations of Binary Format, not all models can be saved as Binary.
        /// </remarks>
        public static Exception IsBinaryCompatible(ROOT model)
        {
            try
            {
                Guard.NotNull(model, nameof(model));
                Guard.IsTrue(model.LogicalBuffers.Count <= 1, nameof(model), $"{nameof(model)} GLB only supports one binary buffer, {model.LogicalBuffers.Count} found.");
            }
            catch (Exception ex)
            {
                return ex;
            }

            // todo: buffer[0].Uri must be null

            return null;
        }

        /// <summary>
        /// Writes a <code>Schema.Gltf</code> model to a writable binary writer
        /// </summary>
        /// <param name="model"><code>Schema.Gltf</code> model</param>
        /// <param name="buffer">Binary buffer to embed in the file, or null</param>
        /// <param name="binaryWriter">Binary Writer</param>
        public static void WriteBinaryModel(this BinaryWriter binaryWriter, ROOT model)
        {
            var ex = IsBinaryCompatible(model); if (ex != null) throw ex;

            var jsonText = model.GetJSON(Newtonsoft.Json.Formatting.None);
            var jsonChunk = Encoding.UTF8.GetBytes(jsonText);
            var jsonPadding = jsonChunk.Length & 3; if (jsonPadding != 0) jsonPadding = 4 - jsonPadding;

            var buffer = model.LogicalBuffers.Count > 0 ? model.LogicalBuffers[0]._Data : null;
            if (buffer != null && buffer.Length == 0) buffer = null;

            var binPadding = buffer == null ? 0 : buffer.Length & 3; if (binPadding != 0) binPadding = 4 - binPadding;

            int fullLength = 4 + 4 + 4;

            fullLength += 8 + jsonChunk.Length + jsonPadding;
            if (buffer != null) fullLength += 8 + buffer.Length + binPadding;

            binaryWriter.Write(GLTFHEADER);
            binaryWriter.Write(GLTFVERSION2);
            binaryWriter.Write(fullLength);

            binaryWriter.Write(jsonChunk.Length + jsonPadding);
            binaryWriter.Write(CHUNKJSON);
            binaryWriter.Write(jsonChunk);
            for (int i = 0; i < jsonPadding; ++i) binaryWriter.Write((Byte)0x20);

            if (buffer != null)
            {
                binaryWriter.Write(buffer.Length + binPadding);
                binaryWriter.Write(CHUNKBIN);
                binaryWriter.Write(buffer);
                for (int i = 0; i < binPadding; ++i) binaryWriter.Write((Byte)0);
            }
        }

        #endregion
    }
}
