namespace FunWithHtml.css.Selector;

using System.Text;
using FunWithHtml.css.Parser;
using FunWithHtml.css.Tokenizer;
using FunWithHTML.css.Parser;
using OneOf;

// https://drafts.csswg.org/selectors-4/#grammar
class Parser {
    public static SelectorList<ComplexSelector>? ConsumeComplexSelectorList(TokenStream input) {
        // <complex-selector-list> = <complex-selector>#
        var selectorList = new SelectorList<ComplexSelector>();
        while (!input.Empty) {
            input.DiscardWhitespace();
            var parts = css.Parser.Parser.ConsumeAListOfComponentValues(input, typeof(CommaToken));
            var stream = new TokenStream(parts);
            if (ConsumeComplexSelector(stream) is ComplexSelector complexSelector) {
                selectorList.Add(complexSelector);
            } else {
                return null;
            }
            input.DiscardAToken(); // discard the possible next CommaToken
        }
        return selectorList;
    }

    static ComplexSelector? ConsumeComplexSelector(TokenStream input) {
        // <complex-selector-unit> [ <combinator>? <complex-selector-unit> ]*
        var complexSelector = new ComplexSelector();
        if (ConsumeComplexSelectorUnit(input) is OneOf<CompoundSelector, PseudoCompoundSelector> complexUnit) {
            complexSelector.Add(complexUnit.IsT0 ? complexUnit.AsT0 : complexUnit.AsT1);
        } else {
            return null;
        }

        while (!input.Empty) {
            input.Mark();
            // Whitespace is required between two <complex-selector-unit>s if the <combinator> between them is omitted. (This indicates the descendant combinator is being used.)
            var hasWhitespace = input.NextToken is WhitespaceToken;
            input.DiscardWhitespace();
            var combinator = ConsumeCombinator(input);
            hasWhitespace |= input.NextToken is WhitespaceToken;
            input.DiscardWhitespace();

            if (combinator is null) {
                if (hasWhitespace) {
                    combinator = new Combinator(Combinator.Type.Descendant);
                } else {
                    input.RestoreAMark();
                    break;
                }
            }

            if (ConsumeComplexSelectorUnit(input) is OneOf<CompoundSelector, PseudoCompoundSelector> item) {
                input.DiscardAMark();
                complexSelector.Add(combinator);
                complexSelector.Add(item.IsT0 ? item.AsT0 : item.AsT1);
            } else {
                input.RestoreAMark();
                break;
            }
        }
        input.DiscardWhitespace();
        if (!input.Empty) return null;
        return complexSelector;

        static OneOf<CompoundSelector, PseudoCompoundSelector>? ConsumeComplexSelectorUnit(TokenStream input) {
            // [ <compound-selector>? <pseudo-compound-selector>* ]!
            CompoundSelector? compoundSelector = null;
            if (ConsumeCompoundSelector(input) is CompoundSelector cp) {
                compoundSelector = cp;
            }
            PseudoCompoundSelector? pseudoCompoundSelector = null;
            while (!input.Empty) {
                if (ConsumePseudoCompoundSelector(input) is PseudoCompoundSelector pcs) {
                    if (pseudoCompoundSelector is null) {
                        pseudoCompoundSelector = pcs;
                        pseudoCompoundSelector.prefix = compoundSelector;
                    }
                }
                break; // todo multiple pseudo-compound-selectors
            }

            if (compoundSelector is null && pseudoCompoundSelector is null) {
                return null;
            } else {
                return pseudoCompoundSelector is not null ? pseudoCompoundSelector : compoundSelector;
            }
        }
    }


    static CompoundSelector? ConsumeCompoundSelector(TokenStream input) {
        // [ <type-selector>? <subclass-selector>* ]!
        var compoundSelector = new CompoundSelector();
        if (ConsumeTypeSelector(input) is SimpleSelector typeSelector) {
            compoundSelector.Add(typeSelector);
        }
        while (!input.Empty) {
            if (ConsumeSubclassSelector(input) is SimpleSelector simpleSelector) {
                compoundSelector.Add(simpleSelector);
            } else {
                break;
            }
        }
        return compoundSelector.Count > 0 ? compoundSelector : null;
    }

    static PseudoCompoundSelector? ConsumePseudoCompoundSelector(TokenStream input) {
        // <pseudo-element-selector> <pseudo-class-selector>*    
        if (ConsumePseudoElementSelector(input) is PseudoElement pseudoElement) {
            var pseudoCompoundSelector = new PseudoCompoundSelector(pseudoElement);
            var list = new List<PseudoClassSelector>();
            while (true) {
                if (ConsumePseudoClassSelector(input) is PseudoClassSelector pcs) {
                    list.Add(pcs);
                } else {
                    break;
                }
            }
            if (list.Count > 0) {
                pseudoCompoundSelector.suffix = list;
            }
            return pseudoCompoundSelector;
        }
        return null;
    }

