namespace FunWithHtml.renderer;

using FunWithHtml.html.TreeBuilder;
using SkiaSharp;
using System.Diagnostics;
using System.Globalization;

public class Styles: List<Block>;

public class Renderer {

    private const int width = 800;
    private const int height = 600;
    private SKImageInfo info;

    private Styles styles;
    private Document document;
    private Element body;

    public Renderer(Styles styles, Document document) {
        this.styles = styles;
        this.document = document;
        body = GetBody(document) ?? throw new InvalidOperationException();
        info = new SKImageInfo(width, height);
    }

    private static Element? GetBody(Document document) {
        var stack = new Stack<Node>();
        foreach (var child in Enumerable.Reverse(document.childNodes)) {
            stack.Push(child);
        }

        while (stack.Count > 0) {
            var node = stack.Pop();
            if (node is Element { localName: "body" } body) return body;
            foreach (var child in Enumerable.Reverse(node.childNodes)) {
                stack.Push(child);
            }
        }
        return null;
    }

    public void Render(string filePath) {
        var body = BuildStyleNode(this.body) switch {
            LayoutElementNode rootElement => rootElement,
            _ => throw new InvalidOperationException("Root element must be a LayoutElementNode."),
        };
        body.width = width;
        body.height = height;

        SetValueDeep(body);
        LayoutChildNodes(body);
        using var surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        PaintChildNodes(body, canvas);

        Save(surface, filePath);
    }

    private static LayoutNode? BuildStyleNode(Node element, LayoutElementNode? parent = null) {
        if (element is Text textNode) {
            return new LayoutTextNode(textNode) {
                parent = parent
            };
        } else if (element is Element elementNode) {
            var layoutNode = new LayoutElementNode() {
                element = elementNode,
                parent = parent,
            };

            layoutNode.childNodes = [.. element.childNodes
                .Where(elem => elem is Text or Element)
                .Select(node => BuildStyleNode(node, layoutNode))
                .OfType<LayoutNode>()
            ];

            return layoutNode;
        } else {
            return null;
        }
    }

    private static void PaintChildNodes(LayoutElementNode node, SKCanvas canvas) {
        foreach (var child in node.childNodes) {
            PaintNodes(child, canvas);
        }
    }

    private static void LayoutChildNodes(LayoutElementNode node) {
        foreach (var child in node.childNodes) {
            LayoutNodes(child);
        }
    }

    private void SetValueDeep(LayoutElementNode node) {
        SetValues(node, styles);
        foreach (var child in node.childNodes.OfType<LayoutElementNode>()) {
            SetValueDeep(child);
        }
    }

    private static void LayoutNodes(LayoutNode node) {
        if (node is LayoutElementNode layoutElementNode) {
            LayoutElementNodes(layoutElementNode);
        } else if (node is LayoutTextNode layoutTextNode) {

        }
    }

