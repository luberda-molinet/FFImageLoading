using System;

namespace FFImageLoading.Concurrency
{
    static class Hasher
    {
        public static UInt32 Rehash(Int32 hash)
        {
            unchecked
            {
                Int64 prod = ((Int64)hash ^ 0x00000000691ac2e9L) * 0x00000000a931b975L;
                return (UInt32)(prod ^ (prod >> 32));
            }
        }
    }
}
