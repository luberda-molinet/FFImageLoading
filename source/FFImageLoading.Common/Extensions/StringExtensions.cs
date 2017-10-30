using System;
using System.Linq;

namespace FFImageLoading
{
    [Preserve(AllMembers = true)]
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

        public static bool IsDataUrl(this string str)
        {
            return !string.IsNullOrWhiteSpace(str) && (
                    str.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                    || str.StartsWith("<", StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsSvgFileUrl(this string str)
        {
            return !string.IsNullOrWhiteSpace(str)
                && str.Split(new[] { '?' }, StringSplitOptions.RemoveEmptyEntries)[0].EndsWith("svg", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSvgDataUrl(this string str)
        {
            return !string.IsNullOrWhiteSpace(str) && (
                    str.StartsWith("data:image/svg", StringComparison.OrdinalIgnoreCase)
                    || str.StartsWith("data:text", StringComparison.OrdinalIgnoreCase)
                    || str.StartsWith("<", StringComparison.OrdinalIgnoreCase));
        }
    }
}
