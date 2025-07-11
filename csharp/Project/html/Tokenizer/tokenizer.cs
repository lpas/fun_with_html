
using System.Text;
using FunWithHtml.html.TreeBuilder;

namespace FunWithHtml.html.Tokenizer;



enum State {
    DataState,
    RCDATAState,
    RAWTEXTState,
    ScriptDataState,
    PLAINTEXTState,
    TagOpenState,
    EndTagOpenState,
    TagNameState,
    RCDATALessTanSignState,
    RCDATAEndTagOpenSTate,
    RCDATAEndTagNameSTate,
    RAWTEXTLessTanSignState,
    RAWTEXTEndTagOpenState,
    RAWTEXTEmdTagNameState,
    ScriptDataLessThanSignState,
    ScriptDataEndTagOpenState,
    ScriptDataEndTagNameState,
    ScriptDataEscapeStartState,
    ScriptDataEscapedDasState,
    ScriptDataEscapedLessTahSignState,
    ScriptDataEscapedEndTagOpenState,
    ScriptDataEscapedEndTagNameState,
    ScriptDataDoubleEscapeStartState,
    ScriptDataDoubleEscapedState,
    ScriptDataDoubleEscapedDashState,
    ScriptDataDoubleEscapedDahsDashState,
    ScriptDataDoubleEscapedLessTanSignState,
    ScriptDataDoubleEscapeEndState,
    BeforeAttributeNameState,
    AttributeNameState,
    AfterAttributeNameState,
    BeforeAttributeValueState,
    AttributeValueDoubleQuotedState,
    AttributeValueSingleQuotedState,
    AttributeValueUnquotesState,
    AfterAttributeValueQuotedState,
    SelfClosingStartTagState,
    BogusCommentState,
    MarkupDeclarationOpenState,
    CommentStartState,
    CommentStartDashState,
    CommentState,
    CommentLessThanSignState,
    CommentLessThanSignBangState,
    CommentLessThanSignBangDashState,
    CommentLessThanSignBangDashDashState,
    CommentEndDashState,
    CommentEndState,
    CommentEndBangState,
    DOCTYPEState,
    BeforeDOCTYPENameState,
    DOCTYPENameState,
    AfterDOCTYPENameState,
    AfterDOCTYPEpublicKeywordState,
    BeforeDOCTYPEpublicIdentifierState,
    DOCTYPEPublicIDentifierDoubleQuotedState,
    DOCTYPEPublicIdentifierSingleQuotedState,
    AfterDOCTYPEPublicIdentifierState,
    BetweenDoctypePublicAndSystemIdentifiersState,
    AfterDoctypeSystemKeywordState,
    BeforeDoctypeSystemIdentifierState,
    DOCTYPESystemIdentifierDoubleQuotedState,
    DOCTYPESystemIdentifierSingleQuotedState,
    AfterDOCTYPESystemIdentifierState,
    BogusDoctypeState,
    CDATASectionState,
    CDATASectionBracketState,
    CDATASectionEndState,
    CharacterReferenceState,
    NamedCharacterReferenceState,
    AmbiguousAmpersandState,
    NumericCharacterReferenceState,
    HexadecimalCharacterReferenceStartState,
    DecimalCharacterReferenceStartState,
    HexadecimalCharacterReferenceState,
    DecimalCharacterReferenceState,
    NumericCharacterReferenceEndState,
}


public class Token { };
public class DOCTYPE(): Token, IEquatable<DOCTYPE> {
    public string? name { get; set; } = null;
    public string? publicId { get; set; } = null;
    public string? systemId { get; set; } = null;
    public bool forceQuirks { get; set; } = false;

    public override string ToString() {
        return base.ToString() + $" {{name: {name}}} {{publicId: {publicId}}} {{systemId: {systemId}}}";
    }

    public override bool Equals(object? obj) => Equals(obj as DOCTYPE);
    public bool Equals(DOCTYPE? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(name, other.name)
            && string.Equals(publicId, other.publicId)
            && string.Equals(systemId, other.systemId)
            && forceQuirks == other.forceQuirks;
    }

    public override int GetHashCode() => HashCode.Combine(name, publicId, systemId, forceQuirks);
}

public class Tag: Token, IEquatable<Tag> {
    public string name { get; set; } = "";
    public bool selfClosing { get; set; } = false;
    public List<Attribute> Attributes { get; set; } = [];

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append(base.ToString());
        sb.Append($" {{name: {name}}}");
        sb.Append(" {Attributes: ");
        sb.AppendJoin(" ", Attributes.Select((item) => $"\"{item.name}\"=\"{item.value}\""));
        sb.Append('}');
        sb.Append($" {{selfClosing: {selfClosing}}}");
        return sb.ToString();
    }

    public override bool Equals(object? obj) => Equals(obj as Tag);
    public bool Equals(Tag? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(name, other.name)
            && selfClosing == other.selfClosing
            && Attributes.Count == other.Attributes.Count && !Attributes.Except(other.Attributes).Any();
    }

    public override int GetHashCode() => HashCode.Combine(name, selfClosing, Attributes);
}

public class StartTag: Tag {
    public StartTag() : base() { }
    public StartTag(string name) : base() {
        this.name = name;
    }
}
public class EndTag: Tag;
public class Comment(): Token, IEquatable<Comment> {
    public string data { get; set; } = "";

    public override string ToString() {
        return base.ToString() + $" {{data: {data}}}";
    }

    public override bool Equals(object? obj) => Equals(obj as Comment);
    public bool Equals(Comment? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(data, other.data);
    }

    public override int GetHashCode() => HashCode.Combine(data);

}

public class Character(char data): Token, IEquatable<Character> {
    public char data { get; set; } = data;

    public override string ToString() {
        string c = data switch {
            '\f' => @"\f",
            '\n' => @"\n",
            '\r' => @"\r",
            '\t' => @"\t",
            _ => data.ToString(),
        };


        return base.ToString() + $" {{data: {c} {(int)data}}}";
    }

    public override bool Equals(object? obj) => Equals(obj as Character);
    public bool Equals(Character? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Equals(data, other.data);
    }

    public override int GetHashCode() => HashCode.Combine(data);

}

public class EndOfFile: Token { }


public class Attribute: IEquatable<Attribute> {
    public string name;
    public string value;

    public Attribute(char name, string value) {
        this.name = name.ToString();
        this.value = value;
    }
    public Attribute(string name, string value) {
        this.name = name;
        this.value = value;
    }

    public override bool Equals(object? obj) => Equals(obj as Attribute);
    public bool Equals(Attribute? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(name, other.name) && string.Equals(value, other.value);
    }

    public override int GetHashCode() => HashCode.Combine(name, value);

}

public class Tokenizer(string content) {
    private readonly string content = NormalizeNewLines(content);
    private int index = 0;

    public int Line { get; private set; } = 1;
    public int Col { get; private set; } = 0;

    internal State state = State.DataState;
    private State returnState = State.DataState;

    private Tag currentTag = new();
    private Attribute currentAttribute;

    private Comment currentCommentTag = new();
    private DOCTYPE currentDOCTYPE = new();

    private Queue<Token> currentTokens = [];