    private static void LayoutElementNodes(LayoutElementNode node, LayoutElementNode? prevNode = null) {
        // 4.1.1    Vertical formatting
        // todo negative margins
        if (prevNode is not null) {
            node.rect.Top = prevNode.rect.Bottom + Math.Max(prevNode.margin.bottom, node.margin.top);
        } else if (node.parent is not null) {
            if (node.parent.padding.top == 0 && node.parent.border.top == 0) { // margin-collapse
                node.rect.Top = node.parent.rect.Top;
            } else {
                node.rect.Top = node.parent.rect.Top + node.parent.padding.top + node.margin.top;
            }
        } else {
            node.rect.Top = node.margin.top;
        }

        node.rect.Top += node.border.top;

        var ChildWidth = 0.0f;
        var ChildHeight = 0.0f;

        // get margin from child if margins are collapsed
        if (node.padding.top == 0 && node.border.top == 0 && node.childNodes.Count > 0) {
            var firstRenderedChild = node.childNodes.Find((node) => node is LayoutElementNode || (node is LayoutTextNode tn && tn.rect.Height > 0));
            if (firstRenderedChild is LayoutElementNode leNode) {
                var maxMargin = Math.Max(leNode.margin.top, node.margin.top);
                node.rect.Top += maxMargin;
                ChildHeight -= leNode.margin.top;
            }
        }

        if (node.parent is not null) {
            node.rect.Left = node.parent.rect.Left + node.parent.padding.left + node.margin.left;
        } else {
            node.rect.Left = node.margin.left;
        }

        if (node.width is null && node.parent is not null) {
            node.width = node.parent.Width
                 - node.parent.padding.left - node.parent.padding.right
                 - node.parent.border.left - node.parent.border.right
                 - node.padding.left - node.padding.right
                 - node.border.left - node.border.right
                 - node.margin.left - node.margin.right;
        }

        LayoutElementNode? prev = null;
        foreach (var child in node.childNodes) {
            if (child is LayoutElementNode layoutElementNode) {
                LayoutElementNodes(layoutElementNode, prev);
                ChildWidth = Math.Max(ChildWidth,
                     layoutElementNode.rect.Width + layoutElementNode.margin.left + layoutElementNode.margin.right);
                ChildHeight +=
                    (prev is null ? layoutElementNode.margin.top :
                        Math.Max(prev.margin.bottom, layoutElementNode.margin.top) - prev.margin.bottom)
                         + layoutElementNode.rect.Height + layoutElementNode.margin.bottom;
                prev = layoutElementNode;
            } else if (child is LayoutTextNode layoutTextNode) {
                LayoutTextNodes(layoutTextNode);
                ChildHeight += layoutTextNode.rect.Height;
            }
        }

        node.rect.Right = node.rect.Left + node.Width;
        node.rect.Bottom = node.rect.Top + node.padding.top + ChildHeight + node.padding.bottom;
    }


    private static void LayoutTextNodes(LayoutTextNode node) {
        if (string.IsNullOrWhiteSpace(node.text.data)) {
            return;
        }
        var height = node.parent.lineHeight * node.parent.fontSize;
        node.rect.Top = node.parent.rect.Top + node.parent.padding.top;
        node.rect.Left = node.parent.rect.Left + node.parent.padding.left;
        node.rect.Right = node.parent.rect.Right - node.parent.padding.right;
        node.rect.Bottom = node.rect.Top + height;
    }

    private static void PaintNodes(LayoutNode node, SKCanvas canvas) {
        if (node is LayoutElementNode layoutElementNode) {
            PaintElementNodes(layoutElementNode, canvas);
        } else if (node is LayoutTextNode layoutTextNode) {
            PaintTextNode(layoutTextNode, canvas);
        }
    }

    private static void PaintTextNode(LayoutTextNode node, SKCanvas canvas) {
        Debug.Assert(node.parent != null, "LayoutTextNodes should always have a parent node");

        var textPaint = new SKPaint {
            Color = node.parent.color,
            IsAntialias = true,
        };
        var font = new SKFont {
            Size = node.parent.fontSize,
            Typeface = SKTypeface.FromFamilyName(node.parent.fontFamily),
        };

        canvas.DrawText(
            node.text.data, node.rect.Left, node.rect.Bottom,
            SKTextAlign.Left, font, textPaint);
    }

    private static void PaintElementNodes(LayoutElementNode node, SKCanvas canvas) {
        using (var paint = new SKPaint()) {
            paint.Style = SKPaintStyle.Fill;
            paint.Color = node.Background;
            paint.IsAntialias = true;
            canvas.DrawRect(node.rect, paint);

            if (node.border.top > 0 || node.border.right > 0 || node.border.bottom > 0 || node.border.left > 0) {
                var borderRect = node.rect;
                paint.Color = node.borderColor ?? node.CurrentColor; // todo this should not be done here
                borderRect.Top -= node.border.top;
                borderRect.Right += node.border.right;
                borderRect.Bottom += node.border.bottom;
                borderRect.Left -= node.border.left;
                var inner = new SKRoundRect(node.rect);
                var border = new SKRoundRect(borderRect);
                canvas.DrawRoundRectDifference(border, inner, paint);
            }

        }

        foreach (var child in node.childNodes) {
            PaintNodes(child, canvas);
        }
    }


