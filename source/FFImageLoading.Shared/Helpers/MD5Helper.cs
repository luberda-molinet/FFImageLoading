using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace FFImageLoading.Helpers
{
    public class MD5Helper : IMD5Helper
    {
        readonly MD5CryptoServiceProvider _provider = new MD5CryptoServiceProvider();

        public string MD5(Stream stream)
        {
            var bytes = _provider.ComputeHash(stream);
            return BitConverter.ToString(bytes)?.ToSanitizedKey();
        }

        public string MD5(string input)
        {
			var bytes = _provider.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes)?.ToSanitizedKey();
        }
    }
}