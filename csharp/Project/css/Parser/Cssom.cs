namespace FunWithHTML.css.Parser;

using FunWithHtml.html.TreeBuilder;
using FunWithHtml.css.Parser;

using USVString = String; // todo
using DOMString = String;
// https://drafts.csswg.org/cssom-1/#cssomstring-type
using CSSOMString = String;
using FunWithHTML.misc;
using OneOf;
using System.Text;

public class Promise<T>(T value) {
    readonly T value = value;
}



// https://drafts.csswg.org/cssom-1/#the-stylesheet-interface
partial class CSSStyleSheet: StyleSheet {
    public static partial CSSStyleSheet Create(CSSStyleSheetInit? options = null);
    public CSSRule? ownerRule { get; private set; }
    public partial CSSRuleList cssRules { get; internal set; }
    public partial ulong insertRule(CSSOMString rule, ulong index = 0);
    public partial void deleteRule(ulong index);

    public partial Promise<CSSStyleSheet> replace(USVString text);
    public partial void replaceSync(USVString text);
};

// https://drafts.csswg.org/cssom-1/#legacy-css-style-sheet-members

partial class CSSStyleSheet {
    [Obsolete]
    public partial CSSRuleList rules { get; }
    [Obsolete]
    public partial long addRule(DOMString selector = "undefined", DOMString block = "undefined", ulong? optionalIndex = null);
    [Obsolete]
    public partial void removeRule(ulong index = 0);
}



// https://drafts.csswg.org/cssom-1/#css-style-sheets
public partial class CSSStyleSheet: StyleSheet {


    public string type => "text/css";
    public string? location { get; private set; }

    public CSSStyleSheet? parentStyleSheet { get; private set; }

    public Element? ownerNode { get; private set; }
    // todo media

    // todo If this property is specified to an attribute of the owner node, the title must be set to the value of that attribute. Whenever the attribute is set, changed or removed, the title must be set to the new value of the attribute, or to the empty string if the attribute is absent.
    public string? title { get; private set; }

    public bool alternate { get; private set; }
    // todo The disabled attribute, on getting, must return true if the disabled flag is set, or false otherwise. On setting, the disabled attribute must set the disabled flag if the new value is true, or unset the disabled flag otherwise.
    public bool disabled { get; private set; }
    private CSSRuleList _cssRules = new CSSRuleList();
    public partial CSSRuleList cssRules {
        get {
            // If the origin-clean flag is unset, throw a SecurityError exception.
            if (originClean is false) throw new SecurityError();
            // Return a read-only, live CSSRuleList object representing the CSS rules.
            return _cssRules;
            // Note: Even though the returned CSSRuleList object is read-only (from the perspective of client-authored script), 
            // it can nevertheless change over time due to its liveness status. For example, invoking the insertRule() 
            // or deleteRule() methods can result in mutations reflected in the returned object.            
        }
        internal set { _cssRules = value; }
    }
    public bool originClean { get; private set; }
    public bool constructed { get; private set; }
    public bool disallowModification { get; private set; }
    public Document? document { get; private set; }
    public DOMString? stylesheetBaseUrl;
    public string? href => location;

    private CSSStyleSheet() { }

