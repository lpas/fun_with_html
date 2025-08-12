

using System.Globalization;
using System.Text;


namespace FunWithHtml.css.Tokenizer;



public abstract class Token {
    public abstract string NAME { get; }
    public override string ToString() {
        return $"<{NAME}-token>";
    }
}

public abstract class ValueToken(string value): Token {
    public string value = value;
    public override string ToString() {
        if (value.Length != 0) {
            return $"<{NAME}-token '{value}'>";
        } else {
            return base.ToString();
        }
    }
}


public class IdentToken(string value): ValueToken(value) {
    public override string NAME => "ident";
}
public class FunctionToken(string value): ValueToken(value) {
    public override string NAME => "function";

}
public class AtKeywordToken(string value): ValueToken(value) {
    public override string NAME => "at-keyword";
}

public enum HashTokenType {
    id,
    unrestricted,
}
public class HashToken(string value): ValueToken(value) {
    public override string NAME => "hash";
    public HashTokenType type = HashTokenType.unrestricted;
    public override string ToString() {
        return $"<{NAME}-token '{value}' {type}>";
    }

}
public class StringToken(string value): ValueToken(value) {
    public override string NAME => "string";
}
public class UrlToken(string value): ValueToken(value) {
    public override string NAME => "url";
}

public class DelimToken(char value): Token {
    public override string NAME => "delim";
    public char value = value;

    public override string ToString() {
        return $"<{NAME}-token {value}>";
    }
}

public class NumberToken(decimal value, NumberTokenType type = NumberTokenType.integer): Token {
    public override string NAME => "number";
    public decimal value = value;
    public NumberTokenType type = type;
    public override string ToString() {
        return $"<{NAME}-token {value.ToString("G", CultureInfo.InvariantCulture)} {type}>";
    }
}

public enum NumberTokenType {
    integer,
    number,
}
public class PercentageToken(decimal value, NumberTokenType type = NumberTokenType.integer): NumberToken(value, type) {
    public override string NAME => "percentage";

}
public class DimensionToken(decimal value, string unit, NumberTokenType type = NumberTokenType.integer): NumberToken(value, type) {
    public override string NAME => "dimension";
    public string unit = unit;
    public override string ToString() {
        return $"<{NAME}-token {value} {unit} {type}>";
    }
}


public class BadStringToken: Token {
    public override string NAME => "bad-string";
};
public class BadUrlToken: Token {
    public override string NAME => "bad-url";
};
public class WhitespaceToken: Token {
    public override string NAME => "whitespace";
};
public class CDOToken: Token {
    public override string NAME => "CDO";
};
public class CDCToken: Token {
    public override string NAME => "CDC";
};
public class ColonToken: Token {
    public override string NAME => "colon";
};
public class SemicolonToken: Token {
    public override string NAME => "semicolon";
};
public class CommaToken: Token {
    public override string NAME => "comma";
};
public class SquareBracketOpenToken: Token {
    public override string NAME => "[";
};
public class SquareBracketCloseToken: Token {
    public override string NAME => "]";
};
public class BracketOpenToken: Token {
    public override string NAME => "(";
};
public class BracketCloseToken: Token {
    public override string NAME => ")";
};
public class CurlyBracesOpenToken: Token {
    public override string NAME => "{";
};
public class CurlyBracesCloseToken: Token {
    public override string NAME => "}";
};

public class EofToken: Token {
    public override string NAME => "eof";
}
class Tokenizer {

    // https://www.w3.org/TR/css-syntax-3/#maximum-allowed-code-point
    private const int MAXIMUM_ALLOWED_CODE_POINT = 0x10FFFF;
    private int index = 0;
    private readonly string input;

    private char? NextInputCodePoint;
    private char? CurrentInputCodePoint;

    public List<string> Errors = [];

    // https://www.w3.org/TR/css-syntax-3/#input-preprocessing
    private static string PreprocessingTheInput(string input) {
        var result = new StringBuilder(input.Length);
        var lastChrWasCR = false;
        foreach (var chr in input) {
            // Replace any U+000D CARRIAGE RETURN (CR) code points, U+000C FORM FEED (FF) code points,
            // or pairs of U+000D CARRIAGE RETURN (CR) followed by U+000A LINE FEED (LF) in input by a single U+000A LINE FEED (LF) code point.            
            if (chr is '\u000D') {
                result.Append('\u000A');
                lastChrWasCR = true;
                continue;
            } else
            if (chr is '\u000C') {
                result.Append('\u000A');
            } else
            if (chr is '\u000A' && lastChrWasCR) {
                // do nothing CR was already replaced with LF
            } else
            // Replace any U+0000 NULL or surrogate code points in input with U+FFFD REPLACEMENT CHARACTER
            if (chr is '\0' || IsSurrogate(chr)) {
                result.Append('\uFFFD');
            } else {
                result.Append(chr);
            }
            lastChrWasCR = false;

        }

        return result.ToString();
    }

