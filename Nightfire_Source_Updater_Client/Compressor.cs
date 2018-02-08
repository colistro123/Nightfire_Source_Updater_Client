using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace Nightfire_Source_Updater_Client
{
    class Compressor
    {
        public const string DEFAULT_COMPRESSION_TYPE = "gz";
        public const string DIRECTORY_NO_COMPRESSION = "0";

        /* Gets the default compression type for a file or folder as an extension */
        static public string getCompressionForFileOrDirType(string filetype)
        {
            string compressionType = Compressor.DEFAULT_COMPRESSION_TYPE;
            switch (filetype)
            {
                case "file":
                    compressionType = Compressor.DEFAULT_COMPRESSION_TYPE;
                    break;
                case "directory":
                    compressionType = Compressor.DIRECTORY_NO_COMPRESSION;
                    break;
            }
            return compressionType;
        }

        static public string getFilePathAndCompressionAppended(string filePath)
        {
            return $"{filePath}.{Compressor.DEFAULT_COMPRESSION_TYPE}";
        }

        /* toMainTreeDir is the root folder where we want to place all the files e.g: nightfiresource */
        static public void CompressFile(FileInfo fileToCompress, string toMainTreeDir)
        {
            using (FileStream originalFileStream = fileToCompress.OpenRead())
            {
                if ((File.GetAttributes(fileToCompress.FullName) &
                   FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ("." + DEFAULT_COMPRESSION_TYPE))
                {
                    string filePath = Path.GetFullPath($"{Path.Combine(toMainTreeDir, fileToCompress.ToString())}.{DEFAULT_COMPRESSION_TYPE}");
                    string DirName = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(DirName);
                    using (FileStream compressedFileStream = File.Create(filePath))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                           CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);
                        }
                    }
                    FileInfo info = new FileInfo(filePath);
                    Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
                                            fileToCompress.ToString(), fileToCompress.Length.ToString(), info.Length.ToString());
                }

            }
        }
    }
}