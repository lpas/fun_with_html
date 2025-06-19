#![allow(unused)]
mod html;
use crate::html::tree_builder::tester::TestReader;
use crate::html::tree_builder::{self, TreeBuilder};

use self::html::tokenizer::Tokenizer;

use std::fs::File;
use std::io::{self, Read};

fn main() -> io::Result<()> {
    // let file_path = "src/index.html";
    // let mut file = File::open(file_path)?;
    // let mut contents = String::new();
    // file.read_to_string(&mut contents)?;

    let file_path = "html5lib-tests/tree-construction/tests1.dat";
    let test_reader = TestReader::<File>::new_from_file(file_path)?;
    let test_case = test_reader.get();
    println!("{:?}", test_case);
    let contents = test_case.data.join("\n");
    let tokenizer = Tokenizer::new(&contents);
    let mut tree_builder = TreeBuilder::new();
    tree_builder.build(tokenizer);
    tree_builder.debug_print();

    TestReader::<File>::assert_eq(&test_case, tree_builder.get_tree());

    Ok(())
}