    private string temporaryBuffer = "";

    private double characterReferenceCode = 0;

    private string? lastStartTagTagName = null;

    public List<ParseError> Errors { get => parseErrors; }
    private List<ParseError> parseErrors = [];

    private bool shouldReconsume = false;
    private char? currentCharacter = null;


    private static string NormalizeNewLines(string str) {
        // To normalize newlines in a string, replace every U+000D CR U+000A LF code point pair with a single U+000A LF code point, and then replace every remaining U+000D CR code point with a U+000A LF code point.
        if (str.Length == 0) return str;
        return str.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private void AddParseError(string error, int? col = null) {
        parseErrors.Add(new ParseError { line = Line, col = col ?? Col, error = error });
    }

    private char? ConsumeNextInputCharacter() {
        if (shouldReconsume) {
            shouldReconsume = false;
            return currentCharacter;
        }
        if (content.Length > index) {
            currentCharacter = content[index++];
            if (currentCharacter == '\n') {
                Col = 0;
                Line++;
            } else {
                Col++;
            }
            if (IsControl((char)currentCharacter) && !IsASCIIWhitespace((char)currentCharacter) && currentCharacter != '\0') {
                AddParseError("control-character-in-input-stream");
            }
        } else {
            index++;
            Col++;
            currentCharacter = null;
        }
        return currentCharacter;
    }

    private void ConsumeNextCharacters(int num) {
        index += num;
        Col += num;
    }

    private void Reconsume() {
        shouldReconsume = true;
    }

    private Token? SetState(State state) {
        this.state = state;
        return null;
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#flush-code-points-consumed-as-a-character-reference
    private void FlushCodePointsConsumedAsACharacterReference() {
        if (returnState is State.AttributeValueDoubleQuotedState or State.AttributeValueSingleQuotedState or State.AttributeValueUnquotesState) {
            currentAttribute.value += temporaryBuffer;
        } else {
            foreach (var temp in temporaryBuffer) {
                currentTokens.Enqueue(new Character(temp));
            }
        }
    }

    private bool IsAppropriateEndTagToken() {
        return currentTag.name == lastStartTagTagName;
    }

    private void HandleTokenEmit(Token token) {
        if (token is StartTag startTag) {
            lastStartTagTagName = startTag.name;
        }
        if (token is EndTag endTag) {
            if (endTag.selfClosing) {
                AddParseError("end-tag-with-trailing-solidus");
                endTag.selfClosing = false;
            }
            if (endTag.Attributes.Count != 0) {
                AddParseError("end-tag-with-attributes");
                endTag.Attributes.Clear();
            }
        }
    }

    public Token NextToken() {
        if (currentTokens.Count > 0) {
            var token = currentTokens.Dequeue();
            HandleTokenEmit(token);
            return token;
        }

        while (true) {
            var token = state switch {
                State.DataState => DataState(),
                State.RCDATAState => RCDATAState(),
                State.RAWTEXTState => RAWTEXTState(),
                State.ScriptDataState => ScriptDataState(),
                State.PLAINTEXTState => PLAINTEXTState(),
                State.TagOpenState => TagOpenState(),
                State.EndTagOpenState => EndTagOpenState(),
                State.TagNameState => TagNameState(),
                State.RCDATALessTanSignState => throw new NotImplementedException(),
                State.RCDATAEndTagOpenSTate => throw new NotImplementedException(),
                State.RCDATAEndTagNameSTate => throw new NotImplementedException(),
                State.RAWTEXTLessTanSignState => throw new NotImplementedException(),
                State.RAWTEXTEndTagOpenState => throw new NotImplementedException(),
                State.RAWTEXTEmdTagNameState => throw new NotImplementedException(),
                State.ScriptDataLessThanSignState => ScriptDataLessThanSignState(),
                State.ScriptDataEndTagOpenState => ScriptDataEndTagOpenState(),
                State.ScriptDataEndTagNameState => ScriptDataEndTagNameState(),
                State.ScriptDataEscapeStartState => ScriptDataEscapeStartState(),
                State.ScriptDataEscapedDasState => throw new NotImplementedException(),
                State.ScriptDataEscapedLessTahSignState => throw new NotImplementedException(),
                State.ScriptDataEscapedEndTagOpenState => throw new NotImplementedException(),
                State.ScriptDataEscapedEndTagNameState => throw new NotImplementedException(),
                State.ScriptDataDoubleEscapeStartState => throw new NotImplementedException(),
                State.ScriptDataDoubleEscapedState => throw new NotImplementedException(),
                State.ScriptDataDoubleEscapedDashState => throw new NotImplementedException(),
                State.ScriptDataDoubleEscapedDahsDashState => throw new NotImplementedException(),
                State.ScriptDataDoubleEscapedLessTanSignState => throw new NotImplementedException(),
                State.ScriptDataDoubleEscapeEndState => throw new NotImplementedException(),
                State.BeforeAttributeNameState => BeforeAttributeNameState(),
                State.AttributeNameState => AttributeNameState(),
                State.AfterAttributeNameState => AfterAttributeNameState(),
                State.BeforeAttributeValueState => BeforeAttributeValueState(),
                State.AttributeValueDoubleQuotedState => AttributeValueDoubleQuotedState(),
                State.AttributeValueSingleQuotedState => AttributeValueSingleQuotedState(),
                State.AttributeValueUnquotesState => AttributeValueUnquotedState(),
                State.AfterAttributeValueQuotedState => AfterAttributeValueQuotedState(),
                State.SelfClosingStartTagState => SelfClosingStartTagState(),
                State.BogusCommentState => BogusCommentState(),
                State.MarkupDeclarationOpenState => MarkupDeclarationOpenState(),
                State.CommentStartState => CommentStartState(),
                State.CommentStartDashState => CommentStartDashState(),
                State.CommentState => CommentState(),
                State.CommentLessThanSignState => CommentLessTanSignState(),
                State.CommentLessThanSignBangState => CommentLessThanSignBangState(),
                State.CommentLessThanSignBangDashState => CommentLessThanSignBangDashState(),
                State.CommentLessThanSignBangDashDashState => CommentLessThanSignBangDashDashState(),
                State.CommentEndDashState => CommentEndDashState(),
                State.CommentEndState => CommentEndState(),
                State.CommentEndBangState => CommentEndBangState(),
                State.DOCTYPEState => DOCTYPEState(),
                State.BeforeDOCTYPENameState => BeforeDOCTYPENameState(),
                State.DOCTYPENameState => DOCTYPENameState(),
                State.AfterDOCTYPENameState => AfterDOCTYPENameState(),
                State.AfterDOCTYPEpublicKeywordState => AfterDOCTYPEpublicKeywordState(),
                State.BeforeDOCTYPEpublicIdentifierState => BeforeDOCTYPEpublicIdentifierState(),
                State.DOCTYPEPublicIDentifierDoubleQuotedState => DOCTYPEPublicIdentifierDoubleQuotedState(),
                State.DOCTYPEPublicIdentifierSingleQuotedState => DOCTYPEPublicIdentifierSingleQuotedState(),
                State.AfterDOCTYPEPublicIdentifierState => AfterDOCTYPEPublicIdentifierState(),
                State.BetweenDoctypePublicAndSystemIdentifiersState => BetweenDoctypePublicAndSystemIdentifiersState(),
                State.AfterDoctypeSystemKeywordState => AfterDoctypeSystemKeywordState(),
                State.BeforeDoctypeSystemIdentifierState => BeforeDoctypeSystemIdentifierState(),
                State.DOCTYPESystemIdentifierDoubleQuotedState => DOCTYPESystemIdentifierDoubleQuotedState(),
                State.DOCTYPESystemIdentifierSingleQuotedState => DOCTYPESystemIdentifierSingleQuotedState(),
                State.AfterDOCTYPESystemIdentifierState => AfterDOCTYPESystemIdentifierState(),
                State.BogusDoctypeState => BogusDoctypeState(),
                State.CDATASectionState => CDATASectionState(),
                State.CDATASectionBracketState => throw new NotImplementedException(),
                State.CDATASectionEndState => throw new NotImplementedException(),
                State.CharacterReferenceState => CharacterReferenceState(),
                State.NamedCharacterReferenceState => NamedCharacterReferenceState(),
                State.AmbiguousAmpersandState => throw new NotImplementedException(),
                State.NumericCharacterReferenceState => NumericCharacterReferenceState(),
                State.HexadecimalCharacterReferenceStartState => HexadecimalCharacterReferenceStartState(),
                State.DecimalCharacterReferenceStartState => DecimalCharacterReferenceStartState(),
                State.HexadecimalCharacterReferenceState => HexadecimalCharacterReferenceState(),
                State.DecimalCharacterReferenceState => DecimalCharacterReferenceState(),
                State.NumericCharacterReferenceEndState => NumericCharacterReferenceEndState(),
                _ => throw new NotImplementedException(),
            };
            if (token != null) {
                HandleTokenEmit(token);
                return token;
            }
        }
    }

    // 13.2.5.1 Data state
    // https://html.spec.whatwg.org/multipage/parsing.html#data-state
    private Token? DataState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '&':
                returnState = State.DataState;
                return SetState(State.CharacterReferenceState);
            case '<': return SetState(State.TagOpenState);
            case '\u0000':
                AddParseError("unexpected-null-character");
                return new Character((char)c);
            case null: return new EndOfFile();
            default: return new Character((char)c);
        }
    }

