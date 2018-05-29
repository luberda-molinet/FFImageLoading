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
        private static readonly Regex urlRe = new Regex(@"url\s*\(\s*#([^\)]+)\)");
        private static readonly Regex keyValueRe = new Regex(@"\s*([\w-]+)\s*:\s*(.*)");
        private static readonly Regex WSRe = new Regex(@"\s{2,}");

        private readonly Dictionary<string, XElement> defs = new Dictionary<string, XElement>();
        private readonly Dictionary<string, SKSvgMask> masks = new Dictionary<string, SKSvgMask>();
        private readonly Dictionary<string, object> fills = new Dictionary<string, object>();
        private readonly Dictionary<string, string> styles = new Dictionary<string, string>();
        private readonly XmlReaderSettings xmlReaderSettings = new XmlReaderSettings()
        {
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = true,
        };

        #if PORTABLE
                // basically use reflection to try and find a method that supports a 
        // file path AND a XmlParserContext...
        private static readonly MethodInfo createReaderMethod;

        static SKSvg()
        {
            // try and find `Create(string, XmlReaderSettings, XmlParserContext)`
            createReaderMethod = typeof(XmlReader).GetRuntimeMethod(
                nameof(XmlReader.Create),
                new[] { typeof(string), typeof(XmlReaderSettings), typeof(XmlParserContext) });
        }
