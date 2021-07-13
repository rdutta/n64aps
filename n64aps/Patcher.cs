/*
 * MIT License
 *
 * Copyright (c) 2021 rdutta (github.com/rdutta)
 * Copyright (c) 2017 Marc Robledo
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
*/

using System;
using System.IO;

namespace n64aps
{
    internal class Patcher
    {
        private static readonly byte[] s_header = new byte[] { 0x41, 0x50, 0x53, 0x31, 0x30, 0x01, 0x00 };
        private static readonly byte[] s_description = new byte[50];
        private static readonly byte[] s_padding = new byte[5];

        private static readonly int s_crcByteSize = 8;
        private static readonly long s_crcRomOffset = 0x10;
        private static readonly long s_crcPatchOffset = 0x3D;

        private static readonly int s_cartIdByteSize = 3;
        private static readonly long s_cartIdRomOffset = 0x3C;
        private static readonly long s_cartIdPatchOffset = 0x3A;

        private static readonly long s_patchDataOffset = 0x4E;

        private static readonly byte s_imageFileFormat = 0x01;
        private static readonly byte s_zero_byte = 0x00;

        private static readonly int s_rleValue = 0;

        internal static readonly string s_extZ64 = ".z64";
        internal static readonly string s_extAps = ".aps";

        private FileInfo? Rom { get; init; }
        private FileInfo? Patch { get; init; }
        private DirectoryInfo? OutDir { get; init; }

        static Patcher()
        {
            for (int i = 0; i < s_description.Length; i++)
            {
                s_description[i] = 0x20;
            }
        }

        internal static void Create(FileInfo rom, FileInfo patch, DirectoryInfo outDir)
        {
            new Patcher
            {
                Rom = rom,
                Patch = patch,
                OutDir = outDir,
            }.Create();
        }

        internal static void Apply(FileInfo rom, FileInfo patch, DirectoryInfo outDir)
        {
            new Patcher
            {
                Rom = rom,
                Patch = patch,
                OutDir = outDir,
            }.Apply();
        }

        internal static void Rename(FileInfo patch, DirectoryInfo outDir)
        {
            new Patcher
            {
                Patch = patch,
                OutDir = outDir,
            }.Rename();
        }

        private void Create()
        {
            string outPath = Path.Combine(this.OutDir!.FullName, this.Rom!.Name.Replace(s_extZ64, s_extAps));

            using Stream patchStream = this.Patch!.OpenRead();

            using Stream romStream = this.Rom.OpenRead();

            using BinaryWriter patchWriter = new(File.OpenWrite(outPath));

            this.WriteHeader(patchWriter, romStream);

            this.WriteBody(patchWriter, romStream, patchStream);
        }

        private void WriteHeader(BinaryWriter apsWriter, Stream romStream)
        {
            Span<byte> crc = stackalloc byte[s_crcByteSize];
            romStream.Position = s_crcRomOffset;
            romStream.Read(crc);

            Span<byte> cartId = stackalloc byte[s_cartIdByteSize];
            romStream.Position = s_cartIdRomOffset;
            romStream.Read(cartId);

            apsWriter.Write(s_header);
            apsWriter.Write(s_description);
            apsWriter.Write(s_imageFileFormat);
            apsWriter.Write(cartId);
            apsWriter.Write(crc);
            apsWriter.Write(s_padding);
            apsWriter.Write((uint)this.Patch!.Length);
        }

        private void WriteBody(BinaryWriter apsWriter, Stream romStream, Stream patchStream)
        {
            romStream.Position = 0;
            patchStream.Position = 0;

            long offset;
            bool isRLE;
            byte b1;
            byte b2;
            byte rleByte;

            MemoryStream memoryStream = new(new byte[byte.MaxValue], 0, byte.MaxValue, true, true);

            while ((offset = patchStream.Position) < this.Patch!.Length)
            {
                b1 = romStream.Position < this.Rom!.Length ? (byte)romStream.ReadByte() : s_zero_byte;
                b2 = (byte)patchStream.ReadByte();

                if (b1 == b2)
                {
                    continue;
                }

                isRLE = true;
                rleByte = b2;

                do
                {
                    memoryStream.WriteByte(b2);

                    if (b2 != rleByte)
                    {
                        isRLE = false;
                    }

                    if ((patchStream.Position >= this.Patch.Length) || memoryStream.Position == byte.MaxValue)
                    {
                        break;
                    }

                    b1 = romStream.Position < this.Rom.Length ? (byte)romStream.ReadByte() : s_zero_byte;
                    b2 = (byte)patchStream.ReadByte();
                } while (b1 != b2);

                if (isRLE && memoryStream.Position > 2)
                {
                    apsWriter.Write((uint)offset);
                    apsWriter.Write(s_zero_byte);
                    apsWriter.Write(rleByte);
                    apsWriter.Write((byte)memoryStream.Position);
                }
                else
                {
                    apsWriter.Write((uint)offset);
                    apsWriter.Write((byte)memoryStream.Position);
                    apsWriter.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
                }

                memoryStream.Position = 0;
            }
        }

        private void Apply()
        {
            using BinaryReader patchReader = new(this.Patch!.OpenRead());
            Stream patchStream = patchReader.BaseStream;

            this.ValidatePatch(this.Rom!.OpenRead(), patchStream);

            string outPath = Path.Combine(this.OutDir!.FullName, this.Rom.Name);
            File.Copy(this.Rom.FullName, outPath);

            using Stream romStream = File.Open(outPath, FileMode.Open);

            patchStream.Position = s_patchDataOffset;

            int length;

            byte data;
            byte dataLength;

            while (patchStream.Position < this.Patch.Length)
            {
                romStream.Position = patchReader.ReadUInt32();
                length = patchReader.ReadByte();

                if (length == s_rleValue)
                {
                    data = patchReader.ReadByte();
                    dataLength = patchReader.ReadByte();

                    for (int i = 0; i < dataLength; i++)
                    {
                        romStream.WriteByte(data);
                    }
                }
                else
                {
                    romStream.Write(patchReader.ReadBytes(length));
                }
            }
        }

        private void ValidatePatch(Stream romStream, Stream patchStream)
        {
            Span<byte> crcRom = stackalloc byte[s_crcByteSize];
            romStream.Position = s_crcRomOffset;
            romStream.Read(crcRom);

            Span<byte> crcPatch = stackalloc byte[s_crcByteSize];
            patchStream.Position = s_crcPatchOffset;
            patchStream.Read(crcPatch);

            for (int i = 0; i < s_crcByteSize; i++)
            {
                if (crcRom[i] != crcPatch[i])
                {
                    string crcRomHex = Utility.BytesToHexString(crcRom);
                    string crcPatchHex = Utility.BytesToHexString(crcPatch);

                    throw new ArgumentException($"Patch validation failed: {this.Rom!.Name} CRC mismatch [{crcRomHex} != {crcPatchHex}]");
                }
            }
        }

        private void Rename()
        {
            using Stream patchStream = this.Patch!.OpenRead();

            patchStream.Position = s_crcPatchOffset;

            Span<byte> crcHi = stackalloc byte[s_crcByteSize / 2];

            patchStream.Read(crcHi);

            File.Copy(this.Patch.FullName, Path.Combine(this.OutDir!.FullName, $"{Utility.BytesToHexString(crcHi)}.aps"));
        }
    }
}
