
namespace FunWithHtml.html.Tokenizer;



enum State {
    DataState,
    RCDATAState,
    RAWTEXTState,
    ScriptDAtaState,
    PLAINTEXState,
    TagOpenState,
    EndTagOpenState,
    TagNameState,
    RCDATALessTanSignState,
    RCDATAEndTagOpenSTate,
    RCDATAEndTagNameSTate,
    RAWTEXTLessTanSignState,
    RAWTEXTEndTagOpenState,
    RAWTEXTEmdTagNameState,
    ScriptDataLessTanSignState,
    ScriptDataEndTagOpenState,
    ScriptDataEndTagNameState,
    ScriptDataEscapeSTartState,
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
    DOCTYPEPublicIdentifierDingleQuotedState,
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
        return base.ToString() + $" {{name: {name}}}";
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

public class Tag: Token {
    public string name { get; set; } = "";
    public bool selfClosing { get; set; } = false;
    public Dictionary<string, string> Attributes { get; set; } = [];

    public override string ToString() {
        return base.ToString() + $" {{name: {name}}}";
    }

}

public class StartTag: Tag {
    public StartTag() : base() { }
    public StartTag(string name) : base() {
        this.name = name;
    }
}
public class EndTag: Tag;
public class Comment(): Token {
    public string data { get; set; } = "";

    public override string ToString() {
        return base.ToString() + $" {{name: {data}}}";
    }

}

public class Character(char data): Token {
    public char data { get; set; } = data;

    public override string ToString() {
        string c = data switch {
            '\f' => @"\f",
            '\n' => @"\n",
            '\r' => @"\r",
            '\t' => @"\t",
            _ => data.ToString(),
        };


        return base.ToString() + $" {{data: {c}}}";
    }

}

class EndOfFile: Token { }


struct Attribute {
    public string name = "";
    public string value = "";

    public Attribute() { }
}

public class Tokenizer(string content) {
    private readonly string content = content;
    private int index = 0;

    public int Line { get => 1; } // todo 
    public int Col { get => index; } // todo

    private State state = State.DataState;
    private State returnState = State.DataState;

    private Tag currentTag = new();
    private Attribute currentAttribute;

    private Comment currentCommentTag = new();
    private DOCTYPE currentDOCTYPE = new();

    private Queue<Token> currentTokens = [];

    private string temporaryBuffer = "";

    private char? ConsumeNextInputCharacter() {
        if (content.Length > index) {
            return content[index++];
        } else {
            index++;
            return null;
        }
    }

    private void ConsumeNextCharacters(int num) {
        index += num;
    }

    private void Reconsume() {
        index--;
    }

    private Token? SetState(State state) {
        this.state = state;
        return null;
    }

    private Token BuildCurrentTagToken() {
        AddCurrentAttributeToCurrentTag();
        return currentTag;
    }

    private void AddCurrentAttributeToCurrentTag() {
        if (currentTag == null) return;
        if (currentAttribute.name == "") {
            /*  When the user agent leaves the attribute name state (and before emitting the tag token,
                if appropriate), the complete attribute's name must be compared to the other attributes on
                the same token; if there is already an attribute on the token with the exact same name,
                then this is a duplicate-attribute parse error and the new attribute must be removed
                from the token. */
            if (!currentTag.Attributes.TryAdd(currentAttribute.name, currentAttribute.value)) {
                // todo emit duplicate-attribute parse error
            }
            currentAttribute = new Attribute();
        }
    }

