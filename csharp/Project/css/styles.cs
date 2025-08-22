using FunWithHtml.css.Parser;
using FunWithHtml.css.Tokenizer;
using FunWithHtml.renderer;
using OneOf;
using SkiaSharp;

namespace FunWithHtml.css;

public enum LengthUnit {
    em,
    ex,
    px,
}

public struct CssLength(decimal value, LengthUnit unit) {
    public decimal value = value;
    public LengthUnit unit = unit;

    public override readonly string ToString() {
        return $"{value}{unit}";
    }
}

public struct CssPercent(decimal value) {
    public decimal value = value;
}

public struct AutoKeyword { }

public class PreStyles() {
    public Display? display;
    public OneOf<NormalKeyword, CssNumber, CssLength, CssPercent>? lineHeight;
    public string? fontFamily;
    public OneOf<FontSize, CssLength, CssPercent>? fontSize;

    public CssColor? color;
    public CssColor? BackgroundColor;

    public CssMargin margin = new();

    public CssPadding padding = new();

    public CssBorderWidth borderWidth = new();

    public CssColor BorderColor;
    public OneOf<CssLength, AutoKeyword> height;
    public OneOf<CssLength, CssPercent, AutoKeyword> width;

    public override string ToString() {
        return $"color: {color} BackgroundColor: {BackgroundColor} margin: {margin} padding: {padding} borderWidth: {borderWidth}";
    }


    public static void SetCssColorValue(List<ComponentValue> tokens, Action<CssColor> setter) {
        if (tokens[0] is HashToken ht) {
            if (SKColor.TryParse($"#{ht.value}", out SKColor color)) {
                setter(new CssColor((uint)color));
            }
        }
    }

    public static void SetCssLineHeight(List<ComponentValue> tokens, Action<OneOf<NormalKeyword, CssNumber, CssLength, CssPercent>?> setter) {
        if (tokens.Count == 0) return;

        OneOf<NormalKeyword, CssNumber, CssLength, CssPercent>? v = tokens[0] switch {
            DimensionToken dt => dt.unit switch {
                "px" => new CssLength(dt.value, LengthUnit.px),
                "em" => new CssLength(dt.value, LengthUnit.em),
                "ex" => new CssLength(dt.value, LengthUnit.ex),
                _ => null
            },
            PercentageToken pt => new CssPercent(pt.value),
            NumberToken nt => new CssNumber(nt.value),
            IdentToken it when it.value == "normal" => new NormalKeyword(),
            _ => null,
        };
        if (v is not null) {
            setter(v);
        }
    }

    public static void SetCssFontFamily(List<ComponentValue> tokens, Action<string?> setter) {
        if (tokens.Count == 0) return;
        string? v = tokens[0] switch {
            IdentToken it => it.value,
            _ => null,
        };

        if (v is not null) {
            setter(v);
        }
    }

    public static void SetCssFontSize(List<ComponentValue> tokens, Action<OneOf<FontSize, CssLength, CssPercent>?> setter) {
        if (tokens.Count == 0) return;
        OneOf<FontSize, CssLength, CssPercent>? v = tokens[0] switch {
            NumberToken nt when nt.value == 0 => new CssLength(0, LengthUnit.px),
            PercentageToken pt => new CssPercent(pt.value),
            DimensionToken dt => dt.unit switch {
                "px" => new CssLength(dt.value, LengthUnit.px),
                "em" => new CssLength(dt.value, LengthUnit.em),
                "ex" => new CssLength(dt.value, LengthUnit.ex),
                _ => null
            },
            IdentToken it => it.value switch {
                "xx-small" => FontSize.xxSmall,
                "x-small" => FontSize.xSmall,
                "small" => FontSize.small,
                "medium" => FontSize.medium,
                "large" => FontSize.large,
                "x-large" => FontSize.xLarge,
                "xx-large" => FontSize.xxLarge,
                "larger" => FontSize.larger,
                "smaller" => FontSize.smaller,
                _ => null,
            },
            _ => null,
        };
        if (v is not null) {
            setter(v);
        }
    }

    public static void BorderWidthSetter(List<ComponentValue> tokens, Action<OneOf<CssLength, BorderWidth>?> setter) {
        if (tokens.Count == 0) return;
        OneOf<CssLength, BorderWidth>? v = tokens[0] switch {
            NumberToken nt when nt.value == 0 => new CssLength(0, LengthUnit.px),
            DimensionToken dt => dt.unit switch {
                "px" => new CssLength(dt.value, LengthUnit.px),
                "em" => new CssLength(dt.value, LengthUnit.em),
                "ex" => new CssLength(dt.value, LengthUnit.ex),
                _ => null
            },
            IdentToken it => it.value switch {
                "thin" => BorderWidth.thin,
                "thick" => BorderWidth.thick,
                "medium" => BorderWidth.medium,
                _ => null,
            },
            _ => null,
        };
        if (v is not null) {
            setter(v);
        }
    }

