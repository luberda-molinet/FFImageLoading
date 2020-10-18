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
                    .Matches(css.Minify(), @"(?<selectors>[A-Za-z0-9_\-\.,\s#]+)\s*{(?<declarations>.+?)}", RegexOptions.IgnoreCase)
                    .Cast<Match>()
                    .Select(m => Regex
                        .Split(m.Groups["selectors"].Value, @",")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(selector => new KeyValuePair<string, string>(
                            key: selector.Trim(),
                            value: m.Groups["declarations"].Value.Trim())))
                    .SelectMany(x => x);

            foreach (var item in items)
            {
                destination.AddOrUpdate(item.Key, item.Value);
            }
        }
		
		static void AddOrUpdate(this Dictionary<string,string> source, string key, string value)
		{
			if(value == null) 
			{
				return;
			}
			
			if(source.ContainsKey(key))
			{
				source[key] += value;
			}
			else
			{
				source.Add(key, value);
			}
		}

        static string Minify(this string css) => Regex.Replace(css, @"(\r\n|\r|\n)", string.Empty).Trim();

		public static Dictionary<string, string> AddFontShorthand(Dictionary<string, string> style, string value)
		{
			var splitted = value.Split(new char[0], StringSplitOptions.RemoveEmptyEntries); // split by whitespaces
			string fontStyle = null;
			string fontVariant = null;
			string fontWeight = null;
			string fontSize = null;
			string lineHeight = null;
			string fontFamily = null;

			foreach (var item in splitted)
			{
				switch (item)
				{
					case "normal":
					case "inherit":
					case "initial":
						if (string.IsNullOrEmpty(fontStyle))
							fontStyle = item;
						else if (string.IsNullOrEmpty(fontVariant))
							fontVariant = item;
						else if (string.IsNullOrEmpty(fontWeight))
							fontWeight = item;
						break;

					case "italic":
					case "oblique":
						fontStyle = item;
						break;

					case "small-caps":
						fontVariant = item;
						break;

					case "bold":
					case "bolder":
					case "lighter":
					case "100":
					case "200":
					case "300":
					case "400":
					case "500":
					case "600":
					case "700":
					case "800":
					case "900":
					case "1000":
						fontWeight = item;
						break;

					default:
						if (fontSize == null)
						{
							var parts = item.Split('/');
							fontSize = parts[0];
							if (parts.Length > 1)
								lineHeight = parts[1];
							break;
						}

						if (string.IsNullOrEmpty(fontFamily))
						{
							fontFamily = item;
						}
						else
						{
							fontFamily = string.Concat(fontFamily, " ", item);
						}

						break;
				}
			}

			if (!string.IsNullOrWhiteSpace(fontStyle))
				style["font-style"] = fontStyle;

			if (!string.IsNullOrWhiteSpace(fontVariant))
				style["font-variant"] = fontVariant;

			if (!string.IsNullOrWhiteSpace(fontWeight))
				style["font-weight"] = fontWeight;

			if (!string.IsNullOrWhiteSpace(fontSize))
				style["font-size"] = fontSize;

			if (!string.IsNullOrWhiteSpace(lineHeight))
				style["line-height"] = lineHeight;

			if (!string.IsNullOrWhiteSpace(fontFamily))
				style["font-family"] = fontFamily;

			return style;
		}
    }
}
