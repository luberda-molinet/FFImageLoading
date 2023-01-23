using System;
using System.IO;

namespace FFImageLoading.Helpers.Exif
{
    internal class SequentialStreamReader : SequentialReader
    {
        private readonly Stream _stream;

        public override long Position => _stream.Position;

        public SequentialStreamReader(Stream stream, bool isMotorolaByteOrder = true)
            : base(isMotorolaByteOrder)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public override byte GetByte()
        {
            var value = _stream.ReadByte();
            if (value == -1)
                throw new IOException("End of data reached.");

            return unchecked((byte)value);
        }

        public override SequentialReader WithByteOrder(bool isMotorolaByteOrder) => isMotorolaByteOrder == IsMotorolaByteOrder ? this : new SequentialStreamReader(_stream, isMotorolaByteOrder);

        public override byte[] GetBytes(int count)
        {
            var bytes = new byte[count];
            GetBytes(bytes, 0, count);
            return bytes;
        }

        public override void GetBytes(byte[] buffer, int offset, int count)
        {
            var totalBytesRead = 0;
            while (totalBytesRead != count)
            {
                var bytesRead = _stream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                    throw new IOException("End of data reached.");
                totalBytesRead += bytesRead;
            }
        }

        public override void Skip(long n)
        {
            if (n < 0)
                throw new ArgumentException("n must be zero or greater.");

            if (_stream.Position + n > _stream.Length)
                throw new IOException("Unable to skip past of end of file");

            _stream.Seek(n, SeekOrigin.Current);
        }

        public override bool TrySkip(long n)
        {
            try
            {
                Skip(n);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }

}