    public static partial CSSStyleSheet Create(CSSStyleSheetInit? options = null) {
        // 1. Construct a new CSSStyleSheet object sheet.
        var sheet = new CSSStyleSheet() {
            // 2. Set sheet’s location to the base URL of the associated Document for the current global object.
            // todo
            location = null,
            // 3. Set sheet’s stylesheet base URL to the baseURL attribute value from options.
            stylesheetBaseUrl = options?.baseURL,
            // 4. Set sheet’s parent CSS style sheet to null.
            parentStyleSheet = null,
            // 5. Set sheet’s owner node to null.
            ownerNode = null,
            // 6. Set sheet’s owner CSS rule to null.
            ownerRule = null,
            // 7. Set sheet’s title to the the empty string.
            title = "",
            // 8. Unset sheet’s alternate flag.
            alternate = false,
            // 9. Set sheet’s origin-clean flag.
            originClean = true,
            // 10. Set sheet’s constructed flag.
            constructed = true,
            // 11. Set sheet’s Constructor document to the associated Document for the current global object.
            // todo
            document = null,
            // 12. If the media attribute of options is a string, create a MediaList object from the string and assign it as sheet’s media.
            // Otherwise, serialize a media query list from the attribute and then create a MediaList object from the resulting string and set it as sheet’s media.
            // todo
            // 13. If the disabled attribute of options is true, set sheet’s disabled flag.
            disabled = options?.disabled ?? false,

        };
        // 14. Return sheet.
        return sheet;
    }

    // https://drafts.csswg.org/cssom-1/#dom-cssstylesheet-insertrule
    public partial ulong insertRule(CSSOMString rule, ulong index) {
        // 1. If the origin-clean flag is unset, throw a SecurityError exception.
        if (!originClean) throw new SecurityError();
        // 2. If the disallow modification flag is set, throw a NotAllowedError DOMException.
        if (disallowModification) throw new NotAllowedError();
        // 3. Let parsed rule be the return value of invoking parse a rule with rule.
        var parsedRule = Parser.ParseARule(rule);
        // 4. If parsed rule is a syntax error, throw a SyntaxError DOMException.
        if (parsedRule.IsT1) throw parsedRule.AsT1;
        // 5. If parsed rule is an @import rule, and the constructed flag is set, throw a SyntaxError DOMException.
        // todo
        // 6. Return the result of invoking insert a CSS rule rule in the CSS rules at index.
        return CSSRule.InsertACssRule(rule, cssRules, index);
    }

    // https://drafts.csswg.org/cssom-1/#dom-cssstylesheet-deleterule
    public partial void deleteRule(ulong index) {
        // 1. If the origin-clean flag is unset, throw a SecurityError exception.
        if (!originClean) throw new SecurityError();
        // 2. If the disallow modification flag is set, throw a NotAllowedError DOMException.
        if (disallowModification) throw new NotAllowedError();
        // 3. Remove a CSS rule in the CSS rules at index.       
        CSSRule.RemoveACssRule(cssRules, index);
    }

    // https://drafts.csswg.org/cssom-1/#dom-cssstylesheet-replace
    public partial Promise<CSSStyleSheet> replace(string text) {
        // 1. Let promise be a promise.
        var promise = new Promise<CSSStyleSheet>(this);
        // 2. If the constructed flag is not set, or the disallow modification flag is set, reject promise with a NotAllowedError DOMException and return promise.
        if (!constructed || disallowModification) {
            // todo reject
            return promise;
        }
        // 3. Set the disallow modification flag.
        disallowModification = true;
        // 4. In parallel, do these steps: // todo
        // 1. Let rules be the result of running parse a stylesheet’s contents from text.
        var rules = Parser.ParseAStylesheetsContents(text);
        // 2. If rules contains one or more @import rules, remove those rules from rules.
        // todo
        // 3. Set sheet’s CSS rules to rules.
        cssRules = rules;
        // 4. Unset sheet’s disallow modification flag.
        disallowModification = false;
        // 5. Resolve promise with sheet.
        // todo
        // 5. Return promise.
        return promise;
    }

    // https://drafts.csswg.org/cssom-1/#dom-cssstylesheet-replacesync
    public partial void replaceSync(string text) {
        // 1. If the constructed flag is not set, or the disallow modification flag is set, throw a NotAllowedError DOMException.
        if (!constructed || disallowModification) throw new NotAllowedError();
        // 2. Let rules be the result of running parse a stylesheet’s contents from text.
        var rules = Parser.ParseAStylesheetsContents(text);
        // 3 If rules contains one or more @import rules, remove those rules from rules.
        // todo
        cssRules = rules;
    }