    // 13.2.5.2 RCDATA state
    // https://html.spec.whatwg.org/multipage/parsing.html#rcdata-state
    private Token? RCDATAState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '&':
                returnState = State.DataState;
                return SetState(State.CharacterReferenceState);
            case '<':
                return SetState(State.RCDATALessTanSignState);
            case '\0':
                AddParseError("unexpected-null-character");
                return new Character('\uFFFD');
            case null:
                return new EndOfFile();
            default:
                return new Character((char)c);
        }
    }

    // 13.2.5.3 RAWTEXT state
    // https://html.spec.whatwg.org/multipage/parsing.html#rawtext-state
    private Token? RAWTEXTState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '<':
                return SetState(State.RAWTEXTLessTanSignState);
            case '\0':
                AddParseError("unexpected-null-character");
                return new Character('\uFFFD');
            case null:
                return new EndOfFile();
            default:
                return new Character((char)c);
        }
    }

    // 13.2.5.4 Script data state
    // https://html.spec.whatwg.org/multipage/parsing.html#script-data-state
    private Token? ScriptDataState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '<':
                return SetState(State.ScriptDataLessThanSignState);
            case '\0':
                AddParseError("unexpected-null-character");
                return new Character('\uFFFD');
            case null:
                return new EndOfFile();
            default:
                return new Character((char)c);
        }
    }

    // 13.2.5.5 PLAINTEXT state
    // https://html.spec.whatwg.org/multipage/parsing.html#plaintext-state
    private Token? PLAINTEXTState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\0':
                    AddParseError("unexpected-null-character");
                    return new Character('\uFFFD');
                case null:
                    return new EndOfFile();
                default:
                    return new Character((char)c);
            }
        }
    }

    // 13.2.5.6 Tag open state
    // https://html.spec.whatwg.org/multipage/parsing.html#tag-open-state
    private Token? TagOpenState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '!':
                return SetState(State.MarkupDeclarationOpenState);
            case '/':
                return SetState(State.EndTagOpenState);
            case >= 'a' and <= 'z' or >= 'A' and <= 'Z':
                currentTag = new StartTag();
                Reconsume();
                return SetState(State.TagNameState);
            case '?':
                AddParseError("unexpected-question-mark-instead-of-tag-name");
                currentCommentTag = new();
                Reconsume();
                return SetState(State.BogusCommentState);
            case null:
                AddParseError("eof-before-tag-name");
                currentTokens.Enqueue(new EndOfFile());
                return new Character('<');
            default:
                AddParseError("invalid-first-character-of-tag-name");
                Reconsume();
                SetState(State.DataState);
                return new Character('<');
        }
    }

    // 13.2.5.7 End tag open state
    // https://html.spec.whatwg.org/multipage/parsing.html#end-tag-open-state
    private Token? EndTagOpenState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case >= 'a' and <= 'z' or >= 'A' and <= 'Z':
                currentTag = new EndTag();
                Reconsume();
                return SetState(State.TagNameState);
            case '>':
                AddParseError("missing-end-tag-name");
                return SetState(State.DataState);
            case null:
                AddParseError("eof-before-tag-name");
                currentTokens.Enqueue(new Character('/'));
                currentTokens.Enqueue(new EndOfFile());
                return new Character('<');
            default:
                AddParseError("invalid-first-character-of-tag-name");
                currentCommentTag = new();
                Reconsume();
                return SetState(State.BogusCommentState);
        }
    }

    // 13.2.5.8 Tag name state
    // https://html.spec.whatwg.org/multipage/parsing.html#tag-name-state
    private Token? TagNameState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    return SetState(State.BeforeAttributeNameState);
                case '/':
                    return SetState(State.SelfClosingStartTagState);
                case '>':
                    SetState(State.DataState);
                    return currentTag;
                case >= 'A' and <= 'Z':
                    currentTag.name += (char)(byte)(c | 0x20);
                    break;
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentTag.name += '\uFFFD';
                    break;
                case null:
                    AddParseError("eof-in-tag");
                    return new EndOfFile();
                default:
                    currentTag.name += c;
                    break;
            }
        }
    }

    // 13.2.5.15 Script data less-than sign state
    // https://html.spec.whatwg.org/multipage/parsing.html#script-data-less-than-sign-state
    private Token? ScriptDataLessThanSignState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '/':
                temporaryBuffer = "";
                return SetState(State.ScriptDataEndTagOpenState);
            case '!':
                SetState(State.ScriptDataEscapeStartState);
                currentTokens.Enqueue(new Character('!'));
                return new Character('<');
            default:
                Reconsume();
                SetState(State.ScriptDataState);
                return new Character('<');
        }
    }

    // 13.2.5.16 Script data end tag open state
    // https://html.spec.whatwg.org/multipage/parsing.html#script-data-end-tag-open-state
    private Token? ScriptDataEndTagOpenState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case >= 'a' and <= 'z' or >= 'A' and <= 'Z':
                currentTag = new EndTag();
                Reconsume();
                return SetState(State.ScriptDataEndTagNameState);
            default:
                Reconsume();
                SetState(State.ScriptDataState);
                currentTokens.Enqueue(new Character('/'));
                return new Character('<');
        }
    }

    // 13.2.5.17 Script data end tag name state
    // https://html.spec.whatwg.org/multipage/parsing.html#script-data-end-tag-name-state
    private Token? ScriptDataEndTagNameState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    throw new NotImplementedException();
                case '/': throw new NotImplementedException();
                case '>':
                    if (IsAppropriateEndTagToken()) {
                        SetState(State.DataState);
                        return currentTag;
                    } else {
                        goto default;
                    }
                case >= 'A' and <= 'Z': throw new NotImplementedException();
                case >= 'a' and <= 'z':
                    currentTag.name += c;
                    temporaryBuffer += c;
                    break;
                default:
                    currentTokens.Enqueue(new Character('<'));
                    currentTokens.Enqueue(new Character('/'));
                    foreach (var chr in temporaryBuffer) {
                        currentTokens.Enqueue(new Character(chr));
                    }
                    Reconsume();
                    SetState(State.ScriptDataState);
                    return currentTokens.Dequeue();
            }
        }

    }

    // 13.2.5.18 Script data escape start state
    // https://html.spec.whatwg.org/multipage/parsing.html#script-data-escape-start-state
    private Token? ScriptDataEscapeStartState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '-':
                SetState(State.ScriptDataEscapeStartState);
                return new Character('-');
            default:
                Reconsume();
                return SetState(State.ScriptDataState);
        }
    }

    // 13.2.5.32 Before attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-name-state
    private Token? BeforeAttributeNameState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    break; // ignore the character
                case '/' or '>' or null:
                    Reconsume();
                    return SetState(State.AfterAttributeNameState);
                case '=':
                    AddParseError("unexpected-equals-sign-before-attribute-name");
                    currentAttribute = new Attribute((char)c, "");
                    currentTag.Attributes.Add(currentAttribute);
                    return SetState(State.AttributeNameState);
                default:
                    currentAttribute = new Attribute("", "");
                    currentTag.Attributes.Add(currentAttribute);
                    Reconsume();
                    return SetState(State.AttributeNameState);
            }
        }
    }

    // 13.2.5.33 Attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-name-state
    private Token? AttributeNameState() {
        /* When the user agent leaves the attribute name state (and before emitting the tag token, if appropriate),
        the complete attribute's name must be compared to the other attributes on the same token; if there is already
        an attribute on the token with the exact same name, then this is a duplicate-attribute parse error and the new
        attribute must be removed from the token. */
        void HandleDuplicatedAttribute() {
            if (currentTag.Attributes.Any((item) => item.name == currentAttribute.name && !ReferenceEquals(item, currentAttribute))) {
                AddParseError("duplicate-attribute");
                currentTag.Attributes.RemoveAt(currentTag.Attributes.Count - 1);
            }
        }
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ' or '/' or '>' or null:
                    Reconsume();
                    HandleDuplicatedAttribute();
                    return SetState(State.AfterAttributeNameState);
                case '=':
                    HandleDuplicatedAttribute();
                    return SetState(State.BeforeAttributeValueState);
                case >= 'A' and <= 'Z':
                    currentAttribute.name += (char)(byte)(c | 0x20);
                    break;
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentAttribute.name += '\uFFFD';
                    break;
                case '"' or '\'' or '<':
                    AddParseError("unexpected-character-in-attribute-name");
                    goto default;
                default:
                    currentAttribute.name += c;
                    break;
            }
        }
    }

    // 13.2.5.34 After attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-name-state
    private Token? AfterAttributeNameState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    break; // ignore the character
                case '/':
                    return SetState(State.SelfClosingStartTagState);
                case '=':
                    return SetState(State.BeforeAttributeValueState);
                case '>':
                    SetState(State.DataState);
                    return currentTag;
                case null:
                    AddParseError("eof-in-tag");
                    return new EndOfFile();
                default:
                    currentAttribute = new Attribute("", "");
                    currentTag.Attributes.Add(currentAttribute);
                    Reconsume();
                    return SetState(State.AttributeNameState);
            }
        }
    }

    // 13.2.5.35 Before attribute value state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-value-state
    private Token? BeforeAttributeValueState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    break; // ignore the character
                case '"':
                    return SetState(State.AttributeValueDoubleQuotedState);
                case '\'':
                    return SetState(State.AttributeValueSingleQuotedState);
                case '>':
                    AddParseError("missing-attribute-value");
                    SetState(State.DataState);
                    return currentTag;
                default:
                    Reconsume();
                    return SetState(State.AttributeValueUnquotesState);
            }
        }
    }

    // 13.2.5.36 Attribute value (double-quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-value-(double-quoted)-state
    private Token? AttributeValueDoubleQuotedState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '"':
                    return SetState(State.AfterAttributeValueQuotedState);
                case '&':
                    returnState = State.AttributeValueDoubleQuotedState;
                    return SetState(State.CharacterReferenceState);
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentAttribute.value += '\uFFFD';
                    break;
                case null:
                    AddParseError("eof-in-tag");
                    return new EndOfFile();
                default:
                    currentAttribute.value += c;
                    break;
            }
        }
    }

    // 13.2.5.37 Attribute value (single-quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-value-(single-quoted)-state
    private Token? AttributeValueSingleQuotedState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\'':
                    return SetState(State.AfterAttributeValueQuotedState);
                case '&':
                    returnState = State.AttributeValueSingleQuotedState;
                    return SetState(State.CharacterReferenceState);
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentAttribute.value += '\uFFFD';
                    break;
                case null:
                    AddParseError("eof-in-tag");
                    return new EndOfFile();
                default:
                    currentAttribute.value += c;
                    break;
            }
        }
    }

    // 13.2.5.38 Attribute value (unquoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-value-(unquoted)-state
    private Token? AttributeValueUnquotedState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    return SetState(State.BeforeAttributeNameState);
                case '&':
                    returnState = State.AttributeValueUnquotesState;
                    return SetState(State.CharacterReferenceState);
                case '>':
                    SetState(State.DataState);
                    return currentTag;
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentAttribute.value += '\uFFFD';
                    break;
                case '"' or '\'' or '<' or '=' or '`':
                    AddParseError("unexpected-character-in-unquoted-attribute-value");
                    goto default;
                case null:
                    AddParseError("eof-in-tag");
                    return new EndOfFile();
                default:
                    currentAttribute.value += c;
                    break;
            }
        }

    }

    // 13.2.5.39 After attribute value (quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-value-(quoted)-state
    private Token? AfterAttributeValueQuotedState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '\t' or '\n' or '\f' or ' ':
                return SetState(State.BeforeAttributeNameState);
            case '/':
                return SetState(State.SelfClosingStartTagState);
            case '>':
                SetState(State.DataState);
                return currentTag;
            case null:
                AddParseError("eof-in-tag");
                return new EndOfFile();
            default:
                AddParseError("missing-whitespace-between-attributes");
                Reconsume();
                return SetState(State.BeforeAttributeNameState);
        }
    }

    // 13.2.5.40 Self-closing start tag state
    // https://html.spec.whatwg.org/multipage/parsing.html#self-closing-start-tag-state
    private Token? SelfClosingStartTagState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '>':
                currentTag.selfClosing = true;
                SetState(State.DataState);
                return currentTag;
            case null:
                AddParseError("eof-in-tag");
                return new EndOfFile();
            default:
                AddParseError("unexpected-solidus-in-tag");
                Reconsume();
                return SetState(State.BeforeAttributeNameState);
        }
    }

    // 13.2.5.41 Bogus comment state
    // https://html.spec.whatwg.org/multipage/parsing.html#bogus-comment-state
    private Token? BogusCommentState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '>':
                    SetState(State.DataState);
                    return currentCommentTag;
                case null:
                    currentTokens.Enqueue(new EndOfFile());
                    return currentCommentTag;
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentCommentTag.data += '\uFFFD';
                    break;
                default:
                    currentCommentTag.data += c;
                    break;
            }
        }
    }

    // 13.2.5.42 Markup declaration open state
    // https://html.spec.whatwg.org/multipage/parsing.html#markup-declaration-open-state
    private Token? MarkupDeclarationOpenState() {
        if (content.Length >= index + 2 && content[index..(index + 2)] == "--") {
            ConsumeNextCharacters(2);
            currentCommentTag = new();
            return SetState(State.CommentStartState);
        } else if (content.Length >= index + 7 && content[index..(index + 7)].Equals("doctype", StringComparison.CurrentCultureIgnoreCase)) {
            ConsumeNextCharacters(7);
            return SetState(State.DOCTYPEState);
        } else if (content.Length >= index + 7 && content[index..(index + 7)].Equals("[CDATA[")) {
            throw new NotImplementedException();
        } else {
            AddParseError("incorrectly-opened-comment", Col + 1);
            currentCommentTag = new();
            return SetState(State.BogusCommentState);
        }
    }

    // 13.2.5.43 Comment start state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-state
    private Token? CommentStartState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '-':
                return SetState(State.CommentStartDashState);
            case '>':
                AddParseError("abrupt-closing-of-empty-comment");
                SetState(State.DataState);
                return currentCommentTag;
            default:
                Reconsume();
                return SetState(State.CommentState);
        }
    }
    // 13.2.5.44 Comment start dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-dash-state
    private Token? CommentStartDashState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '-':
                return SetState(State.CommentEndState);
            case '>':
                AddParseError("abrupt-closing-of-empty-comment");
                SetState(State.DataState);
                return currentCommentTag;
            case null:
                AddParseError("eof-in-comment");
                currentTokens.Enqueue(new EndOfFile());
                return currentCommentTag;
            default:
                currentCommentTag.data += '-';
                Reconsume();
                return SetState(State.CommentState);
        }
    }
    // 13.2.5.45 Comment state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-state
    private Token? CommentState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '<':
                    currentCommentTag.data += c;
                    return SetState(State.CommentLessThanSignState);
                case '-':
                    return SetState(State.CommentEndDashState);
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentCommentTag.data += '\uFFFD';
                    break;
                case null:
                    AddParseError("eof-in-comment");
                    currentTokens.Enqueue(new EndOfFile());
                    return currentCommentTag;
                default:
                    currentCommentTag.data += c;
                    break;
            }
        }
    }

    // 13.2.5.46 Comment less-than sign state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-state
    private Token? CommentLessTanSignState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '!':
                    currentCommentTag.data += c;
                    return SetState(State.CommentLessThanSignBangState);
                case '<':
                    currentCommentTag.data += c;
                    break;
                default:
                    Reconsume();
                    return SetState(State.CommentState);
            }
        }
    }

    // 13.2.5.47 Comment less-than sign bang state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-bang-state
    private Token? CommentLessThanSignBangState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '-':
                return SetState(State.CommentLessThanSignBangDashState);
            default:
                Reconsume();
                return SetState(State.CommentState);
        }
    }

    // 13.2.5.48 Comment less-than sign bang dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-bang-dash-state
    private Token? CommentLessThanSignBangDashState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '-':
                return SetState(State.CommentLessThanSignBangDashDashState);
            default:
                Reconsume();
                return SetState(State.CommentEndDashState);
        }
    }

    // 13.2.5.49 Comment less-than sign bang dash dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-less-than-sign-bang-dash-dash-state
    private Token? CommentLessThanSignBangDashDashState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '>' or null:
                Reconsume();
                return SetState(State.CommentEndState);
            default:
                AddParseError("nested-comment");
                Reconsume();
                return SetState(State.CommentEndState);
        }
    }

    // 13.2.5.50 Comment end dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-dash-state
    private Token? CommentEndDashState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '-':
                return SetState(State.CommentEndState);
            case null:
                AddParseError("eof-in-comment");
                currentTokens.Enqueue(new EndOfFile());
                return currentCommentTag;
            default:
                currentCommentTag.data += '-';
                Reconsume();
                return SetState(State.CommentState);
        }
    }
    // 13.2.5.51 Comment end state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-state
    private Token? CommentEndState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '>':
                    SetState(State.DataState);
                    return currentCommentTag;
                case '!': return SetState(State.CommentEndBangState);
                case '-':
                    currentCommentTag.data += '-';
                    break;
                case null:
                    AddParseError("eof-in-comment");
                    currentTokens.Enqueue(new EndOfFile());
                    return currentCommentTag;
                default:
                    currentCommentTag.data += "--";
                    Reconsume();
                    return SetState(State.CommentState);
            }
        }
    }

    // 13.2.5.52 Comment end bang state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-bang-state
    private Token? CommentEndBangState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '-':
                currentCommentTag.data += "--!";
                return SetState(State.CommentEndDashState);
            case '>':
                AddParseError("incorrectly-closed-comment");
                SetState(State.DataState);
                return currentCommentTag;
            case null:
                AddParseError("eof-in-comment");
                currentTokens.Enqueue(new EndOfFile());
                return currentCommentTag;
            default:
                currentCommentTag.data += "--!";
                Reconsume();
                return SetState(State.CommentState);
        }
    }

    // 13.2.5.53 DOCTYPE state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-state
    private Token? DOCTYPEState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '\t' or '\n' or '\f' or ' ':
                return SetState(State.BeforeDOCTYPENameState);
            case '>':
                Reconsume();
                return SetState(State.BeforeDOCTYPENameState);
            case null:
                AddParseError("eof-in-doctype");
                currentDOCTYPE = new();
                currentDOCTYPE.forceQuirks = true;
                currentTokens.Enqueue(new EndOfFile());
                return currentDOCTYPE;
            default:
                AddParseError("missing-whitespace-before-doctype-name");
                Reconsume();
                return SetState(State.BeforeDOCTYPENameState);
        }
    }
    // 13.2.5.54 Before DOCTYPE name state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-doctype-name-state
    private Token? BeforeDOCTYPENameState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    break; // ignore character
                case >= 'A' and <= 'Z':
                    currentDOCTYPE = new();
                    currentDOCTYPE.name += (char)(byte)(c | 0x20);
                    return SetState(State.DOCTYPENameState);
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentDOCTYPE = new();
                    currentDOCTYPE.name += '\uFFFD';
                    return SetState(State.DOCTYPENameState);
                case '>':
                    AddParseError("missing-doctype-name");
                    currentDOCTYPE = new() {
                        forceQuirks = true
                    };
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE = new() {
                        forceQuirks = true
                    };
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    currentDOCTYPE = new();
                    currentDOCTYPE.name += c;
                    return SetState(State.DOCTYPENameState);
            }
        }
    }
    // 13.2.5.55 DOCTYPE name state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-name-state
    private Token? DOCTYPENameState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    return SetState(State.AfterDOCTYPENameState);
                case '>':
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case >= 'A' and <= 'Z':
                    currentDOCTYPE.name += (char)(byte)(c | 0x20);
                    break;
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentDOCTYPE.name += '\uFFFD';
                    break;
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    currentDOCTYPE.name += c;
                    break;
            }
        }
    }

    // 13.2.5.56 After DOCTYPE name state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-doctype-name-state
    private Token? AfterDOCTYPENameState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    break; // ignore the character
                case '>':
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    var cIndex = index - 1; // index was increased with ConsumeNextInputCharacter;
                    if (content.Length >= cIndex + 6 && content[cIndex..(cIndex + 6)].Equals("PUBLIC", StringComparison.OrdinalIgnoreCase)) {
                        ConsumeNextCharacters(5); // we consume only 5 because ConsumeNextInputCharacter already consumed one
                        return SetState(State.AfterDOCTYPEpublicKeywordState);
                    } else if (content.Length >= cIndex + 6 && content[cIndex..(cIndex + 6)].Equals("SYSTEM", StringComparison.OrdinalIgnoreCase)) {
                        ConsumeNextCharacters(5); // we consume only 5 because ConsumeNextInputCharacter already consumed one
                        return SetState(State.AfterDoctypeSystemKeywordState);
                    } else {
                        AddParseError("invalid-character-sequence-after-doctype-name");
                        currentDOCTYPE.forceQuirks = true;
                        Reconsume();
                        return SetState(State.BogusDoctypeState);
                    }
            }
        }
    }

    // 13.2.5.57 After DOCTYPE public keyword state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-doctype-public-keyword-state
    private Token? AfterDOCTYPEpublicKeywordState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '\t' or '\n' or '\f' or ' ':
                return SetState(State.BeforeDOCTYPEpublicIdentifierState);
            case '"':
                AddParseError("missing-whitespace-after-doctype-public-keyword");
                currentDOCTYPE.publicId = "";
                return SetState(State.DOCTYPEPublicIDentifierDoubleQuotedState);
            case '\'':
                AddParseError("missing-whitespace-after-doctype-public-keyword");
                currentDOCTYPE.publicId = "";
                return SetState(State.DOCTYPEPublicIdentifierSingleQuotedState);
            case '>':
                AddParseError("missing-doctype-public-identifier");
                currentDOCTYPE.forceQuirks = true;
                SetState(State.DataState);
                return currentDOCTYPE;
            case null:
                AddParseError("eof-in-doctype");
                currentDOCTYPE.forceQuirks = true;
                currentTokens.Enqueue(new EndOfFile());
                return currentDOCTYPE;
            default:
                AddParseError("missing-quote-before-doctype-public-identifier");
                currentDOCTYPE.forceQuirks = true;
                Reconsume();
                return SetState(State.BogusDoctypeState);
        }
    }

    // 13.2.5.58 Before DOCTYPE public identifier state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-doctype-public-identifier-state
    private Token? BeforeDOCTYPEpublicIdentifierState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    break; // ignore character
                case '"':
                    currentDOCTYPE.publicId = "";
                    return SetState(State.DOCTYPEPublicIDentifierDoubleQuotedState);
                case '\'':
                    currentDOCTYPE.publicId = "";
                    return SetState(State.DOCTYPEPublicIdentifierSingleQuotedState);
                case '>':
                    AddParseError("missing-doctype-public-identifier");
                    currentDOCTYPE.forceQuirks = true;
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    AddParseError("missing-quote-before-doctype-public-identifier");
                    currentDOCTYPE.forceQuirks = true;
                    Reconsume();
                    return SetState(State.BogusDoctypeState);
            }
        }

    }

    // 13.2.5.59 DOCTYPE public identifier (double-quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-public-identifier-(double-quoted)-state
    private Token? DOCTYPEPublicIdentifierDoubleQuotedState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '"':
                    return SetState(State.AfterDOCTYPEPublicIdentifierState);
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentDOCTYPE.publicId += '\uFFFD';
                    break;
                case '>':
                    AddParseError("abrupt-doctype-public-identifier");
                    currentDOCTYPE.forceQuirks = true;
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    currentDOCTYPE.publicId += c;
                    break;
            }
        }
    }

    // 13.2.5.60 DOCTYPE public identifier (single-quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-public-identifier-(single-quoted)-state
    private Token? DOCTYPEPublicIdentifierSingleQuotedState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\'':
                    return SetState(State.AfterDOCTYPEPublicIdentifierState);
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentDOCTYPE.publicId += '\uFFFD';
                    break;
                case '>':
                    AddParseError("abrupt-doctype-public-identifier");
                    currentDOCTYPE.forceQuirks = true;
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    currentDOCTYPE.publicId += c;
                    break;
            }
        }
    }

    // 3.2.5.61 After DOCTYPE public identifier state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-doctype-public-identifier-state
    private Token? AfterDOCTYPEPublicIdentifierState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '\t' or '\n' or '\f' or ' ':
                return SetState(State.BetweenDoctypePublicAndSystemIdentifiersState);
            case '>':
                SetState(State.DataState);
                return currentDOCTYPE;
            case '"':
                AddParseError("missing-whitespace-between-doctype-public-and-system-identifiers");
                currentDOCTYPE.systemId = "";
                return SetState(State.DOCTYPESystemIdentifierDoubleQuotedState);
            case '\'':
                AddParseError("missing-whitespace-between-doctype-public-and-system-identifiers");
                currentDOCTYPE.systemId = "";
                return SetState(State.DOCTYPESystemIdentifierSingleQuotedState);
            case null:
                AddParseError("eof-in-doctype");
                currentDOCTYPE.forceQuirks = true;
                currentTokens.Enqueue(new EndOfFile());
                return currentDOCTYPE;
            default:
                AddParseError("missing-quote-before-doctype-system-identifier");
                currentDOCTYPE.forceQuirks = true;
                Reconsume();
                return SetState(State.BogusDoctypeState);
        }
    }

    // 13.2.5.62 Between DOCTYPE public and system identifiers state
    // https://html.spec.whatwg.org/multipage/parsing.html#between-doctype-public-and-system-identifiers-state
    private Token? BetweenDoctypePublicAndSystemIdentifiersState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    break; // ignore character
                case '>':
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case '"':
                    currentDOCTYPE.systemId = "";
                    return SetState(State.DOCTYPESystemIdentifierDoubleQuotedState);
                case '\'':
                    currentDOCTYPE.systemId = "";
                    return SetState(State.DOCTYPESystemIdentifierSingleQuotedState);
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    AddParseError("missing-quote-before-doctype-system-identifier");
                    currentDOCTYPE.forceQuirks = true;
                    Reconsume();
                    return SetState(State.BogusDoctypeState);
            }
        }
    }

    // 13.2.5.63 After DOCTYPE system keyword state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-doctype-system-keyword-state
    private Token? AfterDoctypeSystemKeywordState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '\t' or '\n' or '\f' or ' ':
                return SetState(State.BeforeDoctypeSystemIdentifierState);
            case '"':
                AddParseError("missing-whitespace-after-doctype-system-keyword");
                currentDOCTYPE.systemId = "";
                return SetState(State.DOCTYPESystemIdentifierDoubleQuotedState);
            case '\'':
                AddParseError("missing-whitespace-after-doctype-system-keyword");
                currentDOCTYPE.systemId = "";
                return SetState(State.DOCTYPESystemIdentifierSingleQuotedState);
            case '>':
                AddParseError("missing-doctype-system-identifier");
                currentDOCTYPE.forceQuirks = true;
                SetState(State.DataState);
                return currentDOCTYPE;
            case null:
                AddParseError("eof-in-doctype");
                currentDOCTYPE.forceQuirks = true;
                currentTokens.Enqueue(new EndOfFile());
                return currentDOCTYPE;
            default:
                AddParseError("missing-quote-before-doctype-system-identifier");
                currentDOCTYPE.forceQuirks = true;
                Reconsume();
                return SetState(State.BogusDoctypeState);
        }
    }

    // 13.2.5.64 Before DOCTYPE system identifier state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-doctype-system-identifier-state
    private Token? BeforeDoctypeSystemIdentifierState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    break; // ignore character
                case '"':
                    currentDOCTYPE.systemId = "";
                    return SetState(State.DOCTYPESystemIdentifierDoubleQuotedState);
                case '\'':
                    currentDOCTYPE.systemId = "";
                    return SetState(State.DOCTYPESystemIdentifierSingleQuotedState);
                case '>':
                    AddParseError("missing-doctype-system-identifier");
                    currentDOCTYPE.forceQuirks = true;
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    AddParseError("missing-quote-before-doctype-system-identifier");
                    currentDOCTYPE.forceQuirks = true;
                    Reconsume();
                    return SetState(State.BogusDoctypeState);
            }
        }
    }

    // 13.2.5.65 DOCTYPE system identifier (double-quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-system-identifier-(double-quoted)-state
    private Token? DOCTYPESystemIdentifierDoubleQuotedState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '"':
                    return SetState(State.AfterDOCTYPESystemIdentifierState);
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentDOCTYPE.systemId += '\uFFFD';
                    break;
                case '>':
                    AddParseError("abrupt-doctype-system-identifier");
                    currentDOCTYPE.forceQuirks = true;
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    currentDOCTYPE.systemId += c;
                    break;
            }
        }
    }

    // 13.2.5.66 DOCTYPE system identifier (single-quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-system-identifier-(single-quoted)-state
    private Token? DOCTYPESystemIdentifierSingleQuotedState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\'':
                    return SetState(State.AfterDOCTYPESystemIdentifierState);
                case '\0':
                    AddParseError("unexpected-null-character");
                    currentDOCTYPE.systemId += '\uFFFD';
                    break;
                case '>':
                    AddParseError("abrupt-doctype-system-identifier");
                    currentDOCTYPE.forceQuirks = true;
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    currentDOCTYPE.systemId += c;
                    break;
            }
        }
    }


    // 13.2.5.67 After DOCTYPE system identifier state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-doctype-system-identifier-state
    private Token? AfterDOCTYPESystemIdentifierState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ':
                    break; // ignore character
                case '>':
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case null:
                    AddParseError("eof-in-doctype");
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    AddParseError("unexpected-character-after-doctype-system-identifier");
                    Reconsume();
                    return SetState(State.BogusDoctypeState);
            }
        }
    }

    // 13.2.5.68 Bogus DOCTYPE state
    // https://html.spec.whatwg.org/multipage/parsing.html#bogus-doctype-state
    private Token? BogusDoctypeState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '>':
                    SetState(State.DataState);
                    return currentDOCTYPE;
                case '\0':
                    AddParseError("unexpected-null-character");
                    break;
                case null:
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    break; // ignore the character
            }
        }
    }

    // 13.2.5.69 CDATA section state
    // https://html.spec.whatwg.org/multipage/parsing.html#cdata-section-state
    private Token? CDATASectionState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case ']':
                return SetState(State.CDATASectionBracketState);
            case null:
                AddParseError("eof-in-cdata");
                return new EndOfFile();
            default:
                return new Character((char)c);
        }
    }

    // 13.2.5.72 Character reference state
    // https://html.spec.whatwg.org/multipage/parsing.html#character-reference-state
    private Token? CharacterReferenceState() {
        temporaryBuffer = "&";
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case not null when char.IsAsciiLetterOrDigit((char)c):
                Reconsume();
                return SetState(State.NamedCharacterReferenceState);
            case '#':
                temporaryBuffer += c;
                return SetState(State.NumericCharacterReferenceState);
            default:
                FlushCodePointsConsumedAsACharacterReference();
                Reconsume();
                SetState(returnState);
                return currentTokens.Count > 0 ? currentTokens.Dequeue() : null;
        }
    }

    // 13.2.5.73 Named character reference state
    // https://html.spec.whatwg.org/multipage/parsing.html#named-character-reference-state
    private Token? NamedCharacterReferenceState() {
        throw new NotImplementedException();
    }

    // 13.2.5.75 Numeric character reference state
    // https://html.spec.whatwg.org/multipage/parsing.html#numeric-character-reference-state
    private Token? NumericCharacterReferenceState() {
        characterReferenceCode = 0;
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case 'x' or 'X':
                temporaryBuffer += c;
                return SetState(State.HexadecimalCharacterReferenceStartState);
            default:
                Reconsume();
                return SetState(State.DecimalCharacterReferenceStartState);
        }
    }

    // 13.2.5.76 Hexadecimal character reference start state
    // https://html.spec.whatwg.org/multipage/parsing.html#hexadecimal-character-reference-start-state
    private Token? HexadecimalCharacterReferenceStartState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case not null when char.IsAsciiHexDigit((char)c):
                Reconsume();
                return SetState(State.HexadecimalCharacterReferenceState);
            default:
                AddParseError("absence-of-digits-in-numeric-character-reference");
                FlushCodePointsConsumedAsACharacterReference();
                Reconsume();
                SetState(returnState);
                return currentTokens.Count > 0 ? currentTokens.Dequeue() : null;

        }
    }

    // 13.2.5.77 Decimal character reference start state
    // https://html.spec.whatwg.org/multipage/parsing.html#decimal-character-reference-start-state
    private Token? DecimalCharacterReferenceStartState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case not null when char.IsAsciiDigit((char)c):
                Reconsume();
                return SetState(State.DecimalCharacterReferenceState);
            default:
                AddParseError("absence-of-digits-in-numeric-character-reference");
                FlushCodePointsConsumedAsACharacterReference();
                Reconsume();
                SetState(returnState);
                return currentTokens.Count > 0 ? currentTokens.Dequeue() : null;
        }
    }
    // 13.2.5.78 Hexadecimal character reference state
    // https://html.spec.whatwg.org/multipage/parsing.html#hexadecimal-character-reference-state
    private Token? HexadecimalCharacterReferenceState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case not null when char.IsAsciiDigit((char)c):
                    characterReferenceCode = characterReferenceCode * 16 + (int)c - 0x0030;
                    break;
                case not null when char.IsAsciiHexDigitUpper((char)c):
                    characterReferenceCode = characterReferenceCode * 16 + (int)c - 0x0037;
                    break;
                case not null when char.IsAsciiHexDigitLower((char)c):
                    characterReferenceCode = characterReferenceCode * 16 + (int)c - 0x0057;
                    break;
                case ';':
                    return SetState(State.NumericCharacterReferenceEndState);
                default:
                    AddParseError("missing-semicolon-after-character-reference");
                    Reconsume();
                    return SetState(State.NumericCharacterReferenceEndState);
            }
        }
    }

    // 13.2.5.79 Decimal character reference state
    // https://html.spec.whatwg.org/multipage/parsing.html#decimal-character-reference-state
    private Token? DecimalCharacterReferenceState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case not null when char.IsAsciiDigit((char)c):
                    characterReferenceCode = characterReferenceCode * 10 + (int)c - 0x0030;
                    break;
                case ';':
                    return SetState(State.NumericCharacterReferenceEndState);
                default:
                    AddParseError("missing-semicolon-after-character-reference");
                    Reconsume();
                    return SetState(State.NumericCharacterReferenceEndState);
            }
        }
    }

    // 13.2.5.80 Numeric character reference end state
    // https://html.spec.whatwg.org/multipage/parsing.html#numeric-character-reference-end-state
    private Token? NumericCharacterReferenceEndState() {
        // Check the character reference code:
        // If the number is 0x00, then this is a null-character-reference parse error. Set the character reference code to 0xFFFD.
        if (characterReferenceCode == 0x00) {
            AddParseError("null-character-reference");
            characterReferenceCode = 0xFFFD;
        }
        // If the number is greater than 0x10FFFF, then this is a character-reference-outside-unicode-range parse error. Set the character reference code to 0xFFFD.
        if (characterReferenceCode > 0x10FFF) {
            AddParseError("character-reference-outside-unicode-range");
            characterReferenceCode = 0xFFFD;
        }
        // If the number is a surrogate, then this is a surrogate-character-reference parse error. Set the character reference code to 0xFFFD.
        if (IsSurrogate((char)characterReferenceCode)) {
            AddParseError("surrogate-character-reference");
            characterReferenceCode = 0xFFFD;
        }
        // If the number is a noncharacter, then this is a noncharacter-character-reference parse error.
        if (IsNonCharacter(characterReferenceCode)) {
            AddParseError("noncharacter-character-reference");
        }
        // If the number is 0x0D, or a control that's not ASCII whitespace, then this is a control-character-reference parse error. If the number is one of the numbers in the first column of the following table, then find the row with that number in the first column, and set the character reference code to the number in the second column of that row.        
        if (characterReferenceCode is 0x0D || IsControl((char)characterReferenceCode) && !IsASCIIWhitespace((char)characterReferenceCode)) {
            AddParseError("control-character-reference", Col + 1);
            switch (characterReferenceCode) {
                case 0x80: characterReferenceCode = 0x20AC; break;
                case 0x82: characterReferenceCode = 0x201A; break;
                case 0x83: characterReferenceCode = 0x0192; break;
                case 0x84: characterReferenceCode = 0x201E; break;
                case 0x85: characterReferenceCode = 0x2026; break;
                case 0x86: characterReferenceCode = 0x2020; break;
                case 0x87: characterReferenceCode = 0x2021; break;
                case 0x88: characterReferenceCode = 0x02C6; break;
                case 0x89: characterReferenceCode = 0x2030; break;
                case 0x8A: characterReferenceCode = 0x0160; break;
                case 0x8B: characterReferenceCode = 0x2039; break;
                case 0x8C: characterReferenceCode = 0x0152; break;
                case 0x8E: characterReferenceCode = 0x017D; break;
                case 0x91: characterReferenceCode = 0x2018; break;
                case 0x92: characterReferenceCode = 0x2019; break;
                case 0x93: characterReferenceCode = 0x201C; break;
                case 0x94: characterReferenceCode = 0x201D; break;
                case 0x95: characterReferenceCode = 0x2022; break;
                case 0x96: characterReferenceCode = 0x2013; break;
                case 0x97: characterReferenceCode = 0x2014; break;
                case 0x98: characterReferenceCode = 0x02DC; break;
                case 0x99: characterReferenceCode = 0x2122; break;
                case 0x9A: characterReferenceCode = 0x0161; break;
                case 0x9B: characterReferenceCode = 0x203A; break;
                case 0x9C: characterReferenceCode = 0x0153; break;
                case 0x9E: characterReferenceCode = 0x017E; break;
                case 0x9F: characterReferenceCode = 0x0178; break;
            }
        }
        temporaryBuffer = "";
        temporaryBuffer += (char)characterReferenceCode;
        FlushCodePointsConsumedAsACharacterReference();
        SetState(returnState);
        return currentTokens.Count > 0 ? currentTokens.Dequeue() : null;

    }

    // 4.6. Code points
    // https://infra.spec.whatwg.org/#code-points


    private static bool IsLeadingSurrogate(char c) {
        return c is >= '\uD800' and <= '\uDBFF';
    }
    private static bool IsTrailingSurrogate(char c) {
        return c is >= '\uDC00' and <= '\uDFFF';
    }

    private static bool IsSurrogate(char c) {
        return IsLeadingSurrogate(c) || IsTrailingSurrogate(c);
    }

    private static bool IsNonCharacter(double c) {
        return c is >= 0xFDD0 and <= 0xDFEF or 0xFFFF or 0x1FFFE or 0x1FFFF or 0x2FFFE or 0x2FFFF or 0x3FFFE or 0x3FFFF or 0x4FFFE or 0x4FFFF or 0x5FFFE or 0x5FFFF
         or 0x6FFFE or 0x6FFFF or 0x7FFFE or 0x7FFFF or 0x8FFFE or 0x8FFFF or 0x9FFFE or 0x9FFFF or 0xAFFFE or 0xAFFFF or 0xBFFFE or 0xBFFFF or 0xCFFFE or 0xCFFFF or 0xDFFFE or 0xDFFFF
          or 0xEFFFE or 0xEFFFF or 0xFFFFE or 0xFFFFF or 0x10FFFE or 0x10FFFF;
    }

    private static bool IsASCIIWhitespace(char c) {
        return c is '\u0009' or '\u000A' or '\u000C' or '\u000D' or '\u0020';
    }

    private static bool IsC0Control(char c) {
        return c is >= '\0' and <= '\u001F';
    }
    private static bool IsControl(char c) {
        return IsC0Control(c) || c is >= '\u007F' and <= '\u009F';
    }



}
