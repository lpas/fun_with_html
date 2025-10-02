using System.Globalization;
using System.Text;

namespace FunWithHtml.es262;

public class Input(string input) {

    public string Text { get; private set; } = input;
    public bool IsEof { get; private set; } = input.Length == 0;
    public char CurrentChar { get; private set; } = input.Length > 0 ? input[0] : '\0';
    public int Index { get; private set; } = 0;

    public bool Match(char c) {
        return CurrentChar == c;
    }

    public bool Match(char c, char c2) {
        if (Text.Length < Index + 2) return false;
        return CurrentChar == c && Text[Index + 1] == c2;
    }

    public bool Match(char c, char c2, char c3, char c4) {
        if (Text.Length < Index + 4) return false;
        return CurrentChar == c && Text[Index + 1] == c2 && Text[Index + 2] == c3 && Text[Index + 3] == c4;
    }


    public void ConsumeChars(int n) {
        for (var i = 0; i < n; i++) ConsumeChar();
    }

    public char? ConsumeChar() {
        if (Text.Length <= Index + 1) {
            Index++;
            CurrentChar = '\0';
            IsEof = true;
            return null;
        }
        CurrentChar = Text[++Index];
        return CurrentChar;
    }

}


public class Token {

    public virtual string Serialize() {
        return "";
    }
}

public class Lexer {

    // InputElementDiv
    public static List<Token> Consume(Input input) {
        var tokens = new List<Token>();
        while (!input.IsEof) {
            if (WhiteSpace.Consume(input) is WhiteSpace whiteSpace) {
                tokens.Add(whiteSpace);
                continue;
            }
            if (LineTerminator.Consume(input) is LineTerminator lineTerminator) {
                tokens.Add(lineTerminator);
                continue;
            }
            if (Comment.Consume(input) is Comment comment) {
                tokens.Add(comment);
                continue;
            }
            if (CommonToken.Consume(input) is CommonToken commonToken) {
                tokens.Add(commonToken);
                continue;
            }
            // todo DivPunctuator
            if (RightBracePunctuator.Consume(input) is RightBracePunctuator rightBracePunctuator) {
                tokens.Add(rightBracePunctuator);
                continue;
            }
            Console.WriteLine($"FAIL: {input.CurrentChar} ({input.Index}) {input.IsEof}");
            break;
        }
        return tokens;

    }
}


class Comment: Token {
    // https://tc39.es/ecma262/#prod-Comment
    public static Comment? Consume(Input input) {
        return ConsumeMultiLineComment(input) ?? ConsumeSingleLineComment(input);
    }

    static Comment? ConsumeMultiLineComment(Input input) {
        if (!input.Match('/', '*')) return null;
        input.ConsumeChars(2);
        while (true) {
            while (input.CurrentChar != '*') {
                if (input.ConsumeChar() is null) return null;
            }
            if (input.Match('*', '/')) {
                input.ConsumeChars(2);
                return new Comment();
            }
            if (input.ConsumeChar() is null) return null;
        }
    }

    static Comment? ConsumeSingleLineComment(Input input) {
        if (!input.Match('/', '/')) return null;
        while (true) {
            if (LineTerminator.IsLineTerminator(input.CurrentChar) || input.ConsumeChar() is null) {
                return new Comment();
            }
        }
    }
}



class WhiteSpace: Token {

    // https://tc39.es/ecma262/#prod-WhiteSpace
    public static WhiteSpace? Consume(Input input) {
        if (!IsWhitespace(input.CurrentChar)) return null;
        input.ConsumeChar();
        return new WhiteSpace(); // todo consume more then one whitespace if possible
    }

    public static bool IsWhitespace(char c) {
        // todo optimize for ascii
        if (c is '\u0009' or '\u000B' or '\u000C' or '\uFEFF') {
            return true;
        }
        UnicodeCategory category = char.GetUnicodeCategory(c);
        return category == UnicodeCategory.SpaceSeparator;
    }

    public override string Serialize() {
        return " ";
    }
}


class LineTerminator: Token {
    // todo lineTerminator sequence

    // // https://tc39.es/ecma262/#prod-LineTerminator
    public static LineTerminator? Consume(Input input) {
        if (!IsLineTerminator(input.CurrentChar)) return null;
        // todo for line reporting <CR> <LF> should be handled special for line number reporting
        input.ConsumeChar();
        return new LineTerminator();
    }

