using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using SkiaSharp;

namespace FFImageLoading.Svg.Platform
{
	public class SKSvg
	{
		private const float DefaultPPI = 160f;
		private const bool DefaultThrowOnUnsupportedElement = false;

		private static readonly IFormatProvider icult = CultureInfo.InvariantCulture;
		private static readonly XNamespace xlink = "http://www.w3.org/1999/xlink";
		private static readonly XNamespace svg = "http://www.w3.org/2000/svg";
		private static readonly char[] WS = new char[] { ' ', '\t', '\n', '\r' };
		private static readonly Regex unitRe = new Regex("px|pt|em|ex|pc|cm|mm|in");
		private static readonly Regex percRe = new Regex("%");
		private static readonly Regex fillUrlRe = new Regex(@"url\s*\(\s*#([^\)]+)\)");
		private static readonly Regex keyValueRe = new Regex(@"\s*([\w-]+)\s*:\s*(.*)");
		private static readonly Regex WSRe = new Regex(@"\s{2,}");

		private readonly Dictionary<string, XElement> defs = new Dictionary<string, XElement>();

		public SKSvg()
			: this(DefaultPPI, SKSize.Empty)
		{
		}

		public SKSvg(float pixelsPerInch)
			: this(pixelsPerInch, SKSize.Empty)
		{
		}

		public SKSvg(SKSize canvasSize)
			: this(DefaultPPI, canvasSize)
		{
		}

		public SKSvg(float pixelsPerInch, SKSize canvasSize)
		{
			CanvasSize = canvasSize;
			PixelsPerInch = pixelsPerInch;
			ThrowOnUnsupportedElement = DefaultThrowOnUnsupportedElement;
		}

		public float PixelsPerInch { get; set; }
		public bool ThrowOnUnsupportedElement { get; set; }
		public SKRect ViewBox { get; private set; }
		public SKSize CanvasSize { get; private set; }
		public SKPicture Picture { get; private set; }
		public string Description { get; private set; }
		public string Title { get; private set; }
		public string Version { get; private set; }

		public SKPicture Load(string filename)
		{
#if PORTABLE
            // PCL does not have the ability to read a file and use a context
            if (createReaderMethod == null)
            {
                return Load(XDocument.Load(filename));
            }

            // we know that there we can access the method via reflection
            var args = new object[] { filename, null, CreateSvgXmlContext() };
            using (var reader = (XmlReader)createReaderMethod.Invoke(null, args))
            {
                return Load(reader);
            }
#else
			using (var stream = File.OpenRead(filename))
			{
				return Load(stream);
			}
#endif
		}

		public SKPicture Load(Stream stream)
		{
			using (var reader = XmlReader.Create(stream, null, CreateSvgXmlContext()))
			{
				return Load(reader);
			}
		}

		public SKPicture Load(XmlReader reader)
		{
			return Load(XDocument.Load(reader));
		}

		private static XmlParserContext CreateSvgXmlContext()
		{
			var table = new NameTable();
			var manager = new XmlNamespaceManager(table);
			manager.AddNamespace(string.Empty, svg.NamespaceName);
			manager.AddNamespace("xlink", xlink.NamespaceName);
			return new XmlParserContext(null, manager, null, XmlSpace.None);
		}

		private SKPicture Load(XDocument xdoc)
		{
			var svg = xdoc.Root;
			var ns = svg.Name.Namespace;

			// find the defs (gradients) - and follow all hrefs
			foreach (var d in svg.Descendants())
			{
				var id = d.Attribute("id")?.Value?.Trim();
				if (!string.IsNullOrEmpty(id))
					defs[id] = ReadDefinition(d);
			}

			Version = svg.Attribute("version")?.Value;
			Title = svg.Element(ns + "title")?.Value;
			Description = svg.Element(ns + "desc")?.Value ?? svg.Element(ns + "description")?.Value;

			// TODO: parse the "preserveAspectRatio" values properly
			var preserveAspectRatio = svg.Attribute("preserveAspectRatio")?.Value;

			// get the SVG dimensions
			var viewBoxA = svg.Attribute("viewBox") ?? svg.Attribute("viewPort");
			if (viewBoxA != null)
			{
				ViewBox = ReadRectangle(viewBoxA.Value);
			}

			if (CanvasSize.IsEmpty)
			{
				// get the user dimensions
				var widthA = svg.Attribute("width");
				var heightA = svg.Attribute("height");
				var width = ReadNumber(widthA);
				var height = ReadNumber(heightA);
				var size = new SKSize(width, height);

				if (widthA == null)
				{
					size.Width = ViewBox.Width;
				}
				else if (widthA.Value.Contains("%"))
				{
					size.Width *= ViewBox.Width;
				}
				if (heightA == null)
				{
					size.Height = ViewBox.Height;
				}
				else if (heightA != null && heightA.Value.Contains("%"))
				{
					size.Height *= ViewBox.Height;
				}

				// set the property
				CanvasSize = size;
			}

			// create the picture from the elements
			using (var recorder = new SKPictureRecorder())
			using (var canvas = recorder.BeginRecording(SKRect.Create(CanvasSize)))
			{
				// if there is no viewbox, then we don't do anything, otherwise
				// scale the SVG dimensions to fit inside the user dimensions
				if (!ViewBox.IsEmpty && (ViewBox.Width != CanvasSize.Width || ViewBox.Height != CanvasSize.Height))
				{
					if (preserveAspectRatio == "none")
					{
						canvas.Scale(CanvasSize.Width / ViewBox.Width, CanvasSize.Height / ViewBox.Height);
					}
					else
					{
						// TODO: just center scale for now
						var scale = Math.Min(CanvasSize.Width / ViewBox.Width, CanvasSize.Height / ViewBox.Height);
						var centered = SKRect.Create(CanvasSize).AspectFit(ViewBox.Size);
						canvas.Translate(centered.Left, centered.Top);
						canvas.Scale(scale, scale);
					}
				}

				// translate the canvas by the viewBox origin
				canvas.Translate(-ViewBox.Left, -ViewBox.Top);

				// if the viewbox was specified, then crop to that
				if (!ViewBox.IsEmpty)
				{
					canvas.ClipRect(ViewBox);
				}

				LoadElements(svg.Elements(), canvas);

				Picture = recorder.EndRecording();
			}

			return Picture;
		}

