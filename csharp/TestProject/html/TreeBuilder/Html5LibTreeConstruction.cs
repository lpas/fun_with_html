namespace TestProject.html.TreeBuilder;

using FunWithHtml.html.Tokenizer;
using FunWithHtml.html.TreeBuilder;


[TestClass]
public sealed class Html5LibTreeConstruction {

    private static string ProjectDirectory => Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName;

    [TestMethod]
    public void ReadFileVsReadStr() {
        var filePath = Path.Combine(ProjectDirectory, "html5lib-tests", "tree-construction", "tests1.dat");
        var testReader = TestReader.CreateFromFile(filePath);
        int[] skipTests = [29, 30, 57]; // tree building has problems infinity running tests
        // todo fix 31 attributes
        int[] expectWrongTree = [22, 23, 31, 32, 51, 52, 53, 56, 60];
        int[] expectWrongErrors = [27, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 55]; // tree is ok but parse error miss match

        foreach (var (testCase, index) in testReader.GetTestCases().Select((testCase, i) => (testCase, i))) {
            Console.WriteLine(index);
            if (skipTests.Contains(index)) continue;

            var tokenizer = new Tokenizer(string.Join('\n', testCase.data));
            var treeBuilder = new TreeBuilder(tokenizer);
            try {
                treeBuilder.build();
            } catch (Exception e) {
                Console.WriteLine(index);
                Console.WriteLine(e);
                foreach (var line in testCase.data) {
                    Console.WriteLine(line);
                }
                throw;
            }
            try {
                TestReader.AssertEqDocument(testCase, treeBuilder.Document);
            } catch {
                if (expectWrongTree.Contains(index)) continue;
                Console.WriteLine("ERROR TREE");
                Console.WriteLine(index);
                Console.WriteLine(testCase);
                treeBuilder.PrintDebugDocumentTree();
                foreach (var error in treeBuilder.Errors) {
                    Console.WriteLine(error);
                }
                throw;
            }

            try {
                TestReader.AssertEqErrors(testCase, treeBuilder.Errors);
            } catch {
                if (expectWrongErrors.Contains(index)) continue;
                Console.WriteLine("ERROR ERRORS");
                Console.WriteLine(index);
                Console.WriteLine(testCase);
                foreach (var error in treeBuilder.Errors) {
                    Console.WriteLine(error);
                }
                Console.WriteLine("----");
                foreach (var error in tokenizer.Errors) {
                    Console.WriteLine(error);
                }
                throw;
            }

            if (expectWrongTree.Contains(index) || expectWrongErrors.Contains(index)) {
                Console.WriteLine("ERROR SHOULD ERROR");
                Console.WriteLine(index);
                Console.WriteLine(testCase);
                throw new Exception("TEST SHOULD ERROR");
            }

        }
    }
}