    public Tokenizer(string input) {
        this.input = PreprocessingTheInput(input);
        this.NextInputCodePoint = input.Length > 0 ? input[0] : null;
    }

    private char? ConsumeNextInputCodePoint() {
        if (input.Length > index) {
            CurrentInputCodePoint = input[index++];
            NextInputCodePoint = input.Length > index ? input[index] : null;
        } else {
            index++;
            CurrentInputCodePoint = null;
            NextInputCodePoint = null;
        }
        return CurrentInputCodePoint;

    }

    private static bool IsLeadingSurrogate(char c) {
        return c is >= '\uD800' and <= '\uDBFF';
    }
    private static bool IsTrailingSurrogate(char c) {
        return c is >= '\uDC00' and <= '\uDFFF';
    }

    private static bool IsSurrogate(char c) {
        return IsLeadingSurrogate(c) || IsTrailingSurrogate(c);
    }

    private void Reconsume() {
        index--;
        NextInputCodePoint = input.Length > index ? input[index] : null;
    }

    private void ConsumeNextInputCharacters(int v) {
        for (var i = 0; i < v; i++) {
            ConsumeNextInputCodePoint();
        }
    }
    private string NextInputCodePoints(int v) {
        var x = Math.Min(input.Length - index, v);
        return index <= input.Length ? input.Substring(index, x) : "";
    }

    private void AddParseError(string error) {
        Errors.Add(error);
    }

