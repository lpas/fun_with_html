namespace FunWithHtml.css.Parser;

using System.Data;
using System.Diagnostics;
using FunWithHtml.css.Tokenizer;
using FunWithHTML.css.Parser;
using FunWithHTML.misc;
using OneOf;

public class Rule: CSSRule { }

public class AtRule: Rule {
    public string name = "";
    public List<ComponentValue> prelude = [];
    public List<Declaration> declarations = [];
    public List<Rule> childRules = [];
}


public class QualifiedRule: Rule {
    public List<ComponentValue> prelude = [];
    public List<Declaration> declarations = [];
    public List<Rule> childRules = [];
}

public class Declaration {
    public string name = "";
    public List<ComponentValue> value = [];
    public bool important = false;
}

public interface ComponentValue: TokenStream.Item { }


public class Function: ComponentValue {
    public string name = "";
    public List<ComponentValue> value = [];
}

public class SimpleBlock(Token token): ComponentValue {
    public Token token = token;
    public List<ComponentValue> value = [];
}

// https://drafts.csswg.org/css-syntax-3/#parser-definitions
public class TokenStream {
    public interface Item { }

    private readonly Item EofToken = new EofToken();

    private readonly List<Item> tokens;
    private int index = 0;

    private readonly Stack<int> markedIndexes = [];


    public TokenStream(List<Item> tokens) {
        this.tokens = tokens;
    }

    public TokenStream(List<Token> tokens) {
        this.tokens = [.. tokens.Cast<Item>()];
    }
    public TokenStream(List<ComponentValue> tokens) {
        this.tokens = [.. tokens.Cast<Item>()];
    }

    public Item NextToken { get => index < tokens.Count ? tokens[index] : EofToken; }

    public bool Empty { get => NextToken is EofToken; }

    public Item ConsumeAToken() {
        return ConsumeAToken<Item>();
    }
    public T ConsumeAToken<T>() where T : Item {
        if (NextToken is T expectedToken) {
            index++;
            return expectedToken;
        }
        throw new InvalidOperationException($"Expected token of type {typeof(T).Name}, but got {NextToken?.GetType().Name}.");
    }

    public void DiscardAToken() {
        if (!Empty) index++;
    }

    public void Mark() {
        markedIndexes.Push(index);
    }
    public void RestoreAMark() {
        index = markedIndexes.Pop();
    }
    public void DiscardAMark() {
        markedIndexes.Pop();
    }

    public void DiscardWhitespace() {
        while (NextToken is WhitespaceToken) DiscardAToken();
    }
}


public static class Parser {

    #region https://drafts.csswg.org/css-syntax-3/#parser-entry-points

    // https://drafts.csswg.org/css-syntax-3/#normalize-into-a-token-stream
    private static TokenStream NormalizeIntoATokenStream(OneOf<TokenStream, string> input) {
        if (input.IsT0) return input.AsT0;
        if (input.IsT1) return new TokenStream(new Tokenizer(input.AsT1).GetTokenList());
        throw new InvalidOperationException();
    }
    // todo If input is a list of CSS tokens and/or component values, create a new token stream with input as its tokens, and return it.

    private static TokenStream NormalizeIntoATokenStream(string input) {
        return new TokenStream(new Tokenizer(input).GetTokenList());
    }


    // https://drafts.csswg.org/css-syntax-3/#parse-grammar // todo
    public static List<ComponentValue> Parse(string Input) {
        // 1. Normalize input, and set input to the result.
        var input = NormalizeIntoATokenStream(Input);
        // 2. Parse a list of component values from input, and let result be the return value.
        var returnValue = ParseAListOfComponentValues(input);
        // 3. Attempt to match result against grammar. If this is successful, return the matched result; otherwise, return failure.
        return returnValue;
    }

    // https://drafts.csswg.org/css-syntax-3/#parse-comma-list // todo
    // https://drafts.csswg.org/css-syntax-3/#parse-stylesheet
    public static CSSStyleSheet ParseAStylesheet(string input) { // todo optional url
        // If input is a byte stream for a stylesheet, decode bytes from input, and set input to the result.
        // Normalize input, and set input to the result.
        // Create a new stylesheet, with its location set to location (or null, if location was not passed).
        var styleSheet = CSSStyleSheet.Create();
        // Consume a stylesheet’s contents from input, and set the stylesheet’s rules to the result.
        styleSheet.cssRules = ConsumeAStylesheetsContents(NormalizeIntoATokenStream(input));
        // Return the stylesheet.
        return styleSheet;
    }

