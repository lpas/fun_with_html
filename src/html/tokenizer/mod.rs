use std::collections::HashMap;

// DOCTYPE tokens have a name, a public identifier, a system identifier, and a force-quirks flag. When a DOCTYPE token is created, its name, public identifier, and system identifier must be marked as missing (which is a distinct state from the empty string), and the force-quirks flag must be set to off (its other state is on).
#[derive(PartialEq, Eq, Debug, Default, Clone)]
pub struct DOCTYPE {
    name: Option<String>,
    // public_id: Option<String>,
    // system_id: Option<String>,
    // force_quirks: bool,
}

#[derive(PartialEq, Eq, Clone, Debug)]
pub enum TagType {
    StartTag,
    EndTag,
}

#[derive(PartialEq, Eq, Debug, Default, Clone)]
pub struct Tag {
    pub name: String,
    // self_closing: bool,
    attributes: HashMap<String, String>,
}

impl Tag {
    pub fn new() -> Self {
        Tag {
            name: String::new(),
            attributes: HashMap::new(),
        }
    }
}

#[derive(PartialEq, Eq, Debug)]
pub enum Token {
    Doctype(DOCTYPE),
    StartTag(Tag),
    EndTag(Tag),
    Comment(String),
    Character(char),
    EndOfFile,
}

// Comment and character tokens have data.

#[derive(Default)]
struct Attribute {
    name: String,
    value: String,
}

pub struct Tokenizer {
    eof_emitted: bool,

    chars: Vec<char>,
    index: usize,

    current_state: State,

    current_doc_type: DOCTYPE,
    current_tag: Tag,
    current_tag_type: TagType,
    // current_attribute_name: String,
    // current_attribute_value: String,
    current_attribute: Attribute,
    current_comment_data: String,
}

#[derive(Debug)]
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
    AfterAttributeNameStat,
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

macro_rules! new_state {
    ($self_ident:ident, $state_variant:expr) => {{
        $self_ident.current_state = $state_variant;
        None
    }};
}

impl Iterator for Tokenizer {
    type Item = Token;
    fn next(&mut self) -> Option<Self::Item> {
        if self.eof_emitted {
            return None;
        }

        let token = self.next_token();
        if token == Token::EndOfFile {
            self.eof_emitted = true;
        }
        Some(token)
    }
}

impl Tokenizer {
    pub fn new(content: &str) -> Self {
        let chars = content.chars().collect();
        Tokenizer {
            eof_emitted: false,
            current_state: State::DataState,
            chars,
            index: 0,
            current_doc_type: DOCTYPE::default(),
            current_tag: Tag::default(),
            current_tag_type: TagType::StartTag,
            // current_attribute_name: String::new(),
            // current_attribute_value: String::new(),
            current_attribute: Attribute::default(),
            current_comment_data: String::new(),
        }
    }

    fn add_current_attribute_to_current_tag(&mut self) {
        if self.current_attribute.name != String::new() {
            /*  When the user agent leaves the attribute name state (and before emitting the tag token,
            if appropriate), the complete attribute's name must be compared to the other attributes on
            the same token; if there is already an attribute on the token with the exact same name,
            then this is a duplicate-attribute parse error and the new attribute must be removed
            from the token. */
            // todo emit duplicate-attribute parse error
            if !self
                .current_tag
                .attributes
                .contains_key(&self.current_attribute.name)
            {
                self.current_tag.attributes.insert(
                    self.current_attribute.name.clone(),
                    self.current_attribute.value.clone(),
                );
            }
            self.current_attribute = Attribute::default();
        }
    }

    fn build_current_tag_token(&mut self) -> Token {
        self.add_current_attribute_to_current_tag();
        if self.current_tag_type == TagType::StartTag {
            Token::StartTag(self.current_tag.clone())
        } else {
            Token::EndTag(self.current_tag.clone())
        }
    }

