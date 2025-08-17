using FunWithHtml.html.Tokenizer;
using FunWithHtml.html.TreeBuilder;
using FunWithHtml.renderer;

// string path = @"./index.html";
// string content = File.ReadAllText(path);

var content = """
    <UL>
        <LI>1st element of list</LI>
        <LI>2nd element of list</LI>
        <LI>3nd element of list</LI>
    </UL>
""";
var tokenizer = new Tokenizer(content);
var treeBuilder = new TreeBuilder(tokenizer);
treeBuilder.build();

Styles styles = [
    new Block {
        name = "li",
        value = [
            new Line("color", "#fff"),
            new Line("background-color", "#00f"),
            new Line("font-size", "1.5em"),
            new Line("margin-top", "30px"),
            new Line("margin-right", "20px"),
            new Line("margin-bottom", "20px"),
            new Line("margin-left", "20px"),
            new Line("padding-top", "10px"),
            new Line("padding-right", "10px"),
            new Line("padding-bottom", "10px"),
            new Line("padding-left", "10px"),
            new Line("border-width-top", "1px"),
            new Line("border-width-right", "1px"),
            new Line("border-width-bottom", "1px"),
            new Line("border-width-left", "1px"),
        ]
    },
    new Block {
        name = "ul",
        value = [
            new Line("background-color", "#f00"),
            new Line("color", "#000"),
            new Line("font-family", "Arial"),
            new Line("line-height", "1"),
            new Line("font-size", "14px"),
            new Line("margin-top", "40px"),
            new Line("margin-right", "20px"),
            new Line("margin-bottom", "20px"),
            new Line("margin-left", "20px"),
            new Line("padding-top", "0"),
            new Line("padding-right", "10px"),
            new Line("padding-bottom", "10px"),
            new Line("padding-left", "10px"),
            new Line("border-width-top", "0"),
            new Line("border-width-right", "2px"),
            new Line("border-width-bottom", "2px"),
            new Line("border-width-left", "2px"),


            new Line("border-color", "#008000"),
        ]
    }
];

var r = new Renderer(styles, treeBuilder.Document);
r.Render("my_console_drawing.png");

