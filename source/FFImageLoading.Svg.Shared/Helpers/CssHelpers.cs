using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FFImageLoading.Svg.Platform
{
    internal static class CssHelpers
    {
        public static void ParseSelectors(string css, Dictionary<string,string> destination)
        {
            if (string.IsNullOrWhiteSpace(css))
                return;

            var items =
                Regex
                    .Matches(css.Minify(), @"(?<selectors>[a-z0-9_\-\.,\s#]+)\s*{(?<declarations>.+?)}", RegexOptions.IgnoreCase)
                    .Cast<Match>()
                    .Select(m => Regex
                        .Split(m.Groups["selectors"].Value, @",")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(selector => new KeyValuePair<string, string>(
                            key: selector.Trim().ToLowerInvariant(),
                            value: m.Groups["declarations"].Value.Trim())))
                    .SelectMany(x => x);

            foreach (var item in items)
            {
                destination.Add(item.Key, item.Value);
            }
        }

        static string Minify(this string css) => Regex.Replace(css, @"(\r\n|\r|\n)", string.Empty).Trim();
    }
}
