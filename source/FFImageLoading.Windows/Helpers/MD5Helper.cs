using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using System.Runtime.InteropServices.WindowsRuntime;

namespace FFImageLoading.Helpers
{
    public class MD5Helper
    {
        private static HashAlgorithmProvider hashProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);

        public string MD5(string input)
        {
            var bytes = ComputeHash(Encoding.UTF8.GetBytes(input));
            var ret = new char[32];
            for (int i = 0; i < 16; i++)
            {
                ret[i * 2] = (char)hex(bytes[i] >> 4);
                ret[i * 2 + 1] = (char)hex(bytes[i] & 0xf);
            }
            return new string(ret);
        }

        private int hex(int v)
        {
            if (v < 10)
                return '0' + v;
            return 'a' + v - 10;
        }

        public byte[] ComputeHash(byte[] input)
        {
            var hashed = hashProvider.HashData(CryptographicBuffer.CreateFromByteArray(input));
            return hashed.ToArray();
        }
    }
}

