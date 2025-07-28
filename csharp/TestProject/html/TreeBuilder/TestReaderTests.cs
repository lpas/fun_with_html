namespace TestProject.html.TreeBuilder;

using FunWithHtml.html.TreeBuilder;


[TestClass]
public sealed class TestReaderTests {

    private static string ProjectDirectory => Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName;
    private static string TestCaseString => """
#data
Test
#errors
(1,0): expected-doctype-but-got-chars
#document
| <html>
|   <head>
|   <body>
|     "Test"
""";

    [TestMethod]
    public void ReadFileVsReadStr() {
        var testReader = TestReader.CreateFromString(TestCaseString);

        var testCase = testReader.GetTestCases().First();
        var filePath = Path.Combine(ProjectDirectory, "html5lib-tests", "tree-construction", "tests1.dat");
        var fileReader = TestReader.CreateFromFile(filePath);
        var testCaseFile = fileReader.GetTestCases().First();

        Assert.AreEqual(1, testCase.data.Count);
        CollectionAssert.AreEqual(testCase.data, testCaseFile.data);

        Assert.AreEqual(1, testCase.errors.Count);
        CollectionAssert.AreEqual(testCase.errors, testCaseFile.errors);

        Assert.AreEqual(4, testCase.document.Count);
        CollectionAssert.AreEqual(testCase.document, testCaseFile.document);
    }

    [TestMethod]
    public void EmptyData() {
        var testReader = TestReader.CreateFromString("""
            #data

            #errors
            (1,0): expected-doctype-but-got-eof
            #document
            | <html>
            |   <head>
            |   <body>
            """);
        var testCase = testReader.GetTestCases().First();
        Assert.AreEqual(1, testCase.data.Count);
        Assert.AreEqual(1, testCase.errors.Count);
        Assert.AreEqual(3, testCase.document.Count);

    }

    [TestMethod]
    public void TreeWithAttributes() {
        var testReader = TestReader.CreateFromString("""
            #data
            <hr foo="bar">
            #errors
            (1,0): expected-doctype-but-got-eof
            #document
            | <html>
            |   <head>
            |   <body>
            |     <hr>
            |       foo="bar"
            """);
        var testCase = testReader.GetTestCases().First();
        var document = new Document();
        var html = new Element(document, "html");
        document.childNodes.Add(html);
        var head = new Element(document, "head");
        var body = new Element(document, "body");
        html.childNodes.AddRange([head, body]);
        var hr = new Element(document, "hr");
        hr.attributes.Add("foo", "bar");
        body.childNodes.Add(hr);
        TestReader.AssertEqDocument(testCase, document);
    }

    [TestMethod]
    public void TreeWithMultipleAttributes() {
        var testReader = TestReader.CreateFromString("""
            #data
            <hr foo="bar">
            #errors
            (1,0): expected-doctype-but-got-eof
            #document
            | <html>
            |   <head>
            |   <body>
            |     <hr>
            |       bar="baz"
            |       filterUnits=""
            |       filterres=""
            |       foo="bar"
            """);
        var testCase = testReader.GetTestCases().First();
        var document = new Document();
        var html = new Element(document, "html");
        document.childNodes.Add(html);
        var head = new Element(document, "head");
        var body = new Element(document, "body");
        html.childNodes.AddRange([head, body]);
        var hr = new Element(document, "hr");
        hr.attributes.Add("foo", "bar");
        hr.attributes.Add("bar", "baz");
        hr.attributes.Add("filterres", "");
        hr.attributes.Add("filterUnits", "");
        body.childNodes.Add(hr);
        TestReader.AssertEqDocument(testCase, document);
    }

    [TestMethod]
    public void TreeWithMultiLine() {
        var testReader = TestReader.CreateFromString("""
            #data
            test
            test
            #errors
            (2,4): expected-doctype-but-got-chars
            #document
            | <html>
            |   <head>
            |   <body>
            |     "test
            test"
            """);
        var testCase = testReader.GetTestCases().First();
        var document = new Document();
        var html = new Element(document, "html");
        document.childNodes.Add(html);
        var head = new Element(document, "head");
        var body = new Element(document, "body");
        html.childNodes.AddRange([head, body]);
        var text = new Text(document, "test\ntest");
        body.childNodes.Add(text);
        TestReader.AssertEqDocument(testCase, document);
    }

