using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using SkiaSharp;
using System.Threading;

namespace FFImageLoading.Svg.Platform
{
	[Preserve(AllMembers = true)]
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
		private static readonly Regex urlRe = new Regex(@"url\s*\(\s*#([^\)]+)\)");
		private static readonly Regex keyValueRe = new Regex(@"\s*([\w-]+)\s*:\s*(.*)");
		private static readonly Regex WSRe = new Regex(@"\s{2,}");

		private readonly Dictionary<string, string> styles = new Dictionary<string, string>();
		private readonly Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
		private readonly Dictionary<string, SKSvgMask> masks = new Dictionary<string, SKSvgMask>();
		private readonly Dictionary<string, ISKSvgFill> fillDefs = new Dictionary<string, ISKSvgFill>();
		private readonly Dictionary<XElement, string> elementFills = new Dictionary<XElement, string>();
		private readonly Dictionary<XElement, string> strokeElementFills = new Dictionary<XElement, string>();
		private readonly XmlReaderSettings xmlReaderSettings = new XmlReaderSettings()
		{
			DtdProcessing = DtdProcessing.Ignore,
			IgnoreComments = true,
		};

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

		public SKPicture Load(string filename, CancellationToken token = default)
		{
			using (var stream = File.OpenRead(filename))
			{
				return Load(stream, token);
			}
		}

		public SKPicture Load(Stream stream, CancellationToken token = default)
		{
			using (var reader = XmlReader.Create(stream, xmlReaderSettings, CreateSvgXmlContext()))
			{
				return Load(reader, token);
			}
		}

		public SKPicture Load(XmlReader reader, CancellationToken token = default)
		{
			return Load(XDocument.Load(reader), token);
		}

		private static XmlParserContext CreateSvgXmlContext()
		{
			var table = new NameTable();
			var manager = new XmlNamespaceManager(table);
			manager.AddNamespace(string.Empty, svg.NamespaceName);
			manager.AddNamespace("xlink", xlink.NamespaceName);
			return new XmlParserContext(null, manager, null, XmlSpace.None);
		}