    // https://drafts.csswg.org/css-syntax-3/#parse-stylesheet-contents
    public static CSSRuleList ParseAStylesheetsContents(string input) {
        // Normalize input, and set input to the result.

        // Consume a stylesheet’s contents from input, and return the result.
        return ConsumeAStylesheetsContents(NormalizeIntoATokenStream(input));
    }

    // https://drafts.csswg.org/css-syntax-3/#parse-block-contents
    public static List<OneOf<Rule, List<Declaration>>> ParseABlocksContents(string Input) {
        // 1. Normalize input, and set input to the result.
        var input = NormalizeIntoATokenStream(Input);
        // 2. Consume a block’s contents from input, and return the result.        
        return ConsumeABlocksContents(input);
    }

    // https://drafts.csswg.org/css-syntax-3/#parse-rule
    public static OneOf<Rule, SyntaxError> ParseARule(string Input) {
        // Normalize input, and set input to the result.
        var input = NormalizeIntoATokenStream(Input);
        // Discard whitespace from input.
        input.DiscardWhitespace();
        Rule rule;
        // If the next token from input is an <EOF-token>, return a syntax error.
        if (input.NextToken is EofToken) return new SyntaxError();
        // Otherwise, if the next token from input is an <at-keyword-token>, consume an at-rule from input, and let rule be the return value.
        else if (input.NextToken is AtKeywordToken) rule = ConsumeAnAtRule(input);
        // Otherwise, consume a qualified rule from input and let rule be the return value. If nothing or an invalid rule error was returned, return a syntax error.
        else {
            if (ConsumeAQualifiedRule(input) is OneOf<QualifiedRule, InvalidRuleError> obj) {
                if (obj.IsT0) {
                    rule = obj.AsT0;
                } else {
                    return new SyntaxError();
                }
            } else {
                return new SyntaxError();
            }
        }
        // Discard whitespace from input.
        input.DiscardWhitespace();
        // If the next token from input is an <EOF-token>, return rule. Otherwise, return a syntax error.
        if (input.NextToken is EofToken) return rule;
        else return new SyntaxError();
    }

    // https://drafts.csswg.org/css-syntax-3/#parse-declaration // todo
    // https://drafts.csswg.org/css-syntax-3/#parse-component-value // todo
    // https://drafts.csswg.org/css-syntax-3/#parse-list-of-component-values // todo
    public static List<ComponentValue> ParseAListOfComponentValues(OneOf<TokenStream, string> Input) {
        // 1. Normalize input, and set input to the result.
        var input = NormalizeIntoATokenStream(Input);
        // 2. Consume a list of component values from input, and return the result.        
        return ConsumeAListOfComponentValues(input);
    }
    // https://drafts.csswg.org/css-syntax-3/#parse-comma-separated-list-of-component-values // todo
    #endregion

    #region https://drafts.csswg.org/css-syntax-3/#parser-algorithms

    private class InvalidRuleError { }

    // https://drafts.csswg.org/css-syntax-3/#consume-stylesheet-contents
    private static CSSRuleList ConsumeAStylesheetsContents(TokenStream input) {
        // Let rules be an initially empty list of rules.
        var rules = new CSSRuleList();

        // Process input:
        while (true) {
            switch (input.NextToken) {
                // <whitespace-token>
                case WhitespaceToken:
                    // Discard a token from input.
                    input.DiscardAToken();
                    break;
                // <EOF-token>
                case EofToken:
                    // Return rules.
                    return rules;
                // <CDO-token>
                case CDOToken:
                // <CDC-token>
                case CDCToken:
                    // Discard a token from input.
                    input.DiscardAToken();
                    break;
                // <at-keyword-token>
                case AtKeywordToken:
                    // Consume an at-rule from input. If anything is returned, append it to rules.
                    if (ConsumeAnAtRule(input) is AtRule atRule) {
                        rules.Add(atRule);
                    }
                    break;
                // anything else
                default:
                    // Consume a qualified rule from input. If a rule is returned, append it to rules.
                    if (ConsumeAQualifiedRule(input) is OneOf<QualifiedRule, InvalidRuleError> result && result.IsT0) {
                        rules.Add(result.AsT0);
                    }
                    break;
            }
        }
    }