		private void LoadElements(IEnumerable<XElement> elements, SKCanvas canvas)
		{
			foreach (var e in elements)
			{
				ReadElement(e, canvas);
			}
		}

		private void ReadElement(XElement e, SKCanvas canvas)
		{
			ReadElement(e, canvas, null, CreatePaint());
		}

		private void ReadElement(XElement e, SKCanvas canvas, SKPaint stroke, SKPaint fill)
		{
			if (e.Attribute("display")?.Value == "none")
				return;

			// transform matrix
			var transform = ReadTransform(e.Attribute("transform")?.Value ?? string.Empty);
			canvas.Save();
			canvas.Concat(ref transform);

			// SVG element
			var elementName = e.Name.LocalName;
			var isGroup = elementName == "g";

			// read style
			var style = ReadPaints(e, ref stroke, ref fill, isGroup);

			// parse elements
			switch (elementName)
			{
				case "text":
					if (stroke != null || fill != null)
					{
						ReadText(e, canvas, stroke?.Clone(), fill?.Clone());
					}
					break;
				case "rect":
					if (stroke != null || fill != null)
					{
						var x = ReadNumber(e.Attribute("x"));
						var y = ReadNumber(e.Attribute("y"));
						var width = ReadNumber(e.Attribute("width"));
						var height = ReadNumber(e.Attribute("height"));
						var rx = ReadNumber(e.Attribute("rx"));
						var ry = ReadNumber(e.Attribute("ry"));
						var rect = SKRect.Create(x, y, width, height);
						if (rx > 0 || ry > 0)
						{
							if (fill != null)
								canvas.DrawRoundRect(rect, rx, ry, fill);
							if (stroke != null)
								canvas.DrawRoundRect(rect, rx, ry, stroke);
						}
						else
						{
							if (fill != null)
								canvas.DrawRect(rect, fill);
							if (stroke != null)
								canvas.DrawRect(rect, stroke);
						}
					}
					break;
				case "ellipse":
					if (stroke != null || fill != null)
					{
						var cx = ReadNumber(e.Attribute("cx"));
						var cy = ReadNumber(e.Attribute("cy"));
						var rx = ReadNumber(e.Attribute("rx"));
						var ry = ReadNumber(e.Attribute("ry"));
						if (fill != null)
							canvas.DrawOval(cx, cy, rx, ry, fill);
						if (stroke != null)
							canvas.DrawOval(cx, cy, rx, ry, stroke);
					}
					break;
				case "circle":
					if (stroke != null || fill != null)
					{
						var cx = ReadNumber(e.Attribute("cx"));
						var cy = ReadNumber(e.Attribute("cy"));
						var rr = ReadNumber(e.Attribute("r"));
						if (fill != null)
							canvas.DrawCircle(cx, cy, rr, fill);
						if (stroke != null)
							canvas.DrawCircle(cx, cy, rr, stroke);
					}
					break;
				case "path":
					if (stroke != null || fill != null)
					{
						var d = e.Attribute("d")?.Value;
						if (!string.IsNullOrWhiteSpace(d))
						{
							var path = SKPath.ParseSvgPathData(d);
							if (fill != null)
								canvas.DrawPath(path, fill);
							if (stroke != null)
								canvas.DrawPath(path, stroke);
						}
					}
					break;
				case "polygon":
				case "polyline":
					if (stroke != null || fill != null)
					{
						var close = elementName == "polygon";
						var p = e.Attribute("points")?.Value;
						if (!string.IsNullOrWhiteSpace(p))
						{
							var path = ReadPolyPath(p, close);
							if (fill != null)
								canvas.DrawPath(path, fill);
							if (stroke != null)
								canvas.DrawPath(path, stroke);
						}
					}
					break;
				case "g":
					if (e.HasElements)
					{
						// get current group opacity
						float groupOpacity = ReadOpacity(style);
						if (groupOpacity != 1.0f)
						{
							var opacity = (byte)(255 * groupOpacity);
							var opacityPaint = new SKPaint { Color = SKColors.Black.WithAlpha(opacity) };

							// apply the opacity
							canvas.SaveLayer(opacityPaint);
						}

						foreach (var gElement in e.Elements())
						{
							ReadElement(gElement, canvas, stroke?.Clone(), fill?.Clone());
						}

						// restore state
						if (groupOpacity != 1.0f)
							canvas.Restore();
					}
					break;
				case "use":
					if (e.HasAttributes)
					{
						var href = ReadHref(e);
						if (href != null)
						{
							// TODO: copy/process other attributes

							var x = ReadNumber(e.Attribute("x"));
							var y = ReadNumber(e.Attribute("y"));
							var useTransform = SKMatrix.MakeTranslation(x, y);

							canvas.Save();
							canvas.Concat(ref useTransform);

							ReadElement(href, canvas, stroke?.Clone(), fill?.Clone());

							canvas.Restore();
						}
					}
					break;
				case "line":
					if (stroke != null)
					{
						var x1 = ReadNumber(e.Attribute("x1"));
						var x2 = ReadNumber(e.Attribute("x2"));
						var y1 = ReadNumber(e.Attribute("y1"));
						var y2 = ReadNumber(e.Attribute("y2"));
						canvas.DrawLine(x1, y1, x2, y2, stroke);
					}
					break;
				case "switch":
					if (e.HasElements)
					{
						foreach (var ee in e.Elements())
						{
							var requiredFeatures = ee.Attribute("requiredFeatures");
							var requiredExtensions = ee.Attribute("requiredExtensions");
							var systemLanguage = ee.Attribute("systemLanguage");

							// TODO: evaluate requiredFeatures, requiredExtensions and systemLanguage
							var isVisible =
								requiredFeatures == null &&
								requiredExtensions == null &&
								systemLanguage == null;

							if (isVisible)
							{
								ReadElement(ee, canvas, stroke?.Clone(), fill?.Clone());
							}
						}
					}
					break;
				case "defs":
				case "title":
				case "desc":
				case "description":
					// already read earlier
					break;
				default:
					LogOrThrow($"SVG element '{elementName}' is not supported");
					break;
			}

			// restore matrix
			canvas.Restore();
		}

