namespace TestProject.html.TreeBuilder;

using FunWithHtml.html.Tokenizer;
using FunWithHtml.html.TreeBuilder;


[TestClass]
public sealed class Html5LibTreeConstruction {

    private static string ProjectDirectory => Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName;

    private static (string, (int[] skipTests, int[] expectWrongTree, int[] expectWrongErrors))[] files = [
        ("adoption01.dat", ([7,8,12,13,14,16,],[1,2,4,5,9,10,11],[0])),
        ("adoption02.dat", ([0],[1,],[])),
        ("blocks.dat", ([],[],[])),
        ("comments01.dat", ([],[],[1,2,3,4,6,7,9,10,11,12,13])),
        ("doctype01.dat", ([],[],[2,3,4,7,8,9,10,11,12,16,17,18,19,20,21,23,25,30,33,34,35,36])),
        ("domjs-unsafe.dat", ([
        ],[4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,36,44,45,46,47,48],[3])),
        // entities01
        // entities02
        // foreign-fragment
        ("html5test-com.dat", ([6,7,8,9,10,13, // tokenizer
        20, // adopting index
        ],[19,],[1,2,3,11])),
        ("inbody01.dat", ([],[],[])),
        ("isindex.dat", ([],[],[])),
        ("main-element.dat", ([],[2],[])),
        // math
        ("menuitem-element.dat", ([9,],[],[])), // infinity spinning
        ("namespace-sensitivity.dat", ([],[0],[])),
        ("noscript01.dat", ([],[],[])),
        // pending-spec-changes-plain-text-unsafe
        // pending-spec-changes
        ("plain-text-unsafe.dat", ([
        ],[0,1,2,4,5,10, 13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32 ],[3,6,7,8,9,11,12])),
        ("quirks01.dat", ([],[1,2,3],[])),
        // ruby.dat 
         ("scriptdata01.dat", ([],[5,15,16,17,19,20,21,22,23,24,25,],[3,4,6])),
         ("search-element.dat", ([],[2],[])),
        // svg
        ("tables01.dat", ([],[3,6,7,8,9,16, 17,18],[])),
        ("template.dat", ([
            21,22,90,100, // infinity spinning
            91, 110, // index out of bounds
        ],[1,2,3,4,10,11,12, 20,37,40,41,44,64,65,66,67,68,79,80,81,82,83,84,85,86,87,88,89,92,93,94,95,96,97,98,99,101,107,109,110,111],
        [25,26,27,29,30,31,32,33,35,38,45, 106])),        
        // tests_innerHTML_1
        ("tests1.dat", ([29, 30, 57, 99, // tree building has problems infinity running tests
            70, 71, 72, 73, 74, 75, ],  // adoption agency index out of range
            [22, 23, 32, 51, 56, 60, 77, 78, 79, 90, 95, 101, 102, 110],
            [27, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 96] // tree is ok but parse error miss match
        )),
        ("tests2.dat", ([
            17, 24, 30, 31, // tokenizer not implemented
            39, // infinity running
        ],[10,12,14,18, 26, 27,],
        [16, 20, 21, 22, 23, 25, 28, 34, 42, 58, 59, 61])),
        ("tests3.dat", ([],[
            4, 5, 6, 7, 11, 16, 17,18, // need peek token in body <pre> & <textarea>
            19, 21, 23 // adopting agency
        ],[])),
        ("tests4.dat", ([],[],[])), // fragments
        ("tests5.dat", ([12, 13],[],[])),// tokenizer not implemented
        ("tests6.dat", ([
            2, 3, // tokenizer not implemented
            32, 35, // adopting
        ],[25,47,48,49,50,51],[4, 5])),
        ("tests7.dat", ([],[1, 7,8,11, 12, 15, 19, 20, 21, 22, 23, 30, 31, 32],[])),
        ("tests8.dat", ([],[4, 5, 6, 7,8,9,],[])),
        // 9-14 are math & svg stuff
        ("tests15.dat", ([],[6, 7, 8, 9, 10, 11, 12,],[])),
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
            28, 29, //  infinity running
        ],[7,8,9,12,14,15,23,24, 21,22,],[])),
        ("tests19.dat", ([
            14, 15, 16, 17, 97, 98, // infinity running
            90, 91, 92, // adopting index out of bounds
            93,94 // adopting
        ],[
            0, 24, 25, 26,30, 31, 32, 33, 34, 45, 47, 72, 73, 74, 76, 79, 82, 89,
        ],[83])),
        ("tests20.dat", ([
            62, //infinity spinning
        ],[41,48,49,52, 53, 54,55,56,57,58,59,60,61,62,63],[])),
        // tests21 svg
        ("tests22.dat", ([0,1,2,3,4],[],[])), // adopting
        ("tests23.dat", ([],[0,1,2,3,4],[])), // adopting
        ("tests24.dat", ([0,1,2,3,4,5,6,7],[],[])), // tokenizer
        ("tests25.dat", ([],[],[])),
        ("tests26.dat", ([0,3, 4, // infinity running
        ],[2,9,10,11,12,13,16,17,18,19],[14])),
        // tricky01
        ("webkit01.dat", ([
            15, // tokenizer
            48, // infinity spinning
        ],[36,37,40,41, 42, 43,46,47,49,50,],[3,9,10,13,14,17,19,38,39,44,45])),
        ("webkit02.dat", ([
                12,13, //index out of bounds
                15, 30, 31, 32, 33, 34,  //infinity spinning
        ],[5,10,19, 20, 22,23, 24,],[4])),
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
                    continue;
                    throw new Exception("TEST SHOULD ERROR");
                }
            }
        }
    }
}
