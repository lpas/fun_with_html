
namespace html.Tokenizer;



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
    CommentLessThanSignBandState,
    CommentLessThanSignBangDashState,
    CommentLessThanSignBangDashDashState,
    CommentEndDashState,
    CommentEndState,
    CommentEndBandState,
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
public class DOCTYPE(): Token {
    public string? name { get; set; } = null;
    public string? publicId { get; set; } = null;
    public string? systemId { get; set; } = null;
    public bool forceQuirks { get; set; } = false;

    public override string ToString() {
        return base.ToString() + $" {{name: {name}}}";
    }

}

public class Tag(): Token {
    public string name { get; set; } = "";
    public bool selfClosing { get; set; } = false;
    public Dictionary<string, string> Attributes { get; set; } = [];

    public override string ToString() {
        return base.ToString() + $" {{name: {name}}}";
    }

}

public class StartTag: Tag;
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
    private State state = State.DataState;

    private Tag currentTag = new();
    private Attribute currentAttribute;

    private Comment currentCommentTag = new();
    private DOCTYPE currentDOCTYPE = new();

    private char? ConsumeNextInputCharacter() {
        if (content.Length > index) {
            return content[index++];
        } else {
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
                State.AfterAttributeNameState => throw new NotImplementedException(),
                State.BeforeAttributeValueState => BeforeAttributeValueState(),
                State.AttributeValueDoubleQuotedState => AttributeValueDoubleQuotedState(),
                State.AttributeValueSingleQuotedState => throw new NotImplementedException(),
                State.AttributeValueUnquotesState => throw new NotImplementedException(),
                State.AfterAttributeValueQuotedState => AfterAttributeValueQuotedState(),
                State.SelfClosingStartTagState => throw new NotImplementedException(),
                State.BogusCommentState => throw new NotImplementedException(),
                State.MarkupDeclarationOpenState => MarkupDeclarationOpenState(),
                State.CommentStartState => CommentStartState(),
                State.CommentStartDashState => CommentStartDashState(),
                State.CommentState => CommentState(),
                State.CommentLessThanSignState => throw new NotImplementedException(),
                State.CommentLessThanSignBandState => throw new NotImplementedException(),
                State.CommentLessThanSignBangDashState => throw new NotImplementedException(),
                State.CommentLessThanSignBangDashDashState => throw new NotImplementedException(),
                State.CommentEndDashState => CommentEndDashState(),
                State.CommentEndState => CommentEndState(),
                State.CommentEndBandState => throw new NotImplementedException(),
                State.DOCTYPEState => DOCTYPEState(),
                State.BeforeDOCTYPENameState => BeforeDOCTYPENameState(),
                State.DOCTYPENameState => DOCTYPENameState(),
                State.AfterDOCTYPENameState => throw new NotImplementedException(),
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
                State.BogusDoctypeState => throw new NotImplementedException(),
                State.CDATASectionState => throw new NotImplementedException(),
                State.CDATASectionBracketState => throw new NotImplementedException(),
                State.CDATASectionEndState => throw new NotImplementedException(),
                State.CharacterReferenceState => throw new NotImplementedException(),
                State.NamedCharacterReferenceState => throw new NotImplementedException(),
                State.AmbiguousAmpersandState => throw new NotImplementedException(),
                State.NumericCharacterReferenceState => throw new NotImplementedException(),
                State.HexadecimalCharacterReferenceStartState => throw new NotImplementedException(),
                State.DecimalCharacterReferenceStartState => throw new NotImplementedException(),
                State.HexadecimalCharacterReferenceState => throw new NotImplementedException(),
                State.DecimalCharacterReferenceState => throw new NotImplementedException(),
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
        return c switch {
            '&' => throw new NotImplementedException(),
            '<' => SetState(State.TagOpenState),
            '\u0000' => throw new NotImplementedException(),
            null => new EndOfFile(),
            _ => new Character((char)c),
        };
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
            case '?': throw new NotImplementedException();
            case null: throw new NotImplementedException();
            default:
                throw new NotImplementedException();
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
            case '>': throw new NotImplementedException();
            case null: throw new NotImplementedException();
            default: throw new NotImplementedException();
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
                case '/': throw new NotImplementedException();
                case '>':
                    SetState(State.DataState);
                    return BuildCurrentTagToken();
                case >= 'A' and <= 'Z':
                    currentTag.name += (char)(byte)(c | 0x20);
                    break;
                case '\0':
                    throw new NotImplementedException();
                case null:
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
                case '"' or '\'' or '<':
                    throw new NotImplementedException();
                default:
                    currentAttribute.name += c;
                    break;
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
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
                case '\0':
                    throw new NotImplementedException();
                case null:
                    throw new NotImplementedException();
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
            case '/': throw new NotImplementedException();
            case '>':
                SetState(State.DataState);
                return BuildCurrentTagToken();
            case null: throw new NotImplementedException();
            default:
                throw new NotImplementedException();
        }
    }

    // 13.2.5.42 Markup declaration open state
    // https://html.spec.whatwg.org/multipage/parsing.html#markup-declaration-open-state
    private Token? MarkupDeclarationOpenState() {
        if (content[index..(index + 2)] == "--") {
            ConsumeNextCharacters(2);
            currentCommentTag = new();
            return SetState(State.CommentStartState);
        } else if (content[index..(index + 7)].Equals("doctype", StringComparison.CurrentCultureIgnoreCase)) {
            ConsumeNextCharacters(7);
            return SetState(State.DOCTYPEState);
        } else {
            throw new NotImplementedException();
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
                throw new NotImplementedException();
            default:
                Reconsume();
                return SetState(State.CommentState);
        }
    }
    // 13.2.5.44 Comment start dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-dash-state
    private Token? CommentStartDashState() {
        char? c = ConsumeNextInputCharacter();
        throw new NotImplementedException();
    }
    // 13.2.5.45 Comment state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-state
    private Token? CommentState() {
        while (true) {
            char? c = ConsumeNextInputCharacter();
            switch (c) {
                case '<': throw new NotImplementedException();
                case '-': return SetState(State.CommentEndDashState);
                case '\0': throw new NotImplementedException();
                case null: throw new NotImplementedException();
                default:
                    currentCommentTag.data += c;
                    break;
            }
        }
    }
    // 13.2.5.50 Comment end dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-dash-state
    private Token? CommentEndDashState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '-':
                return SetState(State.CommentEndState);
            case null: throw new NotImplementedException();
            default:
                currentCommentTag.data += '-';
                Reconsume();
                return SetState(State.CommentState);
        }
    }
    // 13.2.5.51 Comment end state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-state
    private Token? CommentEndState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '>':
                SetState(State.DataState);
                return currentCommentTag;
            case '!': throw new NotImplementedException();
            case '-': throw new NotImplementedException();
            case null: throw new NotImplementedException();
            default:
                throw new NotImplementedException();
        }
    }
    // 13.2.5.53 DOCTYPE state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-state
    private Token? DOCTYPEState() {
        char? c = ConsumeNextInputCharacter();
        switch (c) {
            case '\t' or '\n' or '\f' or ' ':
                return SetState(State.BeforeDOCTYPENameState);
            case '>': throw new NotImplementedException();
            case null: throw new NotImplementedException();
            default: throw new NotImplementedException();
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
                    throw new NotImplementedException();
                case '\0': throw new NotImplementedException();
                case '>': throw new NotImplementedException();
                case null: throw new NotImplementedException();
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
                case >= 'A' and <= 'Z': throw new NotImplementedException();
                case '\0': throw new NotImplementedException();
                case null: throw new NotImplementedException();
                default:
                    currentDOCTYPE.name += c;
                    break;
            }
        }
    }
}
