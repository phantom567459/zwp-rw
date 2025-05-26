using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;

namespace zwp_rw
{
    //The struct for the ZWP header. This would be referenced once per file run - used to extract or rebuild.
    public struct ZWPHeader
    {
        public UInt32 NORK;
        public UInt32 versionNum;
       // public UInt32 unknown;
        public Int32 fileSize;
        public Int32 fileCount;
        public Int32 dictionaryOffset;

        public ZWPHeader(UInt32 zwpNORK, UInt32 zwpVersionNum, Int32 zwpFileSize, Int32 zwpFileCount, Int32 zwpDictionaryOffset)
        {
            NORK = zwpNORK;
            versionNum = zwpVersionNum;
            fileSize = zwpFileSize;
            fileCount = zwpFileCount;
            dictionaryOffset = zwpDictionaryOffset;
        }
    }

    //The struct for constructing a data dictionary for extraction and also for packing.  Each individual file in a ZWP would have this struct filled.
    public struct TCWFileHeader
    {
        public Int32 count; //I'm adding this in for compatibility - it's quite possible this could be accepted by the main game logic but for now it's always 0.
        public string fileName;
        public Int32 offset;
        public Int32 uncompressedSize;
        public Int32 zSize;

        public TCWFileHeader(string flNm,Int32 tcwOffset, Int32 tcwFileSize, Int32 tcwZSize)
        {
            count = 0;
            fileName = flNm;
            offset = tcwOffset;
            uncompressedSize = tcwFileSize;
            zSize = tcwZSize;
        }
    }

    public class BinaryHelpers
    {
        //old function, no usage currently
       public static void OpenZWPFile(string fileName)
        {
            if ( (!File.Exists(fileName)))
            {
                return;
            }
            else
            {
                File.Open(fileName, FileMode.Open);
            }
        }

