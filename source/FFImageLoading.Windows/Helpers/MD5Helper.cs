using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using System.Runtime.InteropServices.WindowsRuntime;
using System;

namespace FFImageLoading.Helpers
{
    public class MD5Helper
    {
        private static HashAlgorithmProvider hashProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);

        public string MD5(string input)
        {
            var bytes = ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes);
        }

        public byte[] ComputeHash(byte[] input)
        {
            var hashed = hashProvider.HashData(CryptographicBuffer.CreateFromByteArray(input));
            return hashed.ToArray();
        }
    }
}