		private void ReadText(XElement e, SKCanvas canvas, SKPaint stroke, SKPaint fill)
		{
			// TODO: stroke

			var x = ReadNumber(e.Attribute("x"));
			var y = ReadNumber(e.Attribute("y"));
			var xy = new SKPoint(x, y);

			ReadFontAttributes(e, fill);
			fill.TextAlign = ReadTextAlignment(e);

			ReadTextSpans(e, canvas, xy, stroke, fill);
		}

		private void ReadTextSpans(XElement e, SKCanvas canvas, SKPoint location, SKPaint stroke, SKPaint fill)
		{
			var nodes = e.Nodes().ToArray();
			for (int i = 0; i < nodes.Length; i++)
			{
				var c = nodes[i];
				bool isFirst = i == 0;
				bool isLast = i == nodes.Length - 1;

				if (c.NodeType == XmlNodeType.Text)
				{
					// TODO: check for preserve whitespace

					var textSegments = ((XText)c).Value.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
					var count = textSegments.Length;
					if (count > 0)
					{
						if (isFirst)
							textSegments[0] = textSegments[0].TrimStart();
						if (isLast)
							textSegments[count - 1] = textSegments[count - 1].TrimEnd();
						var text = WSRe.Replace(string.Concat(textSegments), " ");

						canvas.DrawText(text, location.X, location.Y, fill);

						location.X += fill.MeasureText(text);
					}
				}
				else if (c.NodeType == XmlNodeType.Element)
				{
					var ce = (XElement)c;
					if (ce.Name.LocalName == "tspan")
					{
						var spanFill = fill.Clone();

						// the current span may want to change the cursor position
						location.X = ReadOptionalNumber(ce.Attribute("x")) ?? location.X;
						location.Y = ReadOptionalNumber(ce.Attribute("y")) ?? location.Y;

						ReadFontAttributes(ce, spanFill);

						var text = ce.Value.Trim();

						canvas.DrawText(text, location.X, location.Y, spanFill);

						location.X += spanFill.MeasureText(text);
					}
				}
			}
		}

		private void ReadFontAttributes(XElement e, SKPaint paint)
		{
			var fontStyle = ReadStyle(e);

			string ffamily;
			if (!fontStyle.TryGetValue("font-family", out ffamily) || string.IsNullOrWhiteSpace(ffamily))
				ffamily = paint.Typeface?.FamilyName;
			var fweight = ReadFontWeight(fontStyle, paint.Typeface?.FontWeight ?? (int)SKFontStyleWeight.Normal);
			var fwidth = ReadFontWidth(fontStyle, paint.Typeface?.FontWidth ?? (int)SKFontStyleWidth.Normal);
			var fstyle = ReadFontStyle(fontStyle, paint.Typeface?.FontSlant ?? SKFontStyleSlant.Upright);
			paint.Typeface = SKTypeface.FromFamilyName(ffamily, fweight, fwidth, fstyle);

			string fsize;
			if (fontStyle.TryGetValue("font-size", out fsize) && !string.IsNullOrWhiteSpace(fsize))
				paint.TextSize = ReadNumber(fsize);
		}

