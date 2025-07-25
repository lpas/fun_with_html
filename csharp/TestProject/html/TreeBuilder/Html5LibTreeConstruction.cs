namespace TestProject.html.TreeBuilder;

using FunWithHtml.html.Tokenizer;
using FunWithHtml.html.TreeBuilder;


[TestClass]
public sealed class Html5LibTreeConstruction {

    private static string ProjectDirectory => Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName;

    private static (string, (int[] skipTests, int[] expectWrongTree, int[] expectWrongErrors))[] files = [
        ("tests1.dat", ([29, 30, 57, 99, // tree building has problems infinity running tests
            70, 71, 72, 73, 74, 75,   // adoption agency index out of range
            102], // not implement it),
            [22, 23, 31, 32, 51, 52, 53, 56, 60, 77, 78, 79, 90, 95, 96, 101, 110],
            [27, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 52, 55] // tree is ok but parse error miss match
        )),
        ("tests2.dat", ([
            17, 24, 30, 31, // tokenizer not implemented
            39, // infinity running
        ],[
            9, // adopting agency
            10,12,14,18, 26, 27,
        ],[
            16, 20, 21, 22, 23, 25, 28, 34, 40, 42, 58, 59, 61
        ])),
        ("tests3.dat", ([],[
            4, 5, 6, 7, 11, 16, 17,18, // need peek token in body <pre> & <textarea>
            19, 21, 23 // adopting agency
        ],[])),
        ("tests4.dat", ([],[],[])), // fragments
        ("tests5.dat", ([
            12, 13, // tokenizer not implemented
        ],[],[])),
        ("tests6.dat", ([
            2, 3, // tokenizer not implemented
            25,
            32, 35, // adopting
            46, // doctype
            47, 48, 49, 50, 51 // frameset stuff
        ],[],[4, 5])),
        ("tests7.dat", ([
            23 // select
        ],[
            1, 7,8,11, 12, 15, 19, 20, 21, 22, 30, 31, 32
        ],[29])),
        ("tests8.dat", ([
        ],[
            4, 5, 6, 7,8,9,
        ],[])),
        // 9-14 are math & svg stuff
        ("tests15.dat", ([],[
            6, 7, 8, 9, 10, 11, 12,
        ],[])),
        ("tests16.dat", ([81,92,93,94,178,189, // not implemented in tokenizer
            195, // infinity running
        ],[
            38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 63, 64, 65, 66, 67, 68, 69, 71,
            137,138,139,140,141,142,143,144,145,146,160,161,162,163,164,165,166,168,194,
        ],[10, 17, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 48, 49, 50, 51, 52, 53, 58, 59,
            109,116,121,122,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,147,148,149,150,155,156,
        ])),
        ("tests17.dat", ([],[0,1,],[])),
        ("tests18.dat", ([
            14, 27, // select
            21,22, 35, // svg
            28, 29, //  infinity running
        ],[7,8,9,12,15,23,24,],[])),
        ("tests19.dat", ([
            0, 18, 30, 31, 32, 33, 34, 73, 81, 82, 83, // math
            19, 72, 74, 75, 76, // svg
            14, 15, 16, 17, 97, 98, // infinity running
            29, // not implemented
            90, 91, 92, // adopting index out of bounds
            93,94 // adopting
        ],[
            24, 25, 26,
            36, 37, 85,// tree compare
            45, 47, 79, 89, 101, 102,
        ],[])),
        ("tests20.dat", ([
            42,48,49, // svg
            52, 53, 54,55,56,57,58,59,60,61,62,63, //math
        ],[41],[])),
        // tests21 svg
        ("tests22.dat", ([4],[0,1,2,3],[])), // adopting
        ("tests23.dat", ([],[0,1,2,3,4],[])), // adopting
        ("tests24.dat", ([0,1,2,3,4,5,6,7],[],[])), // tokenizer
        ("tests25.dat", ([],[],[])),
        ("tests26.dat", ([0,3, 4, // infinity running
        10,11,16,17, // svg
        12,13,18,19 //math
        ],[2,9],[1,5,6,7,8,14])),
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
                Console.WriteLine($"{index}:{scriptingFlag}");
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
}
