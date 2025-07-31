namespace TestProject.html.Tokenizer;

using System.Text.Json;
using FunWithHtml.html;
using FunWithHtml.html.Tokenizer;
using FunWithHtml.html.TreeBuilder;



[TestClass]
public sealed class Html5LibTreeConstruction {

    // files + expected errors
    private static (string, int[])[] files = [
        ("contentModelFlags.test", []),
        ("domjs.test", []),
        ("entities.test", []),
        ("escapeFlag.test", []),
        ("namedEntities.test", []),
        ("numericEntities.test", []),
        // pendingSpecChanges
        ("test1.test", []),
        ("test2.test", []),
        ("test3.test", [79]), // Errors are not in the same order
        ("test4.test", [61]), // no errors raised in lookahead
        // ("unicodeChars.test", []), // todo
        // ("unicodeCharsProblematic.test", []), // todo doubleEscaped
        // ("xmlViolation.test", []),  // todo  xmlViolationTests instead of tests
    ];

    [TestMethod]
    public void TestFiles() {
        foreach (var (file, expectedErrors) in files) {
            var path = $"C:\\code\\fun_with_html\\html5lib-tests\\tokenizer\\{file}";
            var contents = File.ReadAllText(path);
            var tests = JsonSerializer.Deserialize<Tests>(contents);
            foreach (var (test, index) in tests.tests.Select((test, i) => (test, i))) {
                // test4 no 34,35 -- utf16 handling problem with c#
                // if (index == 34) continue;
                // if (index == 35) continue;
                // if (index == 60) continue;
                // if (index == 61) continue;

                // test3
                // if (index == 71) continue;
                // if (index == 79) continue;
                // if (index == 1121) continue;
                // if (index == 1506) continue;

                var description = test.description;
                var input = test.input;
                Console.WriteLine($"{file}:{index}|{description}");
                if (test.doubleEscaped ?? false) continue;
                var error = checkTest(input, test);
                if (error is not null) {
                    if (expectedErrors.Contains(index)) continue;
                    Assert.Fail($"({file}:{index}|{description}) {error}");
                } else if (expectedErrors.Contains(index)) {
                    Assert.Fail($"Expected ERROR: ({file}:{index}|{description})");
                }
            }
        }
    }

    private static string? checkTest(string input, Test test) {
        var output = test.output;
        var lastStartTag = test.lastStartTag;
        var errors = BuildParseErrors(test);
        var testOutput = BuildOutputTokens(output);
        var initialStates = test.initialStates ?? ["Data state"];

        foreach (var startState in initialStates) {
            var state = getStateEnum(startState);
            var tokenizer = new Tokenizer(input) { state = state };
            if (lastStartTag is not null) {
                tokenizer.lastStartTagTagName = lastStartTag;
            }

            List<Token> tokenizerOutput = [];
            try {
                while (true) {
                    var token = tokenizer.NextToken();
                    if (token is EndOfFile) break;
                    tokenizerOutput.Add(token);
                }
            } catch (Exception e) {
                return "EXCEPTION: " + e.ToString();
            }

            if (tokenizerOutput.Count != testOutput.Count) {
                Console.WriteLine("test:");
                foreach (var item in testOutput) {
                    Console.WriteLine(item);
                }
                Console.WriteLine("tokenizer:");
                foreach (var item in tokenizerOutput) {
                    Console.WriteLine(item);
                }
                return "tokenizer != test";
            }

            for (var i = 0; i < tokenizerOutput.Count; i++) {
                if (!tokenizerOutput[i].Equals(testOutput[i])) {
                    // Console.WriteLine("test:");
                    // foreach (var item in testOutput) {
                    //     Console.WriteLine(item);
                    // }
                    // Console.WriteLine("tokenizer:");
                    // foreach (var item in tokenizerOutput) {
                    //     Console.WriteLine(item);
                    // }
                    return $"tokenizer != test {i}: tokenizer ({tokenizerOutput[i]}) test ({testOutput[i]})";
                }
            }

            if (errors.Count != tokenizer.Errors.Count) {
                Console.WriteLine("test:");
                foreach (var error in errors) {
                    Console.WriteLine(error);
                }
                Console.WriteLine("tokenizer:");
                foreach (var error in tokenizer.Errors) {
                    Console.WriteLine(error);
                }
                return "[errors] tokenizer != test";
            }
            for (var i = 0; i < tokenizer.Errors.Count; i++) {
                if (tokenizer.Errors[i].error != errors[i].error) {
                    return $"[errors] tokenizer != test {i}: tokenizer ({tokenizer.Errors[i]}) test ({errors[i]})";
                }
            }
        }

        return null;
    }

    private static List<ParseError> BuildParseErrors(Test test) {
        return [.. (test.errors ?? []).Select(item => new ParseError() { error = item.code, col = item.col, line = item.line })];
    }

    private static List<Token> BuildOutputTokens(List<List<JsonElement>> output) {
        List<Token> testOutput = [];
        foreach (var tItem in output) {
            Token testToken = tItem[0].GetString() switch {
                "DOCTYPE" => new DOCTYPE { name = tItem[1].GetString(), publicId = tItem[2].GetString(), systemId = tItem[3].GetString(), forceQuirks = !tItem[4].GetBoolean() },
                "StartTag" => new StartTag {
                    name = tItem[1].GetString()!,
                    Attributes = [.. tItem[2].Deserialize<Dictionary<string, string>>().Select((item) => new FunWithHtml.html.Tokenizer.Attribute(item.Key, item.Value))],
                    selfClosing = tItem.Count > 3 && tItem[3].GetBoolean()
                },
                "EndTag" => new EndTag { name = tItem[1].GetString()!, selfClosing = tItem.Count > 3 && tItem[3].GetBoolean() },
                "Comment" => new FunWithHtml.html.Tokenizer.Comment { data = tItem[1].GetString()! },
                "Character" => new CharacterStr(tItem[1].GetString()!),
                _ => throw new NotImplementedException(),
            };
            if (testToken is CharacterStr charStr) {
                foreach (var c in charStr.data) {
                    testOutput.Add(new Character(c));
                }
            } else {
                testOutput.Add(testToken);
            }
        }

        return testOutput;
    }

    private static State getStateEnum(string startState) {
        return startState switch {
            "Data state" => State.DataState,
            "PLAINTEXT state" => State.PLAINTEXTState,
            "RCDATA state" => State.RCDATAState,
            "RAWTEXT state" => State.RAWTEXTState,
            "Script data state" => State.ScriptDataState,
            "CDATA section state" => State.CDATASectionState,
            _ => throw new InvalidOperationException(),
        };
    }
}






class OutPUT {
    public string output { set; get; }
}

public class CharacterStr(string data): Token {
    public string data { get; set; } = data;

}

class Tests {
    public required Test[] tests { get; set; }
}

class Test {
    public required string description { get; set; }
    public required string input { get; set; }
    public required List<List<JsonElement>> output { get; set; }
    public List<ErrorItem>? errors { get; set; }
    public List<string>? initialStates { get; set; }
    public string? lastStartTag { get; set; }
    public bool? doubleEscaped { get; set; }
}

class ErrorItem {
    public required string code { get; set; }
    public required int line { get; set; }
    public required int col { get; set; }
}