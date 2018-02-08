using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace Nightfire_Source_Updater_Client
{
    class Decompressor
    {
        private static string directoryPath = @"someDir";
        public static void BeginCompressDecompTests()
        {
            DirectoryInfo directorySelected = new DirectoryInfo(directoryPath);
            //Compress(directorySelected);

            foreach (FileInfo fileToDecompress in directorySelected.GetFiles("*.gz"))
            {
                Decompress(fileToDecompress);
            }
        }
        public static void Decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
                    }
                }
            }
        }
    }
}