		private static SKFontStyleSlant ReadFontStyle(Dictionary<string, string> fontStyle, SKFontStyleSlant defaultStyle = SKFontStyleSlant.Upright)
		{
			SKFontStyleSlant style = defaultStyle;

			string fstyle;
			if (fontStyle.TryGetValue("font-style", out fstyle) && !string.IsNullOrWhiteSpace(fstyle))
			{
				switch (fstyle)
				{
					case "italic":
						style = SKFontStyleSlant.Italic;
						break;
					case "oblique":
						style = SKFontStyleSlant.Oblique;
						break;
					case "normal":
						style = SKFontStyleSlant.Upright;
						break;
					default:
						style = defaultStyle;
						break;
				}
			}

			return style;
		}

		private int ReadFontWidth(Dictionary<string, string> fontStyle, int defaultWidth = (int)SKFontStyleWidth.Normal)
		{
			int width = defaultWidth;

			string fwidth;
			if (fontStyle.TryGetValue("font-stretch", out fwidth) && !string.IsNullOrWhiteSpace(fwidth) && !int.TryParse(fwidth, out width))
			{
				switch (fwidth)
				{
					case "ultra-condensed":
						width = (int)SKFontStyleWidth.UltraCondensed;
						break;
					case "extra-condensed":
						width = (int)SKFontStyleWidth.ExtraCondensed;
						break;
					case "condensed":
						width = (int)SKFontStyleWidth.Condensed;
						break;
					case "semi-condensed":
						width = (int)SKFontStyleWidth.SemiCondensed;
						break;
					case "normal":
						width = (int)SKFontStyleWidth.Normal;
						break;
					case "semi-expanded":
						width = (int)SKFontStyleWidth.SemiExpanded;
						break;
					case "expanded":
						width = (int)SKFontStyleWidth.Expanded;
						break;
					case "extra-expanded":
						width = (int)SKFontStyleWidth.ExtraExpanded;
						break;
					case "ultra-expanded":
						width = (int)SKFontStyleWidth.UltraExpanded;
						break;
					case "wider":
						width = width + 1;
						break;
					case "narrower":
						width = width - 1;
						break;
					default:
						width = defaultWidth;
						break;
				}
			}

			return Math.Min(Math.Max((int)SKFontStyleWidth.UltraCondensed, width), (int)SKFontStyleWidth.UltraExpanded);
		}

		private int ReadFontWeight(Dictionary<string, string> fontStyle, int defaultWeight = (int)SKFontStyleWeight.Normal)
		{
			int weight = defaultWeight;

			string fweight;
			if (fontStyle.TryGetValue("font-weight", out fweight) && !string.IsNullOrWhiteSpace(fweight) && !int.TryParse(fweight, out weight))
			{
				switch (fweight)
				{
					case "normal":
						weight = (int)SKFontStyleWeight.Normal;
						break;
					case "bold":
						weight = (int)SKFontStyleWeight.Bold;
						break;
					case "bolder":
						weight = weight + 100;
						break;
					case "lighter":
						weight = weight - 100;
						break;
					default:
						weight = defaultWeight;
						break;
				}
			}

			return Math.Min(Math.Max((int)SKFontStyleWeight.Thin, weight), (int)SKFontStyleWeight.ExtraBlack);
		}

		private void LogOrThrow(string message)
		{
			if (ThrowOnUnsupportedElement)
				throw new NotSupportedException(message);
			else
				Debug.WriteLine(message);
		}

		private string GetString(Dictionary<string, string> style, string name, string defaultValue = "")
		{
			string v;
			if (style.TryGetValue(name, out v))
				return v;
			return defaultValue;
		}

		private Dictionary<string, string> ReadStyle(string style)
		{
			var d = new Dictionary<string, string>();
			var kvs = style.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var kv in kvs)
			{
				var m = keyValueRe.Match(kv);
				if (m.Success)
				{
					var k = m.Groups[1].Value;
					var v = m.Groups[2].Value;
					d[k] = v;
				}
			}
			return d;
		}

		private Dictionary<string, string> ReadStyle(XElement e)
		{
			// get from local attributes
			var dic = e.Attributes().ToDictionary(k => k.Name.LocalName, v => v.Value);

			var style = e.Attribute("style")?.Value;
			if (!string.IsNullOrWhiteSpace(style))
			{
				// get from stlye attribute
				var styleDic = ReadStyle(style);

				// overwrite
				foreach (var pair in styleDic)
					dic[pair.Key] = pair.Value;
			}

			return dic;
		}

		private Dictionary<string, string> ReadPaints(XElement e, ref SKPaint stroke, ref SKPaint fill, bool isGroup)
		{
			var style = ReadStyle(e);
			ReadPaints(style, ref stroke, ref fill, isGroup);
			return style;
		}