    public static void MarginSetter(List<ComponentValue> tokens, Action<OneOf<CssLength, CssPercent, AutoKeyword>?> setter) {
        if (tokens.Count == 0) return;
        OneOf<CssLength, CssPercent, AutoKeyword>? v = tokens[0] switch {
            NumberToken nt when nt.value == 0 => new CssLength(0, LengthUnit.px),
            DimensionToken dt => dt.unit switch {
                "px" => new CssLength(dt.value, LengthUnit.px),
                "em" => new CssLength(dt.value, LengthUnit.em),
                "ex" => new CssLength(dt.value, LengthUnit.ex),
                _ => null
            },
            PercentageToken pt => new CssPercent(pt.value),
            IdentToken it when it.value == "auto" => new AutoKeyword(),
            _ => null,
        };
        if (v is not null) {
            setter(v);
        }
    }

    public static void PaddingSetter(List<ComponentValue> tokens, Action<OneOf<CssLength, CssPercent>?> setter) {
        if (tokens.Count == 0) return;
        OneOf<CssLength, CssPercent>? v = tokens[0] switch {
            NumberToken nt when nt.value == 0 => new CssLength(0, LengthUnit.px),
            DimensionToken dt => dt.unit switch {
                "px" => new CssLength(dt.value, LengthUnit.px),
                "em" => new CssLength(dt.value, LengthUnit.em),
                "ex" => new CssLength(dt.value, LengthUnit.ex),
                _ => null
            },
            PercentageToken pt => new CssPercent(pt.value),
            _ => null,
        };
        if (v is not null) {
            setter(v);
        }
    }

    public static void DisplaySetter(List<ComponentValue> tokens, Action<Display?> setter) {
        if (tokens.Count == 0) return;
        Display? v = tokens[0] switch {
            IdentToken it => it.value switch {
                "block" => Display.Block,
                "inline" => Display.Inline,
                "list-item" => Display.ListItem,
                "none" => Display.None,
                _ => null,
            },
            _ => null,
        };

        if (v is not null) {
            setter(v);
        }
    }

    public static Display GetDisplayValue(Display? value, LayoutElementNode node) {
        return value ?? Display.Block;
    }

    static decimal ConvertLength(CssLength length, float referenceFontSize) {
        return length.unit switch {
            LengthUnit.ex => new decimal(referenceFontSize) / .7m * length.value,
            LengthUnit.em => new decimal(referenceFontSize) * length.value,
            LengthUnit.px => length.value,
            _ => throw new InvalidOperationException(),
        };
    }

    public static float GetMarginValue(OneOf<CssLength, CssPercent, AutoKeyword>? value, LayoutElementNode node) {
        if (value is null) return 0;
        var v = value?.Match(
            length => ConvertLength(length, node.fontSize),
            percent => throw new NotImplementedException(), // todo
            auto => throw new NotImplementedException() // todo
        );
        return (float)(v ?? 0);
    }

    public static float GetPaddingValue(OneOf<CssLength, CssPercent>? value, LayoutElementNode node) {
        if (value is null) return 0;
        var v = value?.Match(
            length => ConvertLength(length, node.fontSize),
            percent => throw new NotImplementedException() // todo
        );
        return (float)(v ?? 0);
    }

    public static float GetBorderValue(OneOf<CssLength, BorderWidth>? value, LayoutElementNode node) {
        if (value is null) return 0;
        var v = value?.Match(
            length => ConvertLength(length, node.fontSize),
            BorderWidth => throw new NotImplementedException() // todo
        );
        return (float)(v ?? 0);
    }

    public static float getFontSizeValue(OneOf<FontSize, CssLength, CssPercent>? value, LayoutElementNode node) {
        if (value is null) return node.parent?.fontSize ?? 16;
        var v = value?.Match(
            fontSize => throw new NotImplementedException(),
            length => ConvertLength(length, node.parent.fontSize),
            percent => throw new NotImplementedException() // todo
        );
        return (float)(v ?? 0);
    }

    public static string getFontFamilyValue(string? value, LayoutElementNode node) {
        // todo check if valid font family and fallbacks
        if (value is null) return node.parent?.fontFamily ?? "Times New Roman";
        return value;
    }

    public static float getLineHeightValue(OneOf<NormalKeyword, CssNumber, CssLength, CssPercent>? value, LayoutElementNode node) {
        // todo for line-height number the factor gets inherited not the computed value
        if (value is null) return node.parent?.lineHeight ?? 1;
        var v = value?.Match(
            normal => 1.0m,
            number => number.value,
            length => ConvertLength(length, node.fontSize),
            percent => throw new NotImplementedException() // todo
        );
        return (float)(v ?? 0);
    }



}

public struct NormalKeyword { }
public class CssNumber(decimal value) {
    public decimal value = value;
}

public enum FontSize {
    // Absolute
    xxSmall,
    xSmall,
    small,
    medium,
    large,
    xLarge,
    xxLarge,
    // Relative
    larger,
    smaller,
}

public class TopRightBottomRightValues<T> where T : struct {
    public T? top;
    public T? right;
    public T? bottom;
    public T? left;

    public override string ToString() {
        return $"{top},{right},{bottom},{left}";
    }
}

public class CssMargin: TopRightBottomRightValues<OneOf<CssLength, CssPercent, AutoKeyword>> { }
public class CssPadding: TopRightBottomRightValues<OneOf<CssLength, CssPercent>> { }
public class CssBorderWidth: TopRightBottomRightValues<OneOf<CssLength, BorderWidth>> { }

public class CssColor(uint value) {
    public uint value = value;

    public override string ToString() {
        return $"{value}";
    }
}
// struct CssMargin: 

public enum BorderWidth {
    thin,
    medium,
    thick,
}