    // https://drafts.csswg.org/cssom-1/#legacy-css-style-sheet-members
    public partial CSSRuleList rules => cssRules;
    public partial void removeRule(ulong index) => deleteRule(index);
    public partial long addRule(DOMString selector, DOMString block, ulong? optionalIndex) {
        // Let rule be an empty string.
        var rule = "";
        // Append selector to rule.
        rule += selector;
        // Append " { " to rule.
        rule += " { ";
        // If block is not empty, append block, followed by a space, to rule.
        if (!string.IsNullOrEmpty(block)) {
            rule += block + " ";
        }
        // Append "}" to rule
        rule += "}";
        // Let index be optionalIndex if provided, or the number of CSS rules in the stylesheet otherwise.
        var index = optionalIndex ?? cssRules.length;
        // Call insertRule(), with rule and index as arguments.
        insertRule(rule, index);
        // Return -1.
        return -1;
    }

}

// https://drafts.csswg.org/cssom-1/#the-stylesheet-interface
public interface StyleSheet {
    CSSOMString type { get; }
    USVString? href { get; }
    Element? ownerNode { get; } // todo ProcessingInstruction
    CSSStyleSheet? parentStyleSheet { get; }
    DOMString? title { get; }
    // readonly MediaList media; // todo
    bool disabled { get; }
}


public struct CSSStyleSheetInit() {
    public DOMString? baseURL = null;
    //   (MediaList or DOMString) media = ""; // todo
    public bool disabled = false;
};


// https://drafts.csswg.org/cssom-1/#the-cssrulelist-interface
public partial class CSSRuleList: List<CSSRule> {
    public partial CSSRule? item(ulong index);
    public partial ulong length { get; }
};

public partial class CSSRuleList {
    public partial ulong length => (ulong)Count;

    public partial CSSRule? item(ulong index) {
        return this[(int)index];
    }
}

// https://drafts.csswg.org/cssom-1/#the-cssrule-interface
public partial class CSSRule {
    public CSSOMString cssText { get; set; }
    public CSSRule? parentRule { get; internal set; }
    public CSSStyleSheet? parentStyleSheet { get; internal set; }

    // the following attribute and constants are historical
    [Obsolete]
    public partial ushort type { get; }
    [Obsolete]
    public const ushort STYLE_RULE = 1;
    [Obsolete]
    public const ushort CHARSET_RULE = 2;
    [Obsolete]
    public const ushort IMPORT_RULE = 3;
    [Obsolete]
    public const ushort MEDIA_RULE = 4;
    [Obsolete]
    public const ushort FONT_FACE_RULE = 5;
    [Obsolete]
    public const ushort PAGE_RULE = 6;
    [Obsolete]
    public const ushort MARGIN_RULE = 9;
    [Obsolete]
    public const ushort NAMESPACE_RULE = 10;
};

public partial class CSSRule {
    // private string text { get; set; }

    // public string cssText { get => throw new NotImplementedException(); set { } }

    // private ICSSRule? _parentRule;
    // public ICSSRule? parentRule { get => _parentRule; set { if (value is null) _parentRule = value; } }

    // private ICSSStyleSheet? _parentStyleSheet;
    // public ICSSStyleSheet? parentStyleSheet { get => _parentStyleSheet; set { if (value is null) _parentStyleSheet = value; } }

    // private ICSSRuleList childCSSRules;

    public partial ushort type {
        get {
            return this switch {
                CSSStyleRule => 1,
                CSSImportRule => 3,
                // CSSMediaRule => 4, // todo
                // CSSFontFaceRule => 5, // todo
                CSSPageRule => 6,
                // CSSKeyframesRule => 7, // todo                
                // CSSKeyFrameRule => 8, // todo
                CSSMarginRule => 9,
                CSSNamespaceRule => 10,
                // CSSCounterStyleRule => 11,
                // CSSSupportsRule => 12,
                // CSSFontFeatureValuesRule => 14, // todo
                _ => 0,
            };
        }
    }

