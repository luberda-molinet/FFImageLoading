using System;
using System.Text;

namespace FFImageLoading.Helpers.Exif
{
    internal struct StringValue
    {
        public static readonly Encoding DefaultEncoding = Encoding.UTF8;

        public StringValue(byte[] bytes, Encoding encoding = null)
        {
            Bytes = bytes;
            Encoding = encoding;
        }

        public byte[] Bytes { get; }

        public Encoding Encoding { get; }

        public override string ToString() => ToString(Encoding ?? DefaultEncoding);

        public string ToString(Encoding encoder) => encoder.GetString(Bytes, 0, Bytes.Length);
    }
}