		private SKPicture Load(XDocument xdoc, CancellationToken token = default)
		{
			var svg = xdoc.Root;
			var ns = svg.Name.Namespace;

			// find the defs (gradients) - and follow all hrefs
			foreach (var d in svg.Descendants())
			{
				var id = ReadId(d);
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

			token.ThrowIfCancellationRequested();

			// create the picture from the elements
			using (var recorder = new SKPictureRecorder())
			using (var canvas = recorder.BeginRecording(SKRect.Create(CanvasSize)))
			{
				// if there is no viewbox, then we don't do anything, otherwise
				// scale the SVG dimensions to fit inside the user dimensions
				if (!ViewBox.IsEmpty && (Math.Abs(ViewBox.Width - CanvasSize.Width) > float.Epsilon
		  || Math.Abs(ViewBox.Height - CanvasSize.Height) > float.Epsilon))
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

				// read style
				SKPaint stroke = null;
				SKPaint fill = CreatePaint();
				var style = ReadPaints(svg, ref stroke, ref fill, true);

				// read elements
				LoadElements(svg.Elements(), canvas, stroke, fill, token);

				Picture = recorder.EndRecording();
			}

			return Picture;
		}

		private void LoadElements(IEnumerable<XElement> elements, SKCanvas canvas, SKPaint stroke, SKPaint fill, CancellationToken token = default)
		{
			foreach (var e in elements)
			{
				ReadElement(e, canvas, stroke?.Clone(), fill?.Clone());
			}
		}

		private void ReadElement(XElement e, SKCanvas canvas, SKPaint stroke, SKPaint fill, bool isMask = false, CancellationToken token = default)
		{
			token.ThrowIfCancellationRequested();

			if (e.Attribute("display")?.Value == "none")
				return;

			// SVG element
			var elementName = e.Name.LocalName;
			var isGroup = elementName == "g";

			// read style
			var style = ReadPaints(e, ref stroke, ref fill, isGroup, isMask);

			if (style.TryGetValue("display", out var displayStyle) && displayStyle == "none")
				return;

			var xy = ReadElementXY(e);
			canvas.Save();

			try
			{
				var mask = ReadMask(style);
				if (!isMask && mask != null)
				{
					canvas.SaveLayer(new SKPaint());
					canvas.Clear();

					try
					{
						using (var strokePaint = mask.Stroke?.Clone())
						using (var fillPaint = mask.Fill?.Clone())
						{
							// TODO Is it Skia bug? When the same color is used for fill and mask nothing is drawn
							if (strokePaint != null && strokePaint.Color == stroke?.Color)
								strokePaint.Color = new SKColor((byte)~strokePaint.Color.Red, (byte)~strokePaint.Color.Green, (byte)~strokePaint.Color.Blue);

							// TODO Is it Skia bug? When the same color is used for fill and mask nothing is drawn
							if (fillPaint != null && fillPaint.Color == fill?.Color)
								fillPaint.Color = new SKColor((byte)~fillPaint.Color.Red, (byte)~fillPaint.Color.Green, (byte)~fillPaint.Color.Blue);

							foreach (var gElement in mask.Element.Elements())
							{
								ReadElement(gElement, canvas, strokePaint, fillPaint);
							}
						}

						using (var strokePaint = stroke?.Clone())
						using (var fillPaint = fill?.Clone())
						{
							if (strokePaint != null)
								strokePaint.BlendMode = SKBlendMode.SrcIn;

							if (fillPaint != null)
								fillPaint.BlendMode = SKBlendMode.SrcIn;

							ReadElement(e, canvas, strokePaint, fillPaint, true);
						}
					}
					finally
					{
						canvas.Restore();
					}

					return;
				}

				if (elementName != "use")
				{
					// transform matrix
					var transform = ReadTransform(e.Attribute("transform")?.Value ?? string.Empty, xy);
					canvas.Concat(ref transform);
				}

				// clip-path
				var clipPath = ReadClipPath(e.Attribute("clip-path")?.Value ?? string.Empty);
				if (clipPath != null)
				{
					canvas.ClipPath(clipPath);
				}

				// parse elements
				switch (elementName)
				{
					case "image":
						{
							var image = ReadImage(e, canvas.DeviceClipBounds);
							if (image.Bytes != null)
							{
								using (var bitmap = SKBitmap.Decode(image.Bytes))
								{
									if (bitmap != null)
									{
										canvas.DrawBitmap(bitmap, image.Rect);
									}
								}
							}
						}
						break;
					case "text":
						if (stroke != null || fill != null)
						{
							var spans = ReadText(e, stroke?.Clone(), fill?.Clone());
							if (spans.Any())
							{
								canvas.DrawText(spans);
							}
						}
						break;
					case "rect":
					case "ellipse":
					case "circle":
					case "path":
					case "polygon":
					case "polyline":
					case "line":
						if (stroke != null || fill != null)
						{
							var elementPath = ReadElement(e, style);
							if (elementPath == null)
								break;

							if (fill != null && elementFills.TryGetValue(e, out var fillId)
							  && fillDefs.TryGetValue(fillId, out var addFill))
							{
								var elementSize = ReadElementSize(e);
								var bounds = SKRect.Create(xy, elementSize);

								addFill.ApplyFill(fill, bounds);
							}

							if (stroke != null && strokeElementFills.TryGetValue(e,
							  out var strokeFillId) && fillDefs.TryGetValue(strokeFillId, out var addStrokeFill))
							{
								var elementSize = ReadElementSize(e);
								var bounds = SKRect.Create(xy, elementSize);

								addStrokeFill.ApplyFill(stroke, bounds);
							}

							if (fill != null)
							{
								canvas.DrawPath(elementPath, fill);
							}
							if (stroke != null)
							{
								canvas.DrawPath(elementPath, stroke);
							}
						}
						break;
					case "g":
						if (e.HasElements)
						{
							// get current group opacity
							var groupOpacity = ReadOpacity(style);
							try
							{
								if (groupOpacity != 1.0f)
								{
									var opacity = (byte)(255 * groupOpacity);
									var opacityPaint = new SKPaint
									{
										Color = SKColors.Black.WithAlpha(opacity)
									};

									// apply the opacity
									canvas.SaveLayer(opacityPaint);
								}

								foreach (var gElement in e.Elements())
								{
									ReadElement(gElement, canvas, stroke?.Clone(), fill?.Clone(), isMask);
								}
							}
							finally
							{
								// restore state
								if (groupOpacity != 1.0f)
									canvas.Restore();
							}
						}
						break;
					case "use":
						if (e.HasAttributes)
						{
							var href = ReadHref(e);
							if (href != null)
							{
								if (string.Equals(href.Name.LocalName, "symbol", StringComparison.OrdinalIgnoreCase))
								{
									RenderSymbol(href, e, canvas, stroke?.Clone(), fill?.Clone(), e.Attributes());
								}
								else
								{
									ApplyAttributesToElement(e.Attributes(), href, new string[] { "href", "id" });
									ReadElement(href, canvas, stroke?.Clone(), fill?.Clone(), isMask);
								}
							}
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
									ReadElement(ee, canvas, stroke?.Clone(), fill?.Clone(), isMask);
								}
							}
						}
						break;
					case "mask":
						if (e.HasElements)
						{
							masks.Add(ReadId(e), new SKSvgMask(stroke, fill, e));
						}
						break;
					case "style":
						CssHelpers.ParseSelectors(e.Value, styles);
						break;
					case "defs":
						var styleNodes = e.Descendants();
						if (styleNodes != null)
						{
							foreach (var item in styleNodes)
							{
								if (item.Name.LocalName == "style")
								{
									CssHelpers.ParseSelectors(item.Value, styles);
								}
							}
						}
						break;
					case "a":
						foreach (var child in e.Descendants())
						{
							ReadElement(child, canvas, stroke?.Clone(), fill?.Clone(), isMask);
						}
						break;
					case "clipPath":
					case "title":
					case "desc":
					case "description":
						// already read earlier
						break;
					default:
						LogOrThrow($"SVG element '{elementName}' is not supported");
						break;
				}
			}
			finally
			{
				// restore matrix
				canvas.Restore();
			}
		}

		private SKSvgImage ReadImage(XElement e, SKRect sKRect)
		{
			var width = ReadNumber(e.Attribute("width"));
			var height = ReadNumber(e.Attribute("height"));
			if(e.Attribute("width")?.Value?.Contains("%") == true)
			{
				width = ReadNumber(e.Attribute("width")?.Value.Replace("%", ""));
				width = sKRect.Width * (width / 100.0f);
			}

			if (e.Attribute("height")?.Value?.Contains("%") == true)
			{
				height = ReadNumber(e.Attribute("height")?.Value.Replace("%", ""));
				height = sKRect.Height * (height / 100.0f);
			}

			var rect = SKRect.Create(width, height);

			byte[] bytes = null;

			var uri = ReadHrefString(e);
			if (uri != null)
			{
				if (uri.StartsWith("data:"))
				{
					bytes = ReadUriBytes(uri);
				}
				else
				{
					LogOrThrow($"Remote images are not supported");
				}
			}

			return new SKSvgImage(rect, uri, bytes);
		}

		private SKPath ReadElement(XElement e, Dictionary<string, string> style = null)
		{
			var path = new SKPath();

			var elementName = e.Name.LocalName;
			switch (elementName)
			{
				case "rect":
					var rect = ReadRoundedRect(e);
					if (rect.IsRounded)
						path.AddRoundRect(rect.Rect, rect.RadiusX, rect.RadiusY);
					else
						path.AddRect(rect.Rect);
					break;
				case "ellipse":
					var oval = ReadOval(e);
					path.AddOval(oval.BoundingRect);
					break;
				case "circle":
					var circle = ReadCircle(e);
					path.AddCircle(circle.Center.X, circle.Center.Y, circle.Radius);
					break;
				case "path":
				case "polygon":
				case "polyline":
					string data;
					if (elementName == "path")
					{
						data = e.Attribute("d")?.Value;
					}
					else
					{
						data = "M" + e.Attribute("points")?.Value;
						if (elementName == "polygon")
							data += " Z";
					}
					if (!string.IsNullOrWhiteSpace(data))
					{
						path.Dispose();
						path = SKPath.ParseSvgPathData(data);
					}
					path.FillType = ReadFillRule(style);
					break;
				case "line":
					var line = ReadLine(e);
					path.MoveTo(line.P1);
					path.LineTo(line.P2);
					break;
				default:
					path.Dispose();
					path = null;
					break;
			}

			return path;
		}

		private void RenderSymbol(XElement symbol, XElement use, SKCanvas canvas, SKPaint stroke, SKPaint fill, IEnumerable<XAttribute> attributes)
		{
			if (symbol == null || use == null)
				return;

			canvas.Save();
			try
			{
				var point = ReadElementXY(use);
				// adjust the canvas for use's location
				canvas.Translate(point.X, point.Y);

				var symbolViewBox = ReadElementViewBox(symbol);
				var useSize = ReadElementSize(use);
				var aspectRatio = symbol.Attribute("preserveAspectRatio")?.Value;

				ScaleViewBoxToSize(canvas, symbolViewBox, useSize, aspectRatio);

				// adjust the canvas for viewBox's origin
				if (!symbolViewBox.IsEmpty)
					canvas.Translate(-symbolViewBox.Left, -symbolViewBox.Top);

				foreach (var ee in symbol.Elements())
				{
					// apply all attributes to each contained element
					ApplyAttributesToElement(attributes, ee, new string[] { "href", "id", "transform" });
					ReadElement(ee, canvas, stroke?.Clone(), fill?.Clone());
				}
			}
			finally
			{
				canvas.Restore();
			}

		}

		private static void ApplyAttributesToElement(IEnumerable<XAttribute> attributes, XElement e, string[] ignoreAttributes)
		{
			if (e == null || attributes == null)
				return;

			foreach (var attribute in attributes)
			{
				bool skipAttribute = false;
				var name = attribute.Name.LocalName;
				foreach (var ignoreStr in ignoreAttributes)
				{
					if (name.Equals(ignoreStr, StringComparison.OrdinalIgnoreCase))
					{
						skipAttribute = true;
						break;
					}
				}

				if (skipAttribute)
					continue;

				e.SetAttributeValue(attribute.Name, attribute.Value);
			}
		}

		private void ScaleViewBoxToSize(SKCanvas canvas, SKRect viewBox, SKSize size, string aspectRatio)
		{
			// if the viewbox is empty, no scaling is required
			if (viewBox.IsEmpty || Math.Abs(viewBox.Width) < float.Epsilon || Math.Abs(viewBox.Height) < float.Epsilon)
				return;
			// we only want to exit if width and height are both empty because if one is missing, the
			// other will be derived using the aspec ratio
			if (size.IsEmpty)
				return;

			// scale the viewbox to fit into the requested size
			var scaleX = size.Width / viewBox.Width;
			var scaleY = size.Height / viewBox.Height;

			// if either height or width is zero, set the missing scale to the other dimension
			if (Math.Abs(size.Width) < float.Epsilon)
				scaleX = scaleY;
			if (Math.Abs(size.Height) < float.Epsilon)
				scaleY = scaleX;

			if (!string.Equals(aspectRatio, "none", StringComparison.OrdinalIgnoreCase))
			{
				// if aspectRation is anything except "none", scale proportionally to the smallest dimension value
				if (scaleX < scaleY)
					scaleY = scaleX;
				if (scaleY < scaleX)
					scaleX = scaleY;
			}

			canvas.Scale(scaleX, scaleY);
		}

		private SKOval ReadOval(XElement e)
		{
			var cx = ReadNumber(e.Attribute("cx"));
			var cy = ReadNumber(e.Attribute("cy"));
			var rx = ReadNumber(e.Attribute("rx"));
			var ry = ReadNumber(e.Attribute("ry"));

			return new SKOval(new SKPoint(cx, cy), rx, ry);
		}

		private SKCircle ReadCircle(XElement e)
		{
			var cx = ReadNumber(e.Attribute("cx"));
			var cy = ReadNumber(e.Attribute("cy"));
			var rr = ReadNumber(e.Attribute("r"));

			return new SKCircle(new SKPoint(cx, cy), rr);
		}

		private SKLine ReadLine(XElement e)
		{
			var x1 = ReadNumber(e.Attribute("x1"));
			var x2 = ReadNumber(e.Attribute("x2"));
			var y1 = ReadNumber(e.Attribute("y1"));
			var y2 = ReadNumber(e.Attribute("y2"));

			return new SKLine(new SKPoint(x1, y1), new SKPoint(x2, y2));
		}

		private SKRoundedRect ReadRoundedRect(XElement e)
		{
			var width = ReadNumber(e.Attribute("width"));
			var height = ReadNumber(e.Attribute("height"));
			var rx = ReadOptionalNumber(e.Attribute("rx"));
			var ry = ReadOptionalNumber(e.Attribute("ry"));
			var rect = SKRect.Create(width, height);

			return new SKRoundedRect(rect, rx ?? ry ?? 0, ry ?? rx ?? 0);
		}

		private SKText ReadText(XElement e, SKPaint stroke, SKPaint fill)
		{
			var textAlign = ReadTextAlignment(e);
			var baselineShift = ReadBaselineShift(e);

			var style = ReadPaints(e, ref stroke, ref fill, false);
			ReadFontAttributes(style, ref stroke, ref fill);

			var spans = new SKText(new SKPoint(), textAlign);

			// textAlign is used for all spans within the <text> element. If different textAligns would be needed, it is necessary to use
			// several <text> elements instead of <tspan> elements
			fill.TextAlign = SKTextAlign.Left;  // fixed alignment for all spans

			if (stroke != null)
				stroke.TextAlign = SKTextAlign.Left;  // fixed alignment for all spans

			ReadTextElement(e, spans, textAlign, baselineShift, stroke, fill);

			return spans;
		}

		private void ReadTextElement(XElement e, SKText spans, SKTextAlign textAlign, float baselineShift, SKPaint stroke, SKPaint fill)
		{
			var nodes = e.Nodes().ToArray();
			for (int i = 0; i < nodes.Length; i++)
			{
				var clonedFill = fill.Clone();
				var clonedStroke = stroke?.Clone();

				var style = ReadPaints(e, ref clonedStroke, ref clonedFill, false);
				ReadFontAttributes(style, ref clonedStroke, ref clonedFill);

				var c = nodes[i];
				if (c.NodeType == XmlNodeType.Text)
				{
					var isFirst = i == 0;
					var isLast = i == nodes.Length - 1;
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

						if (string.IsNullOrEmpty(text))
							continue;

						spans.Append(new SKTextSpan(text, clonedStroke, clonedFill, baselineShift: baselineShift));
					}
				}
				else if (c is XElement ce && ce.Name.LocalName == "tspan")
				{
					if (ce.HasElements)
					{
						ReadTextElement(ce, spans, textAlign, baselineShift, stroke, clonedFill);
					}
					else
					{
						var text = ce.Value;

						if (string.IsNullOrEmpty(text))
							continue;

						// the current span may want to change the cursor position
						var x = ReadOptionalNumber(ce.Attribute("x"));
						var y = ReadOptionalNumber(ce.Attribute("y"));

						// Don't read text-anchor from tspans!, Only use enclosing text-anchor from text element!
						baselineShift = ReadBaselineShift(ce);

						spans.Append(new SKTextSpan(text, clonedStroke, clonedFill, x, y, baselineShift));
					}
				}
			}
		}

		private void ReadFontAttributes(Dictionary<string, string> style, ref SKPaint stroke, ref SKPaint fill)
		{
			var fontFamily = fill.Typeface?.FamilyName ?? SKTypeface.Default.FamilyName;
			var fontStyle = fill.Typeface?.FontSlant ?? SKFontStyleSlant.Upright;
			var fontWeight = (SKFontStyleWeight?)fill.Typeface?.FontWeight ?? SKFontStyleWeight.Normal;
			var fontWidth = (SKFontStyleWidth?)fill.Typeface?.FontWidth ?? SKFontStyleWidth.Normal;

			if (style.TryGetValue("font-style", out var cssFontStyle))
				TryParseFontStyle(cssFontStyle, out fontStyle, fontStyle);

			if (style.TryGetValue("font-weight", out var cssFontWeight))
				TryParseFontWeight(cssFontWeight, out fontWeight, fontWeight);

			if (style.TryGetValue("font-stretch", out var cssFontStretch))
				TryParseFontWidth(cssFontStretch, out fontWidth, fontWidth);

			if (style.TryGetValue("font-family", out var ffamily))
				fontFamily = ffamily;

			var typeface = SKTypeface.FromFamilyName(fontFamily, fontWeight, fontWidth, fontStyle);

			if (stroke != null)
				stroke.Typeface = typeface;
			fill.Typeface = typeface;

			if (style.TryGetValue("font-size", out var fsize))
			{
				var size = ReadNumber(fsize);

				if (stroke != null)
					stroke.TextSize = size;
				fill.TextSize = size;
			}
		}

		private static SKPathFillType ReadFillRule(Dictionary<string, string> style, SKPathFillType defaultFillRule = SKPathFillType.Winding)
		{
			var fillRule = defaultFillRule;

			if (style != null && style.TryGetValue("fill-rule", out var rule) && !string.IsNullOrWhiteSpace(rule))
			{
				switch (rule)
				{
					case "evenodd":
						fillRule = SKPathFillType.EvenOdd;
						break;
					case "nonzero":
						fillRule = SKPathFillType.Winding;
						break;
					default:
						fillRule = defaultFillRule;
						break;
				}
			}

			return fillRule;
		}

		private static bool TryParseFontStyle(string value, out SKFontStyleSlant fontStyle, SKFontStyleSlant defaultFontStyle = SKFontStyleSlant.Upright)
		{
			switch (value)
			{
				case "italic":
					fontStyle = SKFontStyleSlant.Italic;
					return true;
				case "oblique":
					fontStyle = SKFontStyleSlant.Oblique;
					return true;
				case "normal":
					fontStyle = SKFontStyleSlant.Upright;
					return true;
				default:
					fontStyle = defaultFontStyle;
					return false;
			}
		}

		private bool TryParseFontWidth(string value, out SKFontStyleWidth fontStretch, SKFontStyleWidth defaultFontStretch = SKFontStyleWidth.Normal)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				fontStretch = defaultFontStretch;
				return false;
			}

			switch (value)
			{
				case "ultra-condensed":
					fontStretch = SKFontStyleWidth.UltraCondensed;
					return true;
				case "extra-condensed":
					fontStretch = SKFontStyleWidth.ExtraCondensed;
					return true;
				case "condensed":
					fontStretch = SKFontStyleWidth.Condensed;
					return true;
				case "semi-condensed":
					fontStretch = SKFontStyleWidth.SemiCondensed;
					return true;
				case "normal":
					fontStretch = SKFontStyleWidth.Normal;
					return true;
				case "semi-expanded":
					fontStretch = SKFontStyleWidth.SemiExpanded;
					return true;
				case "expanded":
					fontStretch = SKFontStyleWidth.Expanded;
					return true;
				case "extra-expanded":
					fontStretch = SKFontStyleWidth.ExtraExpanded;
					return true;
				case "ultra-expanded":
					fontStretch = SKFontStyleWidth.UltraExpanded;
					return true;
				case "wider":
					fontStretch = (SKFontStyleWidth)(Math.Min(9, (int)defaultFontStretch + 1));
					return true;
				case "narrower":
					fontStretch = (SKFontStyleWidth)(Math.Max(1, (int)defaultFontStretch - 1));
					return true;

				default:
					fontStretch = defaultFontStretch;
					return false;
			}
		}

		private bool TryParseFontWeight(string value, out SKFontStyleWeight fontWeight, SKFontStyleWeight defaultFontWeight = SKFontStyleWeight.Normal)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				fontWeight = defaultFontWeight;
				return false;
			}

			if (int.TryParse(value, out var number) && number >= 100 && number <= 1000)
			{
				fontWeight = (SKFontStyleWeight)(number / 100 * 100);
				return true;
			}

			switch (value)
			{
				case "normal":
					fontWeight = SKFontStyleWeight.Normal;
					return true;
				case "bold":
					fontWeight = SKFontStyleWeight.Bold;
					return true;
				case "bolder":
					fontWeight = (SKFontStyleWeight)Math.Min(1000, (int)defaultFontWeight + 100);
					return true;
				case "lighter":
					fontWeight = (SKFontStyleWeight)Math.Max(100, (int)defaultFontWeight - 100);
					return true;
				default:
					fontWeight = defaultFontWeight;
					return false;
			}
		}

		private void LogOrThrow(string message)
		{
			if (ThrowOnUnsupportedElement)
				throw new NotSupportedException(message);

			Debug.WriteLine(message);
		}

		private string GetString(Dictionary<string, string> style, string name, string defaultValue = "")
		{
			if (style != null && style.TryGetValue(name, out string v))
				return v;
			return defaultValue;
		}

		private SKSvgMask ReadMask(Dictionary<string, string> style)
		{
			SKSvgMask mask = null;
			var maskID = GetString(style, "mask").Trim();
			if (!string.IsNullOrEmpty(maskID))
			{
				var urlM = urlRe.Match(maskID);
				if (urlM.Success)
				{
					var id = urlM.Groups[1].Value.Trim();
					masks.TryGetValue(id, out mask);
				}
			}
			return mask;
		}

		private string ReadId(XElement d)
		{
			return d.Attribute("id")?.Value?.Trim();
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

					if (k == "font")
					{
						CssHelpers.AddFontShorthand(d, v);
					}
					else
					{
						d[k] = v;
					}
				}
			}
			return d;
		}

		private Dictionary<string, string> ReadStyle(XElement e)
		{
			// get from local attributes
			var dic = e.Attributes().Where(a => HasSvgNamespace(a.Name)).ToDictionary(k => k.Name.LocalName, v => v.Value);

			if (styles.TryGetValue(e.Name.LocalName, out var glStyle))
			{
				// get from stlye attribute
				var styleDic = ReadStyle(glStyle);

				// overwrite
				foreach (var pair in styleDic)
					dic[pair.Key] = pair.Value;
			}
			if (dic.TryGetValue("class", out var className) && styles.TryGetValue("." + className, out glStyle))
			{
				// get from stlye attribute
				var styleDic = ReadStyle(glStyle);

				// overwrite
				foreach (var pair in styleDic)
					dic[pair.Key] = pair.Value;
			}

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

		private static bool HasSvgNamespace(XName name)
		{
			return
				string.IsNullOrEmpty(name.Namespace?.NamespaceName) ||
				name.Namespace == svg ||
				name.Namespace == xlink;
		}

		private SKPoint ReadElementXY(XElement e)
		{
			if (e == null)
				return default;

			var xAttr = e.Attribute("x");
			var yAttr = e.Attribute("y");

			if (xAttr == null && yAttr == null)
				return default;

			var x = ReadNumber(xAttr);
			var y = ReadNumber(yAttr);
			return new SKPoint(x, y);
		}

		private SKRect ReadElementViewBox(XElement e)
		{
			if (e == null)
				return SKRect.Empty;

			var viewBox = new SKRect();
			var tmpViewBoxAttr = e.Attribute("viewBox") ?? e.Attribute("viewPort");
			if (tmpViewBoxAttr != null)
			{
				viewBox = ReadRectangle(tmpViewBoxAttr.Value);
			}

			return viewBox;
		}

		private SKSize ReadElementSize(XElement e)
		{
			if (e == null)
				return SKSize.Empty;

			float width = 0f;
			float height = 0f;
			var element = e;

			while (element.Parent != null)
			{
				var widthAttr = element.Attribute("width");
				if (width <= 0f)
					width = ReadNumber(widthAttr);

				var heightAttr = element.Attribute("height");
				if (height <= 0f)
					height = ReadNumber(heightAttr);

				if (width > 0f || height > 0f)
				{
					var widthIsPercent = widthAttr?.Value?.Contains("%") ?? false;
					var heightIsPercent = heightAttr?.Value?.Contains("%") ?? false;
					if (element.Parent != null && (widthIsPercent || heightIsPercent))
					{
						// if either of the attributes is a %, then find the parent size
						var parentSize = ReadElementSize(element.Parent);
						var viewBox = ReadElementViewBox(element.Parent);
						SKSize viewSize;
						if (viewBox.IsEmpty)
							viewSize = new SKSize(parentSize.Width, parentSize.Height);
						else
							viewSize = new SKSize(viewBox.Width, viewBox.Height);

						if (widthIsPercent)
							width *= parentSize.Width * (viewSize.Width / parentSize.Width);
						if (heightIsPercent)
							height *= parentSize.Height * (viewSize.Height / parentSize.Height);
					}

					break;
				}

				element = element.Parent;
			}

			if (!(width > 0f || height > 0f))
			{
				var root = e?.Document?.Root;
				width = ReadNumber(root?.Attribute("width"));
				height = ReadNumber(root?.Attribute("height"));
			}

			return new SKSize(width, height);
		}

		private Dictionary<string, string> ReadPaints(XElement e, ref SKPaint stroke, ref SKPaint fill, bool isGroup, bool isMask = false)
		{
			var style = ReadStyle(e);

			ReadPaints(style, ref stroke, ref fill, isGroup, out var fillId, out var strokeFillId);

			if (isMask)
			{
				if (stroke != null)
					stroke.BlendMode = SKBlendMode.SrcIn;

				if (fill != null)
					fill.BlendMode = SKBlendMode.SrcIn;
			}

			if (fillId != null)
				elementFills[e] = fillId;

			if (strokeFillId != null)
				strokeElementFills[e] = strokeFillId;

			return style;
		}

		private void ReadPaints(Dictionary<string, string> style, ref SKPaint strokePaint, ref SKPaint fillPaint, bool isGroup, out string fillId, out string strokeFillId)
		{
			fillId = null;
			strokeFillId = null;

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
				if (string.IsNullOrEmpty(stroke) || stroke == "inherit")
				{
					// no change
				}
				else
				{
					if (strokePaint == null || stroke == "initial")
						strokePaint = CreatePaint(true);

					if (ColorHelper.TryParse(stroke, out SKColor color))
					{
						// preserve alpha
						if (color.Alpha == 255 && strokePaint.Color.Alpha > 0)
							strokePaint.Color = color.WithAlpha(strokePaint.Color.Alpha);
						else
							strokePaint.Color = color;
					}
					else
					{
						var urlM = urlRe.Match(stroke);
						if (urlM.Success)
						{
							var id = urlM.Groups[1].Value.Trim();
							if (defs.TryGetValue(id, out var defE))
							{
								switch (defE.Name.LocalName.ToLower())
								{
									case "lineargradient":
										fillDefs[id] = ReadLinearGradient(defE);
										strokeFillId = id;
										break;
									case "radialgradient":
										fillDefs[id] = ReadRadialGradient(defE);
										strokeFillId = id;
										break;
									default:
										LogOrThrow($"Unsupported stroke fill: {stroke}");
										break;
								}
							}
							else
							{
								LogOrThrow($"Invalid fill url reference: {id}");
							}
						}
						else
						{
							LogOrThrow($"Unsupported stroke fill: {stroke}");
						}
					}
				}

				// stroke attributes
				var strokeDashArray = GetString(style, "stroke-dasharray");
				var hasStrokeDashArray = !string.IsNullOrWhiteSpace(strokeDashArray);

				var strokeWidth = GetString(style, "stroke-width");
				var hasStrokeWidth = !string.IsNullOrWhiteSpace(strokeWidth);

				var strokeOpacity = GetString(style, "stroke-opacity");
				var hasStrokeOpacity = !string.IsNullOrWhiteSpace(strokeOpacity);

				var strokeLineCap = GetString(style, "stroke-linecap");
				var hasStrokeLineCap = !string.IsNullOrWhiteSpace(strokeLineCap);

				var strokeLineJoin = GetString(style, "stroke-linejoin");
				var hasStrokeLineJoin = !string.IsNullOrWhiteSpace(strokeLineJoin);

				var strokeMiterLimit = GetString(style, "stroke-miterlimit");
				var hasStrokeMiterLimit = !string.IsNullOrWhiteSpace(strokeMiterLimit);

				if (strokePaint == null)
				{
					if (hasStrokeDashArray ||
						hasStrokeWidth ||
						hasStrokeOpacity ||
						hasStrokeLineCap ||
						hasStrokeLineJoin)
					{
						strokePaint = CreatePaint(true);
					}
				}

				if (hasStrokeDashArray)
				{
					if ("none".Equals(strokeDashArray, StringComparison.OrdinalIgnoreCase))
					{
						// remove any dash
						if (strokePaint != null)
							strokePaint.PathEffect = null;
					}
					else
					{
						// get the dash
						var dashesStrings = strokeDashArray.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
						var dashes = dashesStrings.Select(ReadNumber);
						if (dashesStrings.Length % 2 == 1)
							dashes = dashes.Concat(dashes);

						// get the offset
						var strokeDashOffset = ReadNumber(style, "stroke-dashoffset", 0);

						// set the effect
						strokePaint.PathEffect = SKPathEffect.CreateDash(dashes.ToArray(), strokeDashOffset);
					}
				}

				if (hasStrokeWidth)
					strokePaint.StrokeWidth = ReadNumber(strokeWidth);
				if (hasStrokeOpacity)
					strokePaint.Color = strokePaint.Color.WithAlpha((byte)(ReadNumber(strokeOpacity) * 255));
				if (hasStrokeLineCap)
					strokePaint.StrokeCap = ReadLineCap(strokeLineCap);
				if (hasStrokeLineJoin)
					strokePaint.StrokeJoin = ReadLineJoin(strokeLineJoin);
				if (hasStrokeMiterLimit)
					strokePaint.StrokeMiter = ReadNumber(strokeMiterLimit);
				if (strokePaint != null)
					strokePaint.Color = strokePaint.Color.WithAlpha((byte)(strokePaint.Color.Alpha * elementOpacity));
			}

			// fill
			var fill = GetString(style, "fill").Trim();
			if (fill.Equals("none", StringComparison.OrdinalIgnoreCase))
			{
				fillPaint = null;
			}
			else
			{
				if (string.IsNullOrEmpty(fill) || fill == "inherit")
				{
					// no change
				}
				else if (fill == "initial")
				{
					fillPaint = CreatePaint();
				}
				else
				{
					fillPaint = CreatePaint();

					if (ColorHelper.TryParse(fill, out var color))
					{
						// preserve alpha
						if (color.Alpha == 255 && fillPaint.Color.Alpha > 0)
							fillPaint.Color = color.WithAlpha(fillPaint.Color.Alpha);
						else
							fillPaint.Color = color;
					}
					else
					{
						var urlM = urlRe.Match(fill);
						if (urlM.Success)
						{
							var id = urlM.Groups[1].Value.Trim();
							if (defs.TryGetValue(id, out var defE))
							{
								switch (defE.Name.LocalName.ToLower())
								{
									case "lineargradient":
										fillDefs[id] = ReadLinearGradient(defE);
										fillId = id;
										break;
									case "radialgradient":
										fillDefs[id] = ReadRadialGradient(defE);
										fillId = id;
										break;
									default:
										LogOrThrow($"Unsupported fill: {fill}");
										break;
								}
							}
							else
							{
								LogOrThrow($"Invalid fill url reference: {id}");
							}
						}
						else
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

		private SKStrokeCap ReadLineCap(string strokeLineCap, SKStrokeCap def = SKStrokeCap.Butt)
		{
			switch (strokeLineCap)
			{
				case "butt":
					return SKStrokeCap.Butt;
				case "round":
					return SKStrokeCap.Round;
				case "square":
					return SKStrokeCap.Square;
			}

			return def;
		}

		private SKStrokeJoin ReadLineJoin(string strokeLineJoin, SKStrokeJoin def = SKStrokeJoin.Miter)
		{
			switch (strokeLineJoin)
			{
				case "miter":
					return SKStrokeJoin.Miter;
				case "round":
					return SKStrokeJoin.Round;
				case "bevel":
					return SKStrokeJoin.Bevel;
			}

			return def;
		}

		private SKPaint CreatePaint(bool stroke = false)
		{
			var strokePaint = new SKPaint
			{
				IsAntialias = true,
				IsStroke = stroke,
				Color = stroke ? SKColors.Transparent : SKColors.Black
			};

			if (stroke)
			{
				strokePaint.StrokeWidth = 1f;
				strokePaint.StrokeMiter = 4f;
				strokePaint.StrokeJoin = SKStrokeJoin.Miter;
				strokePaint.StrokeCap = SKStrokeCap.Butt;
			}

			return strokePaint;
		}

		private SKMatrix ReadTransform(string raw, SKPoint xy = default)
		{
			var t = SKMatrix.MakeIdentity();

			if (xy != default && (string.IsNullOrWhiteSpace(raw) || !raw.Contains("translate")))
			{
				var m = SKMatrix.MakeTranslation(xy.X, xy.Y);
				SKMatrix.Concat(ref t, t, m);
			}

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
							nt = SKMatrix.MakeTranslation(ReadNumber(args[1]) + xy.X, ReadNumber(args[2]) + xy.Y);
						}
						else if (args.Length >= 2)
						{
							nt = SKMatrix.MakeTranslation(ReadNumber(args[1]) + xy.X, xy.Y);
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

		private SKPath ReadClipPath(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return null;
			}

			SKPath result = null;
			var read = false;
			var urlM = urlRe.Match(raw);
			if (urlM.Success)
			{
				var id = urlM.Groups[1].Value.Trim();

				if (defs.TryGetValue(id, out XElement defE))
				{
					result = ReadClipPathDefinition(defE);
					if (result != null)
					{
						read = true;
					}
				}
				else
				{
					LogOrThrow($"Invalid clip-path url reference: {id}");
				}
			}

			if (!read)
			{
				LogOrThrow($"Unsupported clip-path: {raw}");
			}

			return result;
		}

		private SKPath ReadClipPathDefinition(XElement e)
		{
			if (e.Name.LocalName != "clipPath" || !e.HasElements)
			{
				return null;
			}

			var result = new SKPath();

			foreach (var ce in e.Elements())
			{
				var el = ce;

				if (ce.Name.LocalName == "use")
				{
					el = ReadHref(ce);
				}

				var path = ReadElement(el);
				if (path != null)
				{
					result.AddPath(path);
				}
				else
				{
					LogOrThrow($"SVG element '{ce.Name.LocalName}' is not supported in clipPath.");
				}
			}

			return result;
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

		private float ReadBaselineShift(XElement element)
		{
			string value = null;
			if (element != null)
			{
				var attrib = element.Attribute("baseline-shift");
				if (attrib != null && !string.IsNullOrWhiteSpace(attrib.Value))
					value = attrib.Value;
				else
				{
					var style = element.Attribute("style");
					if (style != null && !string.IsNullOrWhiteSpace(style.Value))
					{
						value = GetString(ReadStyle(style.Value), "baseline-shift");
					}
				}
			}

			return ReadNumber(value);
		}

		private SKRadialGradient ReadRadialGradient(XElement e)
		{
			var center = new SKPoint(
				ReadNumber(e.Attribute("cx"), 0.5f),
				ReadNumber(e.Attribute("cy"), 0.5f));
			var radius = ReadNumber(e.Attribute("r"), 0.5f);

			//var focusX = ReadOptionalNumber(e.Attribute("fx")) ?? centerX;
			//var focusY = ReadOptionalNumber(e.Attribute("fy")) ?? centerY;
			//var absolute = e.Attribute("gradientUnits")?.Value == "userSpaceOnUse";

			var tileMode = ReadSpreadMethod(e);
			var stops = ReadStops(e);
			var matrix = ReadTransform(e.Attribute("gradientTransform")?.Value ?? string.Empty);

			// TODO: use absolute
			return new SKRadialGradient(center, radius, stops.Keys.ToArray(), stops.Values.ToArray(), tileMode, matrix);
		}

		private SKLinearGradient ReadLinearGradient(XElement e)
		{
			var start = new SKPoint(
				ReadNumber(e.Attribute("x1"), 0f),
				ReadNumber(e.Attribute("y1"), 0f));
			var end = new SKPoint(
				ReadNumber(e.Attribute("x2"), 1f),
				ReadNumber(e.Attribute("y2"), 0f));

			//var absolute = e.Attribute("gradientUnits")?.Value == "userSpaceOnUse";
			var tileMode = ReadSpreadMethod(e);
			var stops = ReadStops(e);
			var matrix = ReadTransform(e.Attribute("gradientTransform")?.Value ?? string.Empty);

			// TODO: use absolute
			return new SKLinearGradient(start, end, stops.Keys.ToArray(), stops.Values.ToArray(), tileMode, matrix);
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
			var href = ReadHrefString(e)?.Substring(1);

			if (!string.IsNullOrEmpty(href) && defs.TryGetValue(href, out var child))
			{
				return new XElement(child);
			}

			return null;
		}

		private static string ReadHrefString(XElement e)
		{
			return (e.Attribute("href") ?? e.Attribute(xlink + "href"))?.Value;
		}

		private SortedDictionary<float, SKColor> ReadStops(XElement e)
		{
			var stops = new SortedDictionary<float, SKColor>();
			var ns = e.Name.Namespace;
			foreach (var se in e.Elements(ns + "stop"))
			{
				var style = ReadStyle(se);

				float offset = 0;
				var color = SKColors.Black;
				byte alpha = 255;

				if (style.TryGetValue("offset", out string offsetValue))
				{
					offset = ReadNumber(offsetValue);
				}

				if (style.TryGetValue("stop-color", out string stopColor))
				{
					ColorHelper.TryParse(stopColor, out color);
				}

				if (style.TryGetValue("stop-opacity", out string stopOpacity))
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
			if (style != null && style.TryGetValue(key, out string strValue))
			{
				value = ReadNumber(strValue);
			}
			return value;
		}

		private byte[] ReadUriBytes(string uri)
		{
			if (!string.IsNullOrEmpty(uri))
			{
				var offset = uri.IndexOf(",");
				if (offset != -1 && offset - 1 < uri.Length)
				{
					uri = uri.Substring(offset + 1);
					return Convert.FromBase64String(uri);
				}
			}

			return null;
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

			if (!float.TryParse(s, NumberStyles.Float, icult, out float v))
			{
				v = 0;
			}

			return m * v;
		}

		private float ReadNumber(XAttribute a, float defaultValue) =>
			a == null ? defaultValue : ReadNumber(a.Value);

		private float ReadNumber(XAttribute a) =>
			ReadNumber(a?.Value);

		private float? ReadOptionalNumber(XAttribute a) =>
			a == null ? (float?)null : ReadNumber(a.Value);

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
	}
}
