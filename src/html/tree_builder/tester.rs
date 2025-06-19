use std::error::Error;
use std::fs::File;
use std::io::{self, BufRead, BufReader, Cursor, ErrorKind, Read};
use std::path::Path;
use std::thread::current;

use crate::html::tree_builder::tree::{Node, Tree};

pub struct TestReader<R>
where
    R: Read,
{
    iter: io::Lines<io::BufReader<R>>,
    last_error: Option<String>,
}

impl<R> TestReader<R>
where
    R: Read,
{
    fn from_reader(reader: BufReader<R>) -> Self {
        TestReader {
            iter: reader.lines(),
            last_error: None,
        }
    }

    pub fn new_from_file<P>(file_name: P) -> io::Result<TestReader<File>>
    where
        P: AsRef<Path>,
    {
        let file = File::open(file_name)?;
        let reader = io::BufReader::new(file);
        Ok(TestReader::from_reader(reader))
    }

    pub fn new_from_string(content: String) -> TestReader<io::Cursor<String>> {
        let cursor = io::Cursor::new(content);
        let reader = io::BufReader::new(cursor);
        TestReader::from_reader(reader)
    }

    fn format_indentation(depth: usize) -> String {
        let indentation = if (depth == 0) {
            "".to_owned()
        } else {
            let mut str = "|".to_owned();
            str.push_str(&" ".repeat(depth * 2 - 1));
            str
        };
        indentation
    }

    pub fn assert_eq(test_case: &TestCase, tree: &Tree<Node>) -> bool {
        let mut tree_iter = tree.into_iter();
        let node = tree_iter.next(); // #document
        for (line_number, line) in test_case.document.iter().enumerate() {
            if let Some(node) = tree_iter.next() {
                let indentation = Self::format_indentation(tree_iter.get_current_depth());
                if format!("{}{:?}", indentation, node) == *line {
                    continue;
                };
                panic!(
                    "assertion `tree == test_case` failed in line {} \n tree:      {:?}\n test_case: {:?}",
                    line_number,
                    format!("{}{:?}", indentation, node),
                    line
                );
            }
            panic!(
                "assertion `tree == test_case` failed in line {} \n tree:      {:?}\n test_case: {:?}",
                line_number, None::<Node>, line
            );
        }
        if let Some(node) = tree_iter.next() {
            let indentation = Self::format_indentation(tree_iter.get_current_depth());
            panic!(
                "assertion `tree == test_case` failed \n tree:      {:?}\n test_case: {:?}",
                format!("{}{:?}", indentation, node),
                None::<String>
            );
        }

        return true;
    }

    pub fn get(self) -> TestCase {
        let mut test_case = TestCase::new();
        let mut current_state = TestState::Data;
        for line in self.iter.map_while(Result::ok) {
            if line == "" {
                break;
            }
            match line.as_str() {
                "#data" => current_state = TestState::Data,
                "#errors" => current_state = TestState::Error,
                "#document" => current_state = TestState::Document,
                "#new-errors" | "#document-fragment" | "#script-off" | "#script-on" => todo!(),
                _ => match current_state {
                    TestState::Data => test_case.data.push(line),
                    TestState::Error => test_case.error.push(line),
                    TestState::Document => test_case.document.push(line),
                },
            }
        }
        test_case
    }
}

#[derive(Debug)]
pub struct TestCase {
    pub data: Vec<String>,
    pub error: Vec<String>,
    pub document: Vec<String>,
}

enum TestState {
    Data,
    Error,
    Document,
}

impl TestCase {
    pub fn new() -> Self {
        TestCase {
            data: Vec::new(),
            error: Vec::new(),
            document: Vec::new(),
        }
    }
}

#[cfg(test)]
mod test {
    use std::fs::File;
    use std::io::{self, BufRead, BufReader, Cursor, Lines, Read};

    use crate::html::tree_builder::tester::TestReader;
    use crate::html::tree_builder::tree::{Document, Element, Node, Text, Tree, debug_print_tree};

    fn create_element_node(tag_name: &str) -> Node {
        Node::Element(Element::new(tag_name.to_string()))
    }
    fn create_text_node(data: &str) -> Node {
        Node::Text(Text::new(data.to_string()))
    }

    #[test]
    fn basic() {
        let data = r#"#data
<!DOCTYPE html><html><head></head><body>Test</body></html>
#errors
(1,0): expected-doctype-but-got-chars
#document
| <html>
|   <head>
|   <body>
|     "Test"
"#;

        let mut test_reader = TestReader::<io::Cursor<String>>::new_from_string(data.to_owned());
        let test_case = test_reader.get();

        let mut tree = Tree::new();
        let document = tree.create_node(Node::Document(Document::new()));
        tree.set_root(document);
        let html = tree.create_node(create_element_node("html"));
        tree.add_children(document, vec![html]);
        let head = tree.create_node(create_element_node("head"));
        let body = tree.create_node(create_element_node("body"));
        tree.add_children(html, vec![head, body]);
        let test = tree.create_node(create_text_node("Test"));
        tree.add_children(body, vec![test]);
        TestReader::<io::Cursor<String>>::assert_eq(&test_case, &tree);
    }

    #[test]
    #[should_panic]
    fn tree_to_many_nodes() {
        let data = r#"#data
<!DOCTYPE html><html><head></head><body>Test</body></html>
#errors
(1,0): expected-doctype-but-got-chars
#document
| <html>
|   <head>
|   <body>
|     "Test"
"#;

        let mut test_reader = TestReader::<io::Cursor<String>>::new_from_string(data.to_owned());
        let test_case = test_reader.get();

        let mut tree = Tree::new();
        let document = tree.create_node(Node::Document(Document::new()));
        tree.set_root(document);
        let html = tree.create_node(create_element_node("html"));
        tree.add_children(document, vec![html]);
        let head = tree.create_node(create_element_node("head"));
        let body = tree.create_node(create_element_node("body"));
        tree.add_children(html, vec![head, body]);
        let test = tree.create_node(create_text_node("Test"));
        // one node to much
        let test2 = tree.create_node(create_text_node("Test"));
        tree.add_children(body, vec![test, test2]);
        TestReader::<io::Cursor<String>>::assert_eq(&test_case, &tree);
    }

    #[test]
    #[should_panic]
    fn tree_not_enough_nodes() {
        let data = r#"#data
<!DOCTYPE html><html><head></head><body>Test</body></html>
#errors
(1,0): expected-doctype-but-got-chars
#document
| <html>
|   <head>
|   <body>
|     "Test"
"#;

        let mut test_reader = TestReader::<io::Cursor<String>>::new_from_string(data.to_owned());
        let test_case = test_reader.get();

        let mut tree = Tree::new();
        let document = tree.create_node(Node::Document(Document::new()));
        tree.set_root(document);
        let html = tree.create_node(create_element_node("html"));
        tree.add_children(document, vec![html]);
        let head = tree.create_node(create_element_node("head"));
        let body = tree.create_node(create_element_node("body"));
        tree.add_children(html, vec![head, body]);
        // missing Test Node
        TestReader::<io::Cursor<String>>::assert_eq(&test_case, &tree);
    }
}
