using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using System.Runtime.InteropServices.WindowsRuntime;
using System;
using System.IO;

namespace FFImageLoading.Helpers
{
    public class MD5Helper : IMD5Helper
    {
        public string MD5(Stream input)
        {
            var hashProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            var hashed = hashProvider.HashData(StreamToByteArray(input).AsBuffer());
            var bytes = hashed.ToArray();
            return BitConverter.ToString(bytes)?.ToSanitizedKey();
        }

        public string MD5(string input)
        {
            var bytes = ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes)?.ToSanitizedKey();
        }

        public byte[] ComputeHash(byte[] input)
        {
            var hashProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            var hashed = hashProvider.HashData(CryptographicBuffer.CreateFromByteArray(input));
            return hashed.ToArray();
        }

        public static byte[] StreamToByteArray(Stream stream)
        {
            if (stream is MemoryStream)
            {
                return ((MemoryStream)stream).ToArray();
            }
            else
            {
                return ReadFully(stream);
            }
        }

        public static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}