    static Combinator? ConsumeCombinator(TokenStream input) {
        //  '>' | '+' | '~' | [ '|' '|' ]
        switch (input.NextToken) {
            case DelimToken { value: '>' or '+' or '~' } delimToken:
                input.DiscardAToken();
                return new Combinator(delimToken.value switch {
                    '>' => Combinator.Type.Child,
                    '+' => Combinator.Type.NextSibling,
                    '~' => Combinator.Type.SubsequentCombinator,
                    _ => throw new InvalidOperationException(),
                });
            default:
                return null;
                // todo ||
        }
    }

    static SimpleSelector? ConsumeTypeSelector(TokenStream input) {
        // <type-selector> = <wq-name> | <ns-prefix>? '*'
        if (ConsumeWqName(input) is CssQualifiedName name) {
            return new TypeSelector(name);
        }

        input.Mark();
        var prefix = ConsumeNsPrefix(input);
        if (input.NextToken is DelimToken { value: '*' }) {
            input.DiscardAMark();
            input.ConsumeAToken();
            return new UniversalSelector(new CssQualifiedName("*", prefix));
        }
        input.RestoreAMark();
        return null;
    }

    static CssQualifiedName? ConsumeWqName(TokenStream input) {
        // <wq-name> = <ns-prefix>? <ident-token>
        input.Mark();
        var prefix = ConsumeNsPrefix(input);
        if (input.NextToken is IdentToken identToken) {
            input.DiscardAMark();
            input.ConsumeAToken();
            return new CssQualifiedName(identToken.value, prefix);
        }
        input.RestoreAMark();
        return null;
    }

    static string? ConsumeNsPrefix(TokenStream input) {
        // <ns-prefix> = [ <ident-token> | '*' ]? '|'
        if (input.NextToken is DelimToken { value: '|' }) {
            input.ConsumeAToken();
            return "|";
        }
        if (input.NextToken is IdentToken or DelimToken { value: '*' }) {
            input.Mark();
            var value = (input.ConsumeAToken() is IdentToken idToken) ? idToken.value : "*";
            if (input.NextToken is DelimToken { value: '|' }) {
                input.ConsumeAToken();
                return value + '|';
            } else {
                input.RestoreAMark();
            }
        }
        return null;
    }


    static SimpleSelector? ConsumeSubclassSelector(TokenStream input) {
        // <id-selector> | <class-selector> | <attribute-selector> | <pseudo-class-selector>
        if (ConsumeIdSelector(input) is IDSelector idSelector) {
            return idSelector;
        }
        if (ConsumeClassSelector(input) is ClassSelector classSelector) {
            return classSelector;
        }
        if (ConsumeAttributeSelector(input) is AttributeSelector attributeSelector) {
            return attributeSelector;
        }
        if (ConsumePseudoClassSelector(input) is PseudoClassSelector pseudoClassSelector) {
            return pseudoClassSelector;
        }
        return null;
    }

    static IDSelector? ConsumeIdSelector(TokenStream input) {
        //  <hash-token>
        if (input.NextToken is HashToken { type: HashTokenType.id } hashToken) {
            input.ConsumeAToken();
            return new IDSelector(hashToken.value);
        }
        return null;
    }

    static ClassSelector? ConsumeClassSelector(TokenStream input) {
        // '.' <ident-token>
        if (input.NextToken is DelimToken { value: '.' }) {
            input.Mark();
            input.ConsumeAToken();
            if (input.NextToken is IdentToken) {
                input.DiscardAMark();
                return new ClassSelector(input.ConsumeAToken<IdentToken>().value);
            } else {
                input.RestoreAMark();
            }
        }
        return null;
    }

