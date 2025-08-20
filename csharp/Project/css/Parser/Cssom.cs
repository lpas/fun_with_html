namespace FunWithHTML.css.Parser;

using FunWithHtml.html.TreeBuilder;
using FunWithHtml.css.Parser;

using USVString = String; // todo
using DOMString = String;
// https://www.w3.org/TR/cssom-1/#cssomstring-type
using CSSOMString = String;
using FunWithHTML.misc;

public class Promise<T>(T value) {
    readonly T value = value;
}


// 


// https://www.w3.org/TR/cssom-1/#css-style-sheets
public class StyleSheet: ICSSStyleSheet {


    public string type => "text/css";
    public string? location { get; private set; }

    public ICSSStyleSheet? parentStyleSheet { get; private set; }

    public Element? ownerNode { get; private set; }
    public ICSSRule? ownerRule { get; private set; }
    // todo media

    // todo If this property is specified to an attribute of the owner node, the title must be set to the value of that attribute. Whenever the attribute is set, changed or removed, the title must be set to the new value of the attribute, or to the empty string if the attribute is absent.
    public string? title { get; private set; }

    public bool alternate { get; private set; }
    // todo The disabled attribute, on getting, must return true if the disabled flag is set, or false otherwise. On setting, the disabled attribute must set the disabled flag if the new value is true, or unset the disabled flag otherwise.
    public bool disabled { get; private set; }
    private ICSSRuleList _cssRules = new CSSRuleList();
    public ICSSRuleList cssRules {
        get {
            // If the origin-clean flag is unset, throw a SecurityError exception.
            if (originClean is false) throw new SecurityError();
            // Return a read-only, live CSSRuleList object representing the CSS rules.
            return _cssRules;
            // Note: Even though the returned CSSRuleList object is read-only (from the perspective of client-authored script), 
            // it can nevertheless change over time due to its liveness status. For example, invoking the insertRule() 
            // or deleteRule() methods can result in mutations reflected in the returned object.            
        }
        set { _cssRules = value; }
    }
    public bool originClean { get; private set; }
    public bool constructed { get; private set; }
    public bool disallowModification { get; private set; }
    public Document? document { get; private set; }
    public DOMString? stylesheetBaseUrl;
    public string? href => location;

    static public ICSSStyleSheet Create(CSSStyleSheetInit? options = null) {
        // Construct a new CSSStyleSheet object sheet.
        var sheet = new StyleSheet() {
            // Set sheet’s location to the base URL of the associated Document for the current global object.
            // todo
            location = null,
            // Set sheet’s stylesheet base URL to the baseURL attribute value from options.
            stylesheetBaseUrl = options?.baseURL,
            // Set sheet’s parent CSS style sheet to null.
            parentStyleSheet = null,
            // Set sheet’s owner node to null.
            ownerNode = null,
            // Set sheet’s owner CSS rule to null.
            ownerRule = null,
            // Set sheet’s title to the the empty string.
            title = "",
            // Unset sheet’s alternate flag.
            alternate = false,
            // Set sheet’s origin-clean flag.
            originClean = true,
            // Set sheet’s constructed flag.
            constructed = true,
            // Set sheet’s Constructor document to the associated Document for the current global object.
            // todo
            document = null,
            // If the media attribute of options is a string, create a MediaList object from the string and assign it as sheet’s media.
            // Otherwise, serialize a media query list from the attribute and then create a MediaList object from the resulting string and set it as sheet’s media.
            // todo
            // If the disabled attribute of options is true, set sheet’s disabled flag.
            disabled = options?.disabled ?? false,

        };
        // Return sheet.
        return sheet;
    }

    // https://www.w3.org/TR/cssom-1/#dom-cssstylesheet-insertrule
    public ulong insertRule(string rule, ulong index = 0) {
        // If the origin-clean flag is unset, throw a SecurityError exception.
        if (!originClean) throw new SecurityError();
        // If the disallow modification flag is set, throw a NotAllowedError DOMException.
        if (disallowModification) throw new NotAllowedError();
        // Let parsed rule be the return value of invoking parse a rule with rule.
        SyntaxError? error = null;
        Rule? parsedRule = null;
        Parser.ParseARule(rule).Switch(
            r => parsedRule = r,
            err => error = err
        );
        // If parsed rule is a syntax error, return parsed rule.
        if (error is not null) return 12;
        // If parsed rule is an @import rule, and the constructed flag is set, throw a SyntaxError DOMException.
        // todo
        // Return the result of invoking insert a CSS rule rule in the CSS rules at index.
        // todo
        return CSSRule.InsertACssRule(rule, cssRules, index);
    }