    // https://drafts.csswg.org/cssom-1/#parse-a-css-rule
    public static OneOf<Rule, SyntaxError> ParseACssRule(string input) {
        // 1. Let rule be the return value of invoking parse a rule with string.
        var rule = Parser.ParseARule(input);
        // 2. If rule is a syntax error, return rule.
        if (rule.IsT1) return rule;
        // 3. Let parsed rule be the result of parsing rule according to the appropriate CSS specifications,
        // dropping parts that are said to be ignored. If the whole style rule is dropped, return a syntax error.
        // todo fixme
        // 4. Return parsed rule.
        return rule;
    }

    // https://drafts.csswg.org/cssom-1/#insert-a-css-rule
    public static ulong InsertACssRule(string rule, CSSRuleList list, ulong index, bool nested = false) { // todo nested 
        // 1. Set length to the number of items in list.
        var length = list.length;
        // 2. If index is greater than length, then throw an IndexSizeError exception.
        if (index > length) throw new IndexSizeError();
        // 3. Set new rule to the results of performing parse a CSS rule on argument rule.
        var newRule = ParseACssRule(rule);
        // 4. If new rule is a syntax error, and nested is set, perform the following substeps:
        if (newRule.IsT1 && nested) {
            // Set declarations to the results of performing parse a CSS declaration block, on argument rule.
            var declaration = CSSStyleDeclaration.ParseACssDeclarationBlock(rule);
            // If declarations is empty, throw a SyntaxError exception.
            if (declaration.Count == 0) throw new SyntaxError();
            // Otherwise, set new rule to a new nested declarations rule with declarations as it contents.            
            // todo
        }
        // 5. If new rule is a syntax error, throw a SyntaxError exception.
        if (newRule.IsT1) throw newRule.AsT1;
        // 6. If new rule cannot be inserted into list at the zero-indexed position index due to constraints specified by CSS, then throw a HierarchyRequestError exception. [CSS21]
        // todo
        // Note: For example, a CSS style sheet cannot contain an @import at-rule after a style rule.
        // 7. If new rule is an @namespace at-rule, and list contains anything other than @import at-rules, and @namespace at-rules, throw an InvalidStateError exception.
        // todo
        // 8. Insert new rule into list at the zero-indexed position index.
        list.Insert((int)index, newRule.AsT0);
        // 9. Return index.
        return index;
    }

    // https://drafts.csswg.org/cssom-1/#remove-a-css-rule
    public static void RemoveACssRule(CSSRuleList list, ulong index) {
        // 1. Set length to the number of items in list.
        var length = list.length;
        // 2. If index is greater than or equal to length, then throw an IndexSizeError exception.
        if (index >= length) throw new IndexSizeError();
        // 3. Set old rule to the indexth item in list.
        var oldRule = list[(int)index];
        // 4. If old rule is an @namespace at-rule, and list contains anything other than @import at-rules, and @namespace at-rules, throw an InvalidStateError exception.
        // todo
        // 5. Remove rule old rule from list at the zero-indexed position index.
        list.RemoveAt((int)index);
        // 6. Set old rule’s parent CSS rule and parent CSS style sheet to null.
        oldRule.parentRule = null;
        oldRule.parentStyleSheet = null;
    }
}

// https://drafts.csswg.org/cssom-1/#the-cssstylerule-interface
public partial class CSSStyleRule: CSSGroupingRule {

    public CSSOMString selectorText { get; set; }
    public CSSStyleProperties style { get; }
}

// https://drafts.csswg.org/cssom-1/#the-cssimportrule-interface
public partial class CSSImportRule: CSSRule {
    public USVString href { get; }
    // public MediaList media; // todo
    public CSSStyleSheet? styleSheet { get; }
    public CSSOMString? layerName { get; }
    public CSSOMString? supportsText { get; }
}

