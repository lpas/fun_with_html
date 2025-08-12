#if DONT_COMPILE_THIS_FILE // todo file ist just a ruff draft of the parser



using System.Data;
using FunWithHtml.css.Tokenizer;

namespace FunWithHtml.css.Parser;

public class Rule { }

public class AtRule: Rule {
    public string name;
    public List<ComponentValue> prelude = [];

    public SimpleBlock? block; // only curly
}


public class QualifiedRule: Rule {
    public List<ComponentValue> prelude = [];
    public SimpleBlock? block; // only curly
}

public class Declaration {
    public string name;
    public List<ComponentValue> value = [];
    public bool important = false;
}

public abstract class ComponentValue { }

public class PreservedToken: ComponentValue {
    public Token token;
}

public class Function: ComponentValue {
    public string name;
    public List<ComponentValue> value;
}

public class SimpleBlock: ComponentValue {
    public Token token;
    public List<ComponentValue> value = [];
}


public class Parser(List<Token> input) {

    private static EofToken eofToken = new();
    private List<Token> input = input;
    private int index = 0;
    private Token CurrentInputToken = eofToken;
    private Token NextInputToken = eofToken;

    private void ConsumeTheNextInputToken() {
        if (reconsume) {
            reconsume = false;
            return;
        }
        CurrentInputToken = NextInputToken;
        NextInputToken = input.Count > index + 1 ? input[index++] : eofToken;
    }
    private bool reconsume = false;
    private void ReconsumeTheCurrentInputToken() {
        reconsume = true;
    }

    private readonly List<string> Errors = [];
    private void AddParseError(string error) {
        Errors.Add(error);
    }

    #region Parser Algorithms (https://www.w3.org/TR/css-syntax-3/#parser-algorithms)