    static AttributeSelector? ConsumeAttributeSelector(TokenStream input) {
        //  '[' <wq-name> ']' | '[' <wq-name> <attr-matcher> [ <string-token> | <ident-token> ] <attr-modifier>? ']'
        // the '[' ']' part got already parsed into a SimpleBlock
        if (input.NextToken is SimpleBlock { token: SquareBracketOpenToken } simpleBlock) {
            var tokens = new TokenStream(simpleBlock.value);
            if (ConsumeWqName(tokens) is CssQualifiedName name) {
                if (tokens.Empty) {
                    input.ConsumeAToken();
                    return new AttributeSelector(name);
                }
                if (ConsumeAttrMatcher(tokens) is AttributeSelector.MatchType matchType && tokens.NextToken is StringToken or IdentToken) {
                    OneOf<StringToken, IdentToken> value = tokens.NextToken is StringToken ? tokens.ConsumeAToken<StringToken>() : tokens.ConsumeAToken<IdentToken>();
                    tokens.DiscardWhitespace();
                    var attrModifier = ConsumeAttrModifier(tokens) ?? AttributeSelector.CaseSensitivity.Default;
                    // todo check if tokens is empty
                    input.ConsumeAToken();
                    return new AttributeSelector(name, matchType, value, attrModifier);
                }
            }
        }
        return null;
    }

    static AttributeSelector.CaseSensitivity? ConsumeAttrModifier(TokenStream input) {
        // <attr-modifier> = i | s
        if (input.NextToken is IdentToken { value: "i" or "s" } identToken) {
            input.ConsumeAToken();
            return identToken.value == "i" ? AttributeSelector.CaseSensitivity.insensitive : AttributeSelector.CaseSensitivity.sensitively;
        }
        return null;
    }

    static AttributeSelector.MatchType? ConsumeAttrMatcher(TokenStream input) {
        //  [ '~' | '|' | '^' | '$' | '*' ]? '='
        if (input.NextToken is DelimToken { value: '=' }) {
            input.ConsumeAToken();
            return AttributeSelector.MatchType.Exactly;
        }
        if (input.NextToken is DelimToken { value: '~' or '|' or '^' or '$' or '*' }) {
            input.Mark();
            var token = input.ConsumeAToken<DelimToken>();
            if (input.NextToken is DelimToken { value: '=' }) {
                input.DiscardAMark();
                input.ConsumeAToken();
                return token.value switch {
                    '~' => AttributeSelector.MatchType.ContainsWhitespaceSeparated,
                    '|' => AttributeSelector.MatchType.StartsWithDashSeparated,
                    '^' => AttributeSelector.MatchType.StartsWithString,
                    '$' => AttributeSelector.MatchType.EndsWithString,
                    '*' => AttributeSelector.MatchType.ContainsSubString,
                    _ => throw new InvalidOperationException(),
                };
            } else {
                input.RestoreAMark();
            }
        }
        return null;
    }

    static PseudoClassSelector? ConsumePseudoClassSelector(TokenStream input) {
        // <pseudo-class-selector> = : <ident-token> |: <function-token> <any-value> )
        // the function token got already parsed to a FunctionToken
        if (input.NextToken is ColonToken) {
            input.Mark();
            input.ConsumeAToken();
            if (input.NextToken is IdentToken) {
                input.DiscardAMark();
                return new PseudoClassSelector(input.ConsumeAToken<IdentToken>().value);
            } else if (input.NextToken is FunctionToken) {
                input.DiscardAMark();
                var function = input.ConsumeAToken<Function>();
                return new PseudoClassSelector(function.name, function.value);
            }
            input.RestoreAMark();
        }
        return null;
    }

    static PseudoElement? ConsumePseudoElementSelector(TokenStream input) {
        // : <pseudo-class-selector> | <legacy-pseudo-element-selector>
        if (input.NextToken is ColonToken) {
            input.Mark();
            input.ConsumeAToken();
            if (ConsumePseudoClassSelector(input) is PseudoClassSelector pseudoClassSelector) {
                input.DiscardAMark();
                return new PseudoElement(pseudoClassSelector.name);
            }
            input.RestoreAMark();
        }
        if (ConsumeLegacyPseudoElementSelector(input) is PseudoElement pseudoElement) {
            return pseudoElement;
        }
        return null;
    }

    private static PseudoElement? ConsumeLegacyPseudoElementSelector(TokenStream input) {
        // <legacy-pseudo-element-selector> =  : [before | after | first-line | first-letter]
        if (input.NextToken is ColonToken) {
            input.Mark();
            input.ConsumeAToken();
            if (input.NextToken is IdentToken { value: "before" or "after" or "first-line" or "first-letter" }) {
                input.DiscardAMark();
                return new PseudoElement(input.ConsumeAToken<IdentToken>().value);
            }
            input.RestoreAMark();
        }
        return null;
    }
}

public interface Selector { }

// https://drafts.csswg.org/css-namespaces-3/#css-qnames
public record struct CssQualifiedName(string name, string? @namespace = null) { }


public class TypeSelector(CssQualifiedName elementName): SimpleSelector {
    public CssQualifiedName elementName = elementName;
}

public class UniversalSelector(CssQualifiedName elementName): SimpleSelector {
    public CssQualifiedName elementName = elementName;
}

