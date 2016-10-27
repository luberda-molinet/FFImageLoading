using System;
using System.Text;
using System.Security.Cryptography;

namespace FFImageLoading.Helpers
{
    public class MD5Helper : IMD5Helper
    {
        public string MD5(string input)
        {
			var bytes = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(input));
			return BitConverter.ToString(bytes);
        }
    }
}