        //Gathers the info from the ZWP file to reference later on while creating a file list or straight up extracting.
        public static ZWPHeader GatherZWPHeaderInfo(string fileName)
        {
                using (var stream = File.Open(fileName, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                    {
                        ZWPHeader curFileHeader = new ZWPHeader();
                        curFileHeader.NORK = reader.ReadUInt32();
                        curFileHeader.versionNum = reader.ReadUInt32();
                        reader.ReadUInt32(); //We're not storing this, unknown what it is.
                        curFileHeader.fileSize = reader.ReadInt32();
                        curFileHeader.fileCount = reader.ReadInt32();
                        curFileHeader.dictionaryOffset = reader.ReadInt32();
                        return curFileHeader;
                    }
                }
        }

        //This function is to write a file list out from a specified ZWP file.  The dictionary is contained at the end of the file and the program determines the offset and reads and then writes the strings out.
        public static void WriteFileListFromZWPHeader(string fileName, ZWPHeader header, string outputFile)
        {
            List<string> namesStored = new List<string>(); 
            using (var stream = File.Open(fileName, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    stream.Position = header.dictionaryOffset;
                    for (int i = 0; i < header.fileCount; i++)
                    {
                        reader.ReadUInt32(); //in legacy DRPacker, this seems to be a counter for the file you're on.  Apparently, Pandemic felt it unnecessary lol
                        namesStored.Add(reader.ReadString()); //ok kinda cool that it has a 7-bit encoded length built in...
                        stream.Position = stream.Position + 12;
                    }
                }
            }
            System.IO.File.WriteAllLines(outputFile, namesStored);
        }

        public static void ExtractFilesFromZWP(string fileName, ZWPHeader header, string extractedFolderName)
        {
            List<TCWFileHeader> tcwFileHeaders = new List<TCWFileHeader>();
            using (var stream = File.Open(fileName, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                {
                    stream.Position = header.dictionaryOffset;
                    TCWFileHeader temp = new TCWFileHeader();
                    //Gather all the file header information from the dictionary at the end of the file
                    for (int i = 0; i < header.fileCount; i++)
                    {
                        reader.ReadUInt32();
                        temp.fileName = reader.ReadString();
                        temp.offset = reader.ReadInt32();
                        temp.uncompressedSize = reader.ReadInt32();
                        temp.zSize = reader.ReadInt32(); //honestly I'm thinking this is better as filesize but aluigis bms script notes otherwise?  Kinda weird but I'll use his naming logic in the meantime
                        tcwFileHeaders.Add(temp);
                    }

                    string newDirName = extractedFolderName;
                    System.IO.Directory.CreateDirectory(newDirName); //this is going to be much cleaner in a folder than dumped in the same folder as everything else
                    foreach (TCWFileHeader var in tcwFileHeaders)
                    {
                        stream.Position = var.offset;
                        byte[] buffer = reader.ReadBytes(var.zSize);
                        string extractedFile = newDirName + "\\" + var.fileName;
                        //File.Create(extractedFile);
                        File.WriteAllBytes(extractedFile, buffer);
#if (DEBUG)
                        Console.WriteLine(extractedFile + " written, " + var.zSize.ToString() + " bytes written");
                        Console.WriteLine("Logging non-zSize = " + var.uncompressedSize);
#endif
                    }
                }
            }
        }

        public static void PackFilesIntoZWP(string packList,string extractedFolderName)
        {
            string newDirName = extractedFolderName;
            var lines = File.ReadLines(packList);

            List<TCWFileHeader> list = new List<TCWFileHeader>();

            foreach (var line in lines)
            {
                bool compressed;
                string fn = line.ToString().Trim();
                string fullFN = newDirName + "\\" + fn;

                FileInfo f = new FileInfo(fullFN);

                Int32 fileLength = Convert.ToInt32(f.Length);

                compressed = false;
                if (fileLength > 0)
                {
                    using (var stream = File.Open(fullFN, FileMode.Open))
                    {
                        using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                        {
                            Int16 header = reader.ReadInt16();
                            if (header == -25480)
                            {
                                compressed = true;
                            }
                            else
                            {
                                compressed = false;
                            }
                        }
                    }
                }

                //build dictionary
                TCWFileHeader temp = new TCWFileHeader();
                temp.fileName = fn;
                temp.zSize = fileLength;
                if (compressed)
                {
                    byte[] rawFile = Decompress(fullFN);
#if (DEBUG)
                    Console.WriteLine(fullFN + " Size: " + rawFile.Length);
#endif
                    temp.uncompressedSize = Convert.ToInt32(rawFile.Length);
                }
                else
                {
                    temp.uncompressedSize = fileLength;
                }
                if (!list.Any())
                {
                    temp.offset = 56; //hard coded to include header
                }
                else
                {
                    temp.offset = list.LastOrDefault().offset + list.LastOrDefault().zSize;
                }

                list.Add(temp);
            }

            byte[] zwpDictionary = null;
            bool looped = false;
            foreach (TCWFileHeader file in list)
            {
                byte[] bytes1 = BitConverter.GetBytes(file.count);
                byte[] oldbytes2 = { Convert.ToByte(file.fileName.Length) };
                byte[] byteFileName = Encoding.ASCII.GetBytes(file.fileName);
                byte[] bytes2 = Combine(oldbytes2, byteFileName);
                byte[] cBytes = Combine(bytes1, bytes2);
                byte[] bytes3 = BitConverter.GetBytes(file.offset);
                byte[] cBytes2 = Combine(cBytes, bytes3);
                byte[] bytes4 = BitConverter.GetBytes(file.uncompressedSize);
                byte[] cBytes3 = Combine(cBytes2, bytes4);
                byte[] bytes5 = BitConverter.GetBytes(file.zSize);
                byte[] record = Combine(cBytes3, bytes5);

                if (looped == false)
                {
                    zwpDictionary = record;
                    looped = true;
                }
                else
                {
                    zwpDictionary = Combine(zwpDictionary, record);
                }
            }

            Int32 DictionarySize = zwpDictionary.Length;


            //build ZWP header
            ZWPHeader finalHeader = new ZWPHeader();
            finalHeader.NORK = 1263685454;
            finalHeader.versionNum = 2;
            finalHeader.fileCount = list.Count;
            finalHeader.dictionaryOffset = list.LastOrDefault().offset + list.LastOrDefault().zSize;
            finalHeader.fileSize = finalHeader.dictionaryOffset + zwpDictionary.Length;

            byte[] zwpHeaderAsBytes = new byte[56];
            byte[] NORKHEADER = BitConverter.GetBytes(finalHeader.NORK);
            byte[] VERSION = BitConverter.GetBytes(finalHeader.versionNum);
            byte[] FILESIZE = BitConverter.GetBytes(finalHeader.fileSize);
            byte[] FILECOUNT = BitConverter.GetBytes(finalHeader.fileCount);
            byte[] DICTOFFSET = BitConverter.GetBytes(finalHeader.dictionaryOffset);

            Buffer.BlockCopy(NORKHEADER, 0, zwpHeaderAsBytes, 0, 4);
            Buffer.BlockCopy(VERSION, 0, zwpHeaderAsBytes, 4, 4);
            Buffer.BlockCopy(FILESIZE, 0, zwpHeaderAsBytes, 12, 4);
            Buffer.BlockCopy(FILECOUNT, 0, zwpHeaderAsBytes, 16, 4);
            Buffer.BlockCopy(DICTOFFSET, 0, zwpHeaderAsBytes, 20, 4);

            /*zwpHeaderAsBytes.SetValue(finalHeader.NORK, 0);
            zwpHeaderAsBytes.SetValue(finalHeader.versionNum, 4);
            zwpHeaderAsBytes.SetValue(Convert.ToInt32(0), 8);
            zwpHeaderAsBytes.SetValue(finalHeader.fileSize, 12);
            zwpHeaderAsBytes.SetValue(finalHeader.fileCount, 16);
            zwpHeaderAsBytes.SetValue(finalHeader.dictionaryOffset, 20);*/

            using (var stream = File.Open(packList + ".zwp", FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    writer.Write(zwpHeaderAsBytes);

                    foreach (var entry in list)
                    {
                        byte[] buffer = File.ReadAllBytes(newDirName + "\\" + entry.fileName);
                        writer.Write(buffer);
                    }

                    writer.Write(zwpDictionary);
                }
            }

            Console.WriteLine("File Written.  Final Size: " + finalHeader.fileSize);
            Console.WriteLine("Files compiled: " + finalHeader.fileCount);
        }

        //helper function for combining byte arrays https://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public static byte[] Decompress(string CompressedFileName)
        {
            // function takes a filename and returns the decompressed contents
            using (FileStream compressedFileStream = File.Open(CompressedFileName, FileMode.Open))
            {
                using (var ds = new ZLibStream(compressedFileStream, CompressionMode.Decompress))
                {
                    using (var ms = new MemoryStream())
                    {
                        ds.CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }
        }

        public static byte[] Compress(string uncompressedFileName)
        {
            // function takes a filename and returns the compressed contents
            using (FileStream uncompressedFileStream = File.OpenRead(uncompressedFileName))
            using (var memoryStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(memoryStream, CompressionMode.Compress, leaveOpen: true))
                {
                    uncompressedFileStream.CopyTo(zlibStream);
                }
                return memoryStream.ToArray();
            }
        }

        public static byte[] CompressZlibCompatible(string inputFile)
        {
            byte[] inputData = File.ReadAllBytes(inputFile);

            using MemoryStream memoryStream = new();
            using (ZLibStream deflateStream = new(memoryStream, CompressionLevel.Optimal, true))
                deflateStream.Write(inputData, 0, inputData.Length);
            byte[] compressedData = memoryStream.ToArray();

            return compressedData;
        }
    }
}
