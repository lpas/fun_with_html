namespace FunWithHtml.renderer;

using FunWithHtml.css;
using FunWithHtml.css.Tokenizer;
using FunWithHtml.html.TreeBuilder;
using SkiaSharp;
using System.Diagnostics;
using System.Globalization;
using System.Text;

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
        LayoutNodes(body);
        using var surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        PaintChildNodes(body, canvas);

        Save(surface, filePath);
    }

    private LayoutNode? BuildStyleNode(Node element, LayoutElementNode? parent = null) {
        if (element is Text textNode) {
            return new LayoutTextNode(textNode) {
                parent = parent
            };
        } else if (element is Element elementNode) {
            var layoutNode = new LayoutElementNode() {
                element = elementNode,
                parent = parent,
            };
            SetValues(layoutNode, styles);

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

    private static bool LayoutNodes(LayoutNode node) {
        if (node is LayoutElementNode layoutElementNode) {
            return LayoutElementNodes(layoutElementNode);
        } else if (node is LayoutTextNode layoutTextNode) {
            return LayoutTextNodes(layoutTextNode);
        }
        return false;
    }

    private static bool LayoutElementNodes(LayoutElementNode node) {
        SetDefaultValues(node);
        // 4.1.1    Vertical formatting
        // todo negative margins
        switch (node.display) {
            case Display.None: return false;
            case Display.Block:
                if (node.prev is null) {
                    if (node.parent is null) {
                        node.rect.Top = node.margin.top;
                    } else {
                        node.rect.Top = node.parent.rect.Top + node.parent.padding.top + node.margin.top;
                        if (node.parent.border.top == 0 && node.parent.padding.top == 0) { // margin-collapse with parent
                            if (node.parent.margin.top > node.margin.top) {
                                node.rect.Top -= node.margin.top;
                            } else {
                                node.rect.Top -= node.parent.margin.top;
                                node.parent.rect.Top += node.margin.top - node.parent.margin.top;
                            }
                        }
                    }
                } else {
                    // margin-collapse with prev child
                    node.rect.Top = node.prev.rect.Bottom + Math.Max(node.margin.top, (node.prev is LayoutElementNode leNode) ? leNode.margin.bottom : 0);
                }
                node.rect.Top += node.border.top;

                node.rect.Left = (node.parent != null ? (node.parent.rect.Left + node.parent.padding.left) : 0) + node.margin.left;

                if (node.width is null && node.parent is not null) {
                    node.width = node.parent.InnerWidth
                         - node.padding.left - node.padding.right
                         - node.border.left - node.border.right
                         - node.margin.left - node.margin.right;
                }
                node.rect.Right = node.rect.Left + node.Width;
                break;
            default:
                throw new NotImplementedException();
        }

        var bottom = 0.0f;
        LayoutNode? prev = null;
        foreach (var child in node.childNodes) {
            child.prev = prev;
            if (!LayoutNodes(child)) continue;
            prev = child;
            bottom = bottom = Math.Max(bottom, child.Bottom);
        }

        node.rect.Bottom = bottom + node.padding.bottom;
        return true;
    }

    private static void SetDefaultValues(LayoutElementNode node) {
        if (node.element is Element { localName: "body" }) {
            node.width = width;
            node.height = height;
        }
    }

    private static bool LayoutTextNodes(LayoutTextNode node) {
        if (string.IsNullOrWhiteSpace(node.text.data)) {
            return false;
        }
        node.rect.Top = node.parent.rect.Top + node.parent.padding.top;
        node.rect.Left = node.parent.rect.Left + node.parent.padding.left;
        node.rect.Right = node.parent.rect.Right - node.parent.padding.right;
        // todo don't create so many fonts cache them 
        var font = new SKFont {
            Size = node.parent.fontSize,
            Typeface = SKTypeface.FromFamilyName(node.parent.fontFamily),
        };
        // 
        var width = node.rect.Width;
        var words = node.text.data.Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var lines = node.textBoxes;
        var currentLine = new StringBuilder();
        var spaceWidth = font.MeasureText(" ");
        var fontSpacing = font.Spacing;
        var lineCount = 0;
        var currentLineWidth = .0f;
        foreach (var word in words) {
            var nextWordWidth = font.MeasureText(word);

            if (currentLineWidth + spaceWidth + nextWordWidth > width) {
                lines.Add((new SKPoint(node.rect.Left, node.rect.Top + fontSpacing * ++lineCount),
                    currentLine.ToString().Trim()));
                currentLine.Clear();
                currentLineWidth = 0;
            }
            currentLine.Append(word + " ");
            currentLineWidth += nextWordWidth + spaceWidth;
        }
        lines.Add((new SKPoint(node.rect.Left, node.rect.Top + fontSpacing * ++lineCount),
            currentLine.ToString().Trim()));
        node.rect.Bottom = lines[^1].Item1.Y;

        return true;
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
        if (node.rect.Width == 0) return;
        var textPaint = new SKPaint {
            Color = node.parent.color,
            IsAntialias = true,
        };
        var font = new SKFont {
            Size = node.parent.fontSize,
            Typeface = SKTypeface.FromFamilyName(node.parent.fontFamily),
        };

        foreach (var (point, text) in node.textBoxes) {
            canvas.DrawText(text, point, font, textPaint);
        }
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

    private static readonly Dictionary<string, Action<List<Token>, PreStyles>> PreStyleHandlers = new() {
        {"background-color", (tokens, preStyles) => PreStyles.SetCssColorValue(tokens, color => preStyles.BackgroundColor = color) },
        {"color", (tokens, preStyles) => PreStyles.SetCssColorValue(tokens, color => preStyles.color = color) },
        {"display", (tokens, preStyles) => PreStyles.DisplaySetter(tokens, display => preStyles.display = display)},

        {"line-height", (tokens, preStyles) => PreStyles.SetCssLineHeight(tokens, v => preStyles.lineHeight = v) },
        {"font-family", (tokens, preStyles) => PreStyles.SetCssFontFamily(tokens, v => preStyles.fontFamily = v) },
        {"font-size", (tokens, preStyles) => PreStyles.SetCssFontSize(tokens, v => preStyles.fontSize = v) },

        { "margin-top", (tokens, preStyles) => PreStyles.MarginSetter(tokens, (v) => preStyles.margin.top = v)},
        { "margin-right", (tokens, preStyles) => PreStyles.MarginSetter(tokens, (v) => preStyles.margin.right = v)},
        { "margin-bottom", (tokens, preStyles) => PreStyles.MarginSetter(tokens, (v) => preStyles.margin.bottom = v)},
        { "margin-left", (tokens, preStyles) => PreStyles.MarginSetter(tokens, (v) => preStyles.margin.left = v)},

        { "padding-top", (tokens, preStyles) => PreStyles.PaddingSetter(tokens, (v) => preStyles.padding.top = v)},
        { "padding-right", (tokens, preStyles) => PreStyles.PaddingSetter(tokens, (v) => preStyles.padding.right = v)},
        { "padding-bottom", (tokens, preStyles) => PreStyles.PaddingSetter(tokens, (v) => preStyles.padding.bottom = v)},
        { "padding-left", (tokens, preStyles) => PreStyles.PaddingSetter(tokens, (v) => preStyles.padding.left = v)},

        { "border-width-top", (tokens, preStyles) => PreStyles.BorderWidthSetter(tokens, (v) => preStyles.borderWidth.top = v)},
        { "border-width-right", (tokens, preStyles) => PreStyles.BorderWidthSetter(tokens, (v) => preStyles.borderWidth.right = v)},
        { "border-width-bottom", (tokens, preStyles) => PreStyles.BorderWidthSetter(tokens, (v) => preStyles.borderWidth.bottom = v)},
        { "border-width-left", (tokens, preStyles) => PreStyles.BorderWidthSetter(tokens, (v) => preStyles.borderWidth.left = v)},
    };

    private static void SetValues(LayoutElementNode node, Styles styles) {
        if (node.element is null) return;

        var preStyle = new PreStyles();
        // collecting values from styles
        foreach (var block in styles.Where(block => node.element.localName == block.name)) {
            foreach (var line in block.value) {
                if (PreStyleHandlers.TryGetValue(line.name, out var preHandler)) {
                    var tokenizer = new Tokenizer(line.value);
                    var tokens = tokenizer.GetTokenList();
                    preHandler(tokens, preStyle);
                }
            }
        }
        // calculating & inheritance
        node.fontFamily = PreStyles.getFontFamilyValue(preStyle.fontFamily, node);
        node.fontSize = PreStyles.getFontSizeValue(preStyle.fontSize, node);
        node.lineHeight = PreStyles.getLineHeightValue(preStyle.lineHeight, node);
        node.color = new SKColor(preStyle.color?.value ?? 0); // 0 is default here
        node.Background = preStyle.BackgroundColor != null ? new SKColor(preStyle.BackgroundColor.value) : SKColor.Empty;
        node.rect = new();
        node.margin = new() {
            top = PreStyles.GetMarginValue(preStyle.margin.top, node),
            right = PreStyles.GetMarginValue(preStyle.margin.right, node),
            bottom = PreStyles.GetMarginValue(preStyle.margin.bottom, node),
            left = PreStyles.GetMarginValue(preStyle.margin.left, node)
        };
        node.padding = new() {
            top = PreStyles.GetPaddingValue(preStyle.padding.top, node),
            right = PreStyles.GetPaddingValue(preStyle.padding.right, node),
            bottom = PreStyles.GetPaddingValue(preStyle.padding.bottom, node),
            left = PreStyles.GetPaddingValue(preStyle.padding.left, node)
        };
        node.border = new() {
            top = PreStyles.GetBorderValue(preStyle.borderWidth.top, node),
            right = PreStyles.GetBorderValue(preStyle.borderWidth.right, node),
            bottom = PreStyles.GetBorderValue(preStyle.borderWidth.bottom, node),
            left = PreStyles.GetBorderValue(preStyle.borderWidth.left, node)
        };
        node.display = PreStyles.GetDisplayValue(preStyle.display, node);

        node.borderColor = node.color; // todo this is the fallback

        node.width = null; // todo
        node.height = null; // todo

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
    public LayoutNode? prev = null;
    public SKRect rect = new();

    public virtual float Bottom { get => rect.Bottom; }
    public virtual float InnerBottom { get => rect.Bottom; }
}

public class LayoutTextNode(Text text): LayoutNode {
    public Text text = text;

    public List<(SKPoint, string)> textBoxes = [];
}

public enum Display {
    Block,
    Inline,
    ListItem,
    None,
}

public class LayoutElementNode: LayoutNode {
    public Element? element = null;

    public List<LayoutNode> childNodes = [];

    public Display display = Display.Block;
    public float lineHeight = 1;
    public string fontFamily = "Arial";
    public float fontSize = 16;
    public SKColor color;
    public SKColor Background = SKColor.Empty;
    public Margin margin = new();
    public Padding padding = new();
    public Border border = new();
    public SKColor? borderColor;

    public float? width = null;
    public float? height = null;

    public float Width { get => InnerWidth + padding.left + padding.right + border.left + border.right; }
    public float InnerWidth { get => width ?? 0; }
    public SKColor CurrentColor { get => color; }

    public override float Bottom { get => rect.Bottom + border.bottom + margin.bottom; }
    public override float InnerBottom { get => rect.Bottom - padding.bottom; }

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