public class AttributeSelector(
    CssQualifiedName attribute,
    AttributeSelector.MatchType matchType = AttributeSelector.MatchType.Attribute,
    OneOf<StringToken, IdentToken>? value = null,
    AttributeSelector.CaseSensitivity caseSensitivity = AttributeSelector.CaseSensitivity.Default
    ): SimpleSelector {
    public enum MatchType {
        Attribute,
        Exactly, // =
        ContainsWhitespaceSeparated, // ~=
        StartsWithDashSeparated, // |=
        StartsWithString, // ^=
        EndsWithString, // $=
        ContainsSubString, // *=
    }
    public enum CaseSensitivity {
        Default,
        insensitive,
        sensitively,
    }

    public CssQualifiedName attribute = attribute;
    public OneOf<StringToken, IdentToken>? value = value;
    public MatchType matchType = matchType;

    public CaseSensitivity caseSensitivity = caseSensitivity;

}

public class ClassSelector(string identifier): SimpleSelector {
    public string identifier = identifier;
}

public class IDSelector(string identifier): SimpleSelector {
    public string identifier = identifier;
}

public class PseudoClassSelector(string name, List<ComponentValue>? arguments = null): SimpleSelector {
    public string name = name;
    public List<ComponentValue>? arguments = arguments;
}

// https://drafts.csswg.org/selectors-4/#simple
public class SimpleSelector: Selector {
    // https://drafts.csswg.org/cssom/#serialize-a-simple-selector
    public string Serialize() {
        var s = new StringBuilder();
        switch (this) {
            // type selector
            case TypeSelector:
            // universal selector
            case UniversalSelector:
                // 1. If the namespace prefix maps to a namespace that is not the default namespace and is not the null namespace (not in a namespace) 
                // append the serialization of the namespace prefix as an identifier, followed by a "|" (U+007C) to s.
                // todo
                // 2. If the namespace prefix maps to a namespace that is the null namespace (not in a namespace) append "|" (U+007C) to s.
                // todo
                // 3. If this is a type selector append the serialization of the element name as an identifier to s.
                if (this is TypeSelector typeSelector) {
                    s.Append(CommonSerializingIdioms.SerializeAnIdentifier(typeSelector.elementName.name));
                }
                // 4. If this is a universal selector append "*" (U+002A) to s.
                else if (this is UniversalSelector universalSelector) {
                    s.Append('*');
                }
                break;
            // attribute selector
            case AttributeSelector attributeSelector:
                // 1. Append "[" (U+005B) to s.
                s.Append('[');
                // 2. If the namespace prefix maps to a namespace that is not the null namespace (not in a namespace) append the serialization of the namespace prefix as an identifier, followed by a "|" (U+007C) to s.
                // todo
                // 3. Append the serialization of the attribute name as an identifier to s.
                s.Append(CommonSerializingIdioms.SerializeAnIdentifier(attributeSelector.attribute.name));
                // 4..If there is an attribute value specified, append "=", "~=", "|=", "^=", "$=", or "*=" as appropriate (depending on the type of attribute selector),
                if (attributeSelector.matchType != AttributeSelector.MatchType.Attribute && attributeSelector.value is OneOf<StringToken, IdentToken> value) {
                    s.Append(attributeSelector.matchType switch {
                        AttributeSelector.MatchType.Exactly => "=",
                        AttributeSelector.MatchType.ContainsWhitespaceSeparated => "~=",
                        AttributeSelector.MatchType.StartsWithDashSeparated => "|=",
                        AttributeSelector.MatchType.StartsWithString => "^=",
                        AttributeSelector.MatchType.EndsWithString => "$=",
                        AttributeSelector.MatchType.ContainsSubString => "*=",
                        _ => throw new InvalidOperationException(),
                    });
                    // followed by the serialization of the attribute value as a string, to s.
                    s.Append(CommonSerializingIdioms.SerializeAString(attributeSelector.value.Value.Match(item => item.value, item => item.value)));
                    // todo
                }
                // 5. If the attribute selector has the case-sensitivity flag present, append " i" (U+0020 U+0069) to s.
                // spec problem: missing s for sensitively
                if (attributeSelector.caseSensitivity == AttributeSelector.CaseSensitivity.insensitive) {
                    s.Append(" i");
                }
                // 6. Append "]" (U+005D) to s.
                s.Append(']');
                break;
            // class selector
            case ClassSelector cls:
                // Append a "." (U+002E), followed by the serialization of the class name as an identifier to s.
                s.Append('.');
                s.Append(CommonSerializingIdioms.SerializeAnIdentifier(cls.identifier));
                break;
            // ID selector
            case IDSelector idSelector:
                // Append a "#" (U+0023), followed by the serialization of the ID as an identifier to s.                
                s.Append('#');
                s.Append(CommonSerializingIdioms.SerializeAnIdentifier(idSelector.identifier));
                break;
            // pseudo-class
            case PseudoClassSelector pseudoClass:
                // If the pseudo-class does not accept arguments append ":" (U+003A), followed by the name of the pseudo-class, to s.
                s.Append(':');
                s.Append(pseudoClass.name);
                // Otherwise, append ":" (U+003A), followed by the name of the pseudo-class, followed by "(" (U+0028), followed by the value of the pseudo-class argument(s) determined as per below, followed by ")" (U+0029), to s.
                // todo
                // :lang()
                // The serialization of a comma-separated list of each argument’s serialization as a string, preserving relative order.
                // :nth-child()
                // :nth-last-child()
                // :nth-of-type()
                // :nth-last-of-type()
                // The result of serializing the value using the rules to serialize an <an+b> value.
                // :not()
                // The result of serializing the value using the rules for serializing a group of selectors.

                break;
        }
        return s.ToString();
    }
}



