using FunWithHtml.html.Tokenizer;
using FunWithHtml.html.TreeBuilder;
using FunWithHtml.renderer;

string path = @"./index.html";
string content = File.ReadAllText(path);
var tokenizer = new Tokenizer(content);
var treeBuilder = new TreeBuilder(tokenizer);
treeBuilder.build();

var r = new Renderer(treeBuilder.Document);
r.Render("my_console_drawing.png");

