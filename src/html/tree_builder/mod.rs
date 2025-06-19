pub mod tester;
pub mod tree;

use std::{cell::RefCell, rc::Rc};

use crate::html::{
    tokenizer::{Tag, Token, Tokenizer},
    tree_builder::tree::{Document, Element, Node, Text, Tree, debug_print_tree},
};

// pub struct Node {
//     children: Vec<Node>,
// }

// pub struct Document {
//     element: Option<Rc<RefCell<Element>>>,
// }

// #[derive(Debug)]
// pub struct Element {
//     name: String,
// }

// // https://dom.spec.whatwg.org/#interface-documenttype
// pub struct DocumentType {
//     node: Node,
//     name: String,
//     publicId: String,
//     systemId: String,
// }

#[derive(Debug)]
enum InsertionMode {
    Initial,
    BeforeHtml,
    BeforeHead,
    InHead,
    BeforeInHead,
    BeforeInHeadNoscript,
    AfterHead,
    InBody,
    Text,
    InTable,
    InTableText,
    InCaption,
    InColumnGroup,
    InTableBody,
    InRow,
    InCell,
    InSelect,
    InSelectInTable,
    InTemplate,
    AfterBody,
    InFrameset,
    AfterFrameset,
    AfterAfterBody,
    AfterAfterFrameset,
}

pub struct TreeBuilder {
    tree: Tree<Node>,
    current_insertion_mode: InsertionMode, // tokenizer: Tokenizer,
    stack_of_open_elements: Vec<usize>,
    head_element_pointer: Option<usize>,
    document: usize,
}

impl TreeBuilder {
    pub fn new() -> Self {
        let mut tree = Tree::new();
        let document = tree.create_node(Node::Document(Document::new()));
        tree.set_root(document);
        Self {
            tree,
            current_insertion_mode: InsertionMode::Initial,
            stack_of_open_elements: vec![],
            head_element_pointer: None,
            document,
        }
    }

    pub fn get_tree(&self) -> &Tree<Node> {
        &self.tree
    }

    pub fn debug_print(&self) {
        debug_print_tree(&self.tree);
    }

