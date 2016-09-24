using System;
using System.Linq;

namespace FFImageLoading
{
    public static class StringExtensions
    {
        public static string ToSanitizedKey(this string key)
        {
            return new string(key.ToCharArray()
                .Where(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                .ToArray());
        }
    }
}