// https://drafts.csswg.org/cssom-1/#the-cssgroupingrule-interface
public partial class CSSGroupingRule: CSSRule {
    public CSSRuleList cssRules { get; }
    public partial long insertRule(CSSOMString rule, ulong index = 0);
}

public partial class CSSGroupingRule {
    public partial long insertRule(CSSOMString rule, ulong index) => throw new NotImplementedException();
}

// https://drafts.csswg.org/cssom-1/#the-cssmediarule-interface
// todo
// https://drafts.csswg.org/cssom-1/#the-csspagerule-interface

public partial class CSSPageDescriptors: CSSStyleDeclaration {
    // todo
}

public partial class CSSPageRule: CSSGroupingRule {
    public CSSOMString selectorText { get; set; }
    // todo put forwards
    public CSSPageDescriptors style;
}

// https://drafts.csswg.org/cssom-1/#the-cssmarginrule-interface
public class CSSMarginDescriptors {
    // todo
}
public partial class CSSMarginRule: CSSRule {
    public CSSOMString name { get; }
    public CSSMarginDescriptors style;
}

// https://drafts.csswg.org/cssom-1/#the-cssnamespacerule-interface
public partial class CSSNamespaceRule: CSSRule {
    public CSSOMString namespaceURI { get; }
    public CSSOMString prefix { get; }
}

// https://drafts.csswg.org/cssom-1/#css-declarations

public class Declaration {
    public string propertyName { get; set; }
    public List<ComponentValue> value { get; set; }
    public bool important = false;
    public bool caseSensitive = false;
}

public partial class CSSStyleDeclaration {
    public bool computed = false;
    public List<Declaration> declarations = [];
    public CSSRule? parentCssRule = null;
    public Element? OwnerNode = null;
    public bool updating = false;
    public partial CSSOMString item(ulong index) => throw new NotImplementedException();
    public partial CSSOMString getPropertyValue(CSSOMString property) => throw new NotImplementedException();
    public partial CSSOMString getPropertyPriority(CSSOMString property) => throw new NotImplementedException();
    public partial void setProperty(CSSOMString property, CSSOMString value, CSSOMString priority) => throw new NotImplementedException();
    public partial CSSOMString removeProperty(CSSOMString property) => throw new NotImplementedException();

    // https://drafts.csswg.org/cssom-1/#parse-a-css-declaration-block
    public static List<Declaration> ParseACssDeclarationBlock(string @string) {
        // 1. Let declarations be the returned declarations from invoking parse a block’s contents with string.
        var declarations = Parser.ParseABlocksContents(@string);
        // 2. Let parsed declarations be a new empty list.
        List<Declaration> parsedDeclarations = [];
        // 3. For each item declaration in declarations, follow these substeps:   
        foreach (var declaration in declarations) {
            // 1. Let parsed declaration be the result of parsing declaration according to the appropriate CSS specifications, dropping parts that are said to be ignored.
            // If the whole declaration is dropped, let parsed declaration be null.
            Declaration? parsedDeclaration = null; // todo fixme
            // 2. If parsed declaration is not null, append it to parsed declarations.
            if (parsedDeclaration is not null) {
                parsedDeclarations.Add(parsedDeclaration);
            }
        }
        // 4. Return parsed declarations.
        return parsedDeclarations;

    }

}

// https://drafts.csswg.org/cssom-1/#the-cssstyledeclaration-interface
public partial class CSSStyleDeclaration {
    public CSSOMString cssText { get; set; }
    public ulong legth { get; }
    public partial CSSOMString item(ulong index);
    public partial CSSOMString getPropertyValue(CSSOMString property);
    public partial CSSOMString getPropertyPriority(CSSOMString property);
    public partial void setProperty(CSSOMString property, CSSOMString value, CSSOMString priority = "");
    public partial CSSOMString removeProperty(CSSOMString property);
    public CSSRule? parentRule { get; }
}