    // https://www.w3.org/TR/css-syntax-3/#consume-list-of-rules
    public List<Rule> ConsumeAListOfRules(bool topLevel) {
        // To consume a list of rules, given a top-level flag:
        // Create an initially empty list of rules.
        List<Rule> rules = [];
        // Repeatedly consume the next input token:
        while (true) {
            ConsumeTheNextInputToken();
            switch (CurrentInputToken) {
                // <whitespace-token>
                case WhitespaceToken:
                    // Do nothing.
                    break;
                // <EOF-token>
                case EofToken:
                    // Return the list of rules.
                    return rules;
                // <CDO-token>
                // <CDC-token>
                case CDOToken:
                case CDCToken:
                    // If the top-level flag is set, do nothing.
                    if (topLevel) {
                        break;
                    }
                    // Otherwise, reconsume the current input token. Consume a qualified rule. If anything is returned, append it to the list of rules.
                    else {
                        ReconsumeTheCurrentInputToken();
                        if (ConsumeAQualifiedRule() is QualifiedRule r)
                            rules.Add(r);
                    }
                    break;

                // <at-keyword-token>
                case AtKeywordToken:
                    // Reconsume the current input token. Consume an at-rule, and append the returned value to the list of rules.
                    ReconsumeTheCurrentInputToken();
                    rules.Add(ConsumeAnAtRule());
                    break;
                // anything else
                default:
                    // Reconsume the current input token. Consume a qualified rule. If anything is returned, append it to the list of rules.
                    ReconsumeTheCurrentInputToken();
                    if (ConsumeAQualifiedRule() is QualifiedRule rule)
                        rules.Add(rule);
                    break;

            }
        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-at-rule
    private AtRule ConsumeAnAtRule() {
        // Consume the next input token. Create a new at-rule with its name set to the value of the current input token, its prelude initially set to an empty list, and its value initially set to nothing.
        ConsumeTheNextInputToken();
        var atRule = new AtRule() { };

        if (CurrentInputToken is ValueToken vToken) {
            atRule.name = vToken.value;
        }
        // Repeatedly consume the next input token:
        while (true) {
            ConsumeTheNextInputToken();
            switch (CurrentInputToken) {
                // <semicolon-token>
                case SemicolonToken:
                    // Return the at-rule.
                    return atRule;
                // <EOF-token>
                case EofToken:
                    // This is a parse error. Return the at-rule.
                    AddParseError("unexpected-eof-in-at-rule");
                    return atRule;
                // <{-token>
                case CurlyBracesOpenToken:
                    // Consume a simple block and assign it to the at-rule’s block. Return the at-rule.
                    atRule.block = ConsumeASimpleBlock();
                    return atRule;
                // simple block with an associated token of <{-token>
                case SimpleBlock { token: CurlyBracesOpenToken } block:
                    // Assign the block to the at-rule’s block. Return the at-rule.
                    atRule.block = block;
                    return atRule;
                // anything else
                default:
                    // Reconsume the current input token. Consume a component value. Append the returned value to the at-rule’s prelude.
                    ReconsumeTheCurrentInputToken();
                    atRule.prelude.Add(ConsumeAComponentValue());
                    break;
            }
        }

    }


    // https://www.w3.org/TR/css-syntax-3/#consume-qualified-rule
    private QualifiedRule? ConsumeAQualifiedRule() {
        // Create a new qualified rule with its prelude initially set to an empty list, and its value initially set to nothing.
        var qualifiedRule = new QualifiedRule();
        // Repeatedly consume the next input token:
        while (true) {
            ConsumeTheNextInputToken();
            switch (CurrentInputToken) {
                // <EOF-token>
                case EofToken:
                    // This is a parse error. Return nothing.
                    AddParseError("unexpected-eof-in-qualified-rule");
                    return null;
                // <{-token>
                case CurlyBracesOpenToken:
                    // Consume a simple block and assign it to the qualified rule’s block. Return the qualified rule.
                    qualifiedRule.block = ConsumeASimpleBlock();
                    return qualifiedRule;
                // simple block with an associated token of <{-token>
                case SimpleBlock { token: CurlyBracesOpenToken } block:
                    // Assign the block to the qualified rule’s block. Return the qualified rule.
                    qualifiedRule.block = block;
                    return qualifiedRule;
                // anything else
                default:
                    // Reconsume the current input token. Consume a component value. Append the returned value to the qualified rule’s prelude.        
                    ReconsumeTheCurrentInputToken();
                    qualifiedRule.prelude.Add(ConsumeAComponentValue());
                    break;
            }
        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-style-block
    private List<Declaration> ConsumeAStyleBlocksContents() {
        // Create an initially empty list of declarations decls, and an initially empty list of rules rules.
        List<Declaration> decls = [];
        List<Rule> rules = [];
        // Repeatedly consume the next input token:
        while (true) {
            ConsumeTheNextInputToken();
            switch (CurrentInputToken) {
                // <whitespace-token>
                case WhitespaceToken:
                // <semicolon-token>
                case SemicolonToken:
                    // Do nothing.
                    break;
                // <EOF-token>
                case EofToken:
                    // Extend decls with rules, then return decls.
                    decls.AddRange(rules);
                    return decls;
                // <at-keyword-token>
                case AtKeywordToken:
                    // Reconsume the current input token. Consume an at-rule, and append the result to rules.
                    ReconsumeTheCurrentInputToken();
                    rules.Add(ConsumeAnAtRule());
                    break;
                // <ident-token>
                case IdentToken:
                    // Initialize a temporary list initially filled with the current input token. As long as the next input token is anything other than a <semicolon-token> or <EOF-token>,
                    // consume a component value and append it to the temporary list. Consume a declaration from the temporary list. If anything was returned, append it to decls.
                    throw new NotImplementedException();
                // <delim-token> with a value of "&" (U+0026 AMPERSAND)                
                case DelimToken { value: '&' }:
                    // Reconsume the current input token. Consume a qualified rule. If anything was returned, append it to rules.
                    ReconsumeTheCurrentInputToken();
                    if (ConsumeAQualifiedRule() is QualifiedRule rule)
                        rules.Add(rule);
                    break;
                // anything else
                default:
                    // This is a parse error. Reconsume the current input token.
                    AddParseError("unexpected-token-in-a-style-block-content");
                    ReconsumeTheCurrentInputToken();
                    // As long as the next input token is anything other than a <semicolon-token> or <EOF-token>, consume a component value and throw away the returned value.
                    if (!(NextInputToken is SemicolonToken or EofToken))
                        ConsumeAComponentValue();
                    break;
            }
        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-list-of-declarations
    private List<Declaration> ConsumeAListOfDeclarations() {
        // Create an initially empty list of declarations.
        List<Declaration> list = [];
        // Repeatedly consume the next input token:
        while (true) {
            switch (CurrentInputToken) {
                // <whitespace-token>
                case WhitespaceToken:
                // <semicolon-token>
                case SemicolonToken:
                    // Do nothing.
                    break;
                // <EOF-token>
                case EofToken:
                    // Return the list of declarations.
                    return list;
                // <at-keyword-token>
                case AtKeywordToken:
                    // Reconsume the current input token. Consume an at-rule. Append the returned rule to the list of declarations.
                    ReconsumeTheCurrentInputToken();
                    list.Add(ConsumeAnAtRule());
                    break;
                // <ident-token>
                case IdentToken:
                    // Initialize a temporary list initially filled with the current input token. As long as the next input token is anything other than a <semicolon-token> or <EOF-token>,
                    // consume a component value and append it to the temporary list. Consume a declaration from the temporary list. If anything was returned, append it to the list of declarations.
                    throw new NotImplementedException();
                // anything else
                default:
                    // This is a parse error. Reconsume the current input token. As long as the next input token is anything other than a <semicolon-token> or <EOF-token>,
                    // consume a component value and throw away the returned value.
                    AddParseError("unexpected-token-in-a-list-of-declarations");
                    ReconsumeTheCurrentInputToken();
                    if (!(NextInputToken is SemicolonToken or EofToken))
                        ConsumeAComponentValue();
                    break;
            }
        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-declaration
    private Declaration? ConsumeADeclaration() {
        // Note: This algorithm assumes that the next input token has already been checked to be an <ident-token>.

        // Consume the next input token. Create a new declaration with its name set to the value of the current input token and its value initially set to an empty list.
        ConsumeTheNextInputToken();
        var declaration = new Declaration {
            name = ((IdentToken)CurrentInputToken).value
        };

        // 1. While the next input token is a <whitespace-token>, consume the next input token.
        while (NextInputToken is WhitespaceToken) {
            ConsumeTheNextInputToken();
        }
        // 2. If the next input token is anything other than a <colon-token>, this is a parse error. Return nothing.
        if (NextInputToken is not ColonToken) {
            AddParseError("expected-colon");
            return null;
        }
        // Otherwise, consume the next input token.
        ConsumeTheNextInputToken();
        // 3. While the next input token is a <whitespace-token>, consume the next input token.
        while (NextInputToken is WhitespaceToken) {
            ConsumeTheNextInputToken();
        }
        // 4. As long as the next input token is anything other than an <EOF-token>, consume a component value and append it to the declaration’s value.
        if (NextInputToken is not EofToken) {
            declaration.value.Add(ConsumeAComponentValue());
        }
        // If the last two non-<whitespace-token>s in the declaration’s value are a <delim-token> with the value "!" followed by an <ident-token> with a value that is an ASCII case-insensitive match for "important",
        // remove them from the declaration’s value and set the declaration’s important flag to true.
        // todo
        // While the last token in the declaration’s value is a <whitespace-token>, remove that token.
        // todo
        // Return the declaration.
        return declaration;
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-component-value
    private ComponentValue ConsumeAComponentValue() {
        // Consume the next input token.
        ConsumeTheNextInputToken();
        // If the current input token is a <{-token>, <[-token>, or <(-token>, consume a simple block and return it.
        if (CurrentInputToken is CurlyBracesOpenToken or SquareBracketOpenToken or BracketOpenToken) {
            return ConsumeASimpleBlock();
        }
        // Otherwise, if the current input token is a <function-token>, consume a function and return it.
        if (CurrentInputToken is FunctionToken) {
            return ConsumeAFunction();
        }
        // Otherwise, return the current input token.
        return CurrentInputToken;
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-simple-block
    private SimpleBlock ConsumeASimpleBlock() {
        // Note: This algorithm assumes that the current input token has already been checked to be an <{-token>, <[-token>, or <(-token>.
        // The ending token is the mirror variant of the current input token. (E.g. if it was called with <[-token>, the ending token is <]-token>.)
        var startToken = CurrentInputToken;
        // Create a simple block with its associated token set to the current input token and with its value initially set to an empty list.
        var block = new SimpleBlock() { token = CurrentInputToken };
        // Repeatedly consume the next input token and process it as follows:
        while (true) {
            ConsumeTheNextInputToken();
            switch (CurrentInputToken) {
                // ending token
                case CurlyBracesCloseToken when startToken is CurlyBracesOpenToken:
                case SquareBracketCloseToken when startToken is SquareBracketOpenToken:
                case BracketCloseToken when startToken is BracketOpenToken:
                    // Return the block.
                    return block;
                // <EOF-token>
                case EofToken:
                    // This is a parse error. Return the block.
                    AddParseError("unexpected-eof-in-simple-block");
                    return block;
                // anything else
                default:
                    // Reconsume the current input token. Consume a component value and append it to the value of the block.
                    ReconsumeTheCurrentInputToken();
                    block.value.Add(ConsumeAComponentValue());
                    break;
            }
        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-function
    private Function ConsumeAFunction() {
        // Note: This algorithm assumes that the current input token has already been checked to be a <function-token>.        
        // Create a function with its name equal to the value of the current input token and with its value initially set to an empty list.
        var function = new Function {
            name = ((FunctionToken)CurrentInputToken).value
        };
        // Repeatedly consume the next input token and process it as follows:
        while (true) {
            ConsumeTheNextInputToken();
            switch (CurrentInputToken) {
                // <)-token>
                case BracketCloseToken:
                    // Return the function.
                    return function;
                // <EOF-token>
                case EofToken:
                    // This is a parse error. Return the function.
                    AddParseError("unexpected-eof-in-function");
                    return function;
                // anything else
                default:
                    // Reconsume the current input token. Consume a component value and append the returned value to the function’s value.
                    ReconsumeTheCurrentInputToken();
                    function.value.Add(ConsumeAComponentValue());
                    break;
            }
        }

    }


    #endregion

}

#endif