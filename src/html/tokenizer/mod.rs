use std::collections::HashMap;

// DOCTYPE tokens have a name, a public identifier, a system identifier, and a force-quirks flag. When a DOCTYPE token is created, its name, public identifier, and system identifier must be marked as missing (which is a distinct state from the empty string), and the force-quirks flag must be set to off (its other state is on).
#[derive(Debug, Default, Clone)]
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

#[derive(Debug, Default, Clone)]
pub struct Tag {
    name: String,
    // self_closing: bool,
    attributes: HashMap<String, String>,
}

#[derive(Debug)]
enum Token {
    Doctype(DOCTYPE),
    StartTag(Tag),
    EndTag(Tag),
    Comment(String),
    Character(char),
    EndOfFile,
}

// Comment and character tokens have data.

pub struct Tokenizer {
    chars: Vec<char>,
    index: usize,
    current_doc_type: DOCTYPE,
    current_tag: Tag,
    current_tag_type: TagType,
    current_attribute_name: String,
    current_attribute_value: String,
    current_comment_data: String,
}

impl Tokenizer {
    pub fn new(content: &str) -> Self {
        let chars = content.chars().collect();
        Tokenizer {
            chars,
            index: 0,
            current_doc_type: DOCTYPE::default(),
            current_tag: Tag::default(),
            current_tag_type: TagType::StartTag,
            current_attribute_name: String::new(),
            current_attribute_value: String::new(),
            current_comment_data: String::new(),
        }
    }

    fn emit(&mut self, token: Token) {
        println!("emit token: {:?}", token)
    }