    // https://drafts.csswg.org/css-syntax-3/#consume-at-rule
    private static AtRule? ConsumeAnAtRule(TokenStream input, bool nested = false) {
        // Assert: The next token is an <at-keyword-token>.
        Debug.Assert(input.NextToken is AtKeywordToken);
        // Consume a token from input, and let rule be a new at-rule with its name set to the returned token’s value,
        //  its prelude initially set to an empty list, and no declarations or child rules.
        var rule = new AtRule() {
            name = input.ConsumeAToken<AtKeywordToken>().value,
        };

        // Process input:
        while (true) {
            switch (input.NextToken) {
                // <semicolon-token>
                case SemicolonToken:
                // <EOF-token>
                case EofToken:
                    // Discard a token from input. If rule is valid in the current context, return it; otherwise return nothing.
                    // todo is valid in the current context
                    input.DiscardAToken();
                    return rule;

                // <}-token>
                case CurlyBracesCloseToken:
                    // If nested is true:
                    if (nested) {
                        // If rule is valid in the current context, return it.
                        // todo check is valid in current context
                        return rule;
                        // Otherwise, return nothing.

                    }
                    // Otherwise, consume a token and append the result to rule’s prelude.
                    rule.prelude.Add(input.ConsumeAToken<CurlyBracesCloseToken>());
                    break;
                // <{-token>
                case CurlyBracesOpenToken:
                    // Consume a block from input, and assign the result to rule’s child rules.
                    var childRules = ConsumeABlocksContents(input);
                    rule.childRules = [.. childRules.Where(item => item.IsT0).Select(item => item.AsT0)];
                    rule.declarations = [.. childRules.Where(item => item.IsT1).Select(item => item.AsT1).SelectMany(i => i)];
                    // Note: If the result contains lists of declarations, how they’re materialized in the CSSOM depends on the rule.
                    //  Some turn them all into nested declarations rules, others will treat them all as declarations, and others will treat the first item differently from the rest.

                    // If rule is valid in the current context, return it. Otherwise, return nothing.
                    // todo check if valid in current context
                    return rule;
                // anything else
                default:
                    // Consume a component value from input and append the returned value to rule’s prelude.
                    rule.prelude.Add(ConsumeAComponentValue(input));
                    break;
            }
        }
    }


    // https://drafts.csswg.org/css-syntax-3/#consume-qualified-rule
    private static OneOf<QualifiedRule, InvalidRuleError>? ConsumeAQualifiedRule(TokenStream input, Type? stopToken = null, bool nested = false) {
        // Let rule be a new qualified rule with its prelude, declarations, and child rules all initially set to empty lists.
        var rule = new QualifiedRule();
        // Process input:
        while (true) {
            switch (input.NextToken) {
                // <EOF-token>
                case EofToken:
                // stop token (if passed)
                case Token t when stopToken is not null && t.GetType() == stopToken:
                    // This is a parse error. Return nothing.
                    // todo parse error
                    return null;
                // <}-token>
                case CurlyBracesCloseToken:
                    // This is a parse error. If nested is true, return nothing. Otherwise, consume a token and append the result to rule’s prelude.
                    // todo parse error
                    if (nested) return null;
                    rule.prelude.Add(input.ConsumeAToken<CurlyBracesCloseToken>());
                    break;
                // <{-token>
                case CurlyBracesOpenToken:
                    // If the first two non-<whitespace-token> values of rule’s prelude are an <ident-token> whose value starts with "--" followed by a <colon-token>, then:
                    if (LooksLikeACustomProperty(rule.prelude)) {
                        // If nested is true, consume the remnants of a bad declaration from input, with nested set to true, and return nothing.
                        if (nested) {
                            ConsumeTheRemnantsOfABadDeclaration(input, true);
                            return null;
                        }
                        // If nested is false, consume a block from input, and return nothing.
                        ConsumeABlock(input);
                        return null;
                    } else {
                        // Otherwise, consume a block from input, and let child rules be the result.
                        var childRules = ConsumeABlock(input);
                        //  If the first item of child rules is a list of declarations, remove it from child rules and assign it to rule’s declarations.
                        if (childRules.Count > 0 && childRules[0].IsT1) {
                            rule.declarations = childRules[0].AsT1;
                            childRules.RemoveAt(0);
                        }
                        //  If any remaining items of child rules are lists of declarations, replace them with nested declarations rules containing the list as its sole child. Assign child rules to rule’s child rules.
                        // todo fixme
                        rule.childRules = childRules.Select(item => item.IsT0 ? item.AsT0 : null).ToList();
                    }
                    // If rule is valid in the current context, return it; otherwise return an invalid rule error.
                    // todo check if valid in current context?
                    return rule;
                // anything else
                default:
                    // Consume a component value from input and append the result to rule’s prelude.
                    rule.prelude.Add(ConsumeAComponentValue(input));
                    break;
            }
        }


        static bool LooksLikeACustomProperty(List<ComponentValue> tokens) {
            using var enumerator = tokens.Where((token) => token is not WhitespaceToken).GetEnumerator();
            return enumerator.MoveNext() && enumerator.Current is IdentToken identToken && identToken.value.StartsWith("--")
                && enumerator.MoveNext() && enumerator.Current is ColonToken;
        }

    }




