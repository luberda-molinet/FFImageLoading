using System;
using System.Text;
using System.Security.Cryptography;

namespace FFImageLoading.Helpers
{
    public class MD5Helper
    {
        private static readonly MD5CryptoServiceProvider checksum = new MD5CryptoServiceProvider();

        public string MD5(string input)
        {
            var bytes = ComputeHash(Encoding.UTF8.GetBytes(input));
            var ret = new char [32];
            for (int i = 0; i < 16; i++){
                ret [i*2] = (char)hex (bytes [i] >> 4);
                ret [i*2+1] = (char)hex (bytes [i] & 0xf);
            }
            return new string (ret);
        }

        private int hex (int v)
        {
            if (v < 10)
                return '0' + v;
            return 'a' + v-10;
        }

        public byte[] ComputeHash(byte[] input)
        {
            return checksum.ComputeHash(input);
        }
    }
}

