namespace TestProject.css;

using System.Text.Json;
using FunWithHtml.css.Parser;

[TestClass]
public sealed class CssSelector {
    private static string ProjectDirectory => Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName;

    private static readonly string[] files = [
        "test1.jsonc",
    ];

    [TestMethod]
    public void TestFiles() {
        var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        foreach (var file in files) {
            var filePath = Path.Combine(ProjectDirectory, "css-tests", "selector", file);
            var contents = File.ReadAllText(filePath);
            var tests = JsonSerializer.Deserialize<List<TestEntry>>(contents, options) ?? throw new InvalidOperationException();
            foreach (var (test, index) in tests.Select((test, i) => (test, i))) {
                Console.WriteLine($"{index}: |{test.Input}|");
                Assert.AreEqual(test.Serialized, SerializeInput(test.Input));
            }
        }
    }

    private string? SerializeInput(string selector) {
        var data = selector + "{ font-size: 1em; }";
        var sheet = Parser.ParseAStylesheet(data);
        if (sheet.cssRules.Count != 1) return null;
        List<ComponentValue> prelude = ((QualifiedRule)sheet.cssRules[0]).prelude ?? throw new NotImplementedException();
        var tokens = new TokenStream(prelude);
        return FunWithHtml.css.Selector.Parser.ConsumeComplexSelectorList(tokens)?.Serialize();
    }

}



class TestEntry {
    public required string Input { get; set; }
    public required string Serialized { get; set; }
}


