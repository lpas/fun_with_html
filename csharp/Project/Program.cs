using FunWithHtml.html.Tokenizer;
using FunWithHtml.html.TreeBuilder;


string path = @"./index.html";
string content = File.ReadAllText(path);

var tokenizer = new Tokenizer(content);
var treeBuilder = new TreeBuilder();
treeBuilder.build(tokenizer);