    [TestMethod]
    public void TreeWithHtmlAttrs() {
        var testReader = TestReader.CreateFromString("""
            #data
            <!DOCTYPE html><html><body><html id=x>
            #errors
            (1,38): non-html-root
            #document
            | <!DOCTYPE html>
            | <html>
            |   id="x"
            |   <head>
            |   <body>
            """);
        var testCase = testReader.GetTestCases().First();
        var document = new Document();
        var html = new Element(document, "html");
        var doctype = new DocumentType(document, "html");
        html.attributes.Add("id", "x");
        document.childNodes.AddRange([doctype, html]);
        var head = new Element(document, "head");
        var body = new Element(document, "body");
        html.childNodes.AddRange([head, body]);
        TestReader.AssertEqDocument(testCase, document);
    }

    [TestMethod]
    public void TreeWithTemplate() {
        var testReader = TestReader.CreateFromString("""
            #data
            <body><template>Hello</template>
            #errors
            no doctype
            #document
            | <html>
            |   <head>
            |   <body>
            |     <template>
            |       content
            |         "Hello"
            """);
        var testCase = testReader.GetTestCases().First();
        var document = new Document();
        var html = new Element(document, "html");
        document.childNodes.Add(html);
        var head = new Element(document, "head");
        var body = new Element(document, "body");
        html.childNodes.AddRange([head, body]);
        var template = new Element(document, "template");
        body.childNodes.Add(template);
        var text = new Text(document, "Hello");
        template.childNodes.Add(text);
        TestReader.AssertEqDocument(testCase, document);
    }


    [TestMethod]
    public void TreeCompare() {
        var testReader = TestReader.CreateFromString(TestCaseString);
        var testCase = testReader.GetTestCases().First();
        var document = new Document();
        var html = new Element(document, "html");
        document.childNodes.Add(html);
        var head = new Element(document, "head");
        var body = new Element(document, "body");
        html.childNodes.AddRange([head, body]);
        var text = new Text(document, "Test");
        body.childNodes.Add(text);
        TestReader.AssertEqDocument(testCase, document);
    }

    [TestMethod]
    public void TreeCompareToManyNodes() {
        var testReader = TestReader.CreateFromString(TestCaseString);
        var testCase = testReader.GetTestCases().First();
        var document = new Document();
        var html = new Element(document, "html");
        document.childNodes.Add(html);
        var head = new Element(document, "head");
        var body = new Element(document, "body");
        html.childNodes.AddRange([head, body]);
        var text = new Text(document, "Test");
        var text2 = new Text(document, "Test");
        body.childNodes.AddRange([text, text2]);

        var ex = Assert.ThrowsException<AssertFailedException>(() => TestReader.AssertEqDocument(testCase, document));
        StringAssert.Contains(ex.Message, "tree != testCase (testCase empty)");
    }

    [TestMethod]
    public void TreeCompareNotEnoughNodes() {
        var testReader = TestReader.CreateFromString(TestCaseString);
        var testCase = testReader.GetTestCases().First();
        var document = new Document();
        var html = new Element(document, "html");
        document.childNodes.Add(html);
        var head = new Element(document, "head");
        var body = new Element(document, "body");
        html.childNodes.AddRange([head, body]);

        var ex = Assert.ThrowsException<AssertFailedException>(() => TestReader.AssertEqDocument(testCase, document));
        StringAssert.Contains(ex.Message, "tree != testCase (document empty)");
    }

    [TestMethod]
    public void TreeCompareWrongNode() {
        var testReader = TestReader.CreateFromString(TestCaseString);
        var testCase = testReader.GetTestCases().First();
        var document = new Document();
        var html = new Element(document, "html");
        document.childNodes.Add(html);
        var head = new Element(document, "head");
        var body = new Element(document, "body");
        html.childNodes.AddRange([head, body]);
        var div = new Element(document, "div");
        body.childNodes.Add(div);

        var ex = Assert.ThrowsException<AssertFailedException>(() => TestReader.AssertEqDocument(testCase, document));
        StringAssert.Contains(ex.Message, "tree != testCase (diff)");
    }

}
