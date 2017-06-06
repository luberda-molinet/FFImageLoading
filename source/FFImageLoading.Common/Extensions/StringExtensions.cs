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

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
    }
}
