namespace TestProject.html.TreeBuilder;

using FunWithHtml.html.Tokenizer;
using FunWithHtml.html.TreeBuilder;


[TestClass]
public sealed class Html5LibTreeConstruction {

    private static string ProjectDirectory => Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName;

    private static (string, (int[] skipTests, int[] expectWrongTree, int[] expectWrongErrors))[] files = [
        ("adoption01.dat", ([],[7,8,13,14],[])),
        ("adoption02.dat", ([],[0],[])),
        ("blocks.dat", ([],[],[])),
        ("comments01.dat", ([],[],[1,2,3,4,6,7,9,10,11,12,13])),
        ("doctype01.dat", ([],[],[2,3,4,7,8,9,10,11,12,16,17,18,19,20,21,23,25,30,33,34,35,36])),
        ("domjs-unsafe.dat", ([],[],[3,4,5,6,7,8,10,14,15,16,36])),
        ("entities01.dat", ([],[],[1,2,4,6,7,14,15,16,17,18,19,20,21,22,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,
            41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,59,60,61,62,64, 66, 67,68,69,70,71,72,73,74])),
        ("entities02.dat", ([],[],[9,10,11,12,13,19,23,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41])),
        // foreign-fragment
        ("html5test-com.dat", ([],[20],[1,2,3,11,13])),
        ("inbody01.dat", ([],[],[])),
        ("isindex.dat", ([],[],[])),
        ("main-element.dat", ([],[],[])),
        ("math.dat", ([],[],[])),
        ("menuitem-element.dat", ([],[],[])),
        ("namespace-sensitivity.dat", ([],[0],[])),
        ("noscript01.dat", ([],[],[])),
        // pending-spec-changes-plain-text-unsafe
        // pending-spec-changes
        ("plain-text-unsafe.dat", ([],[0],[1,2,3,4,5,6,7,8,9,11,12,13,14,15,16,17,18,19,20,26,27,28,29,30,31,32])),
        ("quirks01.dat", ([],[],[])),
        // ruby.dat 
        ("scriptdata01.dat", ([],[],[3,4,6, 15,16,17,19,20,21,22])),
        ("search-element.dat", ([],[],[])),
        ("svg.dat", ([],[],[])),
        ("tables01.dat", ([],[17],[3])),
        ("template.dat", ([],[91,107],[])),
        // tests_innerHTML_1
        ("tests1.dat", ([],[70, 71, 72, 73, 74, 75],[27, 32, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 110])),
        ("tests2.dat", ([],[],[14,16, 17, 18, 20, 21, 22, 23, 25, 26, 27, 28, 30, 31, 34, 42, 58, 59, 61])),
        ("tests3.dat", ([],[21],[])), // adopting agency
        ("tests4.dat", ([],[],[])),
        ("tests5.dat", ([],[],[])),
        ("tests6.dat", ([],[],[4, 5, 25])),
        ("tests7.dat", ([],[],[30, 31, 32])),
        ("tests8.dat", ([],[],[])),
        ("tests9.dat", ([],[23,24,
            25,26 // todo example of tree compare math attributes
            ],[])),
        ("tests10.dat", ([],[22,23,24,25,],[1])),
        ("tests11.dat", ([],[],[])),
        ("tests12.dat", ([],[],[])),
        // tests13.dat is missing
        ("tests14.dat", ([],[],[])),
        ("tests15.dat", ([],[],[6,7,8,9])),
        ("tests16.dat", ([],[137,138,139,140,
        ],[10, 17, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 48, 49, 50, 51, 52, 53, 58, 59,
            38, 39, 40, 41, 42, 43, 44, 45, 46, 69,71,109,116,121,122,123,124,125,126,127,128,129,130,131,132,133,
            134,135,136,137,138,139,140,141,142,143,144,145,147,148,149,150,155,156,166,168
        ])),
        ("tests17.dat", ([],[],[])),
        ("tests18.dat", ([],[],[23,24])),
        ("tests19.dat", ([],[15, 90,91,92],[24,26])),
        ("tests20.dat", ([],[],[])),
        ("tests21.dat", ([],[],[2,3,4,7,8,9,13,16,17])),
        ("tests22.dat", ([],[0,1,2,3,4],[])), // adopting
        ("tests23.dat", ([],[],[])),
        ("tests24.dat", ([],[],[])),
        ("tests25.dat", ([],[],[])),
        ("tests26.dat", ([],[],[9,14])),
        ("tricky01.dat", ([],[1,8,9],[])),
        ("webkit01.dat", ([],[],[3,9,10,13,14,17,19,44])),
        ("webkit02.dat", ([],[12,13,22,23,],[4])),
    ];


    [TestMethod]
    public void ReadFileVsReadStr() {
        foreach (var (file, (skipTests, expectWrongTree, expectWrongErrors)) in files) {
            var filePath = Path.Combine(ProjectDirectory, "html5lib-tests", "tree-construction", file);
            Console.WriteLine(file);
            RunTestsForFile(filePath, skipTests, expectWrongTree, expectWrongErrors);
        }
    }

    private static void RunTestsForFile(string filePath, int[] skipTests, int[] expectWrongTree, int[] expectWrongErrors) {
        var testReader = TestReader.CreateFromFile(filePath);
        foreach (var (testCase, index) in testReader.GetTestCases().Select((testCase, i) => (testCase, i))) {
            // if no scripting is in testcase we run with scriptingFlag on&off otherwise with the specified value
            foreach (var scriptingFlag in testCase.scripting is null ? new bool[] { true, false } : [testCase.scripting.Value]) {
                // Console.WriteLine($"{index}:{scriptingFlag}");
                if (skipTests.Contains(index)) continue;
                if (testCase.documentFragment.Count > 0) continue; // todo handle fragment cases

                var tokenizer = new Tokenizer(string.Join('\n', testCase.data));
                var treeBuilder = new TreeBuilder(tokenizer) { scriptingFlag = scriptingFlag };
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
                    TestReader.PrintDebugDocumentTree(treeBuilder.Document);
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
                    continue;
                    throw new Exception("TEST SHOULD ERROR");
                }
            }
        }
    }
}
