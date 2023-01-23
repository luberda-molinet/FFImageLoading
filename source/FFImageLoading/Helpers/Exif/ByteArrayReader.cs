using System;
namespace FFImageLoading.Helpers.Exif
{
    internal class ByteArrayReader : IndexedReader
    {
        private readonly byte[] _buffer;
        private readonly int _baseOffset;

        public ByteArrayReader(byte[] buffer, int baseOffset = 0, bool isMotorolaByteOrder = true)
            : base(isMotorolaByteOrder)
        {
            if (baseOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(baseOffset), "Must be zero or greater.");

            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _baseOffset = baseOffset;
        }

        public override IndexedReader WithByteOrder(bool isMotorolaByteOrder) => isMotorolaByteOrder == IsMotorolaByteOrder ? this : new ByteArrayReader(_buffer, _baseOffset, isMotorolaByteOrder);

        public override IndexedReader WithShiftedBaseOffset(int shift) => shift == 0 ? this : new ByteArrayReader(_buffer, _baseOffset + shift, IsMotorolaByteOrder);

        public override int ToUnshiftedOffset(int localOffset) => localOffset + _baseOffset;

        public override long Length => _buffer.Length - _baseOffset;

        public override byte GetByte(int index)
        {
            ValidateIndex(index, 1);
            return _buffer[index + _baseOffset];
        }

        protected override void ValidateIndex(int index, int bytesRequested)
        {
            if (!IsValidIndex(index, bytesRequested))
                throw new Exception("Invalid index");
        }

        protected override bool IsValidIndex(int index, int bytesRequested)
        {
            return
                bytesRequested >= 0 &&
                index >= 0 &&
                index + (long)bytesRequested - 1L < Length;
        }

        public override byte[] GetBytes(int index, int count)
        {
            ValidateIndex(index, count);

            var bytes = new byte[count];
            Array.Copy(_buffer, index + _baseOffset, bytes, 0, count);
            return bytes;
        }
    }
}