    private static void SetFloatValue(string value, Action<float> setter) {
        if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue)) {
            setter(floatValue);
        }
    }

    private static void SetIntValue(string value, Action<int> setter) {
        if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue)) {
            setter(intValue);
        }
    }


    private static void SetColorValue(string value, Action<SKColor> setter) {
        if (SKColor.TryParse(value, out SKColor color)) {
            setter(color);
        }
    }

    private static void SetAllPaddings(LayoutElementNode node, float value) => node.padding.top = node.padding.right = node.padding.bottom = node.padding.left = value;
    private static void SetAllMargins(LayoutElementNode node, float value) => node.margin.top = node.margin.right = node.margin.bottom = node.margin.left = value;
    private static void SetAllBorders(LayoutElementNode node, int value) => node.border.top = node.border.right = node.border.bottom = node.border.left = value;

    private static readonly Dictionary<string, Action<string, LayoutElementNode>> StyleHandlers = new() {
        {"background", (value, node) => SetColorValue(value, color => node.Background = color) },
        {"color", (value, node) => SetColorValue(value, color => node.color = color) },
        {"padding", (value, node) => SetFloatValue(value, padding => SetAllPaddings(node, padding)) },
        {"padding-top", (value, node) => SetFloatValue(value, padding => node.padding.top = padding) },
        {"margin", (value, node) => SetFloatValue(value, margin => SetAllMargins(node, margin)) },
        {"margin-top", (value, node) => SetFloatValue(value, margin => node.margin.top = margin) },
        {"border", (value, node) => SetIntValue(value, border => SetAllBorders(node, border)) },
        {"border-top", (value, node) => SetIntValue(value, border => node.border.top = border) },
        {"border-color", (value, node) => SetColorValue(value, color => node.borderColor = color) },
    };

    private static void SetValues(LayoutElementNode node, Styles styles) {
        if (node.element is null) return;

        foreach (var block in styles.Where(block => node.element.localName == block.name)) {
            foreach (var line in block.value) {
                if (StyleHandlers.TryGetValue(line.name, out var handler)) {
                    handler(line.value, node);
                }
            }
        }
    }

    private static void Save(SKSurface surface, string filePath) {
        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);
    }
}

public class Block {
    public string name = "";
    public List<Line> value = [];
}

public class Line(string name, string value) {
    public string name = name;
    public string value = value;
}


public class LayoutNode {

    public LayoutElementNode? parent = null;
}

public class LayoutTextNode(Text text): LayoutNode {
    public Text text = text;
    public SKRect rect = new();
}

public class LayoutElementNode: LayoutNode {
    public Element? element = null;

    public List<LayoutNode> childNodes = [];

    public float lineHeight = 1;
    public string fontFamily = "Arial";
    public float fontSize = 16;
    public SKColor color;
    public SKColor Background = SKColor.Empty;
    public SKRect rect = new();
    public Margin margin = new();
    public Padding padding = new();
    public Border border = new();
    public SKColor? borderColor;

    public float? width = null;
    public float? height = null;

    public float Width { get => (width ?? 0) + padding.left + padding.right + border.left + border.right; }

    public SKColor CurrentColor { get => color; }
}

public class Margin {
    public float top = 0;
    public float right = 0;
    public float bottom = 0;
    public float left = 0;
}

public class Padding: Margin { }
public class Border: Margin { }

// 5.3

// color: ColorValue
// background-color : ColorValue | transparent
// background-image
// background-repeat
// background-attachment
// background-position
// background

// 5.4
// word-spacing
// letter-spacing
// text-decoration
// vertical-align
// text-transform
// text-align
// text-ident
// line-height

// 5.5
// margin-top: <length> | <percentage> | auto -- Percentage values: refer to width of the closest block-level ancestor
// margin-right
// margin-bottom
// margin-left
// margin 
// padding-top: <length> | <percentage>
// padding-right
// padding-bottom
// padding-left
// padding
// border-top-width: thin | medium | thick | <length>
// border-color
// border-style: none | dotted | dashed | solid | double | groove | ridge | inset | outset
// width // <length> | <percentage> | auto
// height

// float
// clear

// 5.6
// display: block | inline | list-item | none
// white-space
// ..

// 6
// units em px

// 6.2 percent units

// 6.3 color - rgb #hex name