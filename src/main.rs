#![allow(unused)]
mod html;
use crate::html::tree_builder::{self, TreeBuilder};

use self::html::tokenizer::Tokenizer;

use std::fs::File;
use std::io::{self, Read};

fn main() -> io::Result<()> {
    let file_path = "src/index.html";
    let mut file = File::open(file_path)?;
    let mut contents = String::new();
    file.read_to_string(&mut contents)?;
    let tokenizer = Tokenizer::new(&contents);
    let mut tree_builder = TreeBuilder::new();
    tree_builder.build(tokenizer);
    tree_builder.debug_print();

    Ok(())
}