		private void ReadPaints(Dictionary<string, string> style, ref SKPaint strokePaint, ref SKPaint fillPaint, bool isGroup)
		{
			// get current element opacity, but ignore for groups (special case)
			float elementOpacity = isGroup ? 1.0f : ReadOpacity(style);

			// stroke
			var stroke = GetString(style, "stroke").Trim();
			if (stroke.Equals("none", StringComparison.OrdinalIgnoreCase))
			{
				strokePaint = null;
			}
			else
			{
				if (string.IsNullOrEmpty(stroke))
				{
					// no change
				}
				else
				{
					if (strokePaint == null)
						strokePaint = CreatePaint(true);

					SKColor color;
					if (ColorHelper.TryParse(stroke, out color))
					{
						// preserve alpha
						if (color.Alpha == 255)
							strokePaint.Color = color.WithAlpha(strokePaint.Color.Alpha);
						else
							strokePaint.Color = color;
					}
				}

				// stroke attributes
				var strokeDashArray = GetString(style, "stroke-dasharray");
				if (!string.IsNullOrWhiteSpace(strokeDashArray))
				{
					if ("none".Equals(strokeDashArray, StringComparison.OrdinalIgnoreCase))
					{
						// remove any dash
						if (strokePaint != null)
							strokePaint.PathEffect = null;
					}
					else
					{
						if (strokePaint == null)
							strokePaint = CreatePaint(true);

						// get the dash
						var dashesStrings = strokeDashArray.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
						var dashes = dashesStrings.Select(ReadNumber).ToArray();
						if (dashesStrings.Length % 2 == 1)
							dashes = dashes.Concat(dashes).ToArray();
						// get the offset
						var strokeDashOffset = ReadNumber(style, "stroke-dashoffset", 0);
						// set the effect
						strokePaint.PathEffect = SKPathEffect.CreateDash(dashes.ToArray(), strokeDashOffset);
					}
				}

				var strokeWidth = GetString(style, "stroke-width");
				if (!string.IsNullOrWhiteSpace(strokeWidth))
				{
					if (strokePaint == null)
						strokePaint = CreatePaint(true);
					strokePaint.StrokeWidth = ReadNumber(strokeWidth);
				}

				var strokeOpacity = GetString(style, "stroke-opacity");
				if (!string.IsNullOrWhiteSpace(strokeOpacity))
				{
					if (strokePaint == null)
						strokePaint = CreatePaint(true);
					strokePaint.Color = strokePaint.Color.WithAlpha((byte)(ReadNumber(strokeOpacity) * 255));
				}

				if (strokePaint != null)
				{
					strokePaint.Color = strokePaint.Color.WithAlpha((byte)(strokePaint.Color.Alpha * elementOpacity));
				}
			}

			// fill
			var fill = GetString(style, "fill").Trim();
			if (fill.Equals("none", StringComparison.OrdinalIgnoreCase))
			{
				fillPaint = null;
			}
			else
			{
				if (string.IsNullOrEmpty(fill))
				{
					// no change
				}
				else
				{
					if (fillPaint == null)
						fillPaint = CreatePaint();

					SKColor color;
					if (ColorHelper.TryParse(fill, out color))
					{
						// preserve alpha
						if (color.Alpha == 255)
							fillPaint.Color = color.WithAlpha(fillPaint.Color.Alpha);
						else
							fillPaint.Color = color;
					}
					else
					{
						var read = false;
						var urlM = fillUrlRe.Match(fill);
						if (urlM.Success)
						{
							var id = urlM.Groups[1].Value.Trim();

							XElement defE;
							if (defs.TryGetValue(id, out defE))
							{
								var gradientShader = ReadGradient(defE);
								if (gradientShader != null)
								{
									// TODO: multiple shaders

									fillPaint.Shader = gradientShader;
									read = true;
								}
								// else try another type (eg: image)
							}
							else
							{
								LogOrThrow($"Invalid fill url reference: {id}");
							}
						}

						if (!read)
						{
							LogOrThrow($"Unsupported fill: {fill}");
						}
					}
				}

				// fill attributes
				var fillOpacity = GetString(style, "fill-opacity");
				if (!string.IsNullOrWhiteSpace(fillOpacity))
				{
					if (fillPaint == null)
						fillPaint = CreatePaint();

					fillPaint.Color = fillPaint.Color.WithAlpha((byte)(ReadNumber(fillOpacity) * 255));
				}

				if (fillPaint != null)
				{
					fillPaint.Color = fillPaint.Color.WithAlpha((byte)(fillPaint.Color.Alpha * elementOpacity));
				}
			}
		}

		private SKPaint CreatePaint(bool stroke = false)
		{
			return new SKPaint
			{
				IsAntialias = true,
				IsStroke = stroke,
				Color = SKColors.Black
			};
		}