    pub fn build(&mut self, tokenizer: Tokenizer) {
        self.current_insertion_mode = InsertionMode::Initial;
        let mut tokenizer_iter = tokenizer.into_iter();
        let mut reprocess_token: Option<Token> = None;

        let mut token: Token = Token::EndOfFile;
        loop {
            if let Some(c_token) = reprocess_token {
                token = c_token;
                reprocess_token = None;
            } else if let Some(c_token) = tokenizer_iter.next() {
                token = c_token
            } else {
                break;
            }
            println!("token emit: {:?}", token);

            match self.current_insertion_mode {
                // https://html.spec.whatwg.org/multipage/parsing.html#the-initial-insertion-mode
                InsertionMode::Initial => match token {
                    Token::Character(c)
                        if c == '\u{0009}'
                            || c == '\u{000A}'
                            || c == '\u{000C}'
                            || c == '\u{000D}'
                            || c == ' ' =>
                    {
                        todo!()
                    }
                    Token::Comment(c) => todo!(),
                    Token::Doctype(doctype) => {
                        // todo not implemented at all
                        self.current_insertion_mode = InsertionMode::BeforeHtml;
                    }
                    _ => {
                        // todo
                        // If the document is not an iframe srcdoc document, then this is a parse error; if the parser cannot change the mode flag is false, set the Document to quirks mode.
                        self.current_insertion_mode = InsertionMode::BeforeHtml;
                        reprocess_token = Some(token);
                    }
                },
                // https://html.spec.whatwg.org/multipage/parsing.html#the-before-html-insertion-mode
                InsertionMode::BeforeHtml => match token {
                    Token::Doctype(doctype) => todo!(),
                    Token::Comment(comment) => todo!(),
                    Token::Character(c)
                        if c == '\u{0009}'
                            || c == '\u{000A}'
                            || c == '\u{000C}'
                            || c == '\u{000D}'
                            || c == ' ' =>
                    {
                        ()
                    }
                    Token::StartTag(tag) if tag.name == "html" => {
                        // create an element for the token in the HTML namespace, with the Document as the intended parent.
                        let element = self.create_element_for_token(tag, "namespace", ()); // todo document is the intended_parent;                         
                        let el_node = self.tree.create_node(element);
                        // Append it to the Document object.
                        self.tree.add_child(self.document, el_node);
                        // Pull this element in the stack of the open elements
                        self.stack_of_open_elements.push(el_node);
                        // Switch the insertion mode to before head
                        self.current_insertion_mode = InsertionMode::BeforeHead;
                    }
                    Token::EndTag(tag)
                        if tag.name != "head"
                            && tag.name != "body"
                            && tag.name != "html"
                            && tag.name != "br" =>
                    {
                        // todo parse error
                        // ignore the token
                    }
                    _ => {
                        let mut tag = Tag::new();
                        tag.name = "html".into();
                        // Create an html element whose node document is the Document object.
                        let element = self.create_element_for_token(tag, "namespace", ());
                        let el_node = self.tree.create_node(element);
                        //  Append it to the Document object.
                        self.tree.add_child(self.document, el_node);
                        //  Put this element in the stack of open elements.
                        self.stack_of_open_elements.push(el_node);
                        // Switch the insertion mode to "before head", then reprocess the token.
                        self.current_insertion_mode = InsertionMode::BeforeHead;
                        reprocess_token = Some(token);
                    }
                },
                // https://html.spec.whatwg.org/multipage/parsing.html#the-before-head-insertion-mode
                InsertionMode::BeforeHead => match token {
                    Token::Character(c)
                        if c == '\u{0009}'
                            || c == '\u{000A}'
                            || c == '\u{000C}'
                            || c == '\u{000D}'
                            || c == ' ' =>
                    {
                        ()
                    }
                    Token::Comment(_) => (), // todo don't ignore comments
                    Token::Doctype(doctype) => todo!(),
                    Token::StartTag(tag) if tag.name == "html" => todo!(),
                    Token::StartTag(tag) if tag.name == "head" => {
                        let element = self.insert_an_html_element(tag);
                        self.head_element_pointer = Some(element);
                        self.current_insertion_mode = InsertionMode::InHead;
                    }
                    Token::EndTag(tag)
                        if tag.name != "head"
                            && tag.name != "body"
                            && tag.name != "html"
                            && tag.name != "br" =>
                    {
                        // todo parse error
                        // ignore the token
                    }
                    _ => {
                        // Insert an HTML element for a "head" start tag token with no attributes.
                        let mut tag = Tag::new();
                        tag.name = "head".into();
                        let element = self.insert_an_html_element(tag);
                        // Set the head element pointer to the newly created head element.
                        self.head_element_pointer = Some(element);
                        // Switch the insertion mode to "in head".
                        self.current_insertion_mode = InsertionMode::InHead;
                        // Reprocess the current token.
                        reprocess_token = Some(token)
                    }
                },
                // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inhead
                InsertionMode::InHead => match token {
                    Token::Character(c)
                        if c == '\u{0009}'
                            || c == '\u{000A}'
                            || c == '\u{000C}'
                            || c == '\u{000D}'
                            || c == ' ' =>
                    // todo insert characters
                    {
                        ()
                    }
                    Token::Comment(comment) => todo!(),
                    Token::StartTag(tag) if tag.name == "html" => todo!(),
                    Token::StartTag(tag)
                        if tag.name == "base"
                            || tag.name == "basefont"
                            || tag.name == "bgsound"
                            || tag.name == "link" =>
                    {
                        todo!()
                    }
                    Token::StartTag(tag) if tag.name == "meta" => {
                        self.insert_an_html_element(tag);
                        self.stack_of_open_elements.pop();
                    }
                    Token::StartTag(tag) if tag.name == "title" => todo!(),
                    Token::StartTag(tag) if tag.name == "noscript" /* todo && scripting_flag.is_enabled() */ || tag.name == "noframes" || tag.name == "style" => {
                        todo!()
                    }
                    Token::StartTag(tag) if tag.name == "noscript" /* && scripting_flag.is_disabled() */ => todo!(),
                    Token::StartTag(tag) if tag.name == "script" => todo!(),
                    Token::EndTag(tag) if tag.name == "head" => {
                        self.stack_of_open_elements.pop();
                        self.current_insertion_mode = InsertionMode::AfterHead;
                    }
                    // todo:  An end tag whose tag name is one of: "body", "html", "br" Act as described in the "anything else" entry below.
                    Token::StartTag(tag) if tag.name == "template" => todo!(),
                    Token::EndTag(tag) if tag.name == "template" => todo!(),
                    Token::StartTag(tag) if tag.name == "head" => todo!(),
                    Token::EndTag(tag) => todo!(),
                    _ => {
                        //Pop the current node (which will be the head element) off the stack of open elements.
                        self.stack_of_open_elements.pop();
                        //Switch the insertion mode to "after head".
                        self.current_insertion_mode = InsertionMode::AfterHead;
                        //Reprocess the token.
                        reprocess_token = Some(token);
                    },
                },
                // https://html.spec.whatwg.org/multipage/parsing.html#the-after-head-insertion-mode
                InsertionMode::AfterHead => match token {
                    Token::Character(c)
                        if c == '\u{0009}'
                            || c == '\u{000A}'
                            || c == '\u{000C}'
                            || c == '\u{000D}'
                            || c == ' ' =>
                    // todo insert characters
                    {
                        ()
                    }
                    Token::Comment(comment) => todo!(),
                    Token::Doctype(doctype) => todo!(),
                    Token::StartTag(tag) if tag.name == "html" => todo!(),
                    Token::StartTag(tag) if tag.name == "body" => {
                        // self.stack_of_open_elements.push(Rc::clone(&element));

                        // self.insert_an_html_element(tag);
                        // todo Set the frameset-ok flag to "not ok".

                        // let element = Rc::new(RefCell::new(element));
                        self.insert_an_html_element(tag);
                        self.current_insertion_mode = InsertionMode::InBody;
                    }
                    Token::StartTag(tag) if tag.name == "frameset" => todo!(),
                    Token::StartTag(tag)
                        if !matches!(
                            tag.name.as_str(),
                            "base"
                                | "basefont"
                                | "bgsound"
                                | "link"
                                | "meta"
                                | "noframes"
                                | "script"
                                | "style"
                                | "template"
                                | "title"
                        ) =>
                    {
                        todo!()
                    }
                    Token::EndTag(tag) if tag.name == "template" => todo!(),
                    // todo An end tag whose tag name is one of: "body", "html", "br" => Act as described in the "anything else" entry below.
                    Token::StartTag(tag) if tag.name == "head" => todo!(),
                    Token::EndTag(tag) => todo!(),
                    _ => {
                        let mut tag = Tag::new();
                        tag.name = "body".into();
                        // Insert an HTML element for a "body" start tag token with no attributes.
                        self.insert_an_html_element(tag);
                        // Switch the insertion mode to "in body".
                        self.current_insertion_mode = InsertionMode::InBody;
                        // Reprocess the current token.
                        reprocess_token = Some(token);
                    }
                },
                // 13.2.6.4.7 The "in body" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inbody
                InsertionMode::InBody => match token {
                    Token::Character(c) if c == '\u{0000}' => todo!(),
                    Token::Character(c)
                        if c == '\u{0009}'
                            || c == '\u{000A}'
                            || c == '\u{000C}'
                            || c == '\u{000D}'
                            || c == ' ' =>
                    // todo insert characters
                    {
                        ()
                    }
                    Token::Character(c) => {
                        // Reconstruct the active formatting elements, if any.
                        // todo
                        // Insert the token's character.
                        self.insert_a_character(c);
                        // Set the frameset-ok flag to "not ok".
                        // todo
                    }
                    Token::Comment(commend) => todo!(),
                    Token::Doctype(doctype) => todo!(),
                    Token::StartTag(tag) if tag.name == "html" => todo!(),
                    Token::StartTag(tag)
                        if !matches!(
                            tag.name.as_str(),
                            "base"
                                | "basefont"
                                | "bgsound"
                                | "link"
                                | "meta"
                                | "noframes"
                                | "script"
                                | "style"
                                | "template"
                                | "title"
                        ) =>
                    {
                        todo!()
                    }
                    Token::EndTag(tag) if tag.name == "tempalte" => todo!(),
                    Token::EndTag(tag) if tag.name == "body" => {
                        // todo
                        self.current_insertion_mode = InsertionMode::AfterBody;
                    }
                    Token::StartTag(tag) if tag.name == "frameset" => todo!(),
                    Token::EndOfFile => {
                        // If the stack of template insertion modes is not empty, then process the token using the rules for the "in template" insertion mode.
                        // todo
                        // Otherwise, follow these steps:
                        // todo
                        // If there is a node in the stack of open elements that is not either a dd element, a dt element, an li element, an optgroup element, an option element, a p element, an rb element, an rp element, an rt element, an rtc element, a tbody element, a td element, a tfoot element, a th element, a thead element, a tr element, the body element, or the html element, then this is a parse error.
                        // todo
                        // Stop parsing.
                        return;
                    }
                    Token::EndTag(tag) if tag.name == "body" => todo!(),
                    _ => todo!(),
                },
                // 13.2.6.4.19 The "after body" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-afterbody
                InsertionMode::AfterBody => match token {
                    Token::Character(c)
                        if c == '\u{0009}'
                            || c == '\u{000A}'
                            || c == '\u{000C}'
                            || c == '\u{000D}'
                            || c == ' ' =>
                    // todo Process the token using the rules for the "in body" insertion mode.
                    {
                        ()
                    }
                    Token::EndTag(tag) if tag.name == "html" => {
                        // todo
                        self.current_insertion_mode = InsertionMode::AfterAfterBody;
                    }
                    _ => todo!(),
                },
                // 13.2.6.4.22 The "after after body" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#the-after-after-body-insertion-mode
                InsertionMode::AfterAfterBody => match token {
                    Token::Character(c)
                        if c == '\u{0009}'
                            || c == '\u{000A}'
                            || c == '\u{000C}'
                            || c == '\u{000D}'
                            || c == ' ' =>
                    // todo Process the token using the rules for the "in body" insertion mode.
                    {
                        ()
                    }
                    Token::EndOfFile => return,
                    _ => todo!(),
                },

                _ => todo!(
                    "Insertion mode not implemented {:?}",
                    self.current_insertion_mode
                ),
            }
        }
    }