    // https://www.w3.org/TR/cssom-1/#dom-cssstylesheet-deleterule
    public void deleteRule(ulong index) {
        // If the origin-clean flag is unset, throw a SecurityError exception.
        if (!originClean) throw new SecurityError();
        // If the disallow modification flag is set, throw a NotAllowedError DOMException.
        if (disallowModification) throw new NotAllowedError();
        // Remove a CSS rule in the CSS rules at index.       
        CSSRule.RemoveACssRule(cssRules, index);
    }

    // https://www.w3.org/TR/cssom-1/#dom-cssstylesheet-replace
    public Promise<ICSSStyleSheet> replace(string text) {
        // Let promise be a promise.
        var promise = new Promise<ICSSStyleSheet>(this);
        // If the constructed flag is not set, or the disallow modification flag is set, reject promise with a NotAllowedError DOMException and return promise.
        if (!constructed || disallowModification) {
            // todo reject
            return promise;
        }
        // Set the disallow modification flag.
        disallowModification = true;
        // In parallel, do these steps:        
        // Let rules be the result of running parse a list of rules from text. If rules is not a list of rules (i.e. an error occurred during parsing), set rules to an empty list.
        var rules = Parser.ParseAListOfRules(text);
        // todo handle error
        // If rules contains one or more @import rules, remove those rules from rules.
        // todo
        // Set sheet’s CSS rules to rules.
        cssRules = rules;
        // Unset sheet’s disallow modification flag.
        disallowModification = false;
        // Resolve promise with sheet.
        // todo
        // Return promise.
        return promise;
    }

    // https://www.w3.org/TR/cssom-1/#synchronously-replace-the-rules-of-a-cssstylesheet
    public void replaceSync(string text) {
        // If the constructed flag is not set, or the disallow modification flag is set, throw a NotAllowedError DOMException.
        if (!constructed || disallowModification) throw new NotAllowedError();
        // Let rules be the result of running parse a list of rules from text. If rules is not a list of rules (i.e. an error occurred during parsing), set rules to an empty list.
        var rules = Parser.ParseAListOfRules(text);
        // todo handle error
        // If rules contains one or more @import rules, remove those rules from rules.
        // todo
        // Set sheet’s CSS rules to rules.
        cssRules = rules;
    }

