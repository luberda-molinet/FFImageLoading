using System;
using System.Text;

namespace FFImageLoading.Helpers.Exif
{
    internal abstract class IndexedReader
    {
        protected IndexedReader(bool isMotorolaByteOrder)
        {
            IsMotorolaByteOrder = isMotorolaByteOrder;
        }

        public bool IsMotorolaByteOrder { get; }

        public abstract IndexedReader WithByteOrder(bool isMotorolaByteOrder);

        public abstract IndexedReader WithShiftedBaseOffset(int shift);

        public abstract int ToUnshiftedOffset(int localOffset);

        public abstract byte GetByte(int index);

        public abstract byte[] GetBytes(int index, int count);

        protected abstract void ValidateIndex(int index, int bytesRequested);

        protected abstract bool IsValidIndex(int index, int bytesRequested);

        public abstract long Length { get; }

        public bool GetBit(int index)
        {
            var byteIndex = index / 8;
            var bitIndex = index % 8;
            ValidateIndex(byteIndex, 1);
            var b = GetByte(byteIndex);
            return ((b >> bitIndex) & 1) == 1;
        }

        public sbyte GetSByte(int index)
        {
            ValidateIndex(index, 1);
            return unchecked((sbyte)GetByte(index));
        }

        public ushort GetUInt16(int index)
        {
            ValidateIndex(index, 2);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return (ushort)
                    (GetByte(index    ) << 8 |
                     GetByte(index + 1));
            }
            // Intel ordering - LSB first
            return (ushort)
                (GetByte(index + 1) << 8 |
                 GetByte(index    ));
        }

        public short GetInt16(int index)
        {
            ValidateIndex(index, 2);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return (short)
                    (GetByte(index    ) << 8 |
                     GetByte(index + 1));
            }
            // Intel ordering - LSB first
            return (short)
                (GetByte(index + 1) << 8 |
                 GetByte(index));
        }

        public int GetInt24(int index)
        {
            ValidateIndex(index, 3);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first (big endian)
                return
                    GetByte(index    ) << 16 |
                    GetByte(index + 1)  << 8 |
                    GetByte(index + 2);
            }
            // Intel ordering - LSB first (little endian)
            return
                GetByte(index + 2) << 16 |
                GetByte(index + 1) <<  8 |
                GetByte(index    );
        }

        public uint GetUInt32(int index)
        {
            ValidateIndex(index, 4);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first (big endian)
                return (uint)
                    (GetByte(index    ) << 24 |
                     GetByte(index + 1) << 16 |
                     GetByte(index + 2) <<  8 |
                     GetByte(index + 3));
            }
            // Intel ordering - LSB first (little endian)
            return (uint)
                (GetByte(index + 3) << 24 |
                 GetByte(index + 2) << 16 |
                 GetByte(index + 1) <<  8 |
                 GetByte(index    ));
        }

        public int GetInt32(int index)
        {
            ValidateIndex(index, 4);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first (big endian)
                return
                    GetByte(index    ) << 24 |
                    GetByte(index + 1) << 16 |
                    GetByte(index + 2) <<  8 |
                    GetByte(index + 3);
            }
            // Intel ordering - LSB first (little endian)
            return
                GetByte(index + 3) << 24 |
                GetByte(index + 2) << 16 |
                GetByte(index + 1) <<  8 |
                GetByte(index    );
        }

        public long GetInt64(int index)
        {
            ValidateIndex(index, 8);
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return
                    (long)GetByte(index    ) << 56 |
                    (long)GetByte(index + 1) << 48 |
                    (long)GetByte(index + 2) << 40 |
                    (long)GetByte(index + 3) << 32 |
                    (long)GetByte(index + 4) << 24 |
                    (long)GetByte(index + 5) << 16 |
                    (long)GetByte(index + 6) <<  8 |
                          GetByte(index + 7);
            }
            // Intel ordering - LSB first
            return
                (long)GetByte(index + 7) << 56 |
                (long)GetByte(index + 6) << 48 |
                (long)GetByte(index + 5) << 40 |
                (long)GetByte(index + 4) << 32 |
                (long)GetByte(index + 3) << 24 |
                (long)GetByte(index + 2) << 16 |
                (long)GetByte(index + 1) <<  8 |
                      GetByte(index    );
        }

        public float GetS15Fixed16(int index)
        {
            ValidateIndex(index, 4);
            if (IsMotorolaByteOrder)
            {
                float res = GetByte(index) << 8 | GetByte(index + 1);
                var d = GetByte(index + 2) << 8 | GetByte(index + 3);
                return (float)(res + d / 65536.0);
            }
            else
            {
                // this particular branch is untested
                var d = GetByte(index + 1) << 8 | GetByte(index);
                float res = GetByte(index + 3) << 8 | GetByte(index + 2);
                return (float)(res + d / 65536.0);
            }
        }

        /// <exception cref="System.IO.IOException"/>
        public float GetFloat32(int index) => BitConverter.ToSingle(BitConverter.GetBytes(GetInt32(index)), 0);

        /// <exception cref="System.IO.IOException"/>
        public double GetDouble64(int index) => BitConverter.Int64BitsToDouble(GetInt64(index));

        /// <exception cref="System.IO.IOException"/>
        public string GetString(int index, int bytesRequested, Encoding encoding)
        {
            var bytes = GetBytes(index, bytesRequested);
            return encoding.GetString(bytes, 0, bytes.Length);
        }

        public string GetNullTerminatedString(int index, int maxLengthBytes, Encoding encoding = null)
        {
            var bytes = GetNullTerminatedBytes(index, maxLengthBytes);

            return (encoding ?? Encoding.UTF8).GetString(bytes, 0, bytes.Length);
        }

        public StringValue GetNullTerminatedStringValue(int index, int maxLengthBytes, Encoding encoding = null)
        {
            var bytes = GetNullTerminatedBytes(index, maxLengthBytes);

            return new StringValue(bytes, encoding);
        }

        public byte[] GetNullTerminatedBytes(int index, int maxLengthBytes)
        {
            var buffer = GetBytes(index, maxLengthBytes);

            // Count the number of non-null bytes
            var length = 0;
            while (length < buffer.Length && buffer[length] != 0)
                length++;

            if (length == maxLengthBytes)
                return buffer;

            var bytes = new byte[length];
            if (length > 0)
                Array.Copy(buffer, 0, bytes, 0, length);
            return bytes;
        }
    }
}