    public Token? NextToken() {
        if (currentTokens.Count > 0) {
            return currentTokens.Dequeue();
        }

        while (true) {
            var token = state switch {
                State.DataState => DataState(),
                State.RCDATAState => throw new NotImplementedException(),
                State.RAWTEXTState => throw new NotImplementedException(),
                State.ScriptDAtaState => throw new NotImplementedException(),
                State.PLAINTEXState => throw new NotImplementedException(),
                State.TagOpenState => TagOpenState(),
                State.EndTagOpenState => EndTagOpenState(),
                State.TagNameState => TagNameState(),
                State.RCDATALessTanSignState => throw new NotImplementedException(),
                State.RCDATAEndTagOpenSTate => throw new NotImplementedException(),
                State.RCDATAEndTagNameSTate => throw new NotImplementedException(),
                State.RAWTEXTLessTanSignState => throw new NotImplementedException(),
                State.RAWTEXTEndTagOpenState => throw new NotImplementedException(),
                State.RAWTEXTEmdTagNameState => throw new NotImplementedException(),
                State.ScriptDataLessTanSignState => throw new NotImplementedException(),
                State.ScriptDataEndTagOpenState => throw new NotImplementedException(),
                State.ScriptDataEndTagNameState => throw new NotImplementedException(),
                State.ScriptDataEscapeSTartState => throw new NotImplementedException(),
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
                State.AfterDOCTYPEpublicKeywordState => throw new NotImplementedException(),
                State.BeforeDOCTYPEpublicIdentifierState => throw new NotImplementedException(),
                State.DOCTYPEPublicIDentifierDoubleQuotedState => throw new NotImplementedException(),
                State.DOCTYPEPublicIdentifierDingleQuotedState => throw new NotImplementedException(),
                State.AfterDOCTYPEPublicIdentifierState => throw new NotImplementedException(),
                State.BetweenDoctypePublicAndSystemIdentifiersState => throw new NotImplementedException(),
                State.AfterDoctypeSystemKeywordState => throw new NotImplementedException(),
                State.BeforeDoctypeSystemIdentifierState => throw new NotImplementedException(),
                State.DOCTYPESystemIdentifierDoubleQuotedState => throw new NotImplementedException(),
                State.DOCTYPESystemIdentifierSingleQuotedState => throw new NotImplementedException(),
                State.AfterDOCTYPESystemIdentifierState => throw new NotImplementedException(),
                State.BogusDoctypeState => BogusDoctypeState(),
                State.CDATASectionState => throw new NotImplementedException(),
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
                State.NumericCharacterReferenceEndState => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),
            };
            if (token != null)
                return token;
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
                // todo parse error
                return new Character((char)c);
            case null: return new EndOfFile();
            default: return new Character((char)c);
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
                // todo parse error
                currentCommentTag = new();
                Reconsume();
                return SetState(State.BogusCommentState);
            case null:
                // todo parse error
                currentTokens.Enqueue(new EndOfFile());
                return new Character('<');
            default:
                // todo parse error
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
                // todo parse error
                return SetState(State.DataState);
            case null:
                // todo parse error
                currentTokens.Enqueue(new Character('/'));
                currentTokens.Enqueue(new EndOfFile());
                return new Character('<');
            default:
                // todo parse error
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
                    return BuildCurrentTagToken();
                case >= 'A' and <= 'Z':
                    currentTag.name += (char)(byte)(c | 0x20);
                    break;
                case '\0':
                    currentTag.name += '\uFFFD';
                    break;
                case null:
                    // todo parse error
                    return new EndOfFile();
                default:
                    currentTag.name += c;
                    break;
            }
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
                    // todo parse error
                    AddCurrentAttributeToCurrentTag();
                    currentAttribute.name += c;
                    return SetState(State.AttributeNameState);
                default:
                    AddCurrentAttributeToCurrentTag();
                    Reconsume();
                    return SetState(State.AttributeNameState);
            }
        }
    }

    // 13.2.5.33 Attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-name-state
    private Token? AttributeNameState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '\t' or '\n' or '\f' or ' ' or '/' or '>' or null:
                    Reconsume();
                    return SetState(State.AfterAttributeNameState);
                case '=':
                    return SetState(State.BeforeAttributeValueState);
                case >= 'A' and <= 'Z':
                    currentAttribute.name += (char)(byte)(c | 0x20);
                    break;
                case '\0':
                    // todo parse error
                    currentAttribute.name += '\uFFFD';
                    break;
                case '"' or '\'' or '<':
                    // todo parse error
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
                    return BuildCurrentTagToken();
                case null:
                    // todo parse error
                    return new EndOfFile();
                default:
                    AddCurrentAttributeToCurrentTag();
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
                    // todo parse error
                    SetState(State.DataState);
                    return BuildCurrentTagToken();
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
                    // todo parse error
                    currentAttribute.value += '\uFFFD';
                    break;
                case null:
                    throw new NotImplementedException();
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
                    // todo parse error
                    currentAttribute.value += '\uFFFD';
                    break;
                case null:
                    // todo parse error
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
                    return BuildCurrentTagToken();
                case '\0':
                    // todo parse error
                    currentAttribute.value += '\uFFFD';
                    break;
                case '"' or '\'' or '<' or '=' or '`':
                    // todo parse error
                    goto default;
                case null:
                    // todo parse error
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
                return BuildCurrentTagToken();
            case null: throw new NotImplementedException();
            default:
                // todo parse error
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
                return BuildCurrentTagToken();
            case null:
                // todo parse error
                return new EndOfFile();
            default:
                // todo parse error
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
                    // todo parse error
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
        if (content.Length > index + 2 && content[index..(index + 2)] == "--") {
            ConsumeNextCharacters(2);
            currentCommentTag = new();
            return SetState(State.CommentStartState);
        } else if (content.Length > index + 7 && content[index..(index + 7)].Equals("doctype", StringComparison.CurrentCultureIgnoreCase)) {
            ConsumeNextCharacters(7);
            return SetState(State.DOCTYPEState);
        } else if (content.Length > index + 7 && content[index..(index + 7)].Equals("[CDATA[")) {
            throw new NotImplementedException();
        } else {
            // todo parse error
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
                // todo parse error
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
                // todo parse error
                SetState(State.DataState);
                return currentCommentTag;
            case null:
                // todo parse error
                currentTokens.Enqueue(new EndOfFile());
                return currentCommentTag;
            default:
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
                    // todo parse error
                    currentCommentTag.data += '\uFFFD';
                    break;
                case null:
                    // todo parse error
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
                // todo parse error
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
                // todo parse error
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
                    // todo parse error
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
                // todo parse error
                SetState(State.DataState);
                return currentCommentTag;
            case null:
                // todo parse error
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
                // todo parse error
                currentDOCTYPE = new();
                currentDOCTYPE.forceQuirks = true;
                currentTokens.Enqueue(new EndOfFile());
                return currentDOCTYPE;
            default:
                // todo parse error
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
                    // todo parse error;
                    currentDOCTYPE = new();
                    currentDOCTYPE.name += '\uFFFD';
                    return SetState(State.DOCTYPENameState);
                case '>':
                    // todo parse error
                    currentDOCTYPE = new();
                    currentDOCTYPE.forceQuirks = true;
                    return SetState(State.DataState);
                case null:
                    // todo parse error;
                    currentDOCTYPE = new();
                    currentDOCTYPE.forceQuirks = true;
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
                    // todo parse error
                    currentDOCTYPE.name += '\uFFFD';
                    break;
                case null:
                    // todo parse error eof-in-doctype
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
                    // todo parse error eof-in-doctype
                    currentDOCTYPE.forceQuirks = true;
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    if (content.Length > index + 6 && content[index..(index + 6)].Equals("PUBLIC", StringComparison.OrdinalIgnoreCase)) {
                        ConsumeNextCharacters(6);
                        return SetState(State.AfterDOCTYPEpublicKeywordState);
                    } else if (content.Length > index + 6 && content[index..(index + 7)].Equals("SYSTEM", StringComparison.OrdinalIgnoreCase)) {
                        ConsumeNextCharacters(6);
                        return SetState(State.AfterDoctypeSystemKeywordState);
                    } else {
                        // todo parse error
                        currentDOCTYPE.forceQuirks = true;
                        Reconsume();
                        return SetState(State.BogusDoctypeState);
                    }
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
                    // todo parse error
                    break;
                case null:
                    currentTokens.Enqueue(new EndOfFile());
                    return currentDOCTYPE;
                default:
                    break; // ignore the character
            }
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
                // Flush code points consumed as a character reference
                foreach (var temp in temporaryBuffer) {
                    currentTokens.Enqueue(new Character(temp));
                }
                Reconsume();
                SetState(returnState);
                return currentTokens.Dequeue();
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
        // todo set character reference code to zero (0)
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
                temporaryBuffer += c;
                return SetState(State.HexadecimalCharacterReferenceStartState);
            default:
                // todo parse error
                // Flush code points consumed as a character reference
                foreach (var temp in temporaryBuffer) {
                    currentTokens.Enqueue(new Character(temp));
                }
                Reconsume();
                SetState(returnState);
                return currentTokens.Dequeue();

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
                // todo parse error
                // Flush code points consumed as a character reference
                foreach (var temp in temporaryBuffer) {
                    currentTokens.Enqueue(new Character(temp));
                }
                Reconsume();
                SetState(returnState);
                return currentTokens.Dequeue();
        }
    }
    // 13.2.5.78 Hexadecimal character reference state
    // https://html.spec.whatwg.org/multipage/parsing.html#hexadecimal-character-reference-state
    private Token? HexadecimalCharacterReferenceState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case not null when char.IsAsciiDigit((char)c):
                throw new NotImplementedException();
            case not null when char.IsAsciiHexDigitUpper((char)c):
                throw new NotImplementedException();
            case not null when char.IsAsciiHexDigitLower((char)c):
                throw new NotImplementedException();
            case ';':
                return SetState(State.NumericCharacterReferenceEndState);
            default:
                // todo parse error
                Reconsume();
                return SetState(State.NumericCharacterReferenceEndState);
        }
    }

    // 13.2.5.79 Decimal character reference state
    // https://html.spec.whatwg.org/multipage/parsing.html#decimal-character-reference-state
    private Token? DecimalCharacterReferenceState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case not null when char.IsAsciiDigit((char)c):
                throw new NotImplementedException();
            case ';':
                return SetState(State.NumericCharacterReferenceEndState);
            default:
                // todo parse error
                Reconsume();
                return SetState(State.NumericCharacterReferenceEndState);
        }
    }

}
