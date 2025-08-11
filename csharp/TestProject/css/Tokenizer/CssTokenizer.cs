namespace TestProject.css.Tokenizer;

using System.Text.Json;
using FunWithHtml.css.Tokenizer;




[TestClass]
public sealed class CssTokenizer {
    private static string ProjectDirectory => Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName;

    private static string[] files = [
        "test1.jsonc",
        "test2.jsonc",
    ];

    [TestMethod]
    public void TestFiles() {
        var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        foreach (var file in files) {
            var filePath = Path.Combine(ProjectDirectory, "css-tests", "tokenizer", file);
            var contents = File.ReadAllText(filePath);
            var tests = JsonSerializer.Deserialize<List<TestEntry>>(contents, options);
            foreach (var (test, index) in tests.Select((test, i) => (test, i))) {
                Console.WriteLine($"{index}: |{test.Input}|");

                List<Token> tokens = [];
                try {
                    var tokenizer = new Tokenizer(test.Input);
                    while (true) {
                        var token = tokenizer.ConsumeAToken();
                        if (token is EofToken) break;
                        tokens.Add(token);
                    }
                } catch (Exception e) {
                    Console.WriteLine(e);
                    throw;
                }

                if (tokens.Count != test.Tokens.Count) {
                    Console.WriteLine("test != tokens");
                    Console.WriteLine("tokenizer:");
                    foreach (var token in tokens) {
                        Console.WriteLine(token);
                    }
                    Console.WriteLine("test:");
                    foreach (var token in test.Tokens) {
                        Console.WriteLine(token);
                    }
                    throw new Exception("test != tokens");
                }

                for (var i = 0; i < tokens.Count; i++) {
                    if (tokens[i].ToString() != test.Tokens[i]) {
                        Console.WriteLine($"test != tokens [{i}]");
                        Console.WriteLine("tokenizer:");
                        Console.WriteLine(tokens[i].ToString());
                        Console.WriteLine("test:");
                        Console.WriteLine(test.Tokens[i].ToString());
                        throw new Exception($"test != tokens [{i}]");
                    }
                }
            }
        }
    }

}



class TestEntry {
    public required string Input { get; set; }
    public required List<string> Tokens { get; set; }
    public required List<string> Errors { get; set; }
}