    fn consume_next_input_character(&mut self) -> Option<char> {
        if self.chars.len() > self.index {
            let char = self.chars[self.index];
            self.index += 1;
            return Some(char);
        } else {
            return None;
        }
    }

    fn consume_next_characters(&mut self, characters_number: usize) {
        self.index += characters_number;
    }

    fn reconsume(&mut self) {
        self.index -= 1;
    }

    pub fn next_token(&mut self) -> Token {
        loop {
            if let Some(token) = match self.current_state {
                State::DataState => self.data_state(),
                State::RCDATAState => todo!("State Not implemented: {:?}", self.current_state),
                State::RAWTEXTState => todo!("State Not implemented: {:?}", self.current_state),
                State::ScriptDAtaState => todo!("State Not implemented: {:?}", self.current_state),
                State::PLAINTEXState => todo!("State Not implemented: {:?}", self.current_state),
                State::TagOpenState => self.tag_open_state(),
                State::EndTagOpenState => self.end_tag_open_state(),
                State::TagNameState => self.tag_name_state(),
                State::RCDATALessTanSignState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::RCDATAEndTagOpenSTate => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::RCDATAEndTagNameSTate => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::RAWTEXTLessTanSignState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::RAWTEXTEndTagOpenState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::RAWTEXTEmdTagNameState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataLessTanSignState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataEndTagOpenState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataEndTagNameState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataEscapeSTartState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataEscapedDasState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataEscapedLessTahSignState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataEscapedEndTagOpenState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataEscapedEndTagNameState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataDoubleEscapeStartState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataDoubleEscapedState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataDoubleEscapedDashState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataDoubleEscapedDahsDashState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataDoubleEscapedLessTanSignState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::ScriptDataDoubleEscapeEndState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::BeforeAttributeNameState => self.before_attribute_name_state(),
                State::AttributeNameState => self.attribute_name_state(),
                State::AfterAttributeNameStat => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::BeforeAttributeValueState => self.before_attribute_value_state(),
                State::AttributeValueDoubleQuotedState => {
                    self.attribute_value_double_quoted_state()
                }
                State::AttributeValueSingleQuotedState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::AttributeValueUnquotesState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::AfterAttributeValueQuotedState => self.after_attribute_value_quoted_state(),
                State::SelfClosingStartTagState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::BogusCommentState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::MarkupDeclarationOpenState => self.markup_declaration_open_state(),
                State::CommentStartState => self.comment_start_state(),
                State::CommentStartDashState => self.comment_start_dash_state(),
                State::CommentState => self.comment_state(),
                State::CommentLessThanSignState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::CommentLessThanSignBandState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::CommentLessThanSignBangDashState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::CommentLessThanSignBangDashDashState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::CommentEndDashState => self.comment_end_dash_state(),
                State::CommentEndState => self.comment_end_state(),
                State::CommentEndBandState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::DOCTYPEState => self.doctype_state(),
                State::BeforeDOCTYPENameState => self.before_doctype_name_state(),
                State::DOCTYPENameState => self.doctype_name_state(),
                State::AfterDOCTYPENameState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::AfterDOCTYPEpublicKeywordState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::BeforeDOCTYPEpublicIdentifierState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::DOCTYPEPublicIDentifierDoubleQuotedState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::DOCTYPEPublicIdentifierDingleQuotedState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::AfterDOCTYPEPublicIdentifierState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::BetweenDoctypePublicAndSystemIdentifiersState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::AfterDoctypeSystemKeywordState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::BeforeDoctypeSystemIdentifierState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::DOCTYPESystemIdentifierDoubleQuotedState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::DOCTYPESystemIdentifierSingleQuotedState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::AfterDOCTYPESystemIdentifierState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::BogusDoctypeState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::CDATASectionState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::CDATASectionBracketState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::CDATASectionEndState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::CharacterReferenceState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::NamedCharacterReferenceState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::AmbiguousAmpersandState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::NumericCharacterReferenceState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::HexadecimalCharacterReferenceStartState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::DecimalCharacterReferenceStartState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::HexadecimalCharacterReferenceState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::DecimalCharacterReferenceState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
                State::NumericCharacterReferenceEndState => {
                    todo!("State Not implemented: {:?}", self.current_state)
                }
            } {
                return token;
            }
        }
    }

