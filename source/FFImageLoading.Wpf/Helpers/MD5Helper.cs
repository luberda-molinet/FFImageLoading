using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FFImageLoading.Helpers
{
    public class MD5Helper : IMD5Helper
    {
        public string MD5(Stream input)
        {
            using (var hashProvider = new MD5CryptoServiceProvider())
            {
                var bytes = hashProvider.ComputeHash(input);
                return BitConverter.ToString(bytes)?.ToSanitizedKey();
            }
        }

        public string MD5(string input)
        {
            using (var hashProvider = new MD5CryptoServiceProvider())
            {
                var bytes = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes)?.ToSanitizedKey();
            }

        }
        //public static byte[] StreamToByteArray(Stream stream)
        //{
        //    if (stream is MemoryStream)
        //    {
        //        return ((MemoryStream)stream).ToArray();
        //    }
        //    else
        //    {
        //        return ReadFully(stream);
        //    }
        //}

        //public static byte[] ReadFully(Stream input)
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        input.CopyTo(ms);
        //        return ms.ToArray();
        //    }
        //}
    }
}