    // https://drafts.csswg.org/css-syntax-3/#consume-block
    private static List<OneOf<Rule, List<Declaration>>> ConsumeABlock(TokenStream input) {
        // Assert: The next token is a <{-token>.
        Debug.Assert(input.NextToken is CurlyBracesOpenToken);
        // Discard a token from input. Consume a block’s contents from input and let rules be the result. Discard a token from input.
        input.DiscardAToken();
        var rules = ConsumeABlocksContents(input);
        input.DiscardAToken();
        // Return rules.
        return rules;
    }

    // https://drafts.csswg.org/css-syntax-3/#consume-block-contents
    private static List<OneOf<Rule, List<Declaration>>> ConsumeABlocksContents(TokenStream input) {
        // Let rules be an empty list, containing either rules or lists of declarations.
        var rules = new List<OneOf<Rule, List<Declaration>>>();
        // Let decls be an empty list of declarations.
        var decls = new List<Declaration>();

        // Process input:
        while (true) {
            switch (input.NextToken) {
                // <whitespace-token>
                case WhitespaceToken:
                // <semicolon-token>
                case SemicolonToken:
                    // Discard a token from input.
                    input.DiscardAToken();
                    break;
                // <EOF-token>
                case EofToken:
                // <}-token>
                case CurlyBracesCloseToken:
                    // Return rules.
                    // todo spec problem?
                    if (decls.Count > 0) {
                        rules.Add(decls);
                        decls = [];
                    }
                    return rules;

                // <at-keyword-token>
                case AtKeywordToken:
                    // If decls is not empty, append it to rules, and set decls to a fresh empty list of declarations.
                    if (decls.Count > 0) {
                        rules.Add(decls);
                        decls = [];
                    }
                    // Consume an at-rule from input, with nested set to true. If a rule was returned, append it to rules.
                    if (ConsumeAnAtRule(input, true) is AtRule atRule) {
                        rules.Add(atRule);
                    }
                    break;
                // anything else
                default:
                    // Mark input.
                    input.Mark();
                    // Consume a declaration from input, with nested set to true. If a declaration was returned, append it to decls, and discard a mark from input.
                    if (ConsumeADeclaration(input, true) is Declaration decl) {
                        decls.Add(decl);
                        input.DiscardAMark();
                    } else {
                        // Otherwise, restore a mark from input, then consume a qualified rule from input, with nested set to true, and <semicolon-token> as the stop token.    
                        input.RestoreAMark();
                        var qualifiedRule = ConsumeAQualifiedRule(input, typeof(SemicolonToken), true);
                        // If nothing was returned
                        if (qualifiedRule is null) {
                            // Do nothing
                        } else {
                            qualifiedRule.Value.Switch(
                                // If a rule was returned
                                rule => {
                                    // If decls is not empty, append decls to rules, and set decls to a fresh empty list of declarations. Append the rule to rules.
                                    if (decls.Count > 0) {
                                        rules.Add(decls);
                                        decls = [];
                                    }
                                    rules.Add(rule);
                                },
                                // If an invalid rule error was returned
                                error => {
                                    // If decls is not empty, append decls to rules, and set decls to a fresh empty list of declarations. (Otherwise, do nothing.)
                                    if (decls.Count > 0) {
                                        rules.Add(decls);
                                        decls = [];
                                    }
                                }
                            );
                        }
                    }
                    break;
            }
        }
    }

