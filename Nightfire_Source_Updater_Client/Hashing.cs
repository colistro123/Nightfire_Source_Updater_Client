using System;
using System.Security.Cryptography;
using System.IO;

namespace Nightfire_Source_Updater_Client
{
    class Hashing
    {
        public byte[] getfileHash(string filePath)
        {
            using (var sha1 = SHA1.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return sha1.ComputeHash(stream);
                }
            }
        }

        public string genFileHash(string filePath)
        {
            return String.Join(String.Empty, getfileHash(filePath));
        }
    }
}