mod html;
use self::html::tokenizer::Tokenizer;

use std::fs::File;
use std::io::{self, Read};

fn main() -> io::Result<()> {
    let file_path = "src/index.html";
    let mut file = File::open(file_path)?;
    let mut contents = String::new();
    file.read_to_string(&mut contents)?;
    let mut tokenizer = Tokenizer::new(&contents);
    tokenizer.run();

    Ok(())
}