    // https://drafts.csswg.org/css-syntax-3/#consume-declaration
    private static Declaration? ConsumeADeclaration(TokenStream input, bool nested = false) {
        // Let decl be a new declaration, with an initially empty name and a value set to an empty list.
        var decl = new Declaration();
        // 1. If the next token is an <ident-token>, consume a token from input and set decl's name to the token’s value.
        if (input.NextToken is IdentToken) {
            decl.name = input.ConsumeAToken<IdentToken>().value;
        } else {
            // Otherwise, consume the remnants of a bad declaration from input, with nested, and return nothing.
            ConsumeTheRemnantsOfABadDeclaration(input, nested);
            return null;
        }
        // 2. Discard whitespace from input.
        input.DiscardWhitespace();
        // 3. If the next token is a <colon-token>, discard a token from input.
        if (input.NextToken is ColonToken) {
            input.DiscardAToken();
        } else {
            // Otherwise, consume the remnants of a bad declaration from input, with nested, and return nothing.
            ConsumeTheRemnantsOfABadDeclaration(input, nested);
            return null;
        }
        // 4. Discard whitespace from input.
        input.DiscardWhitespace();
        // 5. Consume a list of component values from input, with nested, and with <semicolon-token> as the stop token, and set decl’s value to the result.
        decl.value = ConsumeAListOfComponentValues(input, typeof(SemicolonToken), nested);
        // 6. If the last two non-<whitespace-token>s in decl’s value are a <delim-token> with the value "!" followed by an <ident-token> with a value that is an ASCII case-insensitive match for "important", remove them from decl’s value and set decl’s important flag.
        ProcessImportantFlag(decl);
        // 7. While the last item in decl’s value is a <whitespace-token>, remove that token.
        while (decl.value.Count > 0 && decl.value[^1] is WhitespaceToken) decl.value.RemoveAt(decl.value.Count - 1);
        // 8. If decl’s name is a custom property name string, then set decl’s original text to the segment of the original source text string corresponding to the tokens of decl’s value.
        // todo
        // Otherwise, if decl’s value contains a top-level simple block with an associated token of <{-token>, and also contains any other non-<whitespace-token> value, return nothing. (That is, a top-level {}-block is only allowed as the entire value of a non-custom property.)

        // Otherwise, if decl’s name is an ASCII case-insensitive match for "unicode-range", consume the value of a unicode-range descriptor from the segment of the original source text string corresponding to the tokens returned by the consume a list of component values call, and replace decl’s value with the result.

        // 9. If decl is valid in the current context, return it; otherwise return nothing.
        // todo check is valid in current context
        return decl;

        static void ProcessImportantFlag(Declaration decl) {
            var LastTwoItems = decl.value.Select((value, index) => (value, index)).Where(item => item.value is not WhitespaceToken).TakeLast(2).ToList();
            if (LastTwoItems.Count != 2) return;
            var first = LastTwoItems[0];
            var second = LastTwoItems[1];
            if (first.value is DelimToken delimToken && delimToken.value == '!'
                && second.value is IdentToken identToken && identToken.value.Equals("important", StringComparison.OrdinalIgnoreCase)) {
                decl.important = true;
                decl.value.RemoveAt(second.index);
                decl.value.RemoveAt(first.index);
            }
        }

    }

    private static void ConsumeTheRemnantsOfABadDeclaration(TokenStream input, bool nested) {
        // Process input:
        while (true) {
            switch (input.NextToken) {
                // <eof-token>
                case EofToken:
                // <semicolon-token>
                case SemicolonToken:
                    // Discard a token from input, and return nothing.
                    input.DiscardAToken();
                    return;
                // <}-token>
                case CurlyBracesCloseToken:
                    // If nested is true, return nothing. Otherwise, discard a token.
                    if (nested) {
                        return;
                    } else {
                        input.DiscardAToken();
                    }
                    break;
                // anything else
                default:
                    // Consume a component value from input, and do nothing.
                    ConsumeAComponentValue(input);
                    break;
            }
        }

    }

