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

        foreach (var (testCase, index) in testReader.GetTestCases().Select((testCase, i) => (testCase, i))) {
            Console.WriteLine(testCase);
            Console.WriteLine(index);

            var tokenizer = new Tokenizer(string.Join('\n', testCase.data));
            var treeBuilder = new TreeBuilder();
            treeBuilder.build(tokenizer);

            try {
                TestReader.AssertEq(testCase, treeBuilder.Document);
            } catch {
                Console.WriteLine("ERROR");
                Console.WriteLine(testCase);
                treeBuilder.PrintDebugDocumentTree();
                throw;
            }
            Console.WriteLine("OK!");
        }
    }
}