public partial class CSSStyleProperties: CSSStyleDeclaration {
    public CSSOMString cssFloat { get; set; }
}


// https://drafts.csswg.org/cssom/#common-serializing-idioms
public static class CommonSerializingIdioms {
    // https://drafts.csswg.org/cssom/#escape-a-character
    public static string EscapeACharacter(char character) {
        // To escape a character means to create a string of "\" (U+005C), followed by the character.
        return $"\\{character}";
    }
    // https://drafts.csswg.org/cssom/#escape-a-character-as-code-point
    public static string EscapeACharacterAsCodePoint(char character) {
        throw new NotImplementedException();
    }

    // https://drafts.csswg.org/cssom/#serialize-an-identifier
    public static string SerializeAnIdentifier(string identifier) {
        // To serialize an identifier means to create a string represented by the concatenation of, for each character of the identifier:
        var sb = new StringBuilder();
        for (var index = 0; index < identifier.Length; index++) {
            var character = identifier[index];
            sb.Append(character switch {
                // If the character is NULL (U+0000), then the REPLACEMENT CHARACTER (U+FFFD).
                '\0' => '\uFFFD',
                // If the character is in the range [\1-\1f] (U+0001 to U+001F) or is U+007F, then the character escaped as code point.
                (>= '\u0001' and <= '\u001F') or '\u007F' => EscapeACharacterAsCodePoint(character),
                // If the character is the first character and is in the range [0-9] (U+0030 to U+0039), then the character escaped as code point.
                >= '0' and <= '9' when index == 0 => EscapeACharacterAsCodePoint(character),
                // If the character is the second character and is in the range [0-9] (U+0030 to U+0039) and the first character is a "-" (U+002D), then the character escaped as code point.
                >= '0' and <= '9' when index == 1 && identifier[0] == '-' => EscapeACharacterAsCodePoint(character),
                // If the character is the first character and is a "-" (U+002D), and there is no second character, then the escaped character.
                '-' when index == 1 && identifier.Length == 1 => EscapeACharacter(character),
                // If the character is not handled by one of the above rules and is greater than or equal to U+0080, is "-" (U+002D) or "_" (U+005F), or is in one of the ranges [0-9] (U+0030 to U+0039),
                //  [A-Z] (U+0041 to U+005A), or [a-z] (U+0061 to U+007A), then the character itself.
                >= '\u0080' or '-' or '_' or (>= '0' and <= '9') or (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') => character,
                // Otherwise, the escaped character.
                _ => EscapeACharacter(character),
            });
        }
        return sb.ToString();
    }

    // https://drafts.csswg.org/cssom/#serialize-a-string
    public static string SerializeAString(string @string) {
        var sb = new StringBuilder();
        // To serialize a string means to create a string represented by '"' (U+0022), followed by the result of applying the rules below to each character of the given string, followed by '"' (U+0022):
        sb.Append('"');
        for (var index = 0; index < @string.Length; index++) {
            var character = @string[index];
            // If the character is NULL (U+0000), then the REPLACEMENT CHARACTER (U+FFFD).
            sb.Append(character switch {
                // If the character is in the range [\1-\1f] (U+0001 to U+001F) or is U+007F, the character escaped as code point.
                (>= '\u0001' and <= '\u001F') or '\u007F' => EscapeACharacterAsCodePoint(character),
                // If the character is '"' (U+0022) or "\" (U+005C), the escaped character.
                '"' or '\\' => EscapeACharacter(character),
                // Otherwise, the character itself.
                _ => character,
            });
        }
        // Note: "'" (U+0027) is not escaped because strings are always serialized with '"' (U+0022).
        sb.Append('"');
        return sb.ToString();
    }


    // https://drafts.csswg.org/cssom/#serialize-a-comma-separated-list
    public static string SerializeACommaSeparatedList(IEnumerable<string?> values) {
        return string.Join(", ", values);
    }

}