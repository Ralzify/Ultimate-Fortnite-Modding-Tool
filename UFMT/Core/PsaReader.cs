#pragma warning disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UFMT;
using UFMT.Blender;
using UFMT.Core;
using UFMT.FnAssets;
using UFMT.UI;

namespace UFMT.Core
{
    internal static class PsaReader
    {
        internal static int GetAnimationLength(string psaFilePath)
        {
            using (FileStream stream = File.OpenRead(psaFilePath))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    byte[] chunkHeaderBytes = reader.ReadBytes(20);
                    if (chunkHeaderBytes.Length < 20) break;

                    string chunkName = Encoding.ASCII.GetString(chunkHeaderBytes).TrimEnd('\0');
                    reader.ReadInt32();
                    int dataSize = reader.ReadInt32();
                    int dataNum = reader.ReadInt32();

                    long chunkDataStart = stream.Position;

                    if (chunkName.StartsWith("ANIMINFO"))
                    {
                        // Skip directly to NumRawFrames at offset 164 (64B Name + 64B Group + 16B ints + 20B floats/StartBone/FirstRawFrame)
                        stream.Seek(chunkDataStart + 164, SeekOrigin.Begin);
                        return reader.ReadInt32()-1;
                    }

                    stream.Seek(chunkDataStart + ((long)dataSize * dataNum), SeekOrigin.Begin);
                }
                return 0;
            }
        }
    }
}
