namespace FunWithHtml.renderer;

using FunWithHtml.html.TreeBuilder;
using SkiaSharp;
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
        var body = BuildStyleNode(this.body);
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

    private static LayoutNode BuildStyleNode(Element element, LayoutNode? parent = null) {
        var layoutNode = new LayoutNode() {
            element = element,
            parent = parent,
        };
        layoutNode.childNodes = [.. element.childNodes
            .OfType<Element>()
            .Select(node => BuildStyleNode(node, layoutNode))];
        return layoutNode;
    }

    private static void PaintChildNodes(LayoutNode node, SKCanvas canvas) {
        foreach (var child in node.childNodes) {
            PaintNodes(child, canvas);
        }
    }

    private static void LayoutChildNodes(LayoutNode node) {
        foreach (var child in node.childNodes) {
            LayoutNodes(child);
        }
    }

    private void SetValueDeep(LayoutNode node) {
        SetValues(node, styles);
        foreach (var child in node.childNodes) {
            SetValueDeep(child);
        }
    }

    private static void LayoutNodes(LayoutNode node, LayoutNode? prevNode = null) {
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

        // get margin from child if margins are collapsed
        if (node.padding.top == 0 && node.border.top == 0 && node.childNodes.Count > 0) {
            node.rect.Top += node.childNodes[0].margin.top;
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

        var ChildWidth = 0.0f;
        var ChildHeight = 0.0f;
        LayoutNode? prev = null;
        foreach (var child in node.childNodes) {
            LayoutNodes(child, prev);
            ChildWidth = Math.Max(ChildWidth, child.rect.Width + child.margin.left + child.margin.right);
            ChildHeight += (prev is null ? child.margin.top : Math.Max(prev.margin.bottom, child.margin.top) - prev.margin.bottom) + child.rect.Height + child.margin.bottom;
            prev = child;
        }

        if (node.padding.top == 0 && node.border.top == 0 && node.childNodes.Count > 0) {
            ChildHeight -= node.childNodes[0].margin.top; // margin-collapse
        }

        node.rect.Right = node.rect.Left + node.Width;
        node.rect.Bottom = node.rect.Top + node.padding.top + ChildHeight + node.padding.bottom;
    }

    private static void PaintNodes(LayoutNode node, SKCanvas canvas) {
        using (var paint = new SKPaint()) {
            paint.Style = SKPaintStyle.Fill;
            paint.Color = node.Background;
            paint.IsAntialias = true;
            canvas.DrawRect(node.rect, paint);

            if (node.border.top > 0 || node.border.right > 0 || node.border.bottom > 0 || node.border.left > 0) {
                var borderRect = node.rect;
                paint.Color = node.borderColor;
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

    private static void SetValues(LayoutNode node, Styles styles) {
        if (node.element is null) return;
        foreach (var block in styles) {
            if (node.element.localName == block.name) {
                foreach (var line in block.value) {
                    switch (line.name) {
                        case "background":
                            SKColor color;
                            if (SKColor.TryParse(line.value, out color)) {
                                node.Background = color;
                            }
                            break;
                        case "color":
                            if (SKColor.TryParse(line.value, out color)) {
                                node.color = color;
                            }
                            break;
                        case "padding":
                            if (float.TryParse(line.value, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatValue)) {
                                node.padding.top = node.padding.right = node.padding.bottom = node.padding.left = floatValue;
                            }
                            break;
                        case "padding-top":
                            if (float.TryParse(line.value, NumberStyles.Any, CultureInfo.InvariantCulture, out floatValue)) {
                                node.padding.top = floatValue;
                            }
                            break;
                        case "margin":
                            if (float.TryParse(line.value, NumberStyles.Any, CultureInfo.InvariantCulture, out floatValue)) {
                                node.margin.top = node.margin.right = node.margin.bottom = node.margin.left = floatValue;
                            }
                            break;
                        case "margin-top":
                            if (float.TryParse(line.value, NumberStyles.Any, CultureInfo.InvariantCulture, out floatValue)) {
                                node.margin.top = floatValue;
                            }
                            break;
                        case "border":
                            if (int.TryParse(line.value, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue)) {
                                node.border.top = node.border.right = node.border.bottom = node.border.left = intValue;
                            }
                            break;
                        case "border-top":
                            if (int.TryParse(line.value, NumberStyles.Any, CultureInfo.InvariantCulture, out intValue)) {
                                node.border.top = intValue;
                            }
                            break;
                        case "border-color":
                            if (SKColor.TryParse(line.value, out color)) {
                                node.borderColor = color;
                            }
                            break;
                    }
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


public class LayoutNode() {
    public Element? element = null;

    public List<LayoutNode> childNodes = [];
    public LayoutNode? parent = null;


    public SKColor color;
    public SKColor Background = SKColor.Empty;
    public SKRect rect = new();
    public Margin margin = new();
    public Padding padding = new();
    public Border border = new();
    public SKColor borderColor = SKColors.Black; // todo if border color is not set use currentColor 

    public float? width = null;
    public float? height = null;

    public float Width { get => (width ?? 0) + padding.left + padding.right + border.left + border.right; }
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