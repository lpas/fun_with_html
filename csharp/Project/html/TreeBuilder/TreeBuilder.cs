using System.Diagnostics;
using FunWithHtml.html.Tokenizer;

namespace FunWithHtml.html.TreeBuilder;

enum InsertionMode {
    Initial,
    BeforeHtml,
    BeforeHead,
    InHead,
    BeforeInHead,
    BeforeInHeadNoscript,
    AfterHead,
    InBody,
    Text,
    InTable,
    InTableText,
    InCaption,
    InColumnGroup,
    InTableBody,
    InRow,
    InCell,
    InSelect,
    InSelectInTable,
    InTemplate,
    AfterBody,
    InFrameset,
    AfterFrameset,
    AfterAfterBody,
    AfterAfterFrameset,
}

public static class Namespaces {
    public static readonly string HTML = "http://www.w3.org/1999/xhtml";
}

public abstract class Node(Document? ownerDocument) {
    public Document? ownerDocument { get; } = ownerDocument;
    public List<Node> childNodes = [];
}
public class Document(): Node(null) {
    public override string ToString() {
        return "#document";
    }
}
public class Element(Document document, string localName): Node(document) {
    public string? @namespace;
    public string? namespacePrefix;
    public string localName = localName;

    public override string ToString() {
        return $"<{localName}>";
    }
}

public class Text(Document document, string data): Node(document) {
    public string data = data;

    public override string ToString() {
        return $"\"{data}\"";
    }
}

public class TreeBuilder(bool debugPrint = false) {

    private bool debugPrint = debugPrint;

    public Document Document { get => document; }

    private Document document = new();
    private InsertionMode insertionMode = InsertionMode.Initial;
    private List<Node> stackOfOpenElements = [];
    private Node currentNode { get => stackOfOpenElements[^1]; }

    private Element? headElementPointer = null;

    private bool scriptingFlag = false;


