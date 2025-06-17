use std::fmt;

pub struct Tree<T> {
    data: Vec<TreeNode<T>>,
    root: Option<usize>,
}

pub struct TreeNode<T> {
    index: usize,
    children: Vec<usize>,
    data: T,
}

impl<T> Tree<T> {
    pub fn new() -> Self {
        Tree {
            data: Vec::new(),
            root: None,
        }
    }

    pub fn set_root(&mut self, root: usize) {
        self.root = Some(root);
    }

    pub fn create_node(&mut self, data: T) -> usize {
        let index = self.data.len();
        let node = TreeNode {
            index,
            children: Vec::new(),
            data,
        };
        self.data.push(node);
        index
    }

    pub fn add_children(&mut self, parent: usize, children: Vec<usize>) {
        let parent_element = &mut self.data[parent];
        parent_element.children.extend(children.into_iter());
    }

    pub fn add_child(&mut self, parent: usize, child: usize) {
        let parent_element = &mut self.data[parent];
        parent_element.children.push(child);
    }

    pub fn get_node(&self, index: usize) -> &TreeNode<T> {
        &self.data[index]
    }
}

pub fn debug_print_tree<T>(tree: &Tree<T>)
where
    T: std::fmt::Debug,
{
    let mut stack = Vec::new();
    if let Some(root) = tree.root {
        stack.push((root, 0));
    }

    loop {
        if let Some((cur, depth)) = stack.pop() {
            let node = tree.get_node(cur);
            let indentation = if (depth == 0) {
                "".to_owned()
            } else {
                let mut str = "|".to_owned();
                str.push_str(&" ".repeat(depth * 2 - 1));
                str
            };
            println!("{}{:?}", indentation, node.data);
            for x in (&node.children).into_iter().rev() {
                stack.push((*x, depth + 1))
            }
        } else {
            break;
        }
    }
}

pub struct IntoIter<'a, T> {
    tree: &'a Tree<T>,
    stack: Vec<(usize, usize)>,
    current_depth: usize,
}

impl<'a, T> Tree<T> {
    pub fn into_iter(&'a self) -> IntoIter<'a, T> {
        let mut stack = Vec::new();
        let mut current_depth = 0;
        if let Some(root) = self.root {
            stack.push((root, current_depth))
        }

        IntoIter {
            tree: &self,
            stack,
            current_depth,
        }
    }
}

impl<'a, T> IntoIter<'a, T> {
    fn get_current_depth(&self) -> usize {
        self.current_depth
    }
}

impl<'a, T> Iterator for IntoIter<'a, T> {
    type Item = &'a T;

    fn next(&mut self) -> Option<Self::Item> {
        if let Some((cur, depth)) = self.stack.pop() {
            let node = self.tree.get_node(cur);
            self.current_depth = depth;
            for x in (&node.children).into_iter().rev() {
                self.stack.push((*x, depth + 1));
            }
            return Some(&node.data);
        } else {
            self.current_depth = 0;
            None
        }
    }
}

#[derive(PartialEq)]
pub struct Document {}
#[derive(PartialEq)]
pub struct Element {
    tag_name: String,
}

#[derive(PartialEq)]
pub struct Text {
    data: String,
}

#[derive(PartialEq)]
pub enum Node {
    Document(Document),
    Element(Element),
    Text(Text),
}

impl fmt::Debug for Node {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::Document(document) => write!(f, "#document"),
            Self::Element(element) => write!(f, "<{}>", element.tag_name),
            Self::Text(text) => write!(f, "\"{}\"", text.data),
        }
    }
}

impl Document {
    pub fn new() -> Self {
        Document {}
    }
}

impl Element {
    pub fn new(tag_name: String) -> Self {
        Element { tag_name }
    }
}

impl Text {
    pub fn new(data: String) -> Self {
        Text { data }
    }
}

#[cfg(test)]
mod test {
    use crate::html::tree_builder::tree::debug_print_tree;

    use super::{Document, Element, Node, Text, Tree};

    fn create_element_node(tag_name: &str) -> Node {
        Node::Element(Element::new(tag_name.to_string()))
    }
    fn create_text_node(data: &str) -> Node {
        Node::Text(Text::new(data.to_string()))
    }

    #[test]
    fn basic() {
        let mut tree = Tree::new();

        let document = tree.create_node(Node::Document(Document::new()));
        tree.set_root(document);
        let html = tree.create_node(create_element_node("html"));
        tree.add_children(document, vec![html]);
        let head = tree.create_node(create_element_node("head"));
        let body = tree.create_node(create_element_node("body"));
        tree.add_children(html, vec![head, body]);
        let test = tree.create_node(create_text_node("Text"));
        tree.add_children(body, vec![test]);

        let mut iter = tree.into_iter();
        assert_eq!(iter.next(), Some(&Node::Document(Document::new())));
        assert_eq!(iter.get_current_depth(), 0);
        assert_eq!(iter.next(), Some(&create_element_node("html")));
        assert_eq!(iter.get_current_depth(), 1);
        assert_eq!(iter.next(), Some(&create_element_node("head")));
        assert_eq!(iter.get_current_depth(), 2);
        assert_eq!(iter.next(), Some(&create_element_node("body")));
        assert_eq!(iter.get_current_depth(), 2);
        assert_eq!(iter.next(), Some(&create_text_node("Text")));
        assert_eq!(iter.get_current_depth(), 3);
        assert_eq!(iter.next(), None);
    }
}