		private SKMatrix ReadTransform(string raw)
		{
			var t = SKMatrix.MakeIdentity();

			if (string.IsNullOrWhiteSpace(raw))
			{
				return t;
			}

			var calls = raw.Trim().Split(new[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var c in calls)
			{
				var args = c.Split(new[] { '(', ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				var nt = SKMatrix.MakeIdentity();
				switch (args[0])
				{
					case "matrix":
						if (args.Length == 7)
						{
							nt.Values = new float[]
							{
								ReadNumber(args[1]), ReadNumber(args[3]), ReadNumber(args[5]),
								ReadNumber(args[2]), ReadNumber(args[4]), ReadNumber(args[6]),
								0, 0, 1
							};
						}
						else
						{
							LogOrThrow($"Matrices are expected to have 6 elements, this one has {args.Length - 1}");
						}
						break;
					case "translate":
						if (args.Length >= 3)
						{
							nt = SKMatrix.MakeTranslation(ReadNumber(args[1]), ReadNumber(args[2]));
						}
						else if (args.Length >= 2)
						{
							nt = SKMatrix.MakeTranslation(ReadNumber(args[1]), 0);
						}
						break;
					case "scale":
						if (args.Length >= 3)
						{
							nt = SKMatrix.MakeScale(ReadNumber(args[1]), ReadNumber(args[2]));
						}
						else if (args.Length >= 2)
						{
							var sx = ReadNumber(args[1]);
							nt = SKMatrix.MakeScale(sx, sx);
						}
						break;
					case "rotate":
						var a = ReadNumber(args[1]);
						if (args.Length >= 4)
						{
							var x = ReadNumber(args[2]);
							var y = ReadNumber(args[3]);
							var t1 = SKMatrix.MakeTranslation(x, y);
							var t2 = SKMatrix.MakeRotationDegrees(a);
							var t3 = SKMatrix.MakeTranslation(-x, -y);
							SKMatrix.Concat(ref nt, ref t1, ref t2);
							SKMatrix.Concat(ref nt, ref nt, ref t3);
						}
						else
						{
							nt = SKMatrix.MakeRotationDegrees(a);
						}
						break;
					default:
						LogOrThrow($"Can't transform {args[0]}");
						break;
				}
				SKMatrix.Concat(ref t, ref t, ref nt);
			}

			return t;
		}

		private SKPath ReadPolyPath(string pointsData, bool closePath)
		{
			var path = SKPath.ParseSvgPathData("M" + pointsData);

			if (closePath)
				path?.Close();

			return path;
		}

		private SKTextAlign ReadTextAlignment(XElement element)
		{
			string value = null;
			if (element != null)
			{
				var attrib = element.Attribute("text-anchor");
				if (attrib != null && !string.IsNullOrWhiteSpace(attrib.Value))
					value = attrib.Value;
				else
				{
					var style = element.Attribute("style");
					if (style != null && !string.IsNullOrWhiteSpace(style.Value))
					{
						value = GetString(ReadStyle(style.Value), "text-anchor");
					}
				}
			}

			switch (value)
			{
				case "end":
					return SKTextAlign.Right;
				case "middle":
					return SKTextAlign.Center;
				default:
					return SKTextAlign.Left;
			}
		}

		private SKShader ReadGradient(XElement defE)
		{
			switch (defE.Name.LocalName)
			{
				case "linearGradient":
					return ReadLinearGradient(defE);
				case "radialGradient":
					return ReadRadialGradient(defE);
			}
			return null;
		}

		private SKShader ReadRadialGradient(XElement e)
		{
			var centerX = ReadNumber(e.Attribute("cx"));
			var centerY = ReadNumber(e.Attribute("cy"));
			//var focusX = ReadOptionalNumber(e.Attribute("fx")) ?? centerX;
			//var focusY = ReadOptionalNumber(e.Attribute("fy")) ?? centerY;
			var radius = ReadNumber(e.Attribute("r"));
			//var absolute = e.Attribute("gradientUnits")?.Value == "userSpaceOnUse";
			var tileMode = ReadSpreadMethod(e);
			var stops = ReadStops(e);

			// TODO: check gradientTransform attribute
			// TODO: use absolute

			return SKShader.CreateRadialGradient(
				new SKPoint(centerX, centerY),
				radius,
				stops.Values.ToArray(),
				stops.Keys.ToArray(),
				tileMode);
		}

		private SKShader ReadLinearGradient(XElement e)
		{
			var startX = ReadNumber(e.Attribute("x1"));
			var startY = ReadNumber(e.Attribute("y1"));
			var endX = ReadNumber(e.Attribute("x2"));
			var endY = ReadNumber(e.Attribute("y2"));
			//var absolute = e.Attribute("gradientUnits")?.Value == "userSpaceOnUse";
			var tileMode = ReadSpreadMethod(e);
			var stops = ReadStops(e);

			// TODO: check gradientTransform attribute
			// TODO: use absolute

			return SKShader.CreateLinearGradient(
				new SKPoint(startX, startY),
				new SKPoint(endX, endY),
				stops.Values.ToArray(),
				stops.Keys.ToArray(),
				tileMode);
		}

		private static SKShaderTileMode ReadSpreadMethod(XElement e)
		{
			var repeat = e.Attribute("spreadMethod")?.Value;
			switch (repeat)
			{
				case "reflect":
					return SKShaderTileMode.Mirror;
				case "repeat":
					return SKShaderTileMode.Repeat;
				case "pad":
				default:
					return SKShaderTileMode.Clamp;
			}
		}

		private XElement ReadDefinition(XElement e)
		{
			var union = new XElement(e.Name);
			union.Add(e.Elements());
			union.Add(e.Attributes());

			var child = ReadHref(e);
			if (child != null)
			{
				union.Add(child.Elements());
				union.Add(child.Attributes().Where(a => union.Attribute(a.Name) == null));
			}

			return union;
		}

		private XElement ReadHref(XElement e)
		{
			var href = e.Attribute(xlink + "href")?.Value?.Substring(1);
			XElement child;
			if (string.IsNullOrEmpty(href) || !defs.TryGetValue(href, out child))
			{
				child = null;
			}
			return child;
		}

		private SortedDictionary<float, SKColor> ReadStops(XElement e)
		{
			var stops = new SortedDictionary<float, SKColor>();

			var ns = e.Name.Namespace;
			foreach (var se in e.Elements(ns + "stop"))
			{
				var style = ReadStyle(se);

				var offset = ReadNumber(style["offset"]);
				var color = SKColors.Black;
				byte alpha = 255;

				string stopColor;
				if (style.TryGetValue("stop-color", out stopColor))
				{
					// preserve alpha
					if (ColorHelper.TryParse(stopColor, out color) && color.Alpha == 255)
						alpha = color.Alpha;
				}

				string stopOpacity;
				if (style.TryGetValue("stop-opacity", out stopOpacity))
				{
					alpha = (byte)(ReadNumber(stopOpacity) * 255);
				}

				color = color.WithAlpha(alpha);
				stops[offset] = color;
			}

			return stops;
		}

		private float ReadOpacity(Dictionary<string, string> style)
		{
			return Math.Min(Math.Max(0.0f, ReadNumber(style, "opacity", 1.0f)), 1.0f);
		}

		private float ReadNumber(Dictionary<string, string> style, string key, float defaultValue)
		{
			float value = defaultValue;
			string strValue;
			if (style.TryGetValue(key, out strValue))
			{
				value = ReadNumber(strValue);
			}
			return value;
		}

		private float ReadNumber(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
				return 0;

			var s = raw.Trim();
			var m = 1.0f;

			if (unitRe.IsMatch(s))
			{
				if (s.EndsWith("in", StringComparison.Ordinal))
				{
					m = PixelsPerInch;
				}
				else if (s.EndsWith("cm", StringComparison.Ordinal))
				{
					m = PixelsPerInch / 2.54f;
				}
				else if (s.EndsWith("mm", StringComparison.Ordinal))
				{
					m = PixelsPerInch / 25.4f;
				}
				else if (s.EndsWith("pt", StringComparison.Ordinal))
				{
					m = PixelsPerInch / 72.0f;
				}
				else if (s.EndsWith("pc", StringComparison.Ordinal))
				{
					m = PixelsPerInch / 6.0f;
				}
				s = s.Substring(0, s.Length - 2);
			}
			else if (percRe.IsMatch(s))
			{
				s = s.Substring(0, s.Length - 1);
				m = 0.01f;
			}

			float v;
			if (!float.TryParse(s, NumberStyles.Float, icult, out v))
			{
				v = 0;
			}
			return m * v;
		}

		private float ReadNumber(XAttribute a) => ReadNumber(a?.Value);

		private float? ReadOptionalNumber(XAttribute a) => a == null ? (float?)null : ReadNumber(a.Value);

		private SKRect ReadRectangle(string s)
		{
			var r = new SKRect();
			var p = s.Split(WS, StringSplitOptions.RemoveEmptyEntries);
			if (p.Length > 0)
				r.Left = ReadNumber(p[0]);
			if (p.Length > 1)
				r.Top = ReadNumber(p[1]);
			if (p.Length > 2)
				r.Right = r.Left + ReadNumber(p[2]);
			if (p.Length > 3)
				r.Bottom = r.Top + ReadNumber(p[3]);
			return r;
		}

		static class ColorHelper
		{
			public static bool TryParse(string str, out SKColor color)
			{
				if (str.StartsWith("rgb(", StringComparison.Ordinal))
				{
					str = str.Substring(4, str.Length - 4).TrimEnd(')');
					var values = str.Split(',');
					var r = Convert.ToInt32(values[0]);
					var g = Convert.ToInt32(values[1]);
					var b = Convert.ToInt32(values[2]);
					str = $"#{r:X2}{g:X2}{b:X2}";
				}

				if (!SKColor.TryParse(str, out color))
				{
					string hexString = null;

					if (HexValues.TryGetValue(str, out hexString))
					{
						return SKColor.TryParse(hexString, out color);
					}

					return false;
				}

				return true;
			}

			static Dictionary<string, string> hexValues;

			internal static Dictionary<string, string> HexValues
			{
				get
				{
					if (hexValues == null)
					{
						hexValues = new Dictionary<string, string>()
					{
						{"aliceblue","#f0f8ff"},{"antiquewhite","#faebd7"},{"aqua","#00ffff"},{"aquamarine","#7fffd4"},{"azure","#f0ffff"},{"beige","#f5f5dc"},{"bisque","#ffe4c4"},{"black","#000000"},{"blanchedalmond","#ffebcd"},{"blue","#0000ff"},{"blueviolet","#8a2be2"},{"brown","#a52a2a"},{"burlywood","#deb887"},{"cadetblue","#5f9ea0"},{"chartreuse","#7fff00"},{"chocolate","#d2691e"},{"coral","#ff7f50"},{"cornflowerblue","#6495ed"},{"cornsilk","#fff8dc"},{"crimson","#dc143c"},{"cyan","#00ffff"},{"darkblue","#00008b"},{"darkcyan","#008b8b"},{"darkgoldenrod","#b8860b"},{"darkgray","#a9a9a9"},{"darkgreen","#006400"},{"darkgrey","#a9a9a9"},{"darkkhaki","#bdb76b"},{"darkmagenta","#8b008b"},{"darkolivegreen","#556b2f"},{"darkorange","#ff8c00"},{"darkorchid","#9932cc"},{"darkred","#8b0000"},{"darksalmon","#e9967a"},{"darkseagreen","#8fbc8f"},{"darkslateblue","#483d8b"},{"darkslategray","#2f4f4f"},{"darkslategrey","#2f4f4f"},{"darkturquoise","#00ced1"},{"darkviolet","#9400d3"},{"deeppink","#ff1493"},{"deepskyblue","#00bfff"},{"dimgray","#696969"},{"dimgrey","#696969"},{"dodgerblue","#1e90ff"},{"firebrick","#b22222"},{"floralwhite","#fffaf0"},{"forestgreen","#228b22"},{"fuchsia","#ff00ff"},{"gainsboro","#dcdcdc"},{"ghostwhite","#f8f8ff"},{"gold","#ffd700"},{"goldenrod","#daa520"},{"gray","#808080"},{"green","#008000"},{"greenyellow","#adff2f"},{"grey","#808080"},{"honeydew","#f0fff0"},{"hotpink","#ff69b4"},{"indianred","#cd5c5c"},{"indigo","#4b0082"},{"ivory","#fffff0"},{"khaki","#f0e68c"},{"lavender","#e6e6fa"},{"lavenderblush","#fff0f5"},{"lawngreen","#7cfc00"},{"lemonchiffon","#fffacd"},{"lightblue","#add8e6"},{"lightcoral","#f08080"},{"lightcyan","#e0ffff"},{"lightgoldenrodyellow","#fafad2"},{"lightgray","#d3d3d3"},{"lightgreen","#90ee90"},{"lightgrey","#d3d3d3"},{"lightpink","#ffb6c1"},{"lightsalmon","#ffa07a"},{"lightseagreen","#20b2aa"},{"lightskyblue","#87cefa"},{"lightslategray","#778899"},{"lightslategrey","#778899"},{"lightsteelblue","#b0c4de"},{"lightyellow","#ffffe0"},{"lime","#00ff00"},{"limegreen","#32cd32"},{"linen","#faf0e6"},{"magenta","#ff00ff"},{"maroon","#800000"},{"mediumaquamarine","#66cdaa"},{"mediumblue","#0000cd"},{"mediumorchid","#ba55d3"},{"mediumpurple","#9370db"},{"mediumseagreen","#3cb371"},{"mediumslateblue","#7b68ee"},{"mediumspringgreen","#00fa9a"},{"mediumturquoise","#48d1cc"},{"mediumvioletred","#c71585"},{"midnightblue","#191970"},{"mintcream","#f5fffa"},{"mistyrose","#ffe4e1"},{"moccasin","#ffe4b5"},{"navajowhite","#ffdead"},{"navy","#000080"},{"oldlace","#fdf5e6"},{"olive","#808000"},{"olivedrab","#6b8e23"},{"orange","#ffa500"},{"orangered","#ff4500"},{"orchid","#da70d6"},{"palegoldenrod","#eee8aa"},{"palegreen","#98fb98"},{"paleturquoise","#afeeee"},{"palevioletred","#db7093"},{"papayawhip","#ffefd5"},{"peachpuff","#ffdab9"},{"peru","#cd853f"},{"pink","#ffc0cb"},{"plum","#dda0dd"},{"powderblue","#b0e0e6"},{"purple","#800080"},{"rebeccapurple","#663399"},{"red","#ff0000"},{"rosybrown","#bc8f8f"},{"royalblue","#4169e1"},{"saddlebrown","#8b4513"},{"salmon","#fa8072"},{"sandybrown","#f4a460"},{"seagreen","#2e8b57"},{"seashell","#fff5ee"},{"sienna","#a0522d"},{"silver","#c0c0c0"},{"skyblue","#87ceeb"},{"slateblue","#6a5acd"},{"slategray","#708090"},{"slategrey","#708090"},{"snow","#fffafa"},{"springgreen","#00ff7f"},{"steelblue","#4682b4"},{"tan","#d2b48c"},{"teal","#008080"},{"thistle","#d8bfd8"},{"tomato","#ff6347"},{"turquoise","#40e0d0"},{"violet","#ee82ee"},{"wheat","#f5deb3"},{"white","#ffffff"},{"whitesmoke","#f5f5f5"},{"yellow","#ffff00"},{"yellowgreen","#9acd32"}
					};
					}

					return hexValues;
				}
			}
		}
	}
}