    fn emit_current_tag_token(&mut self) {
        if self.current_attribute_name != "".to_string() {
            self.current_tag.attributes.insert(
                self.current_attribute_name.clone(),
                self.current_attribute_value.clone(),
            );
            self.current_attribute_name = String::new();
            self.current_attribute_value = String::new();
        }

        if self.current_tag_type == TagType::StartTag {
            self.emit(Token::StartTag(self.current_tag.clone()))
        } else {
            self.emit(Token::EndTag(self.current_tag.clone()))
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

    pub fn run(&mut self) {
        self.data_state();
    }

    // 13.2.5.1 Data state
    // https://html.spec.whatwg.org/multipage/parsing.html#data-state
    fn data_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '&' => todo!(),
                '<' => self.tag_open_state(),
                '\u{0000}' => todo!(),
                _ => {
                    self.emit(Token::Character(char));
                    self.data_state()
                }
            }
        } else {
            self.emit(Token::EndOfFile)
        }
    }

    // 13.2.5.6 Tag open state
    // https://html.spec.whatwg.org/multipage/parsing.html#tag-open-state
    fn tag_open_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '!' => self.markup_declaration_open_state(),
                '/' => self.end_tag_open_state(),
                _ if char.is_ascii_alphabetic() => {
                    self.current_tag_type = TagType::StartTag;
                    self.current_tag = Tag {
                        name: "".to_string(),
                        attributes: HashMap::new(),
                    };
                    self.reconsume();
                    self.tag_name_state();
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
    fn end_tag_open_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if char.is_ascii_alphabetic() => {
                    self.current_tag_type = TagType::EndTag;
                    self.current_tag = Tag {
                        name: "".to_string(),
                        attributes: HashMap::new(),
                    };
                    self.reconsume();
                    self.tag_name_state();
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
    fn tag_name_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => self.before_attribute_name_state(),
                '/' => todo!(),
                '>' => {
                    self.emit_current_tag_token();
                    self.data_state()
                }
                _ if char.is_ascii_uppercase() => {
                    self.current_tag.name.push(char.to_ascii_lowercase());
                    self.tag_name_state();
                }
                '\u{0000}' => todo!(),
                _ => {
                    self.current_tag.name.push(char);
                    self.tag_name_state();
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.32 Before attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-name-state
    fn before_attribute_name_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => self.before_attribute_name_state(),
                '/' => todo!(),
                '>' => todo!(),
                '=' => todo!(),
                _ => {
                    self.current_attribute_name = String::new();
                    self.current_attribute_value = String::new();
                    self.reconsume();
                    self.attribute_name_state();
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.33 Attribute name state
    // https://html.spec.whatwg.org/multipage/parsing.html#attribute-name-state
    fn attribute_name_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => todo!(),
                '/' | '>' => todo!(),
                '=' => self.before_attribute_value_state(),
                _ if char.is_ascii_uppercase() => todo!(),
                '\u{0000}' => todo!(),
                '"' | '\'' | '<' => todo!(),
                _ => {
                    self.current_attribute_name.push(char);
                    self.attribute_name_state();
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.35 Before attribute value state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-attribute-value-state
    fn before_attribute_value_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => self.before_attribute_value_state(),
                '"' => self.attribute_value_double_quoted_state(),
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
    fn attribute_value_double_quoted_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '"' => self.after_attribute_value_quoted_state(),
                '&' => todo!(),
                '\u{0000}' => todo!(),
                _ => {
                    self.current_attribute_value.push(char);
                    self.attribute_value_double_quoted_state();
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.39 After attribute value (quoted) state
    // https://html.spec.whatwg.org/multipage/parsing.html#after-attribute-value-(quoted)-state
    fn after_attribute_value_quoted_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => todo!(),
                '/' => todo!(),
                '>' => {
                    self.emit_current_tag_token();
                    self.data_state();
                }
                _ => todo!(),
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.42 Markup declaration open state
    // https://html.spec.whatwg.org/multipage/parsing.html#markup-declaration-open-state
    fn markup_declaration_open_state(&mut self) {
        if String::from_iter(&self.chars[self.index..self.index + 2]) == "--" {
            self.consume_next_characters(2);
            self.current_comment_data = String::new();
            self.comment_start_state();
        } else if lowercase_char_slice(&self.chars[self.index..self.index + 7]) == "doctype" {
            self.consume_next_characters(7);
            self.doctype_state()
        } else {
            todo!()
        }
    }

    // 13.2.5.43 Comment start state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-state
    fn comment_start_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '-' => self.comment_start_dash_state(),
                '>' => todo!(),
                _ => {
                    self.reconsume();
                    self.comment_state();
                }
            }
        } else {
            todo!()
        }
    }
    // 13.2.5.44 Comment start dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-start-dash-state
    fn comment_start_dash_state(&mut self) {
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
    fn comment_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '<' => todo!(),
                '-' => self.commend_end_dash_state(),
                '\u{0000}' => todo!(),
                _ => {
                    self.current_comment_data.push(char);
                    self.comment_state();
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.50 Comment end dash state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-dash-state
    fn commend_end_dash_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '-' => self.commend_end_state(),
                _ => {
                    self.current_comment_data.push('-');
                    self.reconsume();
                    self.comment_state();
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.51 Comment end state
    // https://html.spec.whatwg.org/multipage/parsing.html#comment-end-state
    fn commend_end_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                '>' => {
                    self.emit(Token::Comment(self.current_comment_data.clone()));
                    self.data_state()
                }
                _ => todo!(),
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.53 DOCTYPE state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-state
    fn doctype_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => self.before_doctype_name_state(),
                _ => todo!(),
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.54 Before DOCTYPE name state
    // https://html.spec.whatwg.org/multipage/parsing.html#before-doctype-name-state
    fn before_doctype_name_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => self.before_doctype_name_state(),
                _ if char.is_ascii_uppercase() => todo!(),
                '\u{0000}' => todo!(),
                '>' => todo!(),
                _ => {
                    self.current_doc_type = DOCTYPE::default();
                    self.current_doc_type.name = Some(String::from(char));
                    self.doctype_name_state()
                }
            }
        } else {
            todo!()
        }
    }

    // 13.2.5.55 DOCTYPE name state
    // https://html.spec.whatwg.org/multipage/parsing.html#doctype-name-state
    fn doctype_name_state(&mut self) {
        if let Some(char) = self.consume_next_input_character() {
            match char {
                _ if is_one_of_tab_lf_ff_space(char) => todo!(),
                _ if char.is_ascii_uppercase() => {
                    todo!(); //Append the lowercase version of the current input character (add 0x0020 to the character's code point) to the current DOCTYPE token's name.
                    self.doctype_name_state()
                }
                '\u{0000}' => todo!(),
                '>' => {
                    self.emit(Token::Doctype(self.current_doc_type.clone()));
                    self.data_state();
                }
                _ => {
                    self.current_doc_type
                        .name
                        .as_mut()
                        .expect("name should be a string")
                        .push(char);
                    self.doctype_name_state()
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
