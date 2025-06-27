
using FunWithHTML.html.TreeBuilder;

namespace TestProject.html.TreeBuilder;


public struct TestCase {
    public List<string> data = [];
    public List<string> errors = [];
    public List<string> document = [];

    public TestCase() { }


    public override string ToString() {
        return $"==TestCase==\n" +
               $"#data:\n{string.Join("\n", data)}\n" +
               $"#errors:\n{string.Join("\n", errors)}\n" +
               $"#document:\n{string.Join("\n", document)}\n" +
               $" ";
    }
}

enum TestState {
    Data,
    Errors,
    Document,
}

public class TestReader(IEnumerable<string> iter) {
    private IEnumerable<string> iter = iter;

    public static TestReader CreateFromFile(string path) {
        return new TestReader(File.ReadLines(path));
    }

    public static TestReader CreateFromString(string str) {
        return new TestReader(str.Split([Environment.NewLine], StringSplitOptions.None));
    }


    public TestCase Get() {
        var testCase = new TestCase();
        var currentState = TestState.Data;
        foreach (var line in iter) {
            if (line == "") break;
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
                case "#new-errors" or "#document-fragment" or "#script-off" or "#script-on":
                    throw new NotImplementedException();
                default:
                    switch (currentState) {
                        case TestState.Data: testCase.data.Add(line); break;
                        case TestState.Errors: testCase.errors.Add(line); break;
                        case TestState.Document: testCase.document.Add(line); break;
                    }
                    break;
            }
        }
        return testCase;

    }

    public static void AssertEq(TestCase testCase, Document document) {
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


}