    public void build(Tokenizer.Tokenizer tokenizer) {
        Token? reprocessToken = null;

        while (true) {
            Token? token;
            if (reprocessToken != null) {
                token = reprocessToken;
                reprocessToken = null;
            } else {
                token = tokenizer.NextToken();
            }
            if (token == null) break;
            if (token is EndOfFile) break;

            if (debugPrint) Debug.WriteLine(token);


            switch (insertionMode) {
                // 13.2.6.4.1 The "initial" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#the-initial-insertion-mode
                case InsertionMode.Initial:
                    switch (token) {
                        case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                            break; // ignore the token
                        case DOCTYPE:
                            // todo implement the right way
                            insertionMode = InsertionMode.BeforeHtml;
                            break;
                        default:
                            // todo If the document is not an iframe srcdoc document, then this is a parse error; if the parser cannot change the mode flag is false, set the Document to quirks mode.
                            // In any case, switch the insertion mode to "before html",                            
                            insertionMode = InsertionMode.BeforeHtml;
                            // then reprocess the token.
                            reprocessToken = token;
                            break;
                    }
                    break;
                // 13.2.6.4.2 The "before html" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#the-before-html-insertion-mode
                case InsertionMode.BeforeHtml:
                    switch (token) {
                        case DOCTYPE: throw new NotImplementedException();
                        case Comment: throw new NotImplementedException();
                        case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                            break; // ignore the token
                        case StartTag { name: "html" }: {
                                // Create an element for the token in the HTML namespace, with the Document as the intended parent.
                                var element = CreateAnElementForTheToken((Tag)token, Namespaces.HTML, document);
                                // Append it to the Document object.
                                document.childNodes.Add(element);
                                // Put this element in the stack of open elements.
                                stackOfOpenElements.Add(element);
                                // Switch the insertion mode to "before head".
                                insertionMode = InsertionMode.BeforeHead;
                                break;
                            }
                        case EndTag { name: "head" or "body" or "html" or "br" }:
                            throw new NotImplementedException();
                        case EndTag:
                            throw new NotImplementedException();
                        default: {
                                // Create an html element whose node document is the Document object. Append it to the Document object. Put this element in the stack of open elements.
                                var element = CreateAnElement(document, "html", Namespaces.HTML);
                                document.childNodes.Add(element);
                                stackOfOpenElements.Add(element);
                                // Switch the insertion mode to "before head", then reprocess the token.                            
                                insertionMode = InsertionMode.BeforeHead;
                                reprocessToken = token;
                                break;
                            }

                    }
                    break;
                // 13.2.6.4.3 The "before head" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#the-before-head-insertion-mode
                case InsertionMode.BeforeHead:
                    switch (token) {
                        case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                            break; // ignore the token
                        case Comment: throw new NotImplementedException();
                        case DOCTYPE: throw new NotImplementedException();
                        case StartTag { name: "html" }: throw new NotImplementedException();
                        case StartTag { name: "head" }: {
                                // Insert an HTML element for the token.
                                var element = InsertAnHTMLElement(token);
                                //Set the head element pointer to the newly created head element.
                                headElementPointer = element;
                                //Switch the insertion mode to "in head".
                                insertionMode = InsertionMode.InHead;
                            }
                            break;
                        case EndTag { name: "head" or "body" or "html" or "br" }: throw new NotImplementedException();
                        case EndTag: throw new NotImplementedException();
                        default: {
                                // Insert an HTML element for a "head" start tag token with no attributes.
                                var element = InsertAnHTMLElement(new StartTag("head"));
                                // Set the head element pointer to the newly created head element.
                                headElementPointer = element;
                                // Switch the insertion mode to "in head".
                                insertionMode = InsertionMode.InHead;
                                // Reprocess the current token.
                                reprocessToken = token;
                                break;
                            }
                    }
                    break;
                // 13.2.6.4.4 The "in head" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inhead
                case InsertionMode.InHead:
                    switch (token) {
                        case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                            InsertACharacter(token);
                            break;
                        case Comment: throw new NotImplementedException();
                        case DOCTYPE: throw new NotImplementedException();
                        case StartTag { name: "html" }: throw new NotImplementedException();
                        case StartTag { name: "base" or "basefont" or "bgsound" or "link" }: throw new NotImplementedException();
                        case StartTag { name: "meta" }:
                            // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
                            InsertAnHTMLElement(token);
                            stackOfOpenElements.Pop();
                            // Acknowledge the token's self-closing flag, if it is set.

                            // If the active speculative HTML parser is null, then:

                            // 1. If the element has a charset attribute, and getting an encoding from its value results in an encoding, and the confidence is currently tentative, then change the encoding to the resulting encoding.

                            // 2. Otherwise, if the element has an http-equiv attribute whose value is an ASCII case-insensitive match for the string "Content-Type", and the element has a content attribute, and applying the algorithm for extracting a character encoding from a meta element to that attribute's value returns an encoding, and the confidence is currently tentative, then change the encoding to the extracted encoding.

                            break;
                        case StartTag { name: "title" }: throw new NotImplementedException();
                        case StartTag { name: "noscript" } when scriptingFlag: throw new NotImplementedException();
                        case StartTag { name: "noscript" } when !scriptingFlag: throw new NotImplementedException();
                        case StartTag { name: "script" }: throw new NotImplementedException();
                        case EndTag { name: "head" }:
                            // Pop the current node (which will be the head element) off the stack of open elements.
                            stackOfOpenElements.Pop();
                            // Switch the insertion mode to "after head".
                            insertionMode = InsertionMode.AfterHead;
                            break;
                        case EndTag { name: "body" or "html" or "br" }: throw new NotImplementedException();
                        case StartTag { name: "template" }: throw new NotImplementedException();
                        case EndTag { name: "template" }: throw new NotImplementedException();
                        case StartTag { name: "head" } or EndTag: throw new NotImplementedException();
                        default:
                            //Pop the current node (which will be the head element) off the stack of open elements.
                            stackOfOpenElements.Pop();
                            //Switch the insertion mode to "after head".
                            insertionMode = InsertionMode.AfterHead;
                            //Reprocess the token.
                            reprocessToken = token;
                            break;
                    }
                    break;
                // 13.2.6.4.6 The "after head" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#the-after-head-insertion-mode
                case InsertionMode.AfterHead:
                    switch (token) {
                        case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                            InsertACharacter(token);
                            break;
                        case Comment: throw new NotImplementedException();
                        case DOCTYPE: throw new NotImplementedException();
                        case StartTag { name: "html" }: throw new NotImplementedException();
                        case StartTag { name: "body" }:
                            // Insert an HTML element for the token.
                            InsertAnHTMLElement(token);
                            // Set the frameset-ok flag to "not ok".
                            // todo
                            // Switch the insertion mode to "in body".
                            insertionMode = InsertionMode.InBody;
                            break;
                        case StartTag { name: "frameset" }: throw new NotImplementedException();
                        case StartTag { name: "base" or "basefont" or "bgsound" or "link" or "meta" or "noframes" or "script" or "style" or "template" or "title" }:
                            throw new NotImplementedException();
                        case EndTag { name: "template" }: throw new NotImplementedException();
                        case EndTag { name: "body" or "html" or "br" }: throw new NotImplementedException();
                        case StartTag { name: "head" } or EndTag: throw new NotImplementedException();
                        default:
                            // Insert an HTML element for a "body" start tag token with no attributes.
                            InsertAnHTMLElement(new StartTag("body"));
                            // Switch the insertion mode to "in body".
                            insertionMode = InsertionMode.InBody;
                            // Reprocess the current token.
                            reprocessToken = token;
                            break;
                    }
                    break;
                // 13.2.6.4.7 The "in body" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inbody
                case InsertionMode.InBody:
                    switch (token) {
                        case Character { data: '\0' }: throw new NotImplementedException();
                        case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                            // Reconstruct the active formatting elements, if any.
                            // todo
                            // Insert the token's character.
                            InsertACharacter(token);
                            break;
                        case Character:
                            // Reconstruct the active formatting elements, if any.
                            //todo
                            // Insert the token's character.
                            InsertACharacter(token);
                            // Set the frameset-ok flag to "not ok".
                            // todo
                            break;
                        case Comment: throw new NotImplementedException();
                        case DOCTYPE: throw new NotImplementedException();
                        case StartTag { name: "html" }: throw new NotImplementedException();
                        case StartTag { name: "base" or "basefont" or "bgsound" or "link" or "meta" or "noframes" or "script" or "style" or "template" or "title" }:
                            throw new NotImplementedException();
                        case EndTag { name: "template" }: throw new NotImplementedException();
                        case StartTag { name: "body" }: throw new NotImplementedException();
                        case StartTag { name: "frameset" }: throw new NotImplementedException();
                        case EndOfFile:
                            // If the stack of template insertion modes is not empty, then process the token using the rules for the "in template" insertion mode.
                            // todo
                            // Otherwise, follow these steps:
                            // todo
                            // If there is a node in the stack of open elements that is not either a dd element, a dt element, an li element, an optgroup element, an option element, a p element, an rb element, an rp element, an rt element, an rtc element, a tbody element, a td element, a tfoot element, a th element, a thead element, a tr element, the body element, or the html element, then this is a parse error.
                            // todo
                            // Stop parsing.
                            return;
                        case EndTag { name: "body" }:
                            // If the stack of open elements does not have a body element in scope, this is a parse error; ignore the token.
                            // todo
                            // Otherwise, if there is a node in the stack of open elements that is not either a dd element, a dt element, an li element, an optgroup element, an option element, a p element, an rb element, an rp element, an rt element, an rtc element, a tbody element, a td element, a tfoot element, a th element, a thead element, a tr element, the body element, or the html element, then this is a parse error.
                            // todo
                            // Switch the insertion mode to "after body".
                            insertionMode = InsertionMode.AfterBody;
                            break;
                        case EndTag { name: "html" }: throw new NotImplementedException();
                        case StartTag {
                            name: "address" or "article" or "aside" or "blockquote" or "center" or
                            "details" or "dialog" or "dir" or "div" or "dl" or "fieldset" or "figcaption" or "figure" or
                            "footer" or "header" or "hgroup" or "main" or "menu" or "nav" or "ol" or "p" or "search" or
                            "section" or "summary" or "ul"
                        }:
                            throw new NotImplementedException();
                        case StartTag { name: "h1" or "h2" or "h3" or "h4" or "h5" or "h6" }: throw new NotImplementedException();
                        case StartTag { name: "pre" or "listing" }: throw new NotImplementedException();
                        case StartTag { name: "form" }: throw new NotImplementedException();
                        case StartTag { name: "li" }: throw new NotImplementedException();
                        case StartTag { name: "dd" or "dt" }: throw new NotImplementedException();
                        case StartTag { name: "plaintext" }: throw new NotImplementedException();
                        case StartTag { name: "button" }: throw new NotImplementedException();
                        case EndTag {
                            name: "address" or "article" or "aside" or "blockquote" or "button" or "center" or
                            "details" or "dialog" or "dir" or "div" or "dl" or "fieldset" or "figcaption" or "figure" or
                            "footer" or "header" or "hgroup" or "listing" or "main" or "menu" or "nav" or "ol" or "pre" or
                            "search" or "section" or "summary" or "ul"
                        }:
                            throw new NotImplementedException();
                        case EndTag { name: "form" }: throw new NotImplementedException();
                        case EndTag { name: "p" }: throw new NotImplementedException();
                        case EndTag { name: "li" }: throw new NotImplementedException();
                        case EndTag { name: "dd" or "dt" }: throw new NotImplementedException();
                        case EndTag { name: "h1" or "h2" or "h3" or "h4" or "h5" or "h6" }: throw new NotImplementedException();
                        case EndTag { name: "sarcasm" }: throw new NotImplementedException();
                        case StartTag { name: "a" }: throw new NotImplementedException();
                        case StartTag { name: "b" or "big" or "code" or "em" or "font" or "i" or "s" or "small" or "strike" or "strong" or "tt" or "u" }:
                            throw new NotImplementedException();
                        case StartTag { name: "nobr" }: throw new NotImplementedException();
                        case EndTag { name: "a" or "b" or "big" or "code" or "em" or "font" or "i" or "s" or "small" or "strike" or "strong" or "tt" or "u" }:
                            throw new NotImplementedException();
                        case StartTag { name: "applet" or "marquee" or "object" }: throw new NotImplementedException();
                        case EndTag { name: "applet" or "marquee" or "object" }: throw new NotImplementedException();
                        case StartTag { name: "table" }: throw new NotImplementedException();
                        case EndTag { name: "br" }: throw new NotImplementedException();
                        case StartTag { name: "area" or "br" or "embed" or "img" or "keygen" or "wbr" }: throw new NotImplementedException();
                        case StartTag { name: "input" }: throw new NotImplementedException();
                        case StartTag { name: "param" or "source" or "track" }: throw new NotImplementedException();
                        case StartTag { name: "hr" }: throw new NotImplementedException();
                        case StartTag { name: "image" }: throw new NotImplementedException();
                        case StartTag { name: "textarea" }: throw new NotImplementedException();
                        case StartTag { name: "xmp" }: throw new NotImplementedException();
                        case StartTag { name: "iframe" }: throw new NotImplementedException();
                        case StartTag { name: "noembed" }: throw new NotImplementedException();
                        case StartTag { name: "noscript" } when scriptingFlag: throw new NotImplementedException();
                        case StartTag { name: "select" }: throw new NotImplementedException();
                        case StartTag { name: "optgroup" or "option" }: throw new NotImplementedException();
                        case StartTag { name: "rb" or "rtc" }: throw new NotImplementedException();
                        case StartTag { name: "rp" or "rt" }: throw new NotImplementedException();
                        case StartTag { name: "math" }: throw new NotImplementedException();
                        case StartTag { name: "svg" }: throw new NotImplementedException();
                        case StartTag { name: "caption" or "col" or "colgroup" or "frame" or "head" or "tbody" or "td" or "tfoot" or "th" or "thead" or "tr" }: throw new NotImplementedException();
                        case StartTag: throw new NotImplementedException();
                        case EndTag: throw new NotImplementedException();
                    }

                    break;
                // 13.2.6.4.19 The "after body" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-afterbody
                case InsertionMode.AfterBody:
                    switch (token) {
                        case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                            // todo
                            break;
                        case Comment: throw new NotImplementedException();
                        case DOCTYPE: throw new NotImplementedException();
                        case StartTag { name: "html" }: throw new NotImplementedException();
                        case EndTag { name: "html" }:
                            // If the parser was created as part of the HTML fragment parsing algorithm, this is a parse error; ignore the token. (fragment case)
                            // todo
                            // Otherwise, switch the insertion mode to "after after body".
                            insertionMode = InsertionMode.AfterAfterBody;
                            break;
                        case EndOfFile: throw new NotImplementedException();
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                // 13.2.6.4.22 The "after after body" insertion mode
                // https://html.spec.whatwg.org/multipage/parsing.html#the-after-after-body-insertion-mode
                case InsertionMode.AfterAfterBody:
                    switch (token) {
                        case Comment: throw new NotImplementedException();
                        case DOCTYPE:
                        case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                        case StartTag { name: "html" }:
                            throw new NotImplementedException();
                        case EndOfFile:
                            return; // stop parsing
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                default:
                    throw new NotImplementedException();

            }



        }


    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-a-character
    private void InsertACharacter(Token token) {
        // Let the adjusted insertion location be the appropriate place for inserting a node.
        // todo
        // If the adjusted insertion location is in a Document node, then return.
        if (currentNode is Document) {
            return;
        }
        // If there is a Text node immediately before the adjusted insertion location, then append data to that Text node's data.
        if (currentNode.childNodes.Count > 0 && currentNode.childNodes[^1] is Text) {
            (currentNode.childNodes[^1] as Text).data += (token as Character).data;
        } else {
            // Otherwise, create a new Text node whose data is data and whose node document is the same as that of the element in which the adjusted insertion location finds itself, and insert the newly created node at the adjusted insertion location.                
            currentNode.childNodes.Add(new Text(document, (token as Character).data.ToString()));
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-a-foreign-element
    private Element InsertAForeignElement(Tag token, string @namespace, bool onlyAddToELementStack) {
        // Let the adjustedInsertionLocation be the appropriate place for inserting a node.
        // todo
        // Let element be the result of creating an element for the token given token, namespace, and the element in which the adjustedInsertionLocation finds itself.
        var element = CreateAnElementForTheToken(token, @namespace, currentNode);
        // If onlyAddToElementStack is false, then run insert an element at the adjusted insertion location with element.
        if (!onlyAddToELementStack) {
            InsertAnElementAtTheAdjustedInsertionLocation(element);
        }
        // Push element onto the stack of open elements so that it is the new current node.
        stackOfOpenElements.Add(element);
        //Return element.
        return element;
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-an-element-at-the-adjusted-insertion-location
    private void InsertAnElementAtTheAdjustedInsertionLocation(Element element) {
        // Let the adjusted insertion location be the appropriate place for inserting a node.

        // If it is not possible to insert element at the adjusted insertion location, abort these steps.

        // If the parser was not created as part of the HTML fragment parsing algorithm, then push a new element queue onto element's relevant agent's custom element reactions stack.

        // Insert element at the adjusted insertion location.
        currentNode.childNodes.Add(element);
        // If the parser was not created as part of the HTML fragment parsing algorithm, then pop the element queue from element's relevant agent's custom element reactions stack, and invoke custom element reactions in that queue.
    }
    // https://html.spec.whatwg.org/multipage/parsing.html#insert-an-html-element
    private Element InsertAnHTMLElement(Token token) {
        // To insert an HTML element given a token token: insert a foreign element given token, the HTML namespace, and false.
        return InsertAForeignElement((Tag)token, Namespaces.HTML, false);
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#create-an-element-for-the-token
    private Element CreateAnElementForTheToken(Tag token, string @namespace, Node intendedParent) {
        // 1. If the active speculative HTML parser is not null, then return the result of creating a speculative mock element given namespace, token's tag name, and token's attributes.

        // 2. Otherwise, optionally create a speculative mock element given namespace, token's tag name, and token's attributes.
        // Note: The result is not used. This step allows for a speculative fetch to be initiated from non-speculative parsing. The fetch is still speculative at this point, because, for example, by the time the element is inserted, intended parent might have been removed from the document.

        // 3. Let document be intendedParent's node document.
        // todo should we get the ownerDocument or the document direct?
        var document = intendedParent.ownerDocument;
        // 4. Let localName be token's tag name.
        var localName = token.name;
        // 5. Let is be the value of the "is" attribute in token, if such an attribute exists; otherwise null.
        string? isValue = null; // todo

        // 6. Let registry be the result of looking up a custom element registry given intendedParent.

        // 7. Let definition be the result of looking up a custom element definition given registry, namespace, localName, and is.

        // 8. Let willExecuteScript be true if definition is non-null and the parser was not created as part of the HTML fragment parsing algorithm; otherwise false.
        var willExecuteScript = false; // todo
        // 9. If willExecuteScript is true:

        // 1. Increment document's throw-on-dynamic-markup-insertion counter.
        // 2. If the JavaScript execution context stack is empty, then perform a microtask checkpoint.
        // 3. Push a new element queue onto document's relevant agent's custom element reactions stack.

        // 10. Let element be the result of creating an element given document, localName, namespace, null, is, willExecuteScript, and registry.
        // Note: This will cause custom element constructors to run, if willExecuteScript is true. However, since we incremented the throw-on-dynamic-markup-insertion counter,
        //       this cannot cause new characters to be inserted into the tokenizer, or the document to be blown away.
        var element = CreateAnElement(
            document,
            localName,
            @namespace,
            null,
            isValue,
            willExecuteScript
        // registry,
        );

        // 11. Append each attribute in the given token to element.
        // Note: This can enqueue a custom element callback reaction for the attributeChangedCallback, which might run immediately (in the next step).
        // Note: Even though the is attribute governs the creation of a customized built-in element, it is not present during the execution of the relevant custom element constructor; it is appended in this step, along with all other attributes.

        // 12. If willExecuteScript is true:

        // 1. Let queue be the result of popping from document's relevant agent's custom element reactions stack. (This will be the same element queue as was pushed above.)
        // 2. Invoke custom element reactions in queue.
        // 3. Decrement document's throw-on-dynamic-markup-insertion counter.

        // 13. If element has an xmlns attribute in the XMLNS namespace whose value is not exactly the same as the element's namespace, that is a parse error. Similarly, if element has an xmlns:xlink attribute in the XMLNS namespace whose value is not the XLink Namespace, that is a parse error.

        // 14. If element is a resettable element and not a form-associated custom element, then invoke its reset algorithm. (This initializes the element's value and checkedness based on the element's attributes.)

        // 15. If element is a form-associated element and not a form-associated custom element, the form element pointer is not null, there is no template element on the stack of open elements, element is either not listed or doesn't have a form attribute, and the intendedParent is in the same tree as the element pointed to by the form element pointer, then associate element with the form element pointed to by the form element pointer and set element's parser inserted flag.

        // 16. Return element.
        return element;
    }

    //https://dom.spec.whatwg.org/#concept-create-element
    private Element CreateAnElement(Document document, string localName, string? @namespace, string? prefix = null, string? isValue = null, bool synchronousCustomElements = false) {
        if (debugPrint) Debug.WriteLine($"createELement {localName}");
        return new Element(document, localName);
    }

    public void PrintDebugDocumentTree() {
        PrintDebugDocumentTree(document);
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


public static class ListExtensions {
    public static void Pop<T>(this List<T> list) {
        if (list == null) {
            throw new ArgumentNullException(nameof(list));
        }
        if (list.Count == 0) {
            throw new InvalidOperationException("The list is empty. Cannot pop from an empty List.");
        }
        list.RemoveAt(list.Count - 1);
    }
}