    #region Tokenizer Algorithms (https://www.w3.org/TR/css-syntax-3/#tokenizer-algorithms)
    // https://www.w3.org/TR/css-syntax-3/#consume-token
    public Token ConsumeAToken() {

        // Consume comments.
        ConsumeComments();

        // Consume the next input code point.
        char? c = ConsumeNextInputCodePoint();
        switch (c) {
            // whitespace
            case char chr when IsWhitespace(chr):
                // Consume as much whitespace as possible. Return a <whitespace-token>.
                while (IsWhitespace(NextInputCodePoint)) {
                    ConsumeNextInputCodePoint();
                }
                return new WhitespaceToken();
            // U+0022 QUOTATION MARK (")
            case '"':
                // Consume a string token and return it.
                return ConsumeAStringToken();
            // U+0023 NUMBER SIGN (#)
            case '#':
                // If the next input code point is an ident code point or the next two input code points are a valid escape, then:
                if (IsIdentCodePoint(NextInputCodePoint) || AreAValidEscape(NextInputCodePoints(2))) {
                    // 1. Create a <hash-token>.
                    var token = new HashToken("");
                    // 2. If the next 3 input code points would start an ident sequence, set the <hash-token>’s type flag to "id".
                    if (WouldStartAnIdentSequence(NextInputCodePoints(3))) {
                        token.type = HashTokenType.id;
                    }
                    // 3. Consume an ident sequence, and set the <hash-token>’s value to the returned string.
                    token.value = ConsumeAnIdentSequence();
                    // 4. Return the <hash-token>.
                    return token;
                } else {
                    // Otherwise, return a <delim-token> with its value set to the current input code point.
                    return new DelimToken('#');
                }
            // U+0027 APOSTROPHE (')
            case '\'':
                // Consume a string token and return it.
                return ConsumeAStringToken();
            // U+0028 LEFT PARENTHESIS (()
            case '(':
                // Return a <(-token>.
                return new BracketOpenToken();
            // U+0029 RIGHT PARENTHESIS ())
            case ')':
                // Return a <)-token>.
                return new BracketCloseToken();
            // U+002B PLUS SIGN (+)
            case '+':
                // If the input stream starts with a number, reconsume the current input code point, consume a numeric token, and return it.
                if (WouldStartANumber()) {
                    Reconsume();
                    return ConsumeANumericToken();
                } else {
                    // Otherwise, return a <delim-token> with its value set to the current input code point.
                    return new DelimToken('+');
                }
            // U+002C COMMA (,)
            case ',':
                // Return a <comma-token>.
                return new CommaToken();
            // U+002D HYPHEN-MINUS (-)                
            case '-':
                // If the input stream starts with a number, reconsume the current input code point, consume a numeric token, and return it.
                if (WouldStartANumber()) {
                    Reconsume();
                    return ConsumeANumericToken();
                } else if (NextInputCodePoints(2) == "->") {
                    // Otherwise, if the next 2 input code points are U+002D HYPHEN-MINUS U+003E GREATER-THAN SIGN (->), consume them and return a <CDC-token>.
                    ConsumeNextInputCharacters(2);
                    return new CDCToken();
                } else if (WouldStartAnIdentSequence()) {
                    // Otherwise, if the input stream starts with an ident sequence, reconsume the current input code point, consume an ident-like token, and return it.
                    Reconsume();
                    return ConsumeAnIdentLikeToken();
                } else {
                    // Otherwise, return a <delim-token> with its value set to the current input code point.
                    return new DelimToken('-');
                }
            // U+002E FULL STOP (.)    
            case '.':
                // If the input stream starts with a number, reconsume the current input code point, consume a numeric token, and return it.
                if (WouldStartANumber()) {
                    Reconsume();
                    return ConsumeANumericToken();
                } else {
                    // Otherwise, return a <delim-token> with its value set to the current input code point.
                    return new DelimToken('.');
                }
            // U+003A COLON (:)    
            case ':':
                // Return a <colon-token>.
                return new ColonToken();
            // U+003B SEMICOLON (;)
            case ';':
                // Return a <semicolon-token>.
                return new SemicolonToken();
            // U+003C LESS-THAN SIGN (<)
            case '<':
                // If the next 3 input code points are U+0021 EXCLAMATION MARK U+002D HYPHEN-MINUS U+002D HYPHEN-MINUS (!--), consume them and return a <CDO-token>.
                if (NextInputCodePoints(3) == "!--") {
                    ConsumeNextInputCharacters(3);
                    return new CDOToken();
                } else {
                    // Otherwise, return a <delim-token> with its value set to the current input code point.
                    return new DelimToken('<');
                }
            // U+0040 COMMERCIAL AT (@)
            case '@':
                // If the next 3 input code points would start an ident sequence, consume an ident sequence, create an <at-keyword-token> with its value set to the returned value, and return it.
                if (WouldStartAnIdentSequence(NextInputCodePoints(3))) {
                    return new AtKeywordToken(ConsumeAnIdentSequence());
                } else {
                    // Otherwise, return a <delim-token> with its value set to the current input code point.
                    return new DelimToken('@');
                }
            // U+005B LEFT SQUARE BRACKET ([)
            case '[':
                // Return a <[-token>.                
                return new SquareBracketOpenToken();
            // U +005C REVERSE SOLIDUS (\)
            case '\\':
                // If the input stream starts with a valid escape, reconsume the current input code point, consume an ident-like token, and return it.
                if (AreAValidEscape()) {
                    Reconsume();
                    return ConsumeAnIdentLikeToken();
                } else {
                    // Otherwise, this is a parse error. Return a <delim-token> with its value set to the current input code point.                
                    AddParseError("non valid escape sequence");
                    return new DelimToken('\\');
                }
            // U+005D RIGHT SQUARE BRACKET (])
            case ']':
                // Return a <]-token>.
                return new SquareBracketCloseToken();
            // U+007B LEFT CURLY BRACKET ({)
            case '{':
                // Return a <{-token>.
                return new CurlyBracesOpenToken();
            // U+007D RIGHT CURLY BRACKET (})
            case '}':
                // Return a <}-token>.
                return new CurlyBracesCloseToken();
            // digit
            case char chr when IsDigit(chr):
                // Reconsume the current input code point, consume a numeric token, and return it.
                Reconsume();
                return ConsumeANumericToken();
            // ident-start code point                
            case char chr when IsIdentStartCodePoint(chr):
                // Reconsume the current input code point, consume an ident-like token, and return it.
                Reconsume();
                return ConsumeAnIdentLikeToken();
            // EOF
            case null:
                // Return an <EOF-token>.
                return new EofToken();
            // anything else
            default:
                // Return a<delim-token > with its value set to the current input code point.
                return new DelimToken(CurrentInputCodePoint!.Value);

        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-comment
    private void ConsumeComments() {
        // If the next two input code point are U+002F SOLIDUS (/) followed by a U+002A ASTERISK (*), consume them and all following code points up to and including the first U+002A ASTERISK (*) 
        // followed by a U+002F SOLIDUS (/), or up to an EOF code point. Return to the start of this step.
        while (true) {
            if (NextInputCodePoints(2) == "/*") {
                ConsumeNextInputCharacters(2);
                while (CurrentInputCodePoint is not null && NextInputCodePoints(2) != "*/") {
                    ConsumeNextInputCodePoint();
                }
                ConsumeNextInputCharacters(2);
                // If the preceding paragraph ended by consuming an EOF code point, this is a parse error.
                if (CurrentInputCodePoint is null) {
                    AddParseError("eof-in-comment");
                }
            } else {
                break;
            }
        }
        // Return nothing.
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-numeric-token
    private Token ConsumeANumericToken() {
        // Consume a number and let number be the result.
        var number = ConsumeANumber();
        // If the next 3 input code points would start an ident sequence, then:
        if (WouldStartAnIdentSequence(NextInputCodePoints(3))) {
            // 1. Create a <dimension-token> with the same value and type flag as number, and a unit set initially to the empty string.
            // 2. Consume an ident sequence. Set the <dimension-token>’s unit to the returned value.
            // 3. Return the <dimension-token>.
            return new DimensionToken(number.value, ConsumeAnIdentSequence(), number.type);
        } else if (NextInputCodePoint == '%') {
            // Otherwise, if the next input code point is U+0025 PERCENTAGE SIGN (%), consume it. Create a <percentage-token> with the same value as number, and return it.    
            return new PercentageToken(number.value);
        } else {
            // Otherwise, create a <number-token> with the same value and type flag as number, and return it.
            return new NumberToken(number.value, number.type);
        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-ident-like-token
    private Token ConsumeAnIdentLikeToken() {
        // Consume an ident sequence, and let string be the result.
        var @string = ConsumeAnIdentSequence();
        // If string’s value is an ASCII case-insensitive match for "url", and the next input code point is U+0028 LEFT PARENTHESIS ((), consume it.
        // While the next two input code points are whitespace, consume the next input code point.
        // If the next one or two input code points are U+0022 QUOTATION MARK ("), U+0027 APOSTROPHE ('), or whitespace followed by U+0022 QUOTATION MARK (") or U+0027 APOSTROPHE ('),
        // then create a <function-token> with its value set to string and return it.
        // Otherwise, consume a url token, and return it.
        if (string.Equals(@string, "url", StringComparison.OrdinalIgnoreCase) && NextInputCodePoint == '(') {
            ConsumeNextInputCodePoint();
            while (NextInputCodePoints(2) == "  ") {
                ConsumeNextInputCodePoint();
            }
            var chr = NextInputCodePoints(2);
            if ((chr.Length > 1 && chr[0] is '"' or '\'')
            || (chr.Length == 2 && IsWhitespace(chr[0]) && chr[1] is '"' or '\'')
            ) {
                return new FunctionToken(@string);
            } else {
                return ConsumeAUrlToken();
            }
        } else if (NextInputCodePoint == '(') {
            // Otherwise, if the next input code point is U+0028 LEFT PARENTHESIS ((), consume it. Create a <function-token> with its value set to string and return it.    
            ConsumeNextInputCodePoint();
            return new FunctionToken(@string);
        } else {
            // Otherwise, create an <ident-token> with its value set to string and return it.
            return new IdentToken(@string);
        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-string-token
    private Token ConsumeAStringToken() {
        // This algorithm may be called with an ending code point, which denotes the code point that ends the string. If an ending code point is not specified, the current input code point is used.
        var endCodePoint = CurrentInputCodePoint;
        // Initially create a <string-token> with its value set to the empty string.        
        var token = new StringToken("");

        // Repeatedly consume the next input code point from the stream:
        while (true) {
            char? c = ConsumeNextInputCodePoint();
            switch (c) {
                // ending code point
                case char chr when chr == endCodePoint:
                    // Return the <string-token>.
                    return token;
                // EOF
                case null:
                    // This is a parse error. Return the <string-token>.
                    AddParseError("unexpected-eof-in-string-token");
                    return token;
                // newline                    
                case char chr when IsNewline(chr):
                    // This is a parse error. Reconsume the current input code point, create a <bad-string-token>, and return it.
                    AddParseError("unexpected-line-ending-in-string-token");
                    return new BadStringToken();
                // U+005C REVERSE SOLIDUS (\)
                case '\\':
                    // If the next input code point is EOF, do nothing.
                    if (NextInputCodePoint is null) {
                    } else if (IsNewline(NextInputCodePoint)) {
                        // Otherwise, if the next input code point is a newline, consume it.
                        ConsumeNextInputCodePoint();
                    } else if (AreAValidEscape()) {
                        // Otherwise, (the stream starts with a valid escape) consume an escaped code point and append the returned code point to the <string-token>’s value.
                        token.value += ConsumeAnEscapedCodePoint();
                    }
                    break;
                // anything else
                default:
                    // Append the current input code point to the <string-token>’s value.
                    token.value += CurrentInputCodePoint!.Value;
                    break;
            }
        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-url-token
    private Token ConsumeAUrlToken() {
        // Note: This algorithm assumes that the initial "url(" has already been consumed.
        // This algorithm also assumes that it’s being called to consume an "unquoted" value, like url(foo). A quoted value, like url("foo"), is parsed as a <function-token>.
        // Consume an ident-like token automatically handles this distinction; this algorithm shouldn’t be called directly otherwise.

        // 1. Initially create a <url-token> with its value set to the empty string.
        var token = new UrlToken("");
        // 2. Consume as much whitespace as possible.
        if (IsWhitespace(CurrentInputCodePoint)) {
            while (IsWhitespace(NextInputCodePoint)) {
                ConsumeNextInputCodePoint();
            }
        }
        while (true) {
            // 3. Repeatedly consume the next input code point from the stream:
            var c = ConsumeNextInputCodePoint();
            switch (c) {
                // U+0029 RIGHT PARENTHESIS ())
                case ')':
                    // Return the <url-token>.
                    return token;
                // EOF
                case null:
                    // This is a parse error. Return the <url-token>.
                    AddParseError("unexpected-eof-in-url");
                    return token;
                // whitespace                    
                case char chr when IsWhitespace(chr):
                    // Consume as much whitespace as possible. If the next input code point is U+0029 RIGHT PARENTHESIS ()) or EOF, consume it and return the <url-token> 
                    // (if EOF was encountered, this is a parse error); otherwise, consume the remnants of a bad url, create a <bad-url-token>, and return it.
                    while (IsWhitespace(NextInputCodePoint)) {
                        ConsumeNextInputCodePoint();
                    }
                    if (NextInputCodePoint is ')' or null) {
                        if (ConsumeNextInputCodePoint() is null) {
                            AddParseError("unexpected-eof-in-url");
                        }
                        return token;
                    }
                    ConsumeTheRemnantsOfABadUrl();
                    return new BadUrlToken();
                // U+0022 QUOTATION MARK (")
                case '"':
                // U+0027 APOSTROPHE (')
                case '\'':
                // U+0028 LEFT PARENTHESIS (()
                case '(':
                // non-printable code point
                case char chr when IsNonPrintable(chr):
                    // This is a parse error. Consume the remnants of a bad url, create a <bad-url-token>, and return it.
                    AddParseError("bad-url");
                    ConsumeTheRemnantsOfABadUrl();
                    return new BadUrlToken();
                // U+005C REVERSE SOLIDUS (\)
                case '\\':
                    // If the stream starts with a valid escape, consume an escaped code point and append the returned code point to the <url-token>’s value.
                    // Otherwise, this is a parse error. Consume the remnants of a bad url, create a <bad-url-token>, and return it.
                    if (AreAValidEscape()) {
                        token.value += ConsumeAnEscapedCodePoint();
                        break;
                    } else {
                        AddParseError("invalid-escaped-value");
                        ConsumeTheRemnantsOfABadUrl();
                        return new BadUrlToken();
                    }
                // anything else
                default:
                    // Append the current input code point to the <url-token>’s value.
                    token.value += c.Value;
                    break;
            }
        }
    }


    // https://www.w3.org/TR/css-syntax-3/#consume-escaped-code-point
    private char ConsumeAnEscapedCodePoint() {
        // Consume the next input code point.

        var c = ConsumeNextInputCodePoint();
        // hex digit
        if (IsHexDigit(c)) {
            var str = $"{c}";
            // Consume as many hex digits as possible, but no more than 5.
            for (var i = 0; i < 5; i++) {
                if (IsHexDigit(NextInputCodePoint)) {
                    str += ConsumeNextInputCodePoint();
                } else {
                    break;
                }
            }
            // Note that this means 1-6 hex digits have been consumed in total.
            // If the next input code point is whitespace, consume it as well. Interpret the hex digits as a hexadecimal number.
            if (IsWhitespace(NextInputCodePoint)) {
                ConsumeNextInputCodePoint();
            }
            var number = int.Parse(str, NumberStyles.HexNumber);
            // If this number is zero, or is for a surrogate, or is greater than the maximum allowed code point, return U+FFFD REPLACEMENT CHARACTER (�).
            // Otherwise, return the code point with that value.
            if (number is 0 or > MAXIMUM_ALLOWED_CODE_POINT) {
                return '\uFFFD';
            }
            return (char)number;
        }
        // EOF
        if (c is null) {
            // This is a parse error. Return U+FFFD REPLACEMENT CHARACTER (�).
            AddParseError("unexpected-eof-in-escaped-code-point");
            return '\uFFFD';
        }

        // anything else
        // Return the current input code point.
        return c.Value;
    }

    // https://www.w3.org/TR/css-syntax-3/#check-if-two-code-points-are-a-valid-escape
    private bool AreAValidEscape(string? str = null) {
        // This section describes how to check if two code points are a valid escape. The algorithm described here can be called explicitly with two code points, or can be called with the input stream itself.
        // In the latter case, the two code points in question are the current input code point and the next input code point, in that order.
        str ??= $"{CurrentInputCodePoint}{NextInputCodePoint}";
        // Note: This algorithm will not consume any additional code point.

        // If the first code point is not U+005C REVERSE SOLIDUS (\), return false.
        if (str.Length == 0 || str[0] is not '\\') return false;
        // Otherwise, if the second code point is a newline, return false.
        if (str.Length >= 2 && IsNewline(str[1])) return false;
        // Otherwise, return true.
        return true;
    }


    // https://www.w3.org/TR/css-syntax-3/#check-if-three-code-points-would-start-an-ident-sequence
    private bool WouldStartAnIdentSequence(string? str = null) {
        // This section describes how to check if three code points would start an ident sequence. The algorithm described here can be called explicitly with three code points,
        // or can be called with the input stream itself. In the latter case, the three code points in question are the current input code point and the next two input code points, in that order.
        str ??= CurrentInputCodePoint + NextInputCodePoints(2);
        // Note: This algorithm will not consume any additional code points.

        // Look at the first code point:

        if (str.Length == 0) return false;
        return str[0] switch {
            // U+002D HYPHEN-MINUS
            '-' =>
                // If the second code point is an ident-start code point or a U+002D HYPHEN-MINUS,
                (str.Length >= 2 && (IsIdentStartCodePoint(str[1]) || str[1] is '-'))
                ||
                // or the second and third code points are a valid escape, return true. Otherwise, return false.
                (str.Length >= 3 && AreAValidEscape(str[1..2])),
            // ident-start code point
            char chr when IsIdentStartCodePoint(chr) =>
                // Return true.
                true,
            // U+005C REVERSE SOLIDUS (\)
            '\\' =>
                // If the first and second code points are a valid escape, return true. Otherwise, return false.
                str.Length >= 2 && AreAValidEscape(str[0..1]),
            // anything else
            _ => false,// Return false.
        };
    }

    // https://www.w3.org/TR/css-syntax-3/#check-if-three-code-points-would-start-a-number
    private bool WouldStartANumber() {
        // This section describes how to check if three code points would start a number. The algorithm described here can be called explicitly with three code points,
        //  or can be called with the input stream itself. In the latter case, the three code points in question are the current input code point and the next two input code points, in that order.

        // Note: This algorithm will not consume any additional code points.
        var str = CurrentInputCodePoint + NextInputCodePoints(2);

        // Look at the first code point:
        switch (str[0]) {
            case '+':
            case '-':
                // If the second code point is a digit, return true.
                if (str.Length > 1 && IsDigit(str[1])) return true;
                // Otherwise, if the second code point is a U+002E FULL STOP (.) and the third code point is a digit, return true.
                if (str.Length > 2 && str[1] == '.' && IsDigit(str[2])) return true;

                return false;
            case '.':
                // If the second code point is a digit, return true.  Otherwise, return false.
                return str.Length > 1 && IsDigit(str[1]);
            case char chr when IsDigit(chr):
                return true;
            default:
                return false;
        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-name
    private string ConsumeAnIdentSequence() {
        // This section describes how to consume an ident sequence from a stream of code points.
        // It returns a string containing the largest name that can be formed from adjacent code points in the stream, starting from the first.

        // Note: This algorithm does not do the verification of the first few code points that are necessary to ensure the returned code points would constitute an <ident-token>.
        // If that is the intended use, ensure that the stream starts with an ident sequence before calling this algorithm.        

        // Let result initially be an empty string.
        var result = "";
        // Repeatedly consume the next input code point from the stream:
        while (true) {
            var c = ConsumeNextInputCodePoint();
            // ident code point
            if (IsIdentCodePoint(c)) {
                // Append the code point to result.
                result += c;
            } else if (AreAValidEscape()) {
                // the stream starts with a valid escape
                // Consume an escaped code point. Append the returned code point to result.
                result += ConsumeAnEscapedCodePoint();
            } else {
                // anything else
                // Reconsume the current input code point. Return result.
                Reconsume();
                return result;
            }
        }
    }

    // https://www.w3.org/TR/css-syntax-3/#consume-number
    private (decimal value, NumberTokenType type) ConsumeANumber() {
        // Note: This algorithm does not do the verification of the first few code points that are necessary to ensure a number can be obtained from the stream.
        // Ensure that the stream starts with a number before calling this algorithm.

        // Execute the following steps in order:
        // 1. Initially set type to "integer". Let repr be the empty string.
        var type = NumberTokenType.integer;
        var repr = "";
        // 2. If the next input code point is U+002B PLUS SIGN (+) or U+002D HYPHEN-MINUS (-), consume it and append it to repr.
        if (NextInputCodePoint is '+' or '-') {
            repr += ConsumeNextInputCodePoint();
        }
        // 3. While the next input code point is a digit, consume it and append it to repr.
        while (IsDigit(NextInputCodePoint)) {
            repr += ConsumeNextInputCodePoint();
        }
        // 4. If the next 2 input code points are U+002E FULL STOP (.) followed by a digit, then:
        var chr = NextInputCodePoints(2);
        if (chr.Length == 2 && chr[0] == '.' && IsDigit(chr[1])) {
            // 1. Consume them.
            ConsumeNextInputCharacters(2);
            // 2. Append them to repr.
            repr += chr;
            // 3. Set type to "number".
            type = NumberTokenType.number;
            // 4. While the next input code point is a digit, consume it and append it to repr.
            while (IsDigit(NextInputCodePoint)) {
                repr += ConsumeNextInputCodePoint();
            }
        }
        // 5. If the next 2 or 3 input code points are U+0045 LATIN CAPITAL LETTER E (E) or U+0065 LATIN SMALL LETTER E (e), optionally followed by U+002D HYPHEN-MINUS (-) or U+002B PLUS SIGN (+), followed by a digit, then:
        chr = NextInputCodePoints(3);
        var firstIsE = chr.Length > 0 && chr[0] is 'E' or 'e';
        if (firstIsE && (
            (chr.Length == 2 && IsDigit(chr[1]))
            || (chr.Length == 3 && (
                (chr[1] is '+' or '-' && IsDigit(chr[2]))
                || IsDigit(chr[1])
            ))
        )) {
            // 1. Consume them.
            ConsumeNextInputCharacters(chr.Length);
            // 2. Append them to repr.
            repr += chr;
            // 3. Set type to "number".
            type = NumberTokenType.number;
            // 4. While the next input code point is a digit, consume it and append it to repr.
            while (IsDigit(NextInputCodePoint)) {
                repr += ConsumeNextInputCodePoint();
            }
        }
        // 6. Convert repr to a number, and set the value to the returned value.
        var value = ConvertToANumber(repr);
        // 7. Return value and type.
        return (value, type);
    }

    // https://www.w3.org/TR/css-syntax-3/#convert-string-to-number
    private static decimal ConvertToANumber(string repr) {
        // Note: This algorithm does not do any verification to ensure that the string contains only a number. Ensure that the string contains only a valid CSS number before calling this algorithm.
        var length = repr.Length;
        var cIndex = 0;
        // Divide the string into seven components, in order from left to right:        
        // 1. A sign: a single U+002B PLUS SIGN (+) or U+002D HYPHEN-MINUS (-), or the empty string. Let s be the number -1 if the sign is U+002D HYPHEN-MINUS (-); otherwise, let s be the number 1.
        var s = 1;
        if (length > cIndex && repr[cIndex] is '-') {
            s = -1;
            cIndex++;
        } else if (length > cIndex && repr[cIndex] is '+') {
            cIndex++;
        }
        // 2. An integer part: zero or more digits. If there is at least one digit, let i be the number formed by interpreting the digits as a base-10 integer; otherwise, let i be the number 0.
        var iStr = "";
        while (length > cIndex && IsDigit(repr[cIndex])) {
            iStr += repr[cIndex++];
        }
        var i = iStr.Length > 0 ? int.Parse(iStr) : 0;
        // 3. A decimal point: a single U+002E FULL STOP (.), or the empty string.
        if (length > cIndex && repr[cIndex] == '.') {
            cIndex++;
        }
        // 4. A fractional part: zero or more digits. If there is at least one digit, let f be the number formed by interpreting the digits as a base-10 integer and d be the number of digits;
        // otherwise, let f and d be the number 0.
        var fStr = "";
        while (length > cIndex && IsDigit(repr[cIndex])) {
            fStr += repr[cIndex++];
        }
        var f = 0;
        var d = fStr.Length;
        if (d > 0) {
            f = int.Parse(fStr);
        }
        // 5. An exponent indicator: a single U+0045 LATIN CAPITAL LETTER E (E) or U+0065 LATIN SMALL LETTER E (e), or the empty string.
        if (length > cIndex && repr[cIndex] is 'E' or 'e') {
            cIndex++;
        }
        // 6. An exponent sign: a single U+002B PLUS SIGN (+) or U+002D HYPHEN-MINUS (-), or the empty string. Let t be the number -1 if the sign is U+002D HYPHEN-MINUS (-); otherwise, let t be the number 1.
        var t = 1;
        if (length > cIndex && repr[cIndex] is '-') {
            cIndex++;
            t = -1;
        } else if (length > cIndex && repr[cIndex] is '-') {
            cIndex++;
        }
        // 7. An exponent: zero or more digits. If there is at least one digit, let e be the number formed by interpreting the digits as a base-10 integer; otherwise, let e be the number 0.
        var eStr = "";
        while (length > cIndex && IsDigit(repr[cIndex])) {
            eStr += repr[cIndex++];
        }
        var e = eStr.Length > 0 ? int.Parse(eStr) : 0;
        // Return the number s·(i + f·10^{-d})·10^{te}.
        return (decimal)(s * (i + f * Math.Pow(10, -d)) * Math.Pow(10, t * e));
    }


    // https://www.w3.org/TR/css-syntax-3/#consume-remnants-of-bad-url
    private void ConsumeTheRemnantsOfABadUrl() {
        // Repeatedly consume the next input code point from the stream:
        while (true) {
            var c = ConsumeNextInputCodePoint();
            // U+0029 RIGHT PARENTHESIS ())
            // EOF
            if (c is ')' or null) {
                return;
            } else if (AreAValidEscape()) {
                // the input stream starts with a valid escape
                // Consume an escaped code point. 
                ConsumeAnEscapedCodePoint();
                // Note: This allows an escaped right parenthesis ("\)") to be encountered without ending the <bad-url-token>. This is otherwise identical to the "anything else" clause.
            } else {
                // anything else
                // Do nothing.
            }
        }
    }

    #endregion

    #region Definitions (https://www.w3.org/TR/css-syntax-3/#tokenizer-definitions)

    // https://www.w3.org/TR/css-syntax-3/#digit
    private static bool IsDigit(char? c) {
        // A code point between U+0030 DIGIT ZERO (0) and U+0039 DIGIT NINE (9) inclusive.
        return c is >= '0' and <= '9';
    }

    // https://www.w3.org/TR/css-syntax-3/#hex-digit
    private static bool IsHexDigit(char? c) {
        // A digit, or a code point between U+0041 LATIN CAPITAL LETTER A (A) and U+0046 LATIN CAPITAL LETTER F (F) inclusive, 
        // or a code point between U+0061 LATIN SMALL LETTER A (a) and U+0066 LATIN SMALL LETTER F (f) inclusive.
        return IsDigit(c) || c is (>= 'A' and <= 'F') or (>= 'a' and <= 'f');
    }

    // https://www.w3.org/TR/css-syntax-3/#uppercase-letter
    private static bool IsUppercaseLetter(char? c) {
        // A code point between U+0041 LATIN CAPITAL LETTER A (A) and U+005A LATIN CAPITAL LETTER Z (Z) inclusive.
        return c is >= 'A' and <= 'Z';
    }

    // https://www.w3.org/TR/css-syntax-3/#lowercase-letter
    private static bool IsLowercaseLetter(char? c) {
        // A code point between U+0061 LATIN SMALL LETTER A (a) and U+007A LATIN SMALL LETTER Z (z) inclusive.
        return c is >= 'a' and <= 'z';
    }

    // https://www.w3.org/TR/css-syntax-3/#letter
    private static bool IsLetter(char? c) {
        return IsUppercaseLetter(c) || IsLowercaseLetter(c);
    }

    // https://www.w3.org/TR/css-syntax-3/#non-ascii-code-point
    private static bool IsNonASCIICodePoint(char? c) {
        // A code point with a value equal to or greater than U+0080 <control>.
        return c is >= '\u0080';
    }

    // https://www.w3.org/TR/css-syntax-3/#ident-start-code-point
    private static bool IsIdentStartCodePoint(char? c) {
        // A letter, a non-ASCII code point, or U+005F LOW LINE (_).
        return IsLetter(c) || IsNonASCIICodePoint(c) || c is '_';
    }

    // https://www.w3.org/TR/css-syntax-3/#ident-code-point
    private static bool IsIdentCodePoint(char? c) {
        // An ident-start code point, a digit, or U+002D HYPHEN-MINUS (-).
        return IsIdentStartCodePoint(c) || IsDigit(c) || c is '-';
    }

    // https://www.w3.org/TR/css-syntax-3/#non-printable-code-point
    private static bool IsNonPrintable(char chr) {
        // A code point between U+0000 NULL and U+0008 BACKSPACE inclusive, or U+000B LINE TABULATION, 
        // or a code point between U+000E SHIFT OUT and U+001F INFORMATION SEPARATOR ONE inclusive, or U+007F DELETE.        
        return chr is (>= '\u0000' and <= '\u0008') or '\t' or (>= '\u000E' and <= '\u001F') or '\u007F';
    }

    // https://www.w3.org/TR/css-syntax-3/#newline
    private static bool IsNewline(char? c) {
        // U+000A LINE FEED. 
        // Note that U+000D CARRIAGE RETURN and U+000C FORM FEED are not included in this definition, as they are converted to U+000A LINE FEED during preprocessing.        
        return c is '\u000A';
    }

    // https://www.w3.org/TR/css-syntax-3/#whitespace
    private static bool IsWhitespace(char? c) {
        // A newline, U+0009 CHARACTER TABULATION, or U+0020 SPACE.
        return IsNewline(c) || c is '\u0009' or '\u0020';
    }

    #endregion

}

