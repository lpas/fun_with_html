
using System.Text;
using FunWithHtml.html.TreeBuilder;
using TestProject.html.Tokenizer;

namespace TestProject.html.TreeBuilder;


public struct TestCase {
    public List<string> data = [];
    public List<string> errors = [];
    public List<string> newErrors = [];
    public List<string> document = [];
    public List<string> documentFragment = [];
    public bool? scripting = null;

    public TestCase() { }


    public override string ToString() {
        var sb = new StringBuilder();
        sb.AppendLine("==TestCase==");
        sb.AppendLine($"#data:\n{string.Join("\n", data)}");
        sb.AppendLine($"#errors:\n{string.Join("\n", errors)}");
        if (newErrors.Count > 0)
            sb.AppendLine($"#new-errors:\n{string.Join("\n", newErrors)}");
        sb.AppendLine($"#document:\n{string.Join("\n", document)}");
        sb.AppendLine();
        return sb.ToString();
    }
}

enum TestState {
    Data,
    Errors,
    NewErrors,
    Document,
    ScriptOn,
    ScriptOf,
    DocumentFragment,
}

public class TestReader(IEnumerable<string> iter) {
    private IEnumerable<string> iter = iter;

    public static TestReader CreateFromFile(string path) {
        return new TestReader(File.ReadLines(path));
    }

    public static TestReader CreateFromString(string str) {
        return new TestReader(str.Split([Environment.NewLine], StringSplitOptions.None));
    }


    public IEnumerable<TestCase> GetTestCases() {
        var testCase = new TestCase();
        var currentState = TestState.Data;
        var notEmitted = false;
        foreach (var line in iter) {
            if (line == "" && currentState == TestState.Document) {
                yield return testCase;
                notEmitted = false;
                testCase = new TestCase();
                currentState = TestState.Data;
                continue;
            }
            switch (line) {
                case "#data":
                    currentState = TestState.Data;
                    break;
                case "#errors":
                    currentState = TestState.Errors;
                    break;
                case "#document":
                    currentState = TestState.Document;
                    break;
                case "#new-errors":
                    currentState = TestState.NewErrors;
                    break;
                case "#script-on":
                    testCase.scripting = true;
                    currentState = TestState.ScriptOn;
                    break;
                case "#script-off":
                    testCase.scripting = false;
                    currentState = TestState.ScriptOf;
                    break;
                case "#document-fragment":
                    currentState = TestState.DocumentFragment;
                    break;
                default:
                    notEmitted = true;
                    switch (currentState) {
                        case TestState.Data: testCase.data.Add(line); break;
                        case TestState.Errors: testCase.errors.Add(line); break;
                        case TestState.NewErrors: testCase.newErrors.Add(line); break;
                        case TestState.DocumentFragment: testCase.documentFragment.Add(line); break;
                        case TestState.Document:
                            if (line[0] == '|') {
                                testCase.document.Add(line);
                            } else {
                                testCase.document[^1] += '\n' + line;
                            }
                            break;
                    }
                    break;
            }
        }
        // this can happen if the last line is not an empty line
        if (notEmitted) yield return testCase;
    }

    public static void AssertEqDocument(TestCase testCase, Document document) {
        var stack = new Stack<(Node, int)>();
        foreach (var child in Enumerable.Reverse(document.childNodes)) {
            stack.Push((child, 1));
        }
        var iter = testCase.document.GetEnumerator();
        while (stack.Count > 0) {
            var (node, depth) = stack.Pop();
            var indentation = depth == 0 ? "" : ("|" + new string(' ', depth * 2 - 1));
            if (!iter.MoveNext()) {
                Assert.Fail($"tree != testCase (testCase empty) \n tree:     {indentation}{node} \n testCase: {iter.Current}");
            }
            if ($"{indentation}{node}" != iter.Current) {
                Assert.Fail($"tree != testCase (diff) \n tree:     {indentation}{node} \n testCase: {iter.Current}");
            }
            foreach (var child in Enumerable.Reverse(node.childNodes)) {
                stack.Push((child, depth + 1));
            }
            if (node is Element element && element.attributes.Count > 0) {
                foreach (var attr in Enumerable.Reverse(element.attributes)) {
                    stack.Push((new NodeAttr(attr.Key, attr.Value), depth + 1));
                }
            }
        }
        if (iter.MoveNext()) {
            Assert.Fail($"tree != testCase (document empty) \n tree:     \n testCase: {iter.Current}");
        }
    }


    public static void PrintDebugDocumentTree(Node baseNode) {
        var stack = new Stack<(Node, int)>();
        stack.Push((baseNode, 0));
        while (stack.Count > 0) {
            var (node, depth) = stack.Pop();
            var indentation = depth == 0 ? "" : ("|" + new string(' ', depth * 2 - 1));
            Console.WriteLine($"{indentation}{node}");
            foreach (var child in Enumerable.Reverse(node.childNodes)) {
                stack.Push((child, depth + 1));
            }
        }
    }

    internal static void AssertEqErrors(TestCase testCase, List<ParseError> errors) {
        if (testCase.errors.Count != errors.Count) {
            Assert.Fail($"Not the same error count. testCase: {testCase.errors.Count} errors: {errors.Count}");
        }
    }
}