// https://drafts.csswg.org/selectors-4/#compound
class CompoundSelector: List<SimpleSelector>, Selector {
    //  If it contains a type selector or universal selector, that selector must come first in the sequence. Only one type selector or universal selector is allowed in the sequence.

}

class PseudoElement(string name) {
    public string name = name;
} // todo

// https://drafts.csswg.org/selectors-4/#pseudo-compound
class PseudoCompoundSelector(PseudoElement pseudoElement) {
    // pseudo element selector
    // optional followed by additional pseudo-class selectors
    // and optionally preceded by a compound selector or another pseudo-compound-selector


    public PseudoElement pseudoElement = pseudoElement;
    public List<PseudoClassSelector>? suffix;
    public OneOf<CompoundSelector, PseudoCompoundSelector>? prefix;
}

// https://drafts.csswg.org/selectors-4/#selector-combinator
class Combinator(Combinator.Type type) {
    public enum Type {
        Descendant, // whitespace
        Child, // >
        NextSibling, // +
        SubsequentCombinator, // ~
    }

    public Type type = type;

    public string Serialize() {
        return type switch {
            Type.Descendant => "",
            Type.Child => ">",
            Type.NextSibling => "+",
            Type.SubsequentCombinator => "~",
            _ => throw new InvalidOperationException(),
        };
    }
}

// https://drafts.csswg.org/selectors-4/#complex
class ComplexSelector: List<OneOf<CompoundSelector, PseudoCompoundSelector, Combinator>>, Selector {
    // A complex selector is a sequence of one or more compound selectors and/or pseudo-compound selectors, with compound selectors separated by combinators.

    public string Serialize() {
        var s = new StringBuilder();
        // To serialize a selector let s be the empty string, run the steps below for each part of the chain of the selector, and finally return s:

        foreach (var item in this) {
            // If there is only one simple selector in the compound selectors which is a universal selector, append the result of serializing the universal selector to s.

            // Otherwise, for each simple selector in the compound selectors that is not a universal selector of which the namespace prefix maps to a namespace that is not the
            //  default namespace serialize the simple selector and append the result to s.
            if (item.IsT0) {
                foreach (var simpleSelector in item.AsT0) {
                    s.Append(simpleSelector.Serialize());
                }
            }
            // If this is not the last part of the chain of the selector append a single SPACE (U+0020), followed by the combinator ">", "+", "~", ">>", "||", as appropriate,
            // followed by another single SPACE (U+0020) if the combinator was not whitespace, to s.
            if (item.IsT2) {
                s.Append(' ');
                s.Append(item.AsT2.Serialize());
                if (item.AsT2.type != Combinator.Type.Descendant) s.Append(' ');
            }
            // If this is the last part of the chain of the selector and there is a pseudo-element, append "::" followed by the name of the pseudo-element, to s.
            if (item.IsT1) {
                // todo handle prefix
                s.Append("::");
                s.Append(item.AsT1.pseudoElement.name);
            }
        }

        return s.ToString();
    }

}

// https://drafts.csswg.org/selectors-4/#list-of-simple-selectors
class SelectorList<T>: List<T>, Selector where T : Selector {
    public string Serialize() {
        return CommonSerializingIdioms.SerializeACommaSeparatedList(
            // todo this is only for complex lists
            this.OfType<ComplexSelector>().Select(cs => cs.Serialize())
        );
    }
}