    // https://www.w3.org/TR/cssom-1/#legacy-css-style-sheet-members
    [Obsolete]
    public ICSSRuleList rules => cssRules;
    [Obsolete]
    public void removeRule(ulong index = 0) => deleteRule(index);
    [Obsolete]
    public long addRule(DOMString selector = "undefined", DOMString block = "undefined", ulong? optionalIndex = null) {
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




// https://www.w3.org/TR/cssom-1/#the-stylesheet-interface
public interface IStyleSheet {
    CSSOMString type { get; }
    USVString? href { get; }
    Element? ownerNode { get; } // todo ProcessingInstruction
    ICSSStyleSheet? parentStyleSheet { get; }
    DOMString? title { get; }
    // readonly MediaList media; // todo
    bool disabled { get; }
}

// https://www.w3.org/TR/cssom-1/#the-cssstylesheet-interface
public interface ICSSStyleSheet: IStyleSheet {
    public static ICSSStyleSheet Create(CSSStyleSheetInit? options = null) {
        return StyleSheet.Create(options);
    }
    ICSSRule? ownerRule { get; }
    public ICSSRuleList cssRules { get; internal set; }
    ulong insertRule(CSSOMString rule, ulong index = 0);
    void deleteRule(ulong index);

    Promise<ICSSStyleSheet> replace(USVString text);
    void replaceSync(USVString text);
};

public struct CSSStyleSheetInit() {
    public DOMString? baseURL = null;
    //   (MediaList or DOMString) media = "";
    public bool disabled = false;
};


// https://www.w3.org/TR/cssom-1/#the-cssrulelist-interface
public interface ICSSRuleList: IList<ICSSRule> {
    ICSSRule? item(ulong index);
    ulong length { get; }
};

public class CSSRuleList: List<ICSSRule>, ICSSRuleList {
    public ulong length => (ulong)Count;

    public ICSSRule? item(ulong index) {
        return this[(int)index];
    }
}

// https://www.w3.org/TR/cssom-1/#cssrule
public interface ICSSRule {
    CSSOMString cssText { get; set; }
    ICSSRule? parentRule { get; set; }
    ICSSStyleSheet? parentStyleSheet { get; set; }

    // the following attribute and constants are historical
    [Obsolete]
    ushort type { get; }
    [Obsolete]
    const ushort STYLE_RULE = 1;
    [Obsolete]
    const ushort CHARSET_RULE = 2;
    [Obsolete]
    const ushort IMPORT_RULE = 3;
    [Obsolete]
    const ushort MEDIA_RULE = 4;
    [Obsolete]
    const ushort FONT_FACE_RULE = 5;
    [Obsolete]
    const ushort PAGE_RULE = 6;
    [Obsolete]
    const ushort MARGIN_RULE = 9;
    [Obsolete]
    const ushort NAMESPACE_RULE = 10;
};

public class CSSRule: ICSSRule {
    [Obsolete]
    public virtual ushort type { get; } = 0;
    private string text { get; set; }

    public string cssText { get => throw new NotImplementedException(); set { } }

    private ICSSRule? _parentRule;
    public ICSSRule? parentRule { get => _parentRule; set { if (value is null) _parentRule = value; } }

    private ICSSStyleSheet? _parentStyleSheet;
    public ICSSStyleSheet? parentStyleSheet { get => _parentStyleSheet; set { if (value is null) _parentStyleSheet = value; } }

    private ICSSRuleList childCSSRules;

    // https://www.w3.org/TR/cssom-1/#parse-a-css-rule    
    public static Rule? parseACssRule(string input) {
        // 1. Let rule be the return value of invoking parse a rule with string.
        SyntaxError? error = null;
        Rule? parsedRule = null;
        Parser.ParseARule(input).Switch(
            r => parsedRule = r,
            err => error = err
        );
        // 2. If rule is a syntax error, return rule.
        if (error is not null) return null;
        // 3. Let parsed rule be the result of parsing rule according to the appropriate CSS specifications,
        // dropping parts that are said to be ignored. If the whole style rule is dropped, return a syntax error.
        // todo
        // 4. Return parsed rule.
        return parsedRule;
    }

    // https://www.w3.org/TR/cssom-1/#insert-a-css-rule
    public static ulong InsertACssRule(string input, ICSSRuleList list, ulong index) {
        // 1. Set length to the number of items in list.
        var length = list.length;
        // 2. If index is greater than length, then throw an IndexSizeError exception.
        if (index > length) throw new IndexSizeError();
        // 3. Set new rule to the results of performing parse a CSS rule on argument rule.
        var newRule = parseACssRule(input);
        // 4. If new rule is a syntax error, throw a SyntaxError exception.
        if (newRule is null) throw new SyntaxError();
        // 5. If new rule cannot be inserted into list at the zero-index position index due to constraints specified by CSS, then throw a HierarchyRequestError exception. [CSS21]
        // todo
        // Note: For example, a CSS style sheet cannot contain an @import at-rule after a style rule.
        // 6. If new rule is an @namespace at-rule, and list contains anything other than @import at-rules, and @namespace at-rules, throw an InvalidStateError exception.
        // todo
        // 7. Insert new rule into list at the zero-indexed position index.
        list.Insert((int)index, newRule);
        // 8. Return index.
        return index;
    }

    // https://www.w3.org/TR/cssom-1/#remove-a-css-rule
    public static void RemoveACssRule(ICSSRuleList list, ulong index) {
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

class CSSStyleRule: CSSRule {
    [Obsolete]
    public override ushort type { get; } = 1;

    public CSSOMString selectorText { get; } // todo set
    // public CSSStyleDeclaration style { get; }


}



class Declaration {
    public string propertyName { get; set; }
    public string value { get; set; }
    public bool important = false;
    public bool caseSensitive = false;
}

class CSSStyleDeclaration {
    public bool computed = false;
    public List<Declaration> declarations = [];
    public CSSRule? parentCssRule = null;
    public Element? OwnerNode = null;
    public bool updating = false;
}