    public static bool IsLineTerminator(char c) {
        return c is '\u000A' or '\u000D' or '\u2028' or '\u2029';
    }
}


class CommonToken: Token {


    public static CommonToken? Consume(Input input) {
        if (IdentifierName.Consume(input) is IdentifierName identifierName) return identifierName;
        // todo PrivateIdentifier
        if (Punctuator.Consume(input) is Punctuator punctuator) return punctuator;
        if (NumericLiteral.Consume(input) is NumericLiteral numericLiteral) return numericLiteral;
        // StringLiteral
        // Template
        return null;
    }
}


class IdentifierName(string name): CommonToken {
    public string Name { get; private set; } = name;

    public static new IdentifierName? Consume(Input input) {
        if (!IsIdentifierStart(input)) return null;
        var sb = new StringBuilder();
        sb.Append(input.CurrentChar);
        while (input.ConsumeChar() is char chr && IsIdentifierPart(input)) {
            sb.Append(chr);
        }
        return new IdentifierName(sb.ToString());
    }


    static bool IsIdentifierPart(Input input) {
        // todo \ UnicodeEscapeSequence
        return IsIdentifierPartChar(input.CurrentChar);
    }

    static bool IsIdentifierPartChar(char c) {
        if (c is '$') return true;
        return UnicodeCharExtensions.IsIdContinue(c);
    }

    static bool IsIdentifierStart(Input input) {
        // todo \ UnicodeEscapeSequence
        return IsIdentifierStartChar(input.CurrentChar);
    }

    static bool IsIdentifierStartChar(char c) {
        // todo optimize for ascii
        if (c is '$' or '_') return true;
        return UnicodeCharExtensions.IsIdStart(c);
    }

    public override string ToString() {
        return $"{base.ToString()} {Name}";
    }

    public override string Serialize() {
        return Name;
    }

}



class Punctuator(string value): CommonToken {
    public string Value { get; private set; } = value;

    // https://tc39.es/ecma262/#prod-Punctuator
    public static new Punctuator? Consume(Input input) {
        return OptionalChainPunctuator(input) ?? ConsumeOtherPunctuator(input);
    }

    static Punctuator? OptionalChainPunctuator(Input input) {
        // todo optionalChainPunctuator
        return null;
    }

    static Punctuator? ConsumeOtherPunctuator(Input input) {
        // >>>=
        if (input.Match('>', '>', '>', '=')) {
            input.ConsumeChars(4);
            return new Punctuator(">>>=");
        }

        if (input.Text.Length > input.Index + 2 && threeLong(input.Text.AsSpan(input.Index, 3)) is Punctuator punctuator3) {
            input.ConsumeChars(3);
            return punctuator3;
        }

        if (input.Text.Length < input.Index + 1 && twoLong(input.Text.AsSpan(input.Index, 2)) is Punctuator punctuator2) {
            input.ConsumeChars(2);
            return punctuator2;
        }

        // { ( ) [ ] . ; , < > + - * % & | ^ ! ~ ? : = 
        if (input.CurrentChar is '{' or '(' or ')' or '[' or ']' or '.' or ';' or ',' or '<' or '>' or '+' or '-' or '*' or '%' or '&' or '|' or '^' or '!' or '~' or '?' or ':' or '=') {
            var chr = input.CurrentChar;
            input.ConsumeChar();
            return new Punctuator($"{chr}");
        }

        return null;

        // ... === !== >>> **= <<= >>= &&= ||= ??=
        static Punctuator? threeLong(ReadOnlySpan<char> str) {
            if (str[0] == '.' && str[1] == '.' && str[2] == '.') {
                return new Punctuator("...");
            }
            if (str[0] == '=' && str[1] == '=' && str[2] == '=') {
                return new Punctuator("===");
            }
            if (str[0] == '!' && str[1] == '=' && str[2] == '=') {
                return new Punctuator("!==");
            }
            if (str[0] == '>' && str[1] == '>') {
                if (str[2] == '>')
                    return new Punctuator(">>>");
                if (str[2] == '=')
                    return new Punctuator(">>=");
            }
            if (str[0] == '*' && str[1] == '*' && str[2] == '=') {
                return new Punctuator("**=");
            }
            if (str[0] == '<' && str[1] == '<' && str[2] == '=') {
                return new Punctuator("<<=");
            }
            if (str[0] == '&' && str[1] == '&' && str[2] == '=') {
                return new Punctuator("&&=");
            }
            if (str[0] == '|' && str[1] == '|' && str[2] == '=') {
                return new Punctuator("||=");
            }
            if (str[0] == '?' && str[1] == '?' && str[2] == '=') {
                return new Punctuator("??=");
            }

            return null;
        }

        // <= >= == != ** ++ -- << >> && || ?? += -= *= %= &= |= ^=  =>
        static Punctuator? twoLong(ReadOnlySpan<char> str) {
            if (str[0] == '<') {
                if (str[1] == '=') return new Punctuator("<=");
                if (str[1] == '<') return new Punctuator("<<");
            }
            if (str[0] == '>') {
                if (str[1] == '=') return new Punctuator(">=");
                if (str[1] == '>') return new Punctuator(">>");
            }
            if (str[0] == '=') {
                if (str[1] == '=') return new Punctuator("==");
                if (str[1] == '>') return new Punctuator("=>");
            }
            if (str[0] == '!' && str[1] == '=') return new Punctuator("!=");
            if (str[0] == '*') {
                if (str[1] == '*') return new Punctuator("**");
                if (str[1] == '=') return new Punctuator("*=");
            }
            if (str[0] == '+') {
                if (str[1] == '+') return new Punctuator("++");
                if (str[1] == '=') return new Punctuator("+=");
            }
            if (str[0] == '-') {
                if (str[1] == '-') return new Punctuator("--");
                if (str[1] == '=') return new Punctuator("-=");
            }
            if (str[0] == '&') {
                if (str[1] == '&') return new Punctuator("&&");
                if (str[1] == '=') return new Punctuator("&=");
            }
            if (str[0] == '|') {
                if (str[1] == '|') return new Punctuator("||");
                if (str[1] == '=') return new Punctuator("|=");
            }
            if (str[0] == '?' && str[1] == '?') return new Punctuator("??");
            if (str[0] == '%' && str[1] == '=') return new Punctuator("%=");
            if (str[0] == '^' && str[1] == '=') return new Punctuator("^=");

            return null;
        }


    }

    public override string ToString() {
        return $"{base.ToString()} {Value}";
    }

    public override string Serialize() {
        return Value;
    }

}

class RightBracePunctuator: Token {
    public static RightBracePunctuator? Consume(Input input) {
        if (input.CurrentChar is '}') {
            input.ConsumeChar();
            return new RightBracePunctuator();
        }
        return null;
    }

    public override string Serialize() {
        return "}";
    }
}



class NumericLiteral(string value): CommonToken {
    public string value { get; private set; } = value;


    // https://tc39.es/ecma262/#prod-NumericLiteral
    public static new NumericLiteral? Consume(Input input) {
        // todo this is just hacky!
        if (!char.IsAsciiDigit(input.CurrentChar)) return null;
        var sb = new StringBuilder();
        sb.Append(input.CurrentChar);
        while (input.ConsumeChar() is char chr && char.IsAsciiDigit(chr)) {
            sb.Append(chr);
        }
        return new NumericLiteral(sb.ToString());
    }

    public override string ToString() {
        return $"{base.ToString()} {value}";
    }

    public override string Serialize() {
        return value;
    }


}

public static class UnicodeCharExtensions {
    public static bool IsIdStart(char c) {
        // ID_Start characters are derived from the Unicode General_Category of uppercase letters, lowercase letters, titlecase letters, modifier letters, other letters, letter numbers, plus Other_ID_Start, minus Pattern_Syntax and Pattern_White_Space code points.
        // todo this is not fully correct see https://www.unicode.org/reports/tr31/#Table_Lexical_Classes_for_Identifiers
        // missing in the condition are: plus Other_ID_Start, minus Pattern_Syntax and Pattern_White_Space code points.
        return char.GetUnicodeCategory(c) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.LetterNumber;
    }

    public static bool IsIdContinue(char c) {
        // ID_Continue characters include ID_Start characters, plus characters having the Unicode General_Category of nonspacing marks, spacing combining marks, decimal number, connector punctuation, plus Other_ID_Continue, minus Pattern_Syntax and Pattern_White_Space code points.
        // todo this is not full correct see isIdStart
        return IsIdStart(c) || char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.ConnectorPunctuation;
    }
}