#endif

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
            var args = new object[] { filename, xmlReaderSettings, CreateSvgXmlContext() };
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
            using (var reader = XmlReader.Create(stream, xmlReaderSettings, CreateSvgXmlContext()))
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

        private void ReadDefsStyles(XElement root)
        {
            var defs = root.Descendants().FirstOrDefault(x => x.Name.LocalName == "defs");
            if (defs == null)
                return;
            foreach (var style in defs.Descendants().Where(x => x.Name.LocalName == "style"))
            {
                foreach (var st in CssHelpers.ParseSelectors(style.Value))
                    styles[st.Key] = st.Value;
            }
        }

        private SKPicture Load(XDocument xdoc)
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

            ReadDefsStyles(svg);

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

                // read style
                SKPaint stroke = null;
                SKPaint fill = CreatePaint();
                var style = ReadPaints(svg, ref stroke, ref fill, true);

                // read elements
                LoadElements(svg.Elements(), canvas, stroke, fill);

                Picture = recorder.EndRecording();
            }

            return Picture;
        }

        private void LoadElements(IEnumerable<XElement> elements, SKCanvas canvas, SKPaint stroke, SKPaint fill)
        {
            foreach (var e in elements)
            {
                ReadElement(e, canvas, stroke, fill);
            }
        }

        private void ReadElement(XElement e, SKCanvas canvas, SKPaint stroke, SKPaint fill)
        {
            if (e.Attribute("display")?.Value == "none")
                return;

            // transform matrix
            var transform = ReadTransform(e.Attribute("transform")?.Value ?? string.Empty);
            canvas.Save();
            canvas.Concat(ref transform);

            // clip-path
            var clipPath = ReadClipPath(e.Attribute("clip-path")?.Value ?? string.Empty);
            if (clipPath != null)
            {
                canvas.ClipPath(clipPath);
            }

            // SVG element
            var elementName = e.Name.LocalName;
            var isGroup = elementName == "g";

            // read style
            var style = ReadPaints(e, ref stroke, ref fill, isGroup);

            // read mask
            var mask = ReadMask(style);

            // parse elements
            switch (elementName)
            {
                case "style":
                    {
                        foreach (var st in CssHelpers.ParseSelectors(e.Value))
                            styles[st.Key] = st.Value;

                        break;
                    }
                case "image":
                    {
                        var image = ReadImage(e);
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
                        break;
                    }
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
                    {                  
                        var elementPath = ReadElement(e);
                        if (elementPath == null)
                            break;

                        string fillId = e.Attribute("fill")?.Value;
                        object addFill = null;
                        if (!string.IsNullOrWhiteSpace(fillId) && fills.TryGetValue(fillId, out addFill))
                        {
                            var x = ReadNumber(e.Attribute("x"));
                            var y = ReadNumber(e.Attribute("y"));

                            float width = 0f;
                            float height = 0f;
                            var element = e;

                            while (element.Parent != null)
                            {
                                if (!(width > 0f))
                                    width = ReadNumber(element.Attribute("width"));

                                if (!(height > 0f))
                                    height = ReadNumber(element.Attribute("height"));

                                if (width > 0f && height > 0f)
                                    break;

                                element = element.Parent;
                            }

                            if (!(width > 0f && height > 0f))
                            {
                                var root = e?.Document?.Root;
                                width = ReadNumber(root?.Attribute("width"));
                                height = ReadNumber(root?.Attribute("height"));
                            }

                            var addFillType = addFill.GetType();

                            if (addFillType == typeof(SKLinearGradient))
                            {
                                var gradient = (SKLinearGradient)addFill;
                                var startPoint = gradient.GetStartPoint(x, y, width, height);
                                var endPoint = gradient.GetEndPoint(x, y, width, height);

                                var gradientShader = SKShader.CreateLinearGradient(
                                    startPoint,
                                    endPoint,
                                    gradient.Colors,
                                    gradient.Positions,
                                    gradient.TileMode);

                                var gradientPaint = new SKPaint() { Shader = gradientShader, IsAntialias = true, BlendMode = SKBlendMode.SrcOver };

                                canvas.DrawPath(elementPath, gradientPaint);
                                gradientShader.TryDispose();
                                gradientPaint.TryDispose();
                            }
                            else if (addFillType == typeof(SKRadialGradient))
                            {
                                var gradient = (SKRadialGradient)addFill;
                                var centerPoint = gradient.GetCenterPoint(x, y, width, height);
                                var radius = gradient.GetRadius(width, height);

                                var gradientShader = SKShader.CreateRadialGradient(
                                    centerPoint,
                                    radius,
                                    gradient.Colors,
                                    gradient.Positions,
                                    gradient.TileMode);

                                var gradientPaint = new SKPaint() { Shader = gradientShader, IsAntialias = true };

                                canvas.DrawPath(elementPath, gradientPaint);
                                gradientShader.TryDispose();
                                gradientPaint.TryDispose();
                            }
                        }

                        if (mask != null)
                        {
                            canvas.SaveLayer(new SKPaint());
                            foreach (var gElement in mask.Element.Elements())
                            {
                                ReadElement(gElement, canvas, mask.Fill.Clone(), mask.Fill.Clone());
                            }
                            using (var paint = fill.Clone())
                            {
                                paint.BlendMode = SKBlendMode.SrcIn;
                                canvas.DrawPath(elementPath, paint);
                            }
                            canvas.Restore();
                        }
                        else
                        {
                            if (fill != null)
                                canvas.DrawPath(elementPath, fill);                        
                            if (stroke != null)
                                canvas.DrawPath(elementPath, stroke);
                        }
                        break;
                    }
                case "g":
                    if (e.HasElements)
                    {
                        if (mask != null)
                        {
                            canvas.SaveLayer(new SKPaint());
                            foreach (var gElement in mask.Element.Elements())
                            {
                                ReadElement(gElement, canvas, mask.Fill.Clone(), mask.Fill.Clone());
                            }

                            foreach (var gElement in e.Elements())
                            {
                                using (var paint = fill.Clone())
                                {
                                    paint.BlendMode = SKBlendMode.SrcIn;
                                    ReadElement(gElement, canvas, paint, paint);
                                }
                            }
                            canvas.Restore();
                        }
                        else
                        {
                            SKPaint opacityPaint = null;
                            // get current group opacity
                            float groupOpacity = ReadOpacity(style);
                            if (groupOpacity != 1.0f)
                            {
                                var opacity = (byte)(255 * groupOpacity);
                                opacityPaint = new SKPaint { Color = SKColors.Black.WithAlpha(opacity) };
                                canvas.SaveLayer(opacityPaint);
                            }
                            foreach (var gElement in e.Elements())
                            {
                                ReadElement(gElement, canvas, stroke?.Clone(), fill?.Clone());
                            }
                            if (opacityPaint != null)
                            {
                                canvas.Restore();
                            }
                        }
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
                case "mask":
                    if (e.HasElements)
                    {
                        masks.Add(ReadId(e), new SKSvgMask(fill, e));
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

        private SKSvgImage ReadImage(XElement e)
        {
            var x = ReadNumber(e.Attribute("x"));
            var y = ReadNumber(e.Attribute("y"));
            var width = ReadNumber(e.Attribute("width"));
            var height = ReadNumber(e.Attribute("height"));
            var rect = SKRect.Create(x, y, width, height);

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

        private SKPath ReadElement(XElement e)
        {
            var path = new SKPath();

            var elementName = e.Name.LocalName;
            switch (elementName)
            {
                case "rect":
                    var rect = ReadRoundedRect(e);
                    if (rect.IsRounded)
                        path.AddRoundedRect(rect.Rect, rect.RadiusX, rect.RadiusY);
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
                    var d = e.Attribute("d")?.Value;
                    if (!string.IsNullOrWhiteSpace(d))
                    {
                        path.Dispose();
                        path = SKPath.ParseSvgPathData(d);
                    }
                    break;
                case "polygon":
                case "polyline":
                    var close = elementName == "polygon";
                    var p = e.Attribute("points")?.Value;
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        p = "M" + p;
                        if (close)
                            p += " Z";
                        path.Dispose();
                        path = SKPath.ParseSvgPathData(p);
                    }
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
            var x = ReadNumber(e.Attribute("x"));
            var y = ReadNumber(e.Attribute("y"));
            var width = ReadNumber(e.Attribute("width"));
            var height = ReadNumber(e.Attribute("height"));
            var rx = ReadOptionalNumber(e.Attribute("rx"));
            var ry = ReadOptionalNumber(e.Attribute("ry"));
            var rect = SKRect.Create(x, y, width, height);

            return new SKRoundedRect(rect, rx ?? ry ?? 0, ry ?? rx ?? 0);
        }

        private SKText ReadText(XElement e, SKPaint stroke, SKPaint fill)
        {
            // TODO: stroke

            var x = ReadNumber(e.Attribute("x"));
            var y = ReadNumber(e.Attribute("y"));
            var xy = new SKPoint(x, y);
            var textAlign = ReadTextAlignment(e);
            var baselineShift = ReadBaselineShift(e);

            ReadFontAttributes(e, fill);

            return ReadTextSpans(e, xy, textAlign, baselineShift, stroke, fill);
        }

        private SKText ReadTextSpans(XElement e, SKPoint xy, SKTextAlign textAlign, float baselineShift, SKPaint stroke, SKPaint fill)
        {
            var spans = new SKText(xy, textAlign);

            // textAlign is used for all spans within the <text> element. If different textAligns would be needed, it is necessary to use
            // several <text> elements instead of <tspan> elements
            var currentBaselineShift = baselineShift;
            fill.TextAlign = SKTextAlign.Left;  // fixed alignment for all spans

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

                        spans.Append(new SKTextSpan(text, fill.Clone(), baselineShift: currentBaselineShift));
                    }
                }
                else if (c.NodeType == XmlNodeType.Element)
                {
                    var ce = (XElement)c;
                    if (ce.Name.LocalName == "tspan")
                    {
                        // the current span may want to change the cursor position
                        var x = ReadOptionalNumber(ce.Attribute("x"));
                        var y = ReadOptionalNumber(ce.Attribute("y"));
                        var text = ce.Value; //.Trim();

                        var spanFill = fill.Clone();
                        ReadFontAttributes(ce, spanFill);

                        // Don't read text-anchor from tspans!, Only use enclosing text-anchor from text element!
                        currentBaselineShift = ReadBaselineShift(ce);

                        spans.Append(new SKTextSpan(text, spanFill, x, y, currentBaselineShift));
                    }
                }
            }

            return spans;
        }

        private void ReadFontAttributes(XElement e, SKPaint paint)
        {
            var fontStyle = ReadStyle(e);

            if (!fontStyle.TryGetValue("font-family", out string ffamily) || string.IsNullOrWhiteSpace(ffamily))
                ffamily = paint.Typeface?.FamilyName;
            var fweight = ReadFontWeight(fontStyle, paint.Typeface?.FontWeight ?? (int)SKFontStyleWeight.Normal);
            var fwidth = ReadFontWidth(fontStyle, paint.Typeface?.FontWidth ?? (int)SKFontStyleWidth.Normal);
            var fstyle = ReadFontStyle(fontStyle, paint.Typeface?.FontSlant ?? SKFontStyleSlant.Upright);

            paint.Typeface = SKTypeface.FromFamilyName(ffamily, fweight, fwidth, fstyle);

            if (fontStyle.TryGetValue("font-size", out string fsize) && !string.IsNullOrWhiteSpace(fsize))
                paint.TextSize = ReadNumber(fsize);
        }

        private static SKFontStyleSlant ReadFontStyle(Dictionary<string, string> fontStyle, SKFontStyleSlant defaultStyle = SKFontStyleSlant.Upright)
        {
            SKFontStyleSlant style = defaultStyle;

            if (fontStyle.TryGetValue("font-style", out string fstyle) && !string.IsNullOrWhiteSpace(fstyle))
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
            var width = defaultWidth;
            if (fontStyle.TryGetValue("font-stretch", out string fwidth) && !string.IsNullOrWhiteSpace(fwidth) && !int.TryParse(fwidth, out width))
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

        private SKSvgMask ReadMask(Dictionary<string, string> style)
        {
            SKSvgMask mask = null;
            var maskID = GetString(style, "mask").Trim();
            if (!String.IsNullOrEmpty(maskID))
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

        private int ReadFontWeight(Dictionary<string, string> fontStyle, int defaultWeight = (int)SKFontStyleWeight.Normal)
        {
            var weight = defaultWeight;

            if (fontStyle.TryGetValue("font-weight", out string fweight) && !string.IsNullOrWhiteSpace(fweight) && !int.TryParse(fweight, out weight))
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
            if (style.TryGetValue(name, out string v))
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
            var dic = e.Attributes().Where(a => HasSvgNamespace(a.Name)).ToDictionary(k => k.Name.LocalName, v => v.Value);

            string className;
            string glStyle;

            if (styles != null && styles.TryGetValue(e.Name.LocalName, out glStyle))
            {
                // get from stlye attribute
                var styleDic = ReadStyle(glStyle);

                // overwrite
                foreach (var pair in styleDic)
                    dic[pair.Key] = pair.Value;
            }
            if (styles != null && dic.TryGetValue("class", out className)
                && styles.TryGetValue("." + className, out glStyle))
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

                    if (ColorHelper.TryParse(stroke, out SKColor color))
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
                    if (hasStrokeDashArray || hasStrokeWidth || hasStrokeOpacity
                        || hasStrokeLineCap || hasStrokeLineJoin)
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

                if (hasStrokeWidth)
                {
                    if (strokePaint == null)
                        strokePaint = CreatePaint(true);
                    strokePaint.StrokeWidth = ReadNumber(strokeWidth);
                }
                else if (strokePaint != null)
                {
                    strokePaint.StrokeWidth = 1f;
                }

                if (hasStrokeOpacity)
                {
                    if (strokePaint == null)
                        strokePaint = CreatePaint(true);
                    strokePaint.Color = strokePaint.Color.WithAlpha((byte)(ReadNumber(strokeOpacity) * 255));
                }

                if (hasStrokeLineCap)
                {
                    switch (strokeLineCap)
                    {
                        case "butt":
                            strokePaint.StrokeCap = SKStrokeCap.Butt;
                            break;
                        case "round":
                            strokePaint.StrokeCap = SKStrokeCap.Round;
                            break;
                        case "square":
                            strokePaint.StrokeCap = SKStrokeCap.Square;
                            break;
                    }
                }

                if (hasStrokeLineJoin)
                {
                    switch (strokeLineJoin)
                    {
                        case "miter":
                            strokePaint.StrokeJoin = SKStrokeJoin.Miter;
                            break;
                        case "round":
                            strokePaint.StrokeJoin = SKStrokeJoin.Round;
                            break;
                        case "bevel":
                            strokePaint.StrokeJoin = SKStrokeJoin.Bevel;
                            break;
                    }
                }

                if (hasStrokeMiterLimit)
                {
                    strokePaint.StrokeMiter = ReadNumber(strokeMiterLimit);
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

                    if (ColorHelper.TryParse(fill, out SKColor color))
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
                        var urlM = urlRe.Match(fill);
                        if (urlM.Success)
                        {
                            var id = urlM.Groups[1].Value.Trim();

                            if (defs.TryGetValue(id, out XElement defE))
                            {
                                switch (defE.Name.LocalName)
                                {
                                    case "linearGradient":
                                        fillPaint.Color = SKColors.Transparent;
                                        if (!fills.ContainsKey(fill))
                                            fills.Add(fill, ReadLinearGradient(defE));
                                        read = true;
                                        break;
                                    case "radialGradient":
                                        fillPaint.Color = SKColors.Transparent;
                                        if (!fills.ContainsKey(fill))
                                            fills.Add(fill, ReadRadialGradient(defE));
                                        read = true;
                                        break;
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
            var strokePaint = new SKPaint
            {
                IsAntialias = true,
                IsStroke = stroke,
                Color = SKColors.Black
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
                var path = ReadElement(ce);
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
            var cx = e.Attribute("cx");
            var cy = e.Attribute("cy");
            var centerX = cx == null ? 0.5f : ReadNumber(cx);
            var centerY = cy == null ? 0.5f : ReadNumber(cy);
            //var focusX = ReadOptionalNumber(e.Attribute("fx")) ?? centerX;
            //var focusY = ReadOptionalNumber(e.Attribute("fy")) ?? centerY;
            var r = e.Attribute("r");
            var radius = r == null ? 0.5f : ReadNumber(r);
            //var absolute = e.Attribute("gradientUnits")?.Value == "userSpaceOnUse";
            var tileMode = ReadSpreadMethod(e);
            var stops = ReadStops(e);

            // TODO: check gradientTransform attribute
            // TODO: use absolute

            return new SKRadialGradient(centerX, centerY, radius, stops.Keys.ToArray(), stops.Values.ToArray(), tileMode);
        }

        private SKLinearGradient ReadLinearGradient(XElement e)
        {
            var startX = ReadNumber(e.Attribute("x1"));
            var startY = ReadNumber(e.Attribute("y1"));

            var x2 = e.Attribute("x2");

            float endX = x2 == null ? 1f : ReadNumber(x2);
            float endY = ReadNumber(e.Attribute("y2"));

            //var absolute = e.Attribute("gradientUnits")?.Value == "userSpaceOnUse";
            var tileMode = ReadSpreadMethod(e);
            var stops = ReadStops(e);

            // TODO: check gradientTransform attribute
            // TODO: use absolute

            return new SKLinearGradient(startX, startY, endX, endY, stops.Keys.ToArray(), stops.Values.ToArray(), tileMode);
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
            if (string.IsNullOrEmpty(href) || !defs.TryGetValue(href, out XElement child))
            {
                child = null;
            }
            return child;
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

                var offset = ReadNumber(style["offset"]);
                var color = SKColors.Transparent;
                byte alpha = 255;

                if (style.TryGetValue("stop-color", out string stopColor))
                {
                    // preserve alpha
                    if (ColorHelper.TryParse(stopColor, out color))
                        alpha = color.Alpha;
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
            if (style.TryGetValue(key, out string strValue))
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
    }
}