    // 13.2.5.1 Data state
    // https://html.spec.whatwg.org/multipage/parsing.html#data-state
    fn data_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '&' => todo!(),
                '<' => new_state!(self, State::TagOpenState),
                '\u{0000}' => todo!(),
                _ => Some(Token::Character(char)),
            }
        } else {
            Some(Token::EndOfFile)
        }
    }

    // 13.2.5.6 Tag open state
    // https://html.spec.whatwg.org/multipage/parsing.html#tag-open-state
    fn tag_open_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '!' => new_state!(self, State::MarkupDeclarationOpenState),
                '/' => new_state!(self, State::EndTagOpenState),
                _ if char.is_ascii_alphabetic() => {
                    self.current_tag_type = TagType::StartTag;
                    self.current_tag = Tag {
                        name: "".to_string(),
                        attributes: HashMap::new(),
                    };
                    self.reconsume();
                    new_state!(self, State::TagNameState)
                }
                '?' => todo!(),
                _ => todo!(),
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.7 End tag open state
    // https://html.spec.whatwg.org/multipage/parsing.html#end-tag-open-state
    fn end_tag_open_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if char.is_ascii_alphabetic() => {
                    self.current_tag_type = TagType::EndTag;
                    self.current_tag = Tag {
                        name: "".to_string(),
                        attributes: HashMap::new(),
                    };
                    self.reconsume();
                    new_state!(self, State::TagNameState)
                }
                '>' => todo!(),
                _ => todo!(),
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.8 Tag name state
    // https://html.spec.whatwg.org/multipage/parsing.html#tag-name-state
    fn tag_name_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => {
                    new_state!(self, State::BeforeAttributeNameState)
                }
                '/' => todo!(),
                '>' => {
                    self.current_state = State::DataState;
                    Some(self.build_current_tag_token())
                }
                _ if char.is_ascii_uppercase() => {
                    self.current_tag.name.push(char.to_ascii_lowercase());
                    new_state!(self, State::TagNameState)
                }
                '\u{0000}' => todo!(),
                _ => {
                    self.current_tag.name.push(char);
                    None
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.32 Before attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-name-state
    fn before_attribute_name_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => {
                    new_state!(self, State::BeforeAttributeNameState)
                }
                '/' => todo!(),
                '>' => todo!(),
                '=' => todo!(),
                _ => {
                    self.add_current_attribute_to_current_tag();
                    self.reconsume();
                    new_state!(self, State::AttributeNameState)
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.33 Attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-name-state
    fn attribute_name_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => todo!(),
                '/' | '>' => todo!(),
                '=' => new_state!(self, State::BeforeAttributeValueState),
                _ if char.is_ascii_uppercase() => todo!(),
                '\u{0000}' => todo!(),
                '"' | '\'' | '<' => todo!(),
                _ => {
                    self.current_attribute.name.push(char);
                    new_state!(self, State::AttributeNameState)
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.35 Before attribute value state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-value-state
    fn before_attribute_value_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => {
                    new_state!(self, State::BeforeAttributeValueState)
                }
                '"' => new_state!(self, State::AttributeValueDoubleQuotedState),
                '\'' => todo!(),
                '>' => todo!(),
                _ => todo!(),
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.36 Attribute value (double-quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-value-(double-quoted)-state
    fn attribute_value_double_quoted_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '"' => new_state!(self, State::AfterAttributeValueQuotedState),
                '&' => todo!(),
                '\u{0000}' => todo!(),
                _ => {
                    self.current_attribute.value.push(char);
                    new_state!(self, State::AttributeValueDoubleQuotedState)
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.39 After attribute value (quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-value-(quoted)-state
    fn after_attribute_value_quoted_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => {
                    new_state!(self, State::BeforeAttributeNameState)
                }
                '/' => todo!(),
                '>' => {
                    self.current_state = State::DataState;
                    Some(self.build_current_tag_token())
                }
                _ => todo!(),
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.42 Markup declaration open state
    // https://html.spec.whatwg.org/multipage/parsing.html#markup-declaration-open-state
    fn markup_declaration_open_state(&mut self) -> Option<Token> {
        if String::from_iter(&self.chars[self.index..self.index + 2]) == "--" {
            self.consume_next_characters(2);
            self.current_comment_data = String::new();
            new_state!(self, State::CommentStartState)
        } else if lowercase_char_slice(&self.chars[self.index..self.index + 7]) == "doctype" {
            self.consume_next_characters(7);
            new_state!(self, State::DOCTYPEState)
        } else {
            todo!()
        }
    }

    // 13.2.5.43 Comment start state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-state
    fn comment_start_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '-' => new_state!(self, State::CommentStartDashState),
                '>' => todo!(),
                _ => {
                    self.reconsume();
                    new_state!(self, State::CommentState)
                }
            }
        } else {
            todo!()
        }
    }
    // 13.2.5.44 Comment start dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-dash-state
    fn comment_start_dash_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ => todo!(),
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.45 Comment state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-state
    fn comment_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '<' => todo!(),
                '-' => new_state!(self, State::CommentEndDashState),
                '\u{0000}' => todo!(),
                _ => {
                    self.current_comment_data.push(char);
                    new_state!(self, State::CommentState)
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.50 Comment end dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-dash-state
    fn comment_end_dash_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '-' => new_state!(self, State::CommentEndState),
                _ => {
                    self.current_comment_data.push('-');
                    self.reconsume();
                    new_state!(self, State::CommentState)
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.51 Comment end state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-state
    fn comment_end_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '>' => {
                    self.current_state = State::DataState;
                    return Some(Token::Comment(self.current_comment_data.clone()));
                }
                _ => todo!(),
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.53 DOCTYPE state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-state
    fn doctype_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => {
                    new_state!(self, State::BeforeDOCTYPENameState)
                }
                _ => todo!(),
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.54 Before DOCTYPE name state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-doctype-name-state
    fn before_doctype_name_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => {
                    new_state!(self, State::BeforeDOCTYPENameState)
                }
                _ if char.is_ascii_uppercase() => todo!(),
                '\u{0000}' => todo!(),
                '>' => todo!(),
                _ => {
                    self.current_doc_type = DOCTYPE::default();
                    self.current_doc_type.name = Some(String::from(char));
                    new_state!(self, State::DOCTYPENameState)
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.55 DOCTYPE name state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-name-state
    fn doctype_name_state(&mut self) -> Option<Token> {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => todo!(),
                _ if char.is_ascii_uppercase() => {
                    todo!(); //Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current DOCTYPE token's name.
                }
                '\u{0000}' => todo!(),
                '>' => {
                    self.current_state = State::DataState;
                    Some(Token::Doctype(self.current_doc_type.clone()))
                }
                _ => {
                    self.current_doc_type
                        .name
                        .as_mut()
                        .expect("name should be a string")
                        .push(char);
                    new_state!(self, State::DOCTYPENameState)
                } // todo Append the current input character to the current DOCTYPE token's name.
            }
        } else {
            todo!()
        }
    }
}

fn is_one_of_tab_lf_ff_space(char: char) -> bool {
    char == '\u{0009}' || char == '\u{000A}' || char == '\u{000C}' || char == '\u{0020}'
}

fn lowercase_char_slice(slice: &[char]) -> String {
    slice.iter().flat_map(|&c| c.to_lowercase()).collect()
}