    // https://drafts.csswg.org/css-syntax-3/#consume-list-of-components
    internal static List<ComponentValue> ConsumeAListOfComponentValues(TokenStream input, Type? stopToken = null, bool nested = false) {
        // Let values be an empty list of component values.
        var values = new List<ComponentValue>();
        // Process input:
        while (true) {
            switch (input.NextToken) {
                // <eof-token>
                case EofToken:
                // stop token (if passed)
                case Token t when stopToken is not null && t.GetType() == stopToken:
                    // Return values.
                    return values;
                // <}-token>
                case CurlyBracesCloseToken:
                    // If nested is true, return values.
                    if (nested) {
                        return values;
                    }
                    // Otherwise, this is a parse error. Consume a token from input and append the result to values.
                    // todo parse error
                    values.Add(input.ConsumeAToken<CurlyBracesCloseToken>());
                    break;
                // anything else
                default:
                    // Consume a component value from input, and append the result to values.
                    values.Add(ConsumeAComponentValue(input));
                    break;
            }
        }
    }

    // https://drafts.csswg.org/css-syntax-3/#consume-component-value
    private static ComponentValue ConsumeAComponentValue(TokenStream input) {
        // Process input:
        return input.NextToken switch {
            // <{-token>
            // <[-token>
            // <(-token>
            // Consume a simple block from input and return the result.
            CurlyBracesOpenToken or SquareBracketOpenToken or BracketOpenToken => ConsumeASimpleBlock(input),
            // <function-token>
            // Consume a function from input and return the result.
            FunctionToken => ConsumeAFunction(input),
            // anything else
            // Consume a token from input and return the result.     
            _ => (ComponentValue)input.ConsumeAToken(),
        };
    }

    // https://drafts.csswg.org/css-syntax-3/#consume-simple-block
    private static SimpleBlock ConsumeASimpleBlock(TokenStream input) {
        // Assert: the next token of input is <{-token>, <[-token>, or <(-token>.
        Debug.Assert(input.NextToken is CurlyBracesOpenToken or SquareBracketOpenToken or BracketOpenToken);
        // Let ending token be the mirror variant of the next token. (E.g. if it was called with <[-token>, the ending token is <]-token>.)
        var startToken = input.NextToken;
        // Let block be a new simple block with its associated token set to the next token and with its value initially set to an empty list.
        var block = new SimpleBlock(startToken as Token);
        // Discard a token from input.
        input.DiscardAToken();
        // Process input:
        while (true) {
            switch (input.NextToken) {
                // <eof-token>
                case EofToken:
                // ending token
                case CurlyBracesCloseToken when startToken is CurlyBracesOpenToken:
                case SquareBracketCloseToken when startToken is SquareBracketOpenToken:
                case BracketCloseToken when startToken is BracketOpenToken:
                    // Discard a token from input. Return block.
                    input.DiscardAToken();
                    return block;
                // anything else
                default:
                    // Consume a component value from input and append the result to block’s value.
                    block.value.Add(ConsumeAComponentValue(input));
                    break;
            }
        }
    }

    // https://drafts.csswg.org/css-syntax-3/#consume-function
    private static Function ConsumeAFunction(TokenStream input) {
        // Assert: The next token is a <function-token>.
        Debug.Assert(input.NextToken is FunctionToken);
        // Consume a token from input, and let function be a new function with its name equal the returned token’s value, and a value set to an empty list.
        var function = new Function() {
            name = input.ConsumeAToken<FunctionToken>().value,
        };
        // Process input:
        while (true) {
            switch (input.NextToken) {
                // <eof-token>
                case EofToken:
                // <)-token>
                case BracketCloseToken:
                    // Discard a token from input. Return function.
                    input.DiscardAToken();
                    return function;
                // anything else
                default:
                    // Consume a component value from input and append the result to function’s value.
                    function.value.Add(ConsumeAComponentValue(input));
                    break;
            }
        }
    }

    // https://drafts.csswg.org/css-syntax-3/#consume-unicode-range-value

    #endregion
}
