using System;
using System.Text;

namespace FFImageLoading.Helpers.Exif
{
    internal abstract class SequentialReader
    {
        protected SequentialReader(bool isMotorolaByteOrder)
        {
            IsMotorolaByteOrder = isMotorolaByteOrder;
        }

        public bool IsMotorolaByteOrder { get; }

        public abstract long Position { get; }

        public abstract SequentialReader WithByteOrder(bool isMotorolaByteOrder);

        public abstract byte[] GetBytes(int count);

        public abstract void GetBytes(byte[] buffer, int offset, int count);

        public abstract void Skip(long n);

        public abstract bool TrySkip(long n);

        public abstract byte GetByte();

        public sbyte GetSByte() => unchecked((sbyte)GetByte());

        public ushort GetUInt16()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return (ushort)
                    (GetByte() << 8 |
                     GetByte());
            }
            // Intel ordering - LSB first
            return (ushort)
                (GetByte() |
                 GetByte() << 8);
        }

        public short GetInt16()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return (short)
                    (GetByte() << 8 |
                     GetByte());
            }
            // Intel ordering - LSB first
            return (short)
                (GetByte() |
                 GetByte() << 8);
        }

        public uint GetUInt32()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first (big endian)
                return (uint)
                    (GetByte() << 24 |
                     GetByte() << 16 |
                     GetByte() << 8 |
                     GetByte());
            }
            // Intel ordering - LSB first (little endian)
            return (uint)
                (GetByte() |
                 GetByte() << 8 |
                 GetByte() << 16 |
                 GetByte() << 24);
        }

        public int GetInt32()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first (big endian)
                return
                    GetByte() << 24 |
                    GetByte() << 16 |
                    GetByte() << 8 |
                    GetByte();
            }
            // Intel ordering - LSB first (little endian)
            return
                GetByte() |
                GetByte() << 8 |
                GetByte() << 16 |
                GetByte() << 24;
        }

        public long GetInt64()
        {
            if (IsMotorolaByteOrder)
            {
                // Motorola - MSB first
                return
                    (long)GetByte() << 56 |
                    (long)GetByte() << 48 |
                    (long)GetByte() << 40 |
                    (long)GetByte() << 32 |
                    (long)GetByte() << 24 |
                    (long)GetByte() << 16 |
                    (long)GetByte() << 8 |
                          GetByte();
            }
            // Intel ordering - LSB first
            return
                      GetByte() |
                (long)GetByte() << 8 |
                (long)GetByte() << 16 |
                (long)GetByte() << 24 |
                (long)GetByte() << 32 |
                (long)GetByte() << 40 |
                (long)GetByte() << 48 |
                (long)GetByte() << 56;
        }

        public float GetS15Fixed16()
        {
            if (IsMotorolaByteOrder)
            {
                float res = GetByte() << 8 | GetByte();
                var d = GetByte() << 8 | GetByte();
                return (float)(res + d / 65536.0);
            }
            else
            {
                // this particular branch is untested
                var d = GetByte() | GetByte() << 8;
                float res = GetByte() | GetByte() << 8;
                return (float)(res + d / 65536.0);
            }
        }

        public float GetFloat32() => BitConverter.ToSingle(BitConverter.GetBytes(GetInt32()), 0);

        public double GetDouble64() => BitConverter.Int64BitsToDouble(GetInt64());

        public string GetString(int bytesRequested, Encoding encoding)
        {
            var bytes = GetBytes(bytesRequested);
            return encoding.GetString(bytes, 0, bytes.Length);
        }

        public StringValue GetStringValue(int bytesRequested, Encoding encoding = null)
        {
            return new StringValue(GetBytes(bytesRequested), encoding);
        }

        public string GetNullTerminatedString(int maxLengthBytes, Encoding encoding = null)
        {
            var bytes = GetNullTerminatedBytes(maxLengthBytes);

            return (encoding ?? Encoding.UTF8).GetString(bytes, 0, bytes.Length);
        }

        public StringValue GetNullTerminatedStringValue(int maxLengthBytes, Encoding encoding = null)
        {
            var bytes = GetNullTerminatedBytes(maxLengthBytes);

            return new StringValue(bytes, encoding);
        }

        public byte[] GetNullTerminatedBytes(int maxLengthBytes)
        {
            var buffer = new byte[maxLengthBytes];

            // Count the number of non-null bytes
            var length = 0;
            while (length < buffer.Length && (buffer[length] = GetByte()) != 0)
                length++;

            if (length == maxLengthBytes)
                return buffer;

            var bytes = new byte[length];
            if (length > 0)
                Array.Copy(buffer, bytes, length);
            return bytes;
        }
    }
}