    fn insert_an_html_element(&mut self, tag: Tag) -> usize {
        let element = self.create_element_for_token(tag, "namespace", ());
        let el = self.tree.create_node(element);

        if let Some(current) = self.get_current_node() {
            self.tree.add_child(*current, el);
        }
        self.stack_of_open_elements.push(el);
        return el;
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-a-character
    fn insert_a_character(&mut self, c: char) {
        // todo this seems to hacky
        if let Some(cur_index) = self.get_current_node2() {
            let node = self.tree.get_node(cur_index);
            if let Some(last_node) = node.children.top() {
                let node = self.tree.get_node(*last_node);
                // If there is a Text node immediately before the adjusted insertion location, then append data to that Text node's data.
                if let Node::Text(txt) = &node.data {
                    let mut txt = txt.data.clone();
                    txt.push(c);
                    let node = Node::Text(Text::new(txt));
                    self.tree.replace_data_at_index(*last_node, node);
                    return;
                }
            }

            let node = Node::Text(Text::new(c.to_string()));
            let text_node = self.tree.create_node(node);
            self.tree.add_child(cur_index, text_node);
        }
    }

    fn get_current_node(&self) -> Option<&usize> {
        return self.stack_of_open_elements.top();
    }

    fn get_current_node2(&self) -> Option<usize> {
        let len = self.stack_of_open_elements.len();
        if len == 0 {
            return None;
        } else {
            return Some(self.stack_of_open_elements[len - 1]);
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#create-an-element-for-the-token
    // todo what is a token token?? --> i use Tag
    fn create_element_for_token(&self, tag: Tag, namespace: &str, intended_parent: ()) -> Node {
        // 1. If the active speculative HTML parser is not null, then return the result of creating a speculative mock element given namespace, token's tag name, and token's attributes.

        // 2. Otherwise, optionally create a speculative mock element given namespace, token's tag name, and token's attributes.
        // Note: The result is not used. This step allows for a speculative fetch to be initiated from non-speculative parsing. The fetch is still speculative at this point, because, for example, by the time the element is inserted, intended parent might have been removed from the document.

        // 3. Let document be intendedParent's node document.
        let document = {}; // todo
        // 4. Let localName be token's tag name.
        let local_name = tag.name;
        // 5. Let is be the value of the "is" attribute in token, if such an attribute exists; otherwise null.
        let is = {};
        // 6. Let registry be the result of looking up a custom element registry given intendedParent.
        let registry = {}; // todo
        // 7. Let definition be the result of looking up a custom element definition given registry, namespace, localName, and is.

        // 8. Let willExecuteScript be true if definition is non-null and the parser was not created as part of the HTML fragment parsing algorithm; otherwise false.
        let will_execute_script = false; // todo
        // 9. If willExecuteScript is true:

        // 1. Increment document's throw-on-dynamic-markup-insertion counter.
        // 2. If the JavaScript execution context stack is empty, then perform a microtask checkpoint.
        // 3. Push a new element queue onto document's relevant agent's custom element reactions stack.

        // 10. Let element be the result of creating an element given document, localName, namespace, null, is, willExecuteScript, and registry.
        // Note: This will cause custom element constructors to run, if willExecuteScript is true. However, since we incremented the throw-on-dynamic-markup-insertion counter,
        //       this cannot cause new characters to be inserted into the tokenizer, or the document to be blown away.
        let element = self.create_an_element(
            document,
            local_name,
            namespace,
            None,
            is,
            will_execute_script,
            registry,
        );

        // 11. Append each attribute in the given token to element.
        // Note: This can enqueue a custom element callback reaction for the attributeChangedCallback, which might run immediately (in the next step).
        // Note: Even though the is attribute governs the creation of a customized built-in element, it is not present during the execution of the relevant custom element constructor; it is appended in this step, along with all other attributes.

        // 12. If willExecuteScript is true:

        // 1. Let queue be the result of popping from document's relevant agent's custom element reactions stack. (This will be the same element queue as was pushed above.)
        // 2. Invoke custom element reactions in queue.
        // 3. Decrement document's throw-on-dynamic-markup-insertion counter.

        // 13. If element has an xmlns attribute in the XMLNS namespace whose value is not exactly the same as the element's namespace, that is a parse error. Similarly, if element has an xmlns:xlink attribute in the XMLNS namespace whose value is not the XLink Namespace, that is a parse error.

        // 14. If element is a resettable element and not a form-associated custom element, then invoke its reset algorithm. (This initializes the element's value and checkedness based on the element's attributes.)

        // 15. If element is a form-associated element and not a form-associated custom element, the form element pointer is not null, there is no template element on the stack of open elements, element is either not listed or doesn't have a form attribute, and the intendedParent is in the same tree as the element pointed to by the form element pointer, then associate element with the form element pointed to by the form element pointer and set element's parser inserted flag.

        // 16. Return element.
        return element;
    }

    fn create_an_element(
        &self,
        document: (),
        local_name: String,
        namespace: &str,
        none: Option<()>,
        is: (),
        will_execute_script: bool,
        registry: (),
    ) -> Node {
        // todo
        return Node::Element(Element::new(local_name));
    }
}

trait Stack {
    fn top(&self) -> Option<&usize>;
}

impl Stack for Vec<usize> {
    fn top(&self) -> Option<&usize> {
        match &self[..] {
            [] => None,
            [.., n] => Some(n),
        }
    }
}
