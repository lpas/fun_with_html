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
            new Line("background", "#00f"),
            new Line("padding", "10"),
            new Line("margin", "20"),
            new Line("border", "1"),
        ]
    },
    new Block {
        name = "ul",
        value = [
            new Line("background", "#f00"),
            new Line("padding", "10"),
            new Line("padding-top", "0"),
            new Line("margin", "20"),
            new Line("margin-top", "0"),
            new Line("border", "2"),
            new Line("border-top", "0"),
            new Line("border-color", "#008000"),
        ]
    }
];

var r = new Renderer(styles, treeBuilder.Document);
r.Render("my_console_drawing.png");

