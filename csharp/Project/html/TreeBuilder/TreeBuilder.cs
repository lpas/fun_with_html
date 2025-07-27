using System.Diagnostics;
using FunWithHtml.html.Tokenizer;

namespace FunWithHtml.html.TreeBuilder;

enum InsertionMode {
    Initial,
    BeforeHtml,
    BeforeHead,
    InHead,
    InHeadNoscript,
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
    public Node? parent = null;
}
public class Document(): Node(null) {

    public enum QuirksMode {
        noQuirks,
        LimitedQuirks,
        Quirks,
    }

    public QuirksMode mode = QuirksMode.noQuirks;
    public override string ToString() {
        return "#document";
    }
}

public class DocumentType(Document document, string name, string publicId = "", string systemId = ""): Node(document) {
    public string name { get; } = name;
    public string publicId { get; } = publicId;
    public string systemId { get; } = systemId;

    public override string ToString() {
        return $"<!DOCTYPE {name}{(publicId != "" || systemId != "" ? $" \"{publicId}\" \"{systemId}\"" : "")}>";
    }
}


public class CharacterData(Document document, string data): Node(document) {
    public string data { get; } = data;

}
public class Comment(Document document, string data): CharacterData(document, data) {
    public override string ToString() {
        return $"<!-- {data} -->";
    }
}

public class Element(Document document, string localName): Node(document) {
    public string? @namespace;
    public string? namespacePrefix;
    public string localName = localName;
    public Dictionary<string, string> attributes = [];

    public override string ToString() {
        return $"<{localName}>";
    }
}

public class NodeAttr(string name, string value): Node(null) {
    public string name = name;
    public string value = value;

    public override string ToString() {
        return $"{name}=\"{value}\"";
    }

}

public class Text(Document document, string data): Node(document) {
    public string data = data;

    public override string ToString() {
        return $"\"{data}\"";
    }
}

public struct ParseError {
    public int line;
    public int col;
    public string error;

    public override string ToString() {
        return $"({line},{col}): {error}";
    }
}

public class TreeBuilder(Tokenizer.Tokenizer tokenizer, bool debugPrint = false) {

    private bool debugPrint = debugPrint;

    public Document Document { get => document; }

    private Document document = new();
    private InsertionMode insertionMode = InsertionMode.Initial;
    private InsertionMode originalInsertionMode = InsertionMode.Initial;
    private List<Element> stackOfOpenElements = [];
    private Element currentNode { get => stackOfOpenElements.Peek(); }
    public List<ParseError> Errors { get => [.. parseErrors]; }

    private Element? headElementPointer = null;
    private Element? formElementPointer = null;

    internal bool scriptingFlag = false;

    private List<Element?> ListOfActiveFormattingElements = []; // null is a marker

    private bool fosterParenting = false;

    private HashSet<ParseError> parseErrors = [];

    private Tokenizer.Tokenizer tokenizer = tokenizer;
    private bool framesetOk = false;

    private List<Character> pendingTableCharacterTokens = [];

    private List<InsertionMode> stackOfTemplateInsertionModes = [];

    private bool finishedParsing = false;
    private Token token;
    private bool shouldReprocessToken = false;

    private void ReprocessTheToken() {
        shouldReprocessToken = true;
    }

    private void MoveToNextToken() {
        if (shouldReprocessToken) {
            shouldReprocessToken = false;
        } else {
            token = tokenizer.NextToken();
        }
    }


    public void build() {
        while (true) {
            if (finishedParsing) break;
            MoveToNextToken();
            if (debugPrint) Debug.WriteLine(token);


            switch (insertionMode) {
                case InsertionMode.Initial:
                    InsertionModeInitial();
                    break;
                case InsertionMode.BeforeHtml:
                    InsertionModeBeforeHtml();
                    break;
                case InsertionMode.BeforeHead:
                    InsertionModeBeforeHead();
                    break;
                case InsertionMode.InHead:
                    InsertionModeInHead();
                    break;
                case InsertionMode.InHeadNoscript:
                    InsertionModeInHeadNoscript();
                    break;
                case InsertionMode.AfterHead:
                    InsertionModeAfterHead();
                    break;
                case InsertionMode.InBody:
                    InsertionModeInBody();
                    break;
                case InsertionMode.Text:
                    InsertionModeText();
                    break;
                case InsertionMode.InTable:
                    InsertionModeInTable();
                    break;
                case InsertionMode.InTableText:
                    InSertionModeInTableText();
                    break;
                case InsertionMode.InCaption:
                    InsertionModeInCaption();
                    break;
                case InsertionMode.InColumnGroup:
                    InsertionModeInColumnGroup();
                    break;
                case InsertionMode.InTableBody:
                    InsertionModeInTableBody();
                    break;
                case InsertionMode.InRow:
                    InsertionModeInRow();
                    break;
                case InsertionMode.InCell:
                    InsertionModeInCell();
                    break;
                case InsertionMode.InSelect:
                    InsertionModeInSelect();
                    break;
                case InsertionMode.InSelectInTable:
                    InsertionModeInSelectInTable();
                    break;
                case InsertionMode.InTemplate:
                    InsertionModeInTemplate();
                    break;
                case InsertionMode.AfterBody:
                    InsertionModeAfterBody();
                    break;
                case InsertionMode.InFrameset:
                    InsertionModeInFrameset();
                    break;
                case InsertionMode.AfterFrameset:
                    InsertionModeAfterFrameset();
                    break;
                case InsertionMode.AfterAfterBody:
                    InsertionModeAfterAfterBody();
                    break;
                case InsertionMode.AfterAfterFrameset:
                    InsertionModeAfterAfterFrameset();
                    break;

                default:
                    throw new NotImplementedException($"insertionMode: {insertionMode}");

            }



        }


    }

    // 13.2.6.2 Parsing elements that contain only text
    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-elements-that-contain-only-text
    private void GenericRawTextElementParsingAlgorithm(StartTag tag) {
        GenericTextElementParsingAlgorithm(tag, State.RAWTEXTState);
    }
    private void GenericRCDATAElementParsingAlgorithm(StartTag tag) {
        GenericTextElementParsingAlgorithm(tag, State.RCDATAState);
    }

    private void GenericTextElementParsingAlgorithm(StartTag tag, Tokenizer.State state) {
        InsertAnHTMLElement(tag);
        tokenizer.SwitchState(state);
        originalInsertionMode = insertionMode;
        insertionMode = InsertionMode.Text;
    }


    // 13.2.4.3 The list of active formatting elements
    // https://html.spec.whatwg.org/multipage/parsing.html#clear-the-list-of-active-formatting-elements-up-to-the-last-marker
    private void ClearTheListOfACtiveFormattingElementsUpToTheLastMarker() {
        // 1. Let entry be the last (most recently added) entry in the list of active formatting elements.
        while (true) {
            // 2. Remove entry from the list of active formatting elements.
            var entry = ListOfActiveFormattingElements.Pop();
            // 3. If entry was a marker, then stop the algorithm at this point. The list has been cleared up to the last marker.
            if (entry is null) {
                break;
            }
            // 4. Go to step 1.
        }
    }

    private void AddParseError(string error) {
        parseErrors.Add(new ParseError { line = tokenizer.Line, col = tokenizer.Col, error = error });
    }

    private void InsertAComment(Tokenizer.Comment comment, (Node elem, int childPos)? position = null) {
        // Let data be the data given in the comment token being processed.
        var data = comment.data;
        // If position was specified, then let the adjusted insertion location be position. Otherwise, let adjusted insertion location be the appropriate place for inserting a node.
        var adjustedInsertionLocation = position ?? AppropriatePlaceForInsertingANode();
        // Create a Comment node whose data attribute is set to data and whose node document is the same as that of the node in which the adjusted insertion location finds itself.
        var element = new Comment(adjustedInsertionLocation.elem.ownerDocument, data);
        // Insert the newly created node at the adjusted insertion location.
        InsertNode(adjustedInsertionLocation, element);
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#the-initial-insertion-mode
    private void InsertionModeInitial() {
        switch (token) {
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                break; // ignore the token
            case Tokenizer.Comment comment:
                // Insert a comment as the last child of the Document object.
                InsertAComment(comment, (document, document.childNodes.Count));
                break;
            case DOCTYPE doctype:
                // If the DOCTYPE token's name is not "html", or the token's public identifier is not missing, or the token's system identifier is neither missing nor "about:legacy-compat",
                // then there is a parse error.                        
                if (doctype.name is not "html" || doctype.publicId is not null || doctype.systemId is not null or "about:legacy-compat") {
                    AddParseError("InsertionMode.Initial - doctype not html");
                }
                // Append a DocumentType node to the Document node, with its name set to the name given in the DOCTYPE token, or the empty string if the name was missing; 
                // its public ID set to the public identifier given in the DOCTYPE token, or the empty string if the public identifier was missing;
                // and its system ID set to the system identifier given in the DOCTYPE token, or the empty string if the system identifier was missing.
                AppendNode(document, new DocumentType(document, doctype.name ?? "", doctype.publicId ?? "", doctype.systemId ?? ""));
                // todo quirks mode
                insertionMode = InsertionMode.BeforeHtml;
                break;
            default:
                // todo If the document is not an iframe srcdoc document, then this is a parse error; if the parser cannot change the mode flag is false, set the Document to quirks mode.
                AddParseError("expected-doctype");
                // In any case, switch the insertion mode to "before html",                            
                insertionMode = InsertionMode.BeforeHtml;
                // then reprocess the token.
                ReprocessTheToken();
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#the-before-html-insertion-mode
    private void InsertionModeBeforeHtml() {
        switch (token) {
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("before-html-unexpected-doctype");
                break;
            case Tokenizer.Comment comment:
                // Insert a comment as the last child of the Document object.
                InsertAComment(comment, (document, document.childNodes.Count));
                break;
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                break; // ignore the token
            case StartTag { name: "html" }: {
                    // Create an element for the token in the HTML namespace, with the Document as the intended parent.
                    var element = CreateAnElementForAToken((Tag)token, Namespaces.HTML, document);
                    // Append it to the Document object.
                    AppendNode(document, element);
                    // Put this element in the stack of open elements.
                    stackOfOpenElements.Put(element);
                    // Switch the insertion mode to "before head".
                    insertionMode = InsertionMode.BeforeHead;
                    break;
                }
            case EndTag { name: "head" or "body" or "html" or "br" }:
                // Act as described in the "anything else" entry below.
                goto default;
            case EndTag:
                // Parse error. Ignore the token.
                AddParseError("unexpected-end-tag-before-html");
                break;
            default: {
                    // Create an html element whose node document is the Document object. Append it to the Document object. Put this element in the stack of open elements.
                    var element = CreateAnElement(document, "html", Namespaces.HTML);
                    AppendNode(document, element);
                    stackOfOpenElements.Put(element);
                    // Switch the insertion mode to "before head", then reprocess the token.                            
                    insertionMode = InsertionMode.BeforeHead;
                    ReprocessTheToken();
                    break;
                }

        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#the-before-head-insertion-mode
    private void InsertionModeBeforeHead() {
        switch (token) {
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                break; // ignore the token
            case Tokenizer.Comment comment:
                // Insert a comment.
                InsertAComment(comment);
                break;
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("before-head-unexpected-doctype-ignored");
                break;
            case StartTag { name: "html" }: throw new NotImplementedException();
            case StartTag { name: "head" } tagToken: {
                    // Insert an HTML element for the token.
                    var element = InsertAnHTMLElement(tagToken);
                    //Set the head element pointer to the newly created head element.
                    headElementPointer = element;
                    //Switch the insertion mode to "in head".
                    insertionMode = InsertionMode.InHead;
                }
                break;
            case EndTag { name: "head" or "body" or "html" or "br" }:
                // Act as described in the "anything else" entry below.
                goto default;
            case EndTag:
                // Parse error. Ignore the token.
                AddParseError("before-head-unexpected-end-tag");
                break;
            default: {
                    // Insert an HTML element for a "head" start tag token with no attributes.
                    var element = InsertAnHTMLElement(new StartTag("head"));
                    // Set the head element pointer to the newly created head element.
                    headElementPointer = element;
                    // Switch the insertion mode to "in head".
                    insertionMode = InsertionMode.InHead;
                    // Reprocess the current token.
                    ReprocessTheToken();
                    break;
                }
        }
    }


    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inhead
    private void InsertionModeInHead() {
        switch (token) {
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' } cToken:
                // Insert the character.
                InsertACharacter(cToken);
                break;
            case Tokenizer.Comment comment:
                // Insert a comment.
                InsertAComment(comment);
                break;
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("in-head-unexpected-doctype-ignored");
                break;
            case StartTag { name: "html" }:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case StartTag { name: "base" or "basefont" or "bgsound" or "link" } tagToken:
                // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
                InsertAnHTMLElement(tagToken);
                stackOfOpenElements.Pop();
                // Acknowledge the token's self-closing flag, if it is set.
                // todo
                break;
            case StartTag { name: "meta" } tagToken:
                // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
                InsertAnHTMLElement(tagToken);
                stackOfOpenElements.Pop();
                // Acknowledge the token's self-closing flag, if it is set.

                // If the active speculative HTML parser is null, then:

                // 1. If the element has a charset attribute, and getting an encoding from its value results in an encoding, and the confidence is currently tentative, then change the encoding to the resulting encoding.

                // 2. Otherwise, if the element has an http-equiv attribute whose value is an ASCII case-insensitive match for the string "Content-Type", and the element has a content attribute, and applying the algorithm for extracting a character encoding from a meta element to that attribute's value returns an encoding, and the confidence is currently tentative, then change the encoding to the extracted encoding.

                break;
            case StartTag { name: "title" } startTag:
                // Follow the generic RCDATA element parsing algorithm.
                GenericRCDATAElementParsingAlgorithm(startTag);
                break;
            case StartTag { name: "noscript" } startTag when scriptingFlag:
                GenericRawTextElementParsingAlgorithm(startTag);
                break;
            case StartTag { name: "noframes" or "style" } startTag:
                GenericRawTextElementParsingAlgorithm(startTag);
                break;
            case StartTag { name: "noscript" } tagToken when !scriptingFlag:
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                // Switch the insertion mode to "in head noscript".
                insertionMode = InsertionMode.InHeadNoscript;
                break;
            case StartTag { name: "script" }: {
                    // 1. Let the adjusted insertion location be the appropriate place for inserting a node.
                    var adjustedInsertionLocation = AppropriatePlaceForInsertingANode();
                    // 2. Create an element for the token in the HTML namespace, with the intended parent being the element in which the adjusted insertion location finds itself.
                    var element = CreateAnElement(document, "script", Namespaces.HTML);
                    // 3. Set the element's parser document to the Document, and set the element's force async to false.
                    // todo
                    // NOTE: This ensures that, if the script is external, any document.write() calls in the script will execute in-line, instead of blowing the document away, as would happen in most other cases. It also prevents the script from executing until the end tag is seen.
                    // If the parser was created as part of the HTML fragment parsing algorithm, then set the script element's already started to true. (fragment case)
                    // todo
                    // If the parser was invoked via the document.write() or document.writeln() methods, then optionally set the script element's already started to true. (For example, the user agent might use this clause to prevent execution of cross-origin scripts inserted via document.write() under slow network conditions, or when the page has already taken a long time to load.)
                    // todo
                    // Insert the newly created element at the adjusted insertion location.
                    InsertNode(adjustedInsertionLocation, element);
                    // Push the element onto the stack of open elements so that it is the new current node.
                    stackOfOpenElements.Push(element);
                    // Switch the tokenizer to the script data state.
                    tokenizer.SwitchState(State.ScriptDataState);
                    // Set the original insertion mode to the current insertion mode.
                    originalInsertionMode = insertionMode;
                    // Switch the insertion mode to "text".
                    insertionMode = InsertionMode.Text;
                    break;
                }
            case EndTag { name: "head" }:
                // Pop the current node (which will be the head element) off the stack of open elements.
                stackOfOpenElements.Pop();
                // Switch the insertion mode to "after head".
                insertionMode = InsertionMode.AfterHead;
                break;
            case EndTag { name: "body" or "html" or "br" }:
                // Act as described in the "anything else" entry below.
                goto default;
            case StartTag { name: "template" } startTag: {
                    // Run these steps:
                    // 1. Let templateStartTag be the start tag.
                    var templateStartTag = startTag;
                    // 2. Insert a marker at the end of the list of active formatting elements.
                    ListOfActiveFormattingElements.Add(null);
                    // 3. Set the frameset-ok flag to "not ok".
                    framesetOk = false;
                    // 4. Switch the insertion mode to "in template".
                    insertionMode = InsertionMode.InTemplate;
                    // 5. Push "in template" onto the stack of template insertion modes so that it is the new current template insertion mode.
                    stackOfTemplateInsertionModes.Push(InsertionMode.InTemplate);
                    // 6. Let the adjustedInsertionLocation be the appropriate place for inserting a node.
                    var adjustedInsertionLocation = AppropriatePlaceForInsertingANode();
                    // 7. Let intendedParent be the element in which the adjustedInsertionLocation finds itself.
                    var intendedParent = adjustedInsertionLocation.elem;
                    // 8. Let document be intendedParent's node document.
                    var document = intendedParent.ownerDocument;
                    // 9. If any of the following are false:
                    // * templateStartTag's shadowrootmode is not in the None state;
                    // * document's allow declarative shadow roots is true; or
                    // * the adjusted current node is not the topmost element in the stack of open elements,
                    // todo 
                    if (true) {
                        // then insert an HTML element for the token.
                        InsertAnHTMLElement(startTag);
                    } else {
                        // 10. Otherwise:

                        // 1. Let declarativeShadowHostElement be adjusted current node.

                        // 2. Let template be the result of insert a foreign element for templateStartTag, with HTML namespace and true.

                        // 3. Let mode be templateStartTag's shadowrootmode attribute's value.

                        // 4. Let clonable be true if templateStartTag has a shadowrootclonable attribute; otherwise false.

                        // 5. Let serializable be true if templateStartTag has a shadowrootserializable attribute; otherwise false.

                        // 6. Let delegatesFocus be true if templateStartTag has a shadowrootdelegatesfocus attribute; otherwise false.

                        // 7. If declarativeShadowHostElement is a shadow host, then insert an element at the adjusted insertion location with template.

                        // 8. Otherwise:

                        // 1. Let registry be declarativeShadowHostElement's custom element registry.

                        // 2. If templateStartTag has a shadowrootcustomelementregistry attribute, then set registry to null.

                        // 3. Attach a shadow root with declarativeShadowHostElement, mode, clonable, serializable, delegatesFocus, "named", and registry.

                        // If an exception is thrown, then catch it and:

                        // 1. Insert an element at the adjusted insertion location with template.

                        // 2. The user agent may report an error to the developer console.

                        // 3. Return.

                        // 4. Let shadow be declarativeShadowHostElement's shadow root.

                        // 5. Set shadow's declarative to true.

                        // 6. Set template's template contents property to shadow.

                        // 7. Set shadow's available to element internals to true.

                        // 8. If templateStartTag has a shadowrootcustomelementregistry attribute, then set shadow's keep custom element registry null to true.
                    }
                    break;
                }
            case EndTag { name: "template" }:
                // If there is no template element on the stack of open elements, then this is a parse error; ignore the token.
                if (!stackOfOpenElements.Any(item => item.localName == "template")) {
                    AddParseError("in-head-unexpected-end-template-tag-ignored");
                } else {
                    // Otherwise, run these steps:
                    // 1. Generate all implied end tags thoroughly.
                    GenerateImpliedEndTags();
                    // 2. If the current node is not a template element, then this is a parse error.
                    if (currentNode is not Element { localName: "template" }) {
                        AddParseError("in-head-expected-template-as-current-node");
                    }
                    // 3. Pop elements from the stack of open elements until a template element has been popped from the stack.
                    while (stackOfOpenElements.Pop() is not Element { localName: "template" }) { }
                    // 4. Clear the list of active formatting elements up to the last marker.
                    ClearTheListOfACtiveFormattingElementsUpToTheLastMarker();
                    // 5. Pop the current template insertion mode off the stack of template insertion modes.
                    stackOfTemplateInsertionModes.Pop();
                    // 6. Reset the insertion mode appropriately.            
                    ResetTheInsertionModeAppropriately();
                }
                break;
            case StartTag { name: "head" }:
            case EndTag:
                AddParseError("InsertionMode.InHead: EndTag");
                break; // ignore the token
            default:
                //Pop the current node (which will be the head element) off the stack of open elements.
                stackOfOpenElements.Pop();
                //Switch the insertion mode to "after head".
                insertionMode = InsertionMode.AfterHead;
                //Reprocess the token.
                ReprocessTheToken();
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inheadnoscript
    private void InsertionModeInHeadNoscript() {
        switch (token) {
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("in-head-noscript-unexpected-doctype-ignored");
                break;
            case StartTag { name: "html" }:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case EndTag { name: "noscript" }:
                // Pop the current node (which will be a noscript element) from the stack of open elements; the new current node will be a head element.
                stackOfOpenElements.Pop();
                // Switch the insertion mode to "in head".
                insertionMode = InsertionMode.InHead;
                break;
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
            case Tokenizer.Comment:
            case StartTag { name: "basefont" or "bgsound" or "link" or "meta" or "noframes" or "style" }:
                InsertionModeInHead();
                break;
            case EndTag { name: "br" }:
                // Act as described in the "anything else" entry below.
                goto default;
            case StartTag { name: "head" or "noscript" }:
            case EndTag:
                // Parse error. Ignore the token.
                AddParseError("in-head-noscript-unexpected-token-ignored");
                break;
            default:
                // Parse error.
                AddParseError("in-head-no-script-unexpected-token");
                // Pop the current node (which will be a noscript element) from the stack of open elements; the new current node will be a head element.
                stackOfOpenElements.Pop();
                // Switch the insertion mode to "in head".
                insertionMode = InsertionMode.InHead;
                // Reprocess the token.
                ReprocessTheToken();
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#the-after-head-insertion-mode
    private void InsertionModeAfterHead() {
        switch (token) {
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' } cToken:
                // Insert the character.
                InsertACharacter(cToken);
                break;
            case Tokenizer.Comment comment:
                // Insert a comment.
                InsertAComment(comment);
                break;
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("after-head-unexpected-doctype-ignored");
                break;
            case StartTag { name: "html" }:
                //Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case StartTag { name: "body" } tagToken:
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                // Set the frameset-ok flag to "not ok".
                framesetOk = false;
                // Switch the insertion mode to "in body".
                insertionMode = InsertionMode.InBody;
                break;
            case StartTag { name: "frameset" } tagToken:
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                // Switch the insertion mode to "in frameset".
                insertionMode = InsertionMode.InFrameset;
                break;
            case StartTag { name: "base" or "basefont" or "bgsound" or "link" or "meta" or "noframes" or "script" or "style" or "template" or "title" }:
                // Parse error.
                AddParseError("AfterHead -  parse error");
                // Push the node pointed to by the head element pointer onto the stack of open elements.
                stackOfOpenElements.Add(headElementPointer!);
                // Process the token using the rules for the "in head" insertion mode.
                InsertionModeInHead();
                // Remove the node pointed to by the head element pointer from the stack of open elements. (It might not be the current node at this point.)
                for (var i = stackOfOpenElements.Count - 1; i > 0; i--) {
                    if (stackOfOpenElements[i] == headElementPointer) {
                        stackOfOpenElements.RemoveAt(i);
                    }
                }
                // Note: The head element pointer cannot be null at this point.
                break;
            case EndTag { name: "template" }: throw new NotImplementedException();
            case EndTag { name: "body" or "html" or "br" }:
                // Act as described in the "anything else" entry below.
                goto default;
            case StartTag { name: "head" }:
            case EndTag:
                // Parse error. Ignore the token.
                AddParseError("in-head-unexpected-tag");
                break;
            default:
                // Insert an HTML element for a "body" start tag token with no attributes.
                InsertAnHTMLElement(new StartTag("body"));
                // Switch the insertion mode to "in body".
                insertionMode = InsertionMode.InBody;
                // Reprocess the current token.
                ReprocessTheToken();
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inbody
    private void InsertionModeInBody() {
        switch (token) {
            case Character { data: '\0' }:
                // Parse error. Ignore the token.
                AddParseError("in-body-null-character-ignored");
                break;
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' } cToken:
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Insert the token's character.
                InsertACharacter(cToken);
                break;
            case Character cToken:
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Insert the token's character.
                InsertACharacter(cToken);
                // Set the frameset-ok flag to "not ok".
                framesetOk = false;
                break;
            case Tokenizer.Comment comment:
                InsertAComment(comment);
                break;
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("in-body-unexpected-doctype-ignored");
                break;
            case StartTag { name: "html" } tagToken:

                // Parse error.
                AddParseError("in-body-unexpected-html");
                // If there is a template element on the stack of open elements, then ignore the token.
                if (stackOfOpenElements.Any(item => item.localName == "template")) {
                    // ignore 
                } else {
                    // Otherwise, for each attribute on the token, check to see if the attribute is already present on the top element of the stack of open elements. If it is not, add the attribute and its corresponding value to that element.    
                    foreach (var attr in tagToken.Attributes) {
                        stackOfOpenElements[0].attributes.TryAdd(attr.name, attr.value);
                    }
                }
                break;
            case StartTag { name: "base" or "basefont" or "bgsound" or "link" or "meta" or "noframes" or "script" or "style" or "template" or "title" }:
            case EndTag { name: "template" }:
                InsertionModeInHead();
                break;
            case StartTag { name: "body" } tagToken:
                // Parse error.
                AddParseError("body-in-body");
                // If the stack of open elements has only one node on it, or if the second element on the stack of open elements is not a body element,
                // or if there is a template element on the stack of open elements, then ignore the token. (fragment case or there is a template element on the stack)
                if (stackOfOpenElements.Count == 1 || stackOfOpenElements[1].localName != "body" || stackOfOpenElements.Any((item) => item.localName == "template")) {
                    // ignore token
                } else {
                    // Otherwise, set the frameset-ok flag to "not ok"; then, for each attribute on the token, check to see if the attribute is already present on the body element (the second element) 
                    // on the stack of open elements, and if it is not, add the attribute and its corresponding value to that element.
                    framesetOk = false;
                    foreach (var attr in tagToken.Attributes) {
                        stackOfOpenElements[1].attributes.TryAdd(attr.name, attr.value);
                    }
                }

                break;
            case StartTag { name: "frameset" }:
                // Parse error.
                AddParseError("in-body-unexpected-frameset-tag");
                // If the stack of open elements has only one node on it, or if the second element on the stack of open elements is not a body element, then ignore the token. (fragment case or there is a template element on the stack)
                if (stackOfOpenElements.Count == 1 || stackOfOpenElements[1] is not not Element { localName: "body" }) {
                    break;
                }
                // If the frameset-ok flag is set to "not ok", ignore the token.
                if (!framesetOk) {
                    break;
                }
                // Otherwise, run the following steps:
                // 1. Remove the second element on the stack of open elements from its parent node, if it has one.
                throw new NotImplementedException();
                // 2. Pop all the nodes from the bottom of the stack of open elements, from the current node up to, but not including, the root html element.

                // 3. Insert an HTML element for the token.

                // 4. Switch the insertion mode to "in frameset".

                break;
            case EndOfFile: {
                    // If the stack of template insertion modes is not empty, then process the token using the rules for the "in template" insertion mode.
                    // todo
                    // Otherwise, follow these steps:
                    // todo
                    // If there is a node in the stack of open elements that is not either a dd element, a dt element, an li element, an optgroup element, an option element, a p element, an rb element, an rp element, an rt element, an rtc element, a tbody element, a td element, a tfoot element, a th element, a thead element, a tr element, the body element, or the html element, then this is a parse error.
                    if (stackOfOpenElements.Any((elem) => elem is not Element { localName: "dd" or "dt" or "li" or "optgroup" or "option" or "p" or "rb" or "rp" or "rt" or "rtc" or "tbody" or "td" or "tfoot" or "th" or "thead" or "tr" or "body" or "html" })) {
                        AddParseError("expected-closing-tag-but-got-eof");
                    }
                    // Stop parsing.
                    StopParsing();
                    break;
                }
            case EndTag { name: "body" }:
                // If the stack of open elements does not have a body element in scope, this is a parse error; ignore the token.
                if (!HasAElementInScope("body")) {
                    AddParseError("IN BODY NOT BODY IN SCOPE");
                    break;
                }
                // Otherwise, if there is a node in the stack of open elements that is not either a dd element, a dt element, an li element, an optgroup element, an option element, a p element, an rb element, an rp element, an rt element, an rtc element, a tbody element, a td element, a tfoot element, a th element, a thead element, a tr element, the body element, or the html element, then this is a parse error.
                if (stackOfOpenElements.Any((elem) => elem is not Element { localName: "dd" or "dt" or "li" or "optgroup" or "option" or "p" or "rb" or "rp" or "rt" or "rtc" or "tbody" or "td" or "tfoot" or "th" or "thead" or "tr" or "body" or "html" })) {
                    AddParseError("IN BODY END-Tag body Otherwise");
                }
                // Switch the insertion mode to "after body".
                insertionMode = InsertionMode.AfterBody;
                break;
            case EndTag { name: "html" }:
                // If the stack of open elements does not have a body element in scope, this is a parse error; ignore the token.
                if (!HasAElementInScope("body")) {
                    AddParseError("InsertionModeInBody: EndTag html");
                    break;
                }
                // Otherwise, if there is a node in the stack of open elements that is not either a dd element, a dt element, an li element, an optgroup element, an option element, a p element, an rb element, an rp element, an rt element, an rtc element, a tbody element, a td element, a tfoot element, a th element, a thead element, a tr element, the body element, or the html element, then this is a parse error.
                if (stackOfOpenElements.Any((elem) => elem is not Element { localName: "dd" or "dt" or "li" or "optgroup" or "option" or "p" or "rb" or "rp" or "rt" or "rtc" or "tbody" or "td" or "tfoot" or "th" or "thead" or "tr" or "body" or "html" })) {
                    AddParseError("IN BODY END-Tag html");
                }
                // Switch the insertion mode to "after body".
                insertionMode = InsertionMode.AfterBody;
                // Reprocess the token.
                ReprocessTheToken();
                break;
            case StartTag {
                name: "address" or "article" or "aside" or "blockquote" or "center" or
                                    "details" or "dialog" or "dir" or "div" or "dl" or "fieldset" or "figcaption" or "figure" or
                                    "footer" or "header" or "hgroup" or "main" or "menu" or "nav" or "ol" or "p" or "search" or
                                    "section" or "summary" or "ul"
            } tagToken:
                // If the stack of open elements has a p element in button scope, then close a p element.
                if (HasAElementInButtonScope("p")) {
                    CloseAPElement();
                }
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                break;
            case StartTag { name: "h1" or "h2" or "h3" or "h4" or "h5" or "h6" } tagToken:
                // If the stack of open elements has a p element in button scope, then close a p element.
                if (HasAElementInButtonScope("p")) {
                    CloseAPElement();
                }
                // If the current node is an HTML element whose tag name is one of "h1", "h2", "h3", "h4", "h5", or "h6", then this is a parse error; pop the current node off the stack of open elements.
                if (currentNode is Element { localName: "h1" or "h2" or "h3" or "h4" or "h5" or "h6" }) {
                    AddParseError("unexpected-start-tag");
                    stackOfOpenElements.Pop();
                }
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                break;
            case StartTag { name: "pre" or "listing" } tagToken:
                // If the stack of open elements has a p element in button scope, then close a p element.
                if (HasAElementInButtonScope("p")) {
                    CloseAPElement();
                }
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                // If the next token is a U+000A LINE FEED (LF) character token, then ignore that token and move on to the next one. (Newlines at the start of pre blocks are ignored as an authoring convenience.)
                // todo need peek token
                // Set the frameset-ok flag to "not ok".
                framesetOk = false;
                break;
            case StartTag { name: "form" } tagToken:
                // If the form element pointer is not null, and there is no template element on the stack of open elements, then this is a parse error; ignore the token.
                var hasTemplateElementOnStack = stackOfOpenElements.Any((item) => item.localName == "template");
                if (formElementPointer != null && !hasTemplateElementOnStack) {
                    AddParseError("in-body-form");
                } else {
                    // Otherwise:
                    // If the stack of open elements has a p element in button scope, then close a p element.
                    if (HasAElementInButtonScope("p")) {
                        CloseAPElement();
                    }
                    // Insert an HTML element for the token, and, if there is no template element on the stack of open elements, set the form element pointer to point to the element created.
                    var element = InsertAnHTMLElement(tagToken);
                    if (!hasTemplateElementOnStack) {
                        formElementPointer = element;
                    }
                }
                break;
            case StartTag { name: "li" } tagToken: {
                    // 1. Set the frameset-ok flag to "not ok".
                    framesetOk = false;
                    // 2. Initialize node to be the current node (the bottommost node of the stack).
                    // 3. Loop: If node is an li element, then run these substeps:
                    for (var i = stackOfOpenElements.Count - 1; i > 0; i--) {
                        var node = stackOfOpenElements[i];
                        if (node is Element { localName: "li" }) {
                            // 1. Generate implied end tags, except for li elements.
                            GenerateImpliedEndTags("li");
                            // 2. If the current node is not an li element, then this is a parse error.
                            if (currentNode is not Element { localName: "li" }) {
                                AddParseError("insertionMode in body: LI");
                            }
                            // 3. Pop elements from the stack of open elements until an li element has been popped from the stack.
                            while (stackOfOpenElements.Pop() is not Element { localName: "li" }) { }
                            // 4. Jump to the step labeled done below.
                            break;
                        }
                        // 4. If node is in the special category, but is not an address, div, or p element, then jump to the step labeled done below.
                        if (node.localName is not "address" or "div" or "p" && specialListElements.Contains(node.localName)) {
                            break;
                        }
                        // 5. Otherwise, set node to the previous entry in the stack of open elements and return to the step labeled loop.
                        // we done this with the loop up top
                    }
                    // 6. Done: If the stack of open elements has a p element in button scope, then close a p element.
                    if (HasAElementInButtonScope("p")) {
                        CloseAPElement();
                    }
                    // 7. Finally, insert an HTML element for the token.
                    InsertAnHTMLElement(tagToken);
                    break;
                }
            case StartTag { name: "dd" or "dt" } tagToken: {
                    // Run these steps:
                    // 1. Set the frameset-ok flag to "not ok".
                    framesetOk = false;
                    // 2. Initialize node to be the current node (the bottommost node of the stack).
                    // 3. Loop: If node is a dd element, then run these substeps:
                    for (var i = stackOfOpenElements.Count - 1; i > 0; i--) {
                        var node = stackOfOpenElements[i];
                        if (node is Element { localName: "dd" }) {
                            // 1. Generate implied end tags, except for dd elements.
                            GenerateImpliedEndTags("dd");
                            // 2. If the current node is not a dd element, then this is a parse error.
                            if (currentNode is not Element { localName: "dd" }) {
                                AddParseError("in-body-dd-unexpected-current-node");
                            }
                            // 3. Pop elements from the stack of open elements until a dd element has been popped from the stack.
                            while (stackOfOpenElements.Pop() is not Element { localName: "dd" }) { }
                            // 4. Jump to the step labeled done below.
                            break;
                        }
                        // 4. If node is a dt element, then run these substeps:
                        if (node is Element { localName: "dt" }) {
                            // 1. Generate implied end tags, except for dt elements.
                            GenerateImpliedEndTags("dt");
                            // 2. If the current node is not a dt element, then this is a parse error.
                            if (currentNode is not Element { localName: "dt" }) {
                                AddParseError("in-body-dd-unexpected-current-node");
                            }
                            // 3. Pop elements from the stack of open elements until a dt element has been popped from the stack.
                            while (stackOfOpenElements.Pop() is not Element { localName: "dt" }) { }
                            // 4. Jump to the step labeled done below.
                            break;
                        }
                        // 5. If node is in the special category, but is not an address, div, or p element, then jump to the step labeled done below.
                        if (node.localName is not "address" or "div" or "p" && specialListElements.Contains(node.localName)) {
                            break;
                        }
                        // 6. Otherwise, set node to the previous entry in the stack of open elements and return to the step labeled loop.
                        // we done this with the loop up top
                    }
                    // 7. Done: If the stack of open elements has a p element in button scope, then close a p element.
                    // 8. Finally, insert an HTML element for the token.
                    InsertAnHTMLElement(tagToken);
                }
                break;
            case StartTag { name: "plaintext" } tagToken:
                // If the stack of open elements has a p element in button scope, then close a p element.
                if (HasAElementInButtonScope("p")) {
                    CloseAPElement();
                }
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                // Switch the tokenizer to the PLAINTEXT state.
                tokenizer.SwitchState(State.PLAINTEXTState);
                // Note: Once a start tag with the tag name "plaintext" has been seen, all remaining tokens will be character tokens (and a final end-of-file token) because there is no way to switch the tokenizer
                // out of the PLAINTEXT state. However, as the tree builder remains in its existing insertion mode, it might reconstruct the active formatting elements while processing those character tokens.
                // This means that the parser can insert other elements into the plaintext element.
                break;
            case StartTag { name: "button" } tagToken:
                // 1. If the stack of open elements has a button element in scope, then run these substeps:
                if (HasAElementInScope("button")) {
                    // 1. Parse error.
                    AddParseError("InsertionModeInBody: StartTag button");
                    // 2. Generate implied end tags.
                    GenerateImpliedEndTags();
                    // 3. Pop elements from the stack of open elements until a button element has been popped from the stack.
                    while (true) {
                        var element = stackOfOpenElements.Pop();
                        if (element.localName == "button") break;
                    }
                }
                // 2. Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // 3. Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                // 4. Set the frameset-ok flag to "not ok".
                framesetOk = false;
                break;
            case EndTag {
                name: "address" or "article" or "aside" or "blockquote" or "button" or "center" or
                                    "details" or "dialog" or "dir" or "div" or "dl" or "fieldset" or "figcaption" or "figure" or
                                    "footer" or "header" or "hgroup" or "listing" or "main" or "menu" or "nav" or "ol" or "pre" or
                                    "search" or "section" or "summary" or "ul"
            } tagToken: {
                    // If the stack of open elements does not have an element in scope that is an HTML element with the same tag name as that of the token, then this is a parse error; ignore the token.
                    if (!HasAElementInScope(tagToken.name)) {
                        AddParseError("INBODY - ENDTAG");
                        break;
                    }
                    // Otherwise, run these steps:
                    // 1. Generate implied end tags.
                    GenerateImpliedEndTags();
                    // 2. If the current node is not an HTML element with the same tag name as that of the token, then this is a parse error.
                    if (currentNode.localName != tagToken.name) {
                        AddParseError("INBODY - ENDTAG - !==");
                    }
                    // 3. Pop elements from the stack of open elements until an HTML element with the same tag name as the token has been popped from the stack.
                    while (true) {
                        var element = stackOfOpenElements.Pop();
                        if (element.localName == tagToken.name) break;
                    }
                    break;
                }
            case EndTag { name: "form" }: {
                    // If there is no template element on the stack of open elements, then run these substeps:
                    if (!stackOfOpenElements.Any((item) => item.localName == "template")) {
                        // 1. Let node be the element that the form element pointer is set to, or null if it is not set to an element.
                        var node = formElementPointer;
                        // 2. Set the form element pointer to null.
                        formElementPointer = null;
                        // 3. If node is null or if the stack of open elements does not have node in scope, then this is a parse error; return and ignore the token.
                        if (node is null || !HasAElementInScope(node.localName)) {
                            AddParseError("in-body-end-form-node-null");
                            break;
                        }
                        // 4. Generate implied end tags.
                        GenerateImpliedEndTags();
                        // 5. If the current node is not node, then this is a parse error.
                        if (currentNode != node) {
                            AddParseError("in-body-end-form-not-current");
                        }
                        // 6. Remove node from the stack of open elements.
                        stackOfOpenElements.Remove(node);
                    } else {
                        throw new NotImplementedException();
                        // If there is a template element on the stack of open elements, then run these substeps instead:

                        // 1. If the stack of open elements does not have a form element in scope, then this is a parse error; return and ignore the token.

                        // 2. Generate implied end tags.

                        // 3. If the current node is not a form element, then this is a parse error.

                        // 4. Pop elements from the stack of open elements until a form element has been popped from the stack.

                    }
                    break;
                }
            case EndTag { name: "p" }:
                // If the stack of open elements does not have a p element in button scope, then this is a parse error; insert an HTML element for a "p" start tag token with no attributes.
                if (!HasAElementInButtonScope("p")) {
                    AddParseError("InsertionModeInBody: EndTag p");
                    InsertAnHTMLElement(new StartTag("p"));
                }
                // Close a p element.
                CloseAPElement();
                break;
            case EndTag { name: "li" }:
                // If the stack of open elements does not have an li element in list item scope, then this is a parse error; ignore the token.
                if (!HasAElementInListItemScope("li")) {
                    AddParseError("IN BODY - </li>");
                } else {
                    // Otherwise, run these steps:
                    // 1. Generate implied end tags, except for li elements.
                    GenerateImpliedEndTags("li");
                    // 2. If the current node is not an li element, then this is a parse error.
                    if (currentNode.localName != "li") {
                        AddParseError("in BODY - </li> ");
                    }
                    // 3. Pop elements from the stack of open elements until an li element has been popped from the stack.
                    while (true) {
                        if (stackOfOpenElements.Pop().localName == "li") break;
                    }
                }
                break;
            case EndTag { name: "dd" or "dt" } tagToken:
                // If the stack of open elements does not have an element in scope that is an HTML element with the same tag name as that of the token, then this is a parse error; ignore the token.
                if (!HasAElementInScope(tagToken.name)) {
                    AddParseError("in-body-end-dd-dt-not-in-scope");
                } else {
                    // Otherwise, run these steps:
                    // 1. Generate implied end tags, except for HTML elements with the same tag name as the token.
                    GenerateImpliedEndTags();
                    // 2. If the current node is not an HTML element with the same tag name as that of the token, then this is a parse error.
                    if (currentNode.localName != tagToken.name) {
                        AddParseError("in-body-end-dd-dt-wrong-current-node");
                    }
                    // 3. Pop elements from the stack of open elements until an HTML element with the same tag name as the token has been popped from the stack.
                    while (true) {
                        if (stackOfOpenElements.Pop().localName == tagToken.name) break;
                    }
                }
                break;
            case EndTag { name: "h1" or "h2" or "h3" or "h4" or "h5" or "h6" } tagToken:
                // If the stack of open elements does not have an element in scope that is an HTML element and whose tag name is one of "h1", "h2", "h3", "h4", "h5", or "h6", then this is a parse error; ignore the token.
                if (!HasAElementInScope("h1", "h2", "h3", "h4", "h5", "h6")) {
                    AddParseError("IN BODY </hX>");
                } else {
                    // Otherwise, run these steps:
                    // 1. Generate implied end tags.
                    GenerateImpliedEndTags();
                    // 2. If the current node is not an HTML element with the same tag name as that of the token, then this is a parse error.
                    if (currentNode.localName != tagToken.name) {
                        AddParseError("IN BODY <hX> 2");
                    }
                    // 3. Pop elements from the stack of open elements until an HTML element whose tag name is one of "h1", "h2", "h3", "h4", "h5", or "h6" has been popped from the stack.
                    while (true) {
                        if (stackOfOpenElements.Pop().localName is "h1" or "h2" or "h3" or "h4" or "h5" or "h6") break;
                    }
                }
                break;
            case EndTag { name: "sarcasm" } tagToken:
                // Take a deep breath, then act as described in the "any other end tag" entry below.
                InsertionModeInBodyAnyOtherEndTag(tagToken);
                break;
            case StartTag { name: "a" } tagToken:
                // If the list of active formatting elements contains an a element between the end of the list and the last marker on the list (or the start of the list if there is no marker on the list), then this is a parse error; run the adoption agency algorithm for the token, then remove that element from the list of active formatting elements and the stack of open elements if the adoption agency algorithm didn't already remove it (it might not have if the element is not in table scope).
                var ttr1 = (string localName) => { // todo rename/move
                    foreach (var elem in Enumerable.Reverse(ListOfActiveFormattingElements)) {
                        if (elem is not null) {
                            if (elem.localName == localName) {
                                return elem;
                            }
                        } else break;
                    }
                    return null;
                };
                var elem = ttr1("a");
                if (elem is not null) {
                    AddParseError("IN BODY a");
                    AdoptionAgencyAlgorithm(tagToken);
                    ListOfActiveFormattingElements.Remove(elem);
                    stackOfOpenElements.Remove(elem);
                }
                // EXAMPLE: In the non-conforming stream <a href="a">a<table><a href="b">b</table>x, the first a element would be closed upon seeing the second one, and the "x" character would be inside a link to "b", not to "a". This is despite the fact that the outer a element is not in table scope (meaning that a regular </a> end tag at the start of the table wouldn't close the outer a element). The result is that the two a elements are indirectly nested inside each other — non-conforming markup will often result in non-conforming DOMs when parsed.
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Insert an HTML element for the token. Push onto the list of active formatting elements that element.
                ListOfActiveFormattingElements.Push(InsertAnHTMLElement(tagToken));
                break;

            case StartTag { name: "b" or "big" or "code" or "em" or "font" or "i" or "s" or "small" or "strike" or "strong" or "tt" or "u" } tagToken:
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Insert an HTML element for the token. Push onto the list of active formatting elements that element.
                PushOntoTheListOfActiveFormattingElements(InsertAnHTMLElement(tagToken));
                break;
            case StartTag { name: "nobr" } tagToken:
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // If the stack of open elements has a nobr element in scope, then this is a parse error; run the adoption agency algorithm for the token, then once again reconstruct the active formatting elements, if any.
                if (HasAElementInScope("nobr")) {
                    AddParseError("in-body-nobr-in-scope");
                    AdoptionAgencyAlgorithm(tagToken);
                    ReconstructTheActiveFormattingElements();
                }
                // Insert an HTML element for the token. Push onto the list of active formatting elements that element.
                ListOfActiveFormattingElements.Push(InsertAnHTMLElement(tagToken));
                break;
            case EndTag { name: "a" or "b" or "big" or "code" or "em" or "font" or "i" or "s" or "small" or "strike" or "strong" or "tt" or "u" } tagToken:
                // Run the adoption agency algorithm for the token.
                AdoptionAgencyAlgorithm(tagToken);
                break;
            case StartTag { name: "applet" or "marquee" or "object" } tagToken:
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                // Insert a marker at the end of the list of active formatting elements.
                ListOfActiveFormattingElements.Add(null);
                // Set the frameset-ok flag to "not ok".
                framesetOk = false;
                break;
            case EndTag { name: "applet" or "marquee" or "object" } tagToken:
                // If the stack of open elements does not have an element in scope that is an HTML element with the same tag name as that of the token, then this is a parse error; ignore the token.
                if (!HasAElementInScope(tagToken.name)) {
                    AddParseError("IN BODY - not in scope");
                } else {
                    // Otherwise, run these steps:
                    // 1. Generate implied end tags.
                    GenerateImpliedEndTags();
                    // 2. If the current node is not an HTML element with the same tag name as that of the token, then this is a parse error.
                    if (currentNode.localName != tagToken.name) {
                        AddParseError("end-tag-too-early");
                    }
                    // 3. Pop elements from the stack of open elements until an HTML element with the same tag name as the token has been popped from the stack.
                    while (true) {
                        if (stackOfOpenElements.Pop().localName == tagToken.name) break;
                    }
                    // 4. Clear the list of active formatting elements up to the last marker.
                    ClearTheListOfACtiveFormattingElementsUpToTheLastMarker();
                }

                break;
            case StartTag { name: "table" } tagToken:
                // If the Document is not set to quirks mode, and the stack of open elements has a p element in button scope, then close a p element.
                if (document.mode != Document.QuirksMode.Quirks && HasAElementInButtonScope("p")) {
                    CloseAPElement();
                }
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                // Set the frameset-ok flag to "not ok".
                framesetOk = false;
                // Switch the insertion mode to "in table".
                insertionMode = InsertionMode.InTable;
                break;
            case EndTag { name: "br" } tagToken:
                // Parse error. Drop the attributes from the token, and act as described in the next entry; i.e. act as if this was a "br" start tag token with no attributes, rather than the end tag token that it actually is.
                AddParseError("in-body-end-tag-br");

                this.token = new StartTag("br"); // this is not exectly what the comment says but is easier to express here
                ReprocessTheToken();
                break;
            case StartTag { name: "area" or "br" or "embed" or "img" or "keygen" or "wbr" } tagToken:
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
                InsertAnHTMLElement(tagToken);
                stackOfOpenElements.Pop();
                // Acknowledge the token's self-closing flag, if it is set.
                // todo
                // Set the frameset-ok flag to "not ok".
                framesetOk = false;
                break;
            case StartTag { name: "input" } tagToken:
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
                InsertAnHTMLElement(tagToken);
                stackOfOpenElements.Pop();
                // Acknowledge the token's self-closing flag, if it is set.
                // todo
                // If the token does not have an attribute with the name "type", or if it does, but that attribute's value is not an ASCII case-insensitive match for the string "hidden", then: set the frameset-ok flag to "not ok".
                if (!tagToken.Attributes.Any((item) => item.name == "type" && string.Equals(item.value, "hidden", StringComparison.OrdinalIgnoreCase))) {
                    framesetOk = false;
                }
                break;
            case StartTag { name: "param" or "source" or "track" } tagToken:
                // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
                InsertAnHTMLElement(tagToken);
                stackOfOpenElements.Pop();
                // Acknowledge the token's self-closing flag, if it is set.
                // todo
                break;
            case StartTag { name: "hr" } tagToken:
                // If the stack of open elements has a p element in button scope, then close a p element.
                if (HasAElementInButtonScope("p")) {
                    CloseAPElement();
                }
                // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
                InsertAnHTMLElement(tagToken);
                stackOfOpenElements.Pop();
                // Acknowledge the token's self-closing flag, if it is set.
                // todo
                // Set the frameset-ok flag to "not ok".
                framesetOk = false;
                break;
            case StartTag { name: "image" } tagToken:
                AddParseError("In body image tag");
                tagToken.name = "img";
                ReprocessTheToken();
                break;
            case StartTag { name: "textarea" } tagToken:
                // Run these steps:
                // 1. Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                // 2. If the next token is a U+000A LINE FEED (LF) character token, then ignore that token and move on to the next one. (Newlines at the start of textarea elements are ignored as an authoring convenience.)
                // todo need peek token
                // 3. Switch the tokenizer to the RCDATA state.
                tokenizer.SwitchState(State.RCDATAState);
                // 4. Set the original insertion mode to the current insertion mode.
                originalInsertionMode = insertionMode;
                // 5. Set the frameset-ok flag to "not ok".
                framesetOk = false;
                // 6. Switch the insertion mode to "text".
                insertionMode = InsertionMode.Text;
                break;
            case StartTag { name: "xmp" } startTag:
                // If the stack of open elements has a p element in button scope, then close a p element.
                if (HasAElementInButtonScope("p")) {
                    CloseAPElement();
                }
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Set the frameset-ok flag to "not ok".
                framesetOk = false;
                // Follow the generic raw text element parsing algorithm.
                GenericRawTextElementParsingAlgorithm(startTag);
                break;
            case StartTag { name: "iframe" } startTag:
                // Set the frameset-ok flag to "not ok".
                framesetOk = false;
                // Follow the generic raw text element parsing algorithm.
                GenericRawTextElementParsingAlgorithm(startTag);
                break;
            case StartTag { name: "noembed" } startTag:
                // Follow the generic raw text element parsing algorithm.
                GenericRawTextElementParsingAlgorithm(startTag);
                break;
            case StartTag { name: "noscript" } startTag when scriptingFlag:
                // Follow the generic raw text element parsing algorithm.
                GenericRawTextElementParsingAlgorithm(startTag);
                break;
            case StartTag { name: "select" } tagToken:
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                // Set the frameset-ok flag to "not ok".
                framesetOk = false;
                // If the insertion mode is one of "in table", "in caption", "in table body", "in row", or "in cell", then switch the insertion mode to "in select in table". Otherwise, switch the insertion mode to "in select".
                if (insertionMode is InsertionMode.InTable or InsertionMode.InCaption or InsertionMode.InTableBody or InsertionMode.InRow or InsertionMode.InCell) {
                    insertionMode = InsertionMode.InSelectInTable;
                } else {
                    insertionMode = InsertionMode.InSelect;
                }
                break;
            case StartTag { name: "optgroup" or "option" } tagToken:
                // If the current node is an option element, then pop the current node off the stack of open elements.
                if (currentNode is Element { localName: "option" }) {
                    stackOfOpenElements.Pop();
                }
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                break;
            case StartTag { name: "rb" or "rtc" } tagToken:
                // If the stack of open elements has a ruby element in scope, then generate implied end tags. If the current node is not now a ruby element, this is a parse error.
                if (HasAElementInScope("ruby")) {
                    GenerateImpliedEndTags();
                    if (currentNode is not Element { localName: "ruby" }) {
                        AddParseError("in-body-rb-rtc-unexpected-current-node");
                    }
                }
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                break;
            case StartTag { name: "rp" or "rt" } tagToken:
                // If the stack of open elements has a ruby element in scope, then generate implied end tags, except for rtc elements. If the current node is not now a rtc element or a ruby element, this is a parse error.
                if (HasAElementInScope("ruby")) {
                    GenerateImpliedEndTags("rtc");
                    if (currentNode is not Element { localName: "rtc" or "ruby" }) {
                        AddParseError("in-body-rp-rt-unexpected-current-node");
                    }
                }
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                break;
            case StartTag { name: "math" }: throw new NotImplementedException();
            case StartTag { name: "svg" }: throw new NotImplementedException();
            case StartTag { name: "caption" or "col" or "colgroup" or "frame" or "head" or "tbody" or "td" or "tfoot" or "th" or "thead" or "tr" }:
                AddParseError("InBody - unexpected-start-tag-ignored");
                break;
            case StartTag tagToken:
                // Reconstruct the active formatting elements, if any.
                ReconstructTheActiveFormattingElements();
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                break;
            case EndTag tagToken:
                InsertionModeInBodyAnyOtherEndTag(tagToken);
                break;
        }

        void InsertionModeInBodyAnyOtherEndTag(Tag tagToken) {
            // 1. Initialize node to be the current node (the bottommost node of the stack).
            var node = currentNode;
            // 2. Loop: If node is an HTML element with the same tag name as the token, then:
            while (true) {
                if (node.localName == tagToken.name) {
                    // 1. Generate implied end tags, except for HTML elements with the same tag name as the token.
                    GenerateImpliedEndTags(tagToken.name);
                    // 2. If node is not the current node, then this is a parse error.
                    if (currentNode != node) {
                        AddParseError("IN BODY: currentNode != node");
                    }
                    // 3. Pop all the nodes from the current node up to node, including node, then stop these steps.
                    while (true) {
                        if (node == stackOfOpenElements.Pop()) {
                            return;
                        }
                    }
                } else {
                    // 3. Otherwise, if node is in the special category, then this is a parse error; ignore the token, and return.    
                    if (specialListElements.Contains(node.localName)) {
                        AddParseError("unexpected-end-tag");
                        return;
                    }
                }
                // 4. Set node to the previous entry in the stack of open elements.
                node = currentNode;
                // 5. Return to the step labeled loop.
            }
        }

        // https://html.spec.whatwg.org/multipage/parsing.html#close-a-p-element
        void CloseAPElement() {
            // Generate implied end tags, except for p elements.
            GenerateImpliedEndTags("p");
            // If the current node is not a p element, then this is a parse error.
            if (currentNode is not Element { localName: "p" }) {
                AddParseError("CLOSEAPELEMENT not p");
            }
            // Pop elements from the stack of open elements until a p element has been popped from the stack.
            while (true) {
                var element = stackOfOpenElements.Pop();
                if (element.localName == "p") return;
            }
        }

        // https://html.spec.whatwg.org/multipage/parsing.html#adoption-agency-algorithm
        void AdoptionAgencyAlgorithm(Tag tagToken) {
            // 1. Let subject be token's tag name.
            var subject = tagToken.name;
            // 2. If the current node is an HTML element whose tag name is subject, and the current node is not in the list of active formatting elements,
            // then pop the current node off the stack of open elements and return.
            if (currentNode is Element e && e.localName == subject && !ListOfActiveFormattingElements.Contains(currentNode)) {
                stackOfOpenElements.Pop();
                return;
            }
            // 3. Let outerLoopCounter be 0.
            var outerLoopCounter = 0;
            // 4. While true:
            while (true) {
                // 1. If outerLoopCounter is greater than or equal to 8, then return.
                if (outerLoopCounter >= 8) return;
                // 2. Increment outerLoopCounter by 1.
                outerLoopCounter++;
                // 3. Let formattingElement be the last element in the list of active formatting elements that:
                Element? formattingElement = null;
                // * is between the end of the list and the last marker in the list, if any, or the start of the list otherwise, and
                // * has the tag name subject.
                var formattingElementPosInListOfActiveFormattingElements = 0;
                for (var i = ListOfActiveFormattingElements.Count - 1; i >= 0; i--) {
                    var element = ListOfActiveFormattingElements[i];
                    if (element is null) break;
                    if (element.localName == subject) {
                        formattingElement = element;
                        formattingElementPosInListOfActiveFormattingElements = i;
                        break;
                    }
                }
                // If there is no such element, then return and instead act as described in the "any other end tag" entry above.
                if (formattingElement == null) {
                    InsertionModeInBodyAnyOtherEndTag(tagToken);
                    return;
                }
                // 4. If formattingElement is not in the stack of open elements, then this is a parse error; remove the element from the list, and return.
                if (!stackOfOpenElements.Contains(formattingElement)) {
                    AddParseError("AdoptionAgencyAlgorithm: 4.4");
                    ListOfActiveFormattingElements.Remove(formattingElement);
                    return;
                }
                // 5. If formattingElement is in the stack of open elements, but the element is not in scope, then this is a parse error; return.
                if (!HasAElementInScope(formattingElement.localName)) {
                    AddParseError("AdoptionAgencyAlgorithm: HasAElementInScope");
                    return;
                }
                // 6. If formattingElement is not the current node, this is a parse error. (But do not return.)
                if (formattingElement != currentNode) {
                    AddParseError("adoption-agency-1.3");
                }
                // 7. Let furthestBlock be the topmost node in the stack of open elements that is lower in the stack than formattingElement,
                // and is an element in the special category. There might not be one.
                Element? furthestBlock = null;
                var furthestBlockPosInStackOfOpenElements = 0;
                for (var i = stackOfOpenElements.Count - 1; i >= 0; i--) {
                    var element = stackOfOpenElements[i];
                    if (element == formattingElement) break;
                    if (specialListElements.Contains(element.localName)) {
                        furthestBlock = element;
                        furthestBlockPosInStackOfOpenElements = i;
                    }
                }
                // 8. If there is no furthestBlock, then the UA must first pop all the nodes from the bottom of the stack of open elements,
                // from the current node up to and including formattingElement, then remove formattingElement from the list of active formatting elements, and finally return.
                if (furthestBlock == null) {
                    while (stackOfOpenElements.Pop() != formattingElement) { }
                    ListOfActiveFormattingElements.Remove(formattingElement);
                    return;
                }
                // 9. Let commonAncestor be the element immediately above formattingElement in the stack of open elements.
                Element commonAncestor = stackOfOpenElements[stackOfOpenElements.IndexOf(formattingElement) - 1];
                // 10. Let a bookmark note the position of formattingElement in the list of active formatting elements relative to the elements on either side of it in the list.
                var bookmark = formattingElementPosInListOfActiveFormattingElements;
                // 11. Let node and lastNode be furthestBlock.
                var node = furthestBlock;
                var nodePosInStackOfOpenElements = furthestBlockPosInStackOfOpenElements;
                var lastNode = furthestBlock;
                // 12. Let innerLoopCounter be 0.
                var innerLoopCounter = 0;
                // 13. While true:
                while (true) {
                    // 1. Increment innerLoopCounter by 1.
                    innerLoopCounter++;
                    // 2. Let node be the element immediately above node in the stack of open elements, or if node is no longer in the stack of open elements
                    // (e.g. because it got removed by this algorithm), the element that was immediately above node in the stack of open elements before node was removed.
                    node = stackOfOpenElements[--nodePosInStackOfOpenElements];
                    // 3. If node is formattingElement, then break.
                    if (node == formattingElement) break;
                    // 4. If innerLoopCounter is greater than 3 and node is in the list of active formatting elements, then remove node from the list of active formatting elements.
                    if (innerLoopCounter > 3 && ListOfActiveFormattingElements.Contains(node)) {
                        ListOfActiveFormattingElements.Remove(node);
                    }
                    // 5. If node is not in the list of active formatting elements, then remove node from the stack of open elements and continue.
                    if (!ListOfActiveFormattingElements.Contains(node)) {
                        stackOfOpenElements.Remove(node);
                        continue;
                    }
                    // 6. Create an element for the token for which the element node was created, in the HTML namespace, with commonAncestor as the intended parent;
                    // replace the entry for node in the list of active formatting elements with an entry for the new element, 
                    // replace the entry for node in the stack of open elements with an entry for the new element, and let node be the new element.
                    var element = CreateAnElementForAToken(new StartTag(node.localName), Namespaces.HTML, commonAncestor); // todo this is hacky we need the real token;
                    var index = ListOfActiveFormattingElements.IndexOf(node);
                    ListOfActiveFormattingElements[index] = element;
                    stackOfOpenElements[nodePosInStackOfOpenElements] = element;
                    node = element;
                    // 7. If lastNode is furthestBlock, then move the aforementioned bookmark to be immediately after the new node in the list of active formatting elements.
                    if (lastNode == furthestBlock) {
                        bookmark = index + 1;
                    }
                    // 8. Append lastNode to node.
                    AppendNode(node, lastNode);
                    // 9. Set lastNode to node.
                    lastNode = node;

                }
                // 14. Insert whatever lastNode ended up being in the previous step at the appropriate place for inserting a node, 
                // but using commonAncestor as the override target.
                var adjustedInsertionLocation = AppropriatePlaceForInsertingANode(commonAncestor);
                InsertNode(adjustedInsertionLocation, lastNode);
                // 15. Create an element for the token for which formattingElement was created, in the HTML namespace, with furthestBlock as the intended parent.
                var elem = CreateAnElementForAToken(new StartTag(formattingElement.localName), Namespaces.HTML, furthestBlock);
                // 16. Take all of the child nodes of furthestBlock and append them to the element created in the last step.
                elem.childNodes = [.. furthestBlock.childNodes];
                furthestBlock.childNodes.Clear();
                // 17. Append that new element to furthestBlock.
                AppendNode(furthestBlock, elem);
                // 18. Remove formattingElement from the list of active formatting elements,
                // and insert the new element into the list of active formatting elements at the position of the aforementioned bookmark.
                ListOfActiveFormattingElements[bookmark] = elem;
                ListOfActiveFormattingElements.Remove(formattingElement);
                // 19. Remove formattingElement from the stack of open elements,
                // and insert the new element into the stack of open elements immediately below the position of furthestBlock in that stack.
                stackOfOpenElements.Remove(formattingElement);
                var indexA = stackOfOpenElements.IndexOf(furthestBlock);
                stackOfOpenElements.Insert(indexA + 1, elem);
                return;
            }
        }
    }



    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-incdata
    private void InsertionModeText() {
        switch (token) {
            case Character cToken:
                InsertACharacter(cToken);
                break;
            case EndOfFile:
                // Parse error.
                AddParseError("Text mode EOF");
                // If the current node is a script element, then set its already started to true.
                if (currentNode is Element { localName: "script" }) {
                    // todo set already started
                }
                // Pop the current node off the stack of open elements.
                stackOfOpenElements.Pop();
                // Switch the insertion mode to the original insertion mode and reprocess the token.
                insertionMode = originalInsertionMode;
                ReprocessTheToken();
                break;
            case EndTag { name: "script" }:
                // If the active speculative HTML parser is null and the JavaScript execution context stack is empty, then perform a microtask checkpoint.
                // todo
                // Let script be the current node (which will be a script element).
                var script = currentNode;
                // Pop the current node off the stack of open elements.
                stackOfOpenElements.Pop();
                // Switch the insertion mode to the original insertion mode.
                insertionMode = originalInsertionMode;
                // Let the old insertion point have the same value as the current insertion point. Let the insertion point be just before the next input character.
                // todo
                // Increment the parser's script nesting level by one.
                // todo
                // If the active speculative HTML parser is null, then prepare the script element script. This might cause some script to execute, which might cause new characters to be inserted into the tokenizer, and might cause the tokenizer to output more tokens, resulting in a reentrant invocation of the parser.
                // todo
                // Decrement the parser's script nesting level by one. If the parser's script nesting level is zero, then set the parser pause flag to false.
                // todo
                // Let the insertion point have the value of the old insertion point. (In other words, restore the insertion point to its previous value. This value might be the "undefined" value.)
                // todo
                // At this stage, if the pending parsing-blocking script is not null, then:
                // todo
                // If the script nesting level is not zero:
                // Set the parser pause flag to true, and abort the processing of any nested invocations of the tokenizer, yielding control back to the caller. (Tokenization will resume when the caller returns to the "outer" tree construction stage.)
                // todo
                // NOTE: The tree construction stage of this particular parser is being called reentrantly, say from a call to document.write().

                // Otherwise:
                // While the pending parsing-blocking script is not null:

                // 1. Let the script be the pending parsing-blocking script.
                // todo
                // 2. Set the pending parsing-blocking script to null.
                // todo
                // 3. Start the speculative HTML parser for this instance of the HTML parser.
                // todo
                // 4. Block the tokenizer for this instance of the HTML parser, such that the event loop will not run tasks that invoke the tokenizer.
                // todo
                // 5. If the parser's Document has a style sheet that is blocking scripts or the script's ready to be parser-executed is false: spin the event loop until the parser's Document has no style sheet that is blocking scripts and the script's ready to be parser-executed becomes true.
                // todo
                // 6. If this parser has been aborted in the meantime, return.
                // todo
                // NOTE: This could happen if, e.g., while the spin the event loop algorithm is running, the Document gets destroyed, or the document.open() method gets invoked on the Document.

                // 7. Stop the speculative HTML parser for this instance of the HTML parser.
                // todo
                // 8. Unblock the tokenizer for this instance of the HTML parser, such that tasks that invoke the tokenizer can again be run.
                // todo
                // 9. Let the insertion point be just before the next input character.
                // todo
                // 10. Increment the parser's script nesting level by one (it should be zero before this step, so this sets it to one).
                // todo
                // 11. Execute the script element the script.
                // todo
                // 12. Decrement the parser's script nesting level by one. If the parser's script nesting level is zero (which it always should be at this point), then set the parser pause flag to false.
                // todo
                // 13. Let the insertion point be undefined again.
                break;

            case EndTag:
                // Pop the current node off the stack of open elements.
                stackOfOpenElements.Pop();
                // Switch the insertion mode to the original insertion mode.                        
                insertionMode = originalInsertionMode;
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-intable
    private void InsertionModeInTable() {
        // https://html.spec.whatwg.org/multipage/parsing.html#clear-the-stack-back-to-a-table-context
        var ClearTheStackBackToTableContext = () => {
            while (!((ReadOnlySpan<string>)["table", "template", "html"]).Contains(stackOfOpenElements.Peek().localName)) {
                stackOfOpenElements.Pop();
            }
        };

        switch (token) {
            case Character when currentNode is Element { localName: "table" or "tbody" or "tempalte" or "tfoot" or "thead" or "tr" }:
                // Let the pending table character tokens be an empty list of tokens.
                pendingTableCharacterTokens = [];
                // Set the original insertion mode to the current insertion mode.
                originalInsertionMode = insertionMode;
                // Switch the insertion mode to "in table text" and reprocess the token.
                insertionMode = InsertionMode.InTableText;
                ReprocessTheToken();
                break;
            case Tokenizer.Comment comment:
                // Insert a comment.
                InsertAComment(comment);
                break;
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("in-table-unexpeced-doctype-ignored");
                break;
            case StartTag { name: "caption" } tagToken:
                // Clear the stack back to a table context. (See below.)
                ClearTheStackBackToTableContext();
                // Insert a marker at the end of the list of active formatting elements.
                ListOfActiveFormattingElements.Add(null);
                // Insert an HTML element for the token, then switch the insertion mode to "in caption".
                InsertAnHTMLElement(tagToken);
                insertionMode = InsertionMode.InCaption;
                break;
            case StartTag { name: "colgroup" } tagToken:
                // Clear the stack back to a table context. (See below.)
                ClearTheStackBackToTableContext();
                // Insert an HTML element for the token, then switch the insertion mode to "in column group".
                InsertAnHTMLElement(tagToken);
                insertionMode = InsertionMode.InColumnGroup;
                break;
            case StartTag { name: "col" }:
                // Clear the stack back to a table context. (See below.)
                ClearTheStackBackToTableContext();
                // Insert an HTML element for a "colgroup" start tag token with no attributes, then switch the insertion mode to "in column group".
                InsertAnHTMLElement(new StartTag("colgroup"));
                insertionMode = InsertionMode.InColumnGroup;
                // Reprocess the current token.
                ReprocessTheToken();
                break;
            case StartTag { name: "tbody" or "tfoot" or "thead" } tagToken:
                ClearTheStackBackToTableContext();
                InsertAnHTMLElement(tagToken);
                insertionMode = InsertionMode.InTableBody;
                break;
            case StartTag { name: "td" or "th" or "tr" }:
                // Clear the stack back to a table context. (See below.)
                ClearTheStackBackToTableContext();
                // Insert an HTML element for a "tbody" start tag token with no attributes, then switch the insertion mode to "in table body".
                InsertAnHTMLElement(new StartTag("tbody"));
                insertionMode = InsertionMode.InTableBody;
                // Reprocess the current token.
                ReprocessTheToken();
                break;
            case StartTag { name: "table" }:
                // Parse error.
                AddParseError("in-table-unexpected-table-start-tag");
                // If the stack of open elements does not have a table element in table scope, ignore the token.
                if (!HasAElementInTableScope("table")) {
                    // ignore token
                } else {
                    // Otherwise:
                    // Pop elements from this stack until a table element has been popped from the stack.
                    while (stackOfOpenElements.Pop() is not Element { localName: "table" }) ;
                    // Reset the insertion mode appropriately.
                    ResetTheInsertionModeAppropriately();
                    // Reprocess the token.
                    ReprocessTheToken();
                }
                break;
            case EndTag { name: "table" }:
                // If the stack of open elements does not have a table element in table scope, this is a parse error; ignore the token.
                if (!HasAElementInTableScope("table")) {
                    AddParseError("InsertionModeInTable ENDTag: table");
                } else {
                    // Otherwise:
                    // 1. Pop elements from this stack until a table element has been popped from the stack.
                    while (true) {
                        var element = stackOfOpenElements.Pop();
                        if (element.localName == "table") break;
                    }
                    // 2. Reset the insertion mode appropriately.
                    ResetTheInsertionModeAppropriately();
                }
                break;
            case EndTag { name: "body" or "caption" or "col" or "colgroup" or "html" or "tbody" or "td" or "tfoot" or "th" or "thead" or "tr" }:
                // Parse error. Ignore the token.
                AddParseError("in-table-wrong-close-tag");
                break;
            case StartTag { name: "style" or "script" or "template" }:
            case EndTag { name: "template" }:
                // Process the token using the rules for the "in head" insertion mode.
                InsertionModeInHead();
                break;
            case StartTag { name: "input" } tagToken:
                // If the token does not have an attribute with the name "type", or if it does, but that attribute's value is not an ASCII case-insensitive match for the string "hidden", then: act as described in the "anything else" entry below.
                if (!tagToken.Attributes.Any((item) => item.name == "type" && string.Equals(item.value, "hidden", StringComparison.OrdinalIgnoreCase))) {
                    goto default;
                } else {
                    // Otherwise:
                    // Parse error.
                    AddParseError("in-table-input");
                    // Insert an HTML element for the token.
                    InsertAnHTMLElement(tagToken);
                    // Pop that input element off the stack of open elements.
                    stackOfOpenElements.Pop();
                    // Acknowledge the token's self-closing flag, if it is set.
                    // todo
                }
                break;
            case StartTag { name: "form" } tagToken:
                // Parse error.
                AddParseError("in-table-unexpected-tag-form");
                // If there is a template element on the stack of open elements, or if the form element pointer is not null, ignore the token.
                if (stackOfOpenElements.Any(item => item.localName == "template") || formElementPointer is not null) {
                    // ignore token
                } else {
                    // Otherwise:
                    // Insert an HTML element for the token, and set the form element pointer to point to the element created.
                    formElementPointer = InsertAnHTMLElement(tagToken);
                    // Pop that form element off the stack of open elements.
                    stackOfOpenElements.Pop();
                }
                break;
            case EndOfFile:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            default:
                // Parse error. Enable foster parenting, process the token using the rules for the "in body" insertion mode, and then disable foster parenting.
                AddParseError("IN TABLE - default");
                fosterParenting = true;
                InsertionModeInBody();
                fosterParenting = false;
                break;
        }
    }




    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-intabletext
    private void InSertionModeInTableText() {
        switch (token) {
            case Character { data: '\0' }: throw new NotImplementedException();
            case Character cToken:
                // Append the character token to the pending table character tokens list.
                pendingTableCharacterTokens.Add(cToken);
                break;
            default:
                // If any of the tokens in the pending table character tokens list are character tokens that are not ASCII whitespace,
                // then this is a parse error: reprocess the character tokens in the pending table character tokens list using
                // the rules given in the "anything else" entry in the "in table" insertion mode.
                if (pendingTableCharacterTokens.Any((item) => item.data is not '\u0009' or '\u000A' or '\u000C' or '\u000D' or '\u0020')) {
                    AddParseError("InTableText");
                    // todo copied code
                    var currentToken = token;
                    fosterParenting = true;
                    pendingTableCharacterTokens.ForEach((token) => {
                        this.token = token;
                        InsertionModeInBody();
                    });
                    token = currentToken;
                    fosterParenting = false;
                } else {
                    // Otherwise, insert the characters given by the pending table character tokens list.
                    pendingTableCharacterTokens.ForEach(InsertACharacter);
                }
                // Switch the insertion mode to the original insertion mode and reprocess the token.
                insertionMode = originalInsertionMode;
                ReprocessTheToken();
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-incaption
    private void InsertionModeInCaption() {
        switch (token) {
            case EndTag { name: "caption" }:
                // If the stack of open elements does not have a caption element in table scope, this is a parse error; ignore the token. (fragment case)
                if (!HasAElementInTableScope("caption")) {
                    AddParseError("in-caption-table-scope");
                } else {
                    // Otherwise:
                    // Generate implied end tags.
                    GenerateImpliedEndTags();
                    // Now, if the current node is not a caption element, then this is a parse error.
                    if (currentNode is not Element { localName: "caption" }) {
                        AddParseError("in-caption-is-not-caption");
                    }
                    // Pop elements from this stack until a caption element has been popped from the stack.
                    while (true) {
                        if (stackOfOpenElements.Pop() is Element { localName: "caption" }) break;
                    }
                    // Clear the list of active formatting elements up to the last marker.
                    ClearTheListOfACtiveFormattingElementsUpToTheLastMarker();
                    // Switch the insertion mode to "in table".
                    insertionMode = InsertionMode.InTable;
                }
                break;
            case StartTag { name: "caption" or "col" or "colgroup" or "tbody" or "td" or "tfoot" or "th" or "thead" or "tr" }:
            case EndTag { name: "table" }:
                //  If the stack of open elements does not have a caption element in table scope, this is a parse error; ignore the token. (fragment case)
                if (!HasAElementInTableScope("caption")) {
                    AddParseError("in-caption-no-caption-in-scope");
                } else {
                    // Otherwise:
                    // Generate implied end tags.
                    GenerateImpliedEndTags();
                    // Now, if the current node is not a caption element, then this is a parse error.
                    if (currentNode is not Element { localName: "caption" }) {
                        AddParseError("in-caption-current-node-not-caption");
                    }
                    // Pop elements from this stack until a caption element has been popped from the stack.
                    while (stackOfOpenElements.Pop() is not Element { localName: "caption" }) { }
                    // Clear the list of active formatting elements up to the last marker.
                    ClearTheListOfACtiveFormattingElementsUpToTheLastMarker();
                    // Switch the insertion mode to "in table".
                    insertionMode = InsertionMode.InTable;
                    // Reprocess the token.
                    ReprocessTheToken();
                }
                break;
            case EndTag { name: "body" or "col" or "colgroup" or "html" or "tbody" or "td" or "td" or "tfoot" or "th" or "thead" or "tr" }:
                // Parse error. Ignore the token.
                AddParseError("in-caption-unexpected-end-token-ignored");
                break;
            default:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-incolgroup

    private void InsertionModeInColumnGroup() {
        switch (token) {
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' } cToken:
                InsertACharacter(cToken);
                break;
            case Tokenizer.Comment comment:
                // Insert a comment.
                InsertAComment(comment);
                break;
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("in-column-group-unexpected-doctype-ignored");
                break;
            case StartTag { name: "html" }:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case StartTag { name: "col" } tagToken:
                // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
                InsertAnHTMLElement(tagToken);
                stackOfOpenElements.Pop();
                // Acknowledge the token's self-closing flag, if it is set.
                // todo
                break;
            case EndTag { name: "colgroup" }:
                // If the current node is not a colgroup element, then this is a parse error; ignore the token.
                if (currentNode is not Element { localName: "colgroup" }) {
                    AddParseError("in-column-group-unexpected-end-tag-ignored");
                } else {
                    // Otherwise, pop the current node from the stack of open elements. Switch the insertion mode to "in table".
                    stackOfOpenElements.Pop();
                    insertionMode = InsertionMode.InTable;
                }
                break;
            case EndTag { name: "col" }:
                // Parse error. Ignore the token.
                AddParseError("in-column-group-unexpected-col-end-token-ignored");
                break;
            case StartTag { name: "template" }:
            case EndTag { name: "template" }:
                // Process the token using the rules for the "in head" insertion mode.
                InsertionModeInHead();
                break;
            case EndOfFile:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            default:
                // If the current node is not a colgroup element, then this is a parse error; ignore the token.
                if (currentNode is not Element { localName: "colgroup" }) {
                    AddParseError("InColumnGroup - default");
                } else {
                    // Otherwise, pop the current node from the stack of open elements.
                    stackOfOpenElements.Pop();
                    // Switch the insertion mode to "in table".
                    insertionMode = InsertionMode.InTable;
                    // Reprocess the token.
                    ReprocessTheToken();
                }
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-intbody

    private void InsertionModeInTableBody() {
        // https://html.spec.whatwg.org/multipage/parsing.html#clear-the-stack-back-to-a-table-body-context
        var ClearTheStackBackToTableContext = () => {
            while (!((ReadOnlySpan<string>)["tbody", "tfoot", "thead", "template", "html"]).Contains(stackOfOpenElements.Peek().localName)) {
                stackOfOpenElements.Pop();
            }
        };

        switch (token) {
            case StartTag { name: "tr" } tagToken:
                // Clear the stack back to a table body context. (See below.)
                ClearTheStackBackToTableContext();
                // Insert an HTML element for the token, then switch the insertion mode to "in row".
                InsertAnHTMLElement(tagToken);
                insertionMode = InsertionMode.InRow;
                break;
            case StartTag { name: "th" or "td" }:
                AddParseError("unexpected-cell-in-table-body");
                // Clear the stack back to a table body context. (See below.)
                ClearTheStackBackToTableContext();
                // Insert an HTML element for a "tr" start tag token with no attributes, then switch the insertion mode to "in row".
                InsertAnHTMLElement(new StartTag("tr"));
                insertionMode = InsertionMode.InRow;
                // Reprocess the current token.
                ReprocessTheToken();
                break;
            case EndTag { name: "tbody" or "tfoot" or "thead" } tagToken:
                // If the stack of open elements does not have an element in table scope that is an HTML element with the same tag name as the token, this is a parse error; ignore the token.
                if (!HasAElementInTableScope(tagToken.name)) {
                    AddParseError("in-table-body-unexpected-end-tag");
                } else {
                    // Otherwise:
                    // 1. Clear the stack back to a table body context. (See below.)
                    ClearTheStackBackToTableContext();
                    // 2. Pop the current node from the stack of open elements. Switch the insertion mode to "in table".
                    stackOfOpenElements.Pop();
                    insertionMode = InsertionMode.InTable;
                }
                break;
            case StartTag { name: "caption" or "col" or "colgroup" or "tbody" or "tfood" or "thead" }:
            case EndTag { name: "table" }:
                // If the stack of open elements does not have a tbody, thead, or tfoot element in table scope, this is a parse error; ignore the token.
                if (!HasAElementInTableScope("tbody", "thead", "tfoot")) {
                    AddParseError("TBODY, THEAD, TFoot");
                } else {
                    // Otherwise:
                    // 1. Clear the stack back to a table body context. (See below.)
                    ClearTheStackBackToTableContext();
                    // 2. Pop the current node from the stack of open elements. Switch the insertion mode to "in table".
                    stackOfOpenElements.Pop();
                    insertionMode = InsertionMode.InTable;
                    // 3. Reprocess the token.
                    ReprocessTheToken();
                }
                break;
            case EndTag { name: "body" or "caption" or "col" or "colgroup" or "html" or "td" or "th" or "tr" }:
                // Parse error. Ignore the token.
                AddParseError("in-table-body-unexpected-end-tag-ignored");
                break;
            default:
                // Process the token using the rules for the "in table" insertion mode.
                InsertionModeInTable();
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-intr

    private void InsertionModeInRow() {
        var ClearTheStackBackToTableRowContext = () => {
            while (!((ReadOnlySpan<string>)["tr", "template", "html"]).Contains(stackOfOpenElements.Peek().localName)) {
                stackOfOpenElements.Pop();
            }
        };
        switch (token) {
            case StartTag { name: "th" or "td" } tagToken:
                // Clear the stack back to a table row context. (See below.)
                ClearTheStackBackToTableRowContext();
                // Insert an HTML element for the token, then switch the insertion mode to "in cell".
                InsertAnHTMLElement(tagToken);
                insertionMode = InsertionMode.InCell;
                // Insert a marker at the end of the list of active formatting elements.
                ListOfActiveFormattingElements.Add(null);
                break;
            case EndTag { name: "tr" }:
                // If the stack of open elements does not have a tr element in table scope, this is a parse error; ignore the token.
                if (!HasAElementInTableScope("tr")) {
                    AddParseError("IN ROW - TR - table scope");
                    break;
                }
                // Otherwise:

                // 1. Clear the stack back to a table row context. (See below.)
                ClearTheStackBackToTableRowContext();
                // 2. Pop the current node (which will be a tr element) from the stack of open elements. Switch the insertion mode to "in table body".
                stackOfOpenElements.Pop();
                insertionMode = InsertionMode.InTableBody;
                break;
            case StartTag { name: "caption" or "col" or "colgroup" or "tbody" or "tfoot" or "thead" or "tr" }:
            case EndTag { name: "table" }:
                // If the stack of open elements does not have a tr element in table scope, this is a parse error; ignore the token.
                if (!HasAElementInTableScope("tr")) {
                    AddParseError("TR");
                } else {
                    // Otherwise:
                    // 1. Clear the stack back to a table row context. (See below.)
                    ClearTheStackBackToTableRowContext();
                    // 2. Pop the current node (which will be a tr element) from the stack of open elements. Switch the insertion mode to "in table body".
                    stackOfOpenElements.Pop();
                    insertionMode = InsertionMode.InTableBody;
                    // 3. Reprocess the token.
                    ReprocessTheToken();
                }
                break;
            case EndTag { name: "tbody" or "tfoot" or "thead" } tagToken:
                // If the stack of open elements does not have an element in table scope that is an HTML element with the same tag name as the token, this is a parse error; ignore the token.
                if (!HasAElementInTableScope(tagToken.name)) {
                    AddParseError("in-row-tbody-not-in-table-scope");
                    break;
                }
                // If the stack of open elements does not have a tr element in table scope, ignore the token.
                if (!HasAElementInTableScope("tr")) {
                    AddParseError("in-row-tr-missing");
                    break;
                }
                // Otherwise:
                // Clear the stack back to a table row context. (See below.)
                ClearTheStackBackToTableRowContext();
                // Pop the current node (which will be a tr element) from the stack of open elements. Switch the insertion mode to "in table body".
                stackOfOpenElements.Pop();
                insertionMode = InsertionMode.InTableBody;
                // Reprocess the token.
                ReprocessTheToken();
                break;
            case EndTag { name: "body" or "caption" or "col" or "colgroup" or "html" or "td" or "th" } tagToken:
                // If the stack of open elements does not have an element in table scope that is an HTML element with the same tag name as the token, this is a parse error; ignore the token.
                if (!HasAElementInTableScope(tagToken.name)) {
                    AddParseError("in-row-end-tag-body");
                    break; // ignore token
                }
                // If the stack of open elements does not have a tr element in table scope, ignore the token.
                if (!HasAElementInTableScope("tr")) {
                    break; // ignore token
                }
                // Otherwise:
                // 1. Clear the stack back to a table row context. (See below.)
                ClearTheStackBackToTableRowContext();
                // 2. Pop the current node (which will be a tr element) from the stack of open elements. Switch the insertion mode to "in table body".
                stackOfOpenElements.Pop();
                insertionMode = InsertionMode.InTableBody;
                // 3. Reprocess the token.
                ReprocessTheToken();
                break;
            default:
                InsertionModeInTable();
                break;

        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-intd

    private void InsertionModeInCell() {
        var CloseTheCell = () => {

            // Generate implied end tags.
            GenerateImpliedEndTags();
            // If the current node is not now a td element or a th element, then this is a parse error.
            if (currentNode is not Element { localName: "td" or "th" }) {
                AddParseError("unexpected-cell-end-tag");
            }
            // Pop elements from the stack of open elements until a td element or a th element has been popped from the stack.
            while (true) {
                var element = stackOfOpenElements.Pop();
                if (element.localName is "td" or "th") break;
            }
            // Clear the list of active formatting elements up to the last marker.
            ClearTheListOfACtiveFormattingElementsUpToTheLastMarker();
            // Switch the insertion mode to "in row".
            insertionMode = InsertionMode.InRow;
        };
        switch (token) {
            case EndTag { name: "td" or "th" } tagToken:
                // If the stack of open elements does not have an element in table scope that is an HTML element with the same tag name as that of the token, then this is a parse error; ignore the token.
                if (!HasAElementInTableScope(tagToken.name)) {
                    AddParseError("IN CELL - not in table scope");
                } else {
                    // Otherwise:
                    // 1. Generate implied end tags.
                    GenerateImpliedEndTags();
                    // 2. Now, if the current node is not an HTML element with the same tag name as the token, then this is a parse error.
                    if (currentNode.localName != tagToken.name) {
                        AddParseError("IN CELL - Otherwise not same");
                    }
                    // 3. Pop elements from the stack of open elements until an HTML element with the same tag name as the token has been popped from the stack.
                    while (true) {
                        if (stackOfOpenElements.Pop().localName == tagToken.name) break;
                    }
                    // 4. Clear the list of active formatting elements up to the last marker.
                    ClearTheListOfACtiveFormattingElementsUpToTheLastMarker();
                    // 5. Switch the insertion mode to "in row".
                    insertionMode = InsertionMode.InRow;
                }
                break;
            case StartTag { name: "caption" or "col" or "colgroup" or "tbody" or "td" or "tfoot" or "th" or "thead" or "tr" }:
                // Assert: The stack of open elements has a td or th element in table scope.
                // todo
                // Close the cell (see below) and reprocess the token.
                CloseTheCell();
                ReprocessTheToken();
                break;
            case EndTag { name: "body" or "caption" or "col" or "colgroup" or "html" }:
                // Parse error. Ignore the token.
                AddParseError("in-cell-unexpected-end-token-ignored");
                break;
            case EndTag { name: "table" or "tbody" or "tfoot" or "thead" or "tr" } tagToken:
                // If the stack of open elements does not have an element in table scope that is an HTML element with the same tag name as that of the token, then this is a parse error; ignore the token.
                if (!HasAElementInTableScope(tagToken.name)) {
                    AddParseError("in-cell-unexpected-end-tag-ignored");
                } else {
                    // Otherwise, close the cell (see below) and reprocess the token.
                    CloseTheCell();
                    ReprocessTheToken();
                }
                break;
            default:
                InsertionModeInBody();
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inselect

    private void InsertionModeInSelect() {
        switch (token) {
            case Character { data: '\0' }:
                // Parse error. Ignore the token.
                AddParseError("in-select-null-character-ignored");
                break;
            case Character cToken:
                InsertACharacter(cToken);
                break;
            case Tokenizer.Comment comment:
                // Insert a comment.
                InsertAComment(comment);
                break;
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("in-select-unexpected-doctype-ignored");
                break;
            case StartTag { name: "html" }:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case StartTag { name: "option" } tagToken:
                // If the current node is an option element, pop that node from the stack of open elements.
                if (currentNode is Element { localName: "option" }) {
                    stackOfOpenElements.Pop();
                }
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                break;
            case StartTag { name: "optgroup" } tagToken:
                // If the current node is an option element, pop that node from the stack of open elements.
                if (currentNode is Element { localName: "option" }) stackOfOpenElements.Pop();
                // If the current node is an optgroup element, pop that node from the stack of open elements.
                if (currentNode is Element { localName: "optgroup" }) stackOfOpenElements.Pop();
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                break;
            case StartTag { name: "hr" } tagToken:
                // If the current node is an option element, pop that node from the stack of open elements.
                if (currentNode is Element { localName: "option" }) stackOfOpenElements.Pop();
                // If the current node is an optgroup element, pop that node from the stack of open elements.
                if (currentNode is Element { localName: "optgroup" }) stackOfOpenElements.Pop();
                // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
                InsertAnHTMLElement(tagToken);
                stackOfOpenElements.Pop();
                // Acknowledge the token's self-closing flag, if it is set.
                // todo
                break;
            case EndTag { name: "optgroup" }:
                // First, if the current node is an option element, and the node immediately before it in the stack of open elements is an optgroup element, then pop the current node from the stack of open elements.
                if (currentNode is Element { localName: "option" } && stackOfOpenElements[^2] is Element { localName: "optgroup" }) stackOfOpenElements.Pop();
                // If the current node is an optgroup element, then pop that node from the stack of open elements. Otherwise, this is a parse error; ignore the token.
                if (currentNode is Element { localName: "optgroup" }) {
                    stackOfOpenElements.Pop();
                } else {
                    AddParseError("in-select-unexpected-optgroup-endtag");
                }
                break;
            case EndTag { name: "option" }:
                // If the current node is an option element, then pop that node from the stack of open elements. Otherwise, this is a parse error; ignore the token.
                if (currentNode is Element { localName: "option" }) {
                    stackOfOpenElements.Pop();
                } else {
                    AddParseError("IN SELECT - END OPTION");
                }
                break;
            case EndTag { name: "select" }:
                // If the stack of open elements does not have a select element in select scope, this is a parse error; ignore the token. (fragment case)
                if (!HasAElementInSelectScope("select")) {
                    AddParseError("IN SELECT: no select in scope");
                } else {
                    // Otherwise:
                    // Pop elements from the stack of open elements until a select element has been popped from the stack.
                    while (true) {
                        if (stackOfOpenElements.Pop() is Element { localName: "select" }) {
                            break;
                        }
                    }
                    // Reset the insertion mode appropriately.
                    ResetTheInsertionModeAppropriately();
                }
                break;

            case StartTag { name: "select" }:
                // Parse error.
                AddParseError("IN SELECT: select in select");
                // If the stack of open elements does not have a select element in select scope, ignore the token. (fragment case)
                if (!HasAElementInSelectScope("select")) {

                } else {
                    // Otherwise:
                    // 1. Pop elements from the stack of open elements until a select element has been popped from the stack.
                    while (true) {
                        if (stackOfOpenElements.Pop() is Element { localName: "select" }) {
                            break;
                        }
                    }
                    // 2. Reset the insertion mode appropriately.
                    ResetTheInsertionModeAppropriately();
                    // 3. NOTE: It just gets treated like an end tag.
                }
                break;
            case StartTag { name: "input" or "keygen" or "textarea" }:
                // Parse error.
                AddParseError("in-select-unexpected-start-tag");
                // If the stack of open elements does not have a select element in select scope, ignore the token. (fragment case)
                if (!HasAElementInSelectScope("select")) {
                    // ignore token
                } else {
                    // Otherwise:
                    // Pop elements from the stack of open elements until a select element has been popped from the stack.
                    while (stackOfOpenElements.Pop() is not Element { localName: "select" }) { }
                    // Reset the insertion mode appropriately.
                    ResetTheInsertionModeAppropriately();
                    // Reprocess the token.
                    ReprocessTheToken();
                }
                break;
            case StartTag { name: "script" or "template" }:
            case EndTag { name: "template" }:
                // Process the token using the rules for the "in head" insertion mode.
                InsertionModeInHead();
                break;
            case EndOfFile:
                InsertionModeInBody();
                break;
            default:
                AddParseError("IN SELECT: default");
                // ignore the token;
                break;

        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inselectintable

    private void InsertionModeInSelectInTable() {
        switch (token) {
            case StartTag { name: "caption" or "table" or "tbody" or "tfoot" or "thead" or "tr" or "td" or "th" }:
                // Parse error.
                AddParseError("select-in-table-unexpected-start-tag");
                // Pop elements from the stack of open elements until a select element has been popped from the stack.
                while (stackOfOpenElements.Pop() is not Element { localName: "select" }) { }
                // Reset the insertion mode appropriately.
                ResetTheInsertionModeAppropriately();
                // Reprocess the token.
                ReprocessTheToken();
                break;
            case EndTag { name: "caption" or "table" or "tbody" or "tfoot" or "thead" or "tr" or "td" or "th" } tagToken:
                // Parse error.
                AddParseError("in-select-in-table-unexpected-end-tag");
                // If the stack of open elements does not have an element in table scope that is an HTML element with the same tag name as that of the token, then ignore the token.
                if (!HasAElementInTableScope(tagToken.name)) {
                    break;
                } else {
                    // Otherwise:
                    // 1. Pop elements from the stack of open elements until a select element has been popped from the stack.
                    while (stackOfOpenElements.Pop() is not Element { localName: "select" }) { }
                    // 2. Reset the insertion mode appropriately.
                    ResetTheInsertionModeAppropriately();
                    // 3. Reprocess the token.
                    ReprocessTheToken();
                }
                break;
            default:
                // Parse error. Ignore the token.
                AddParseError("in-select-unexpected-token-ignored");
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-intemplate
    private void InsertionModeInTemplate() {
        switch (token) {
            case Character:
            case Tokenizer.Comment:
            case DOCTYPE:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case StartTag { name: "base" or "basefont" or "bgsound" or "link" or "meta" or "noframes" or "script" or "style" or "template" or "title" }:
            case EndTag { name: "template" }:
                // Process the token using the rules for the "in head" insertion mode.
                InsertionModeInHead();
                break;
            case StartTag { name: "caption" or "colgroup" or "tbody" or "tfoot" or "thead" }:
                // Pop the current template insertion mode off the stack of template insertion modes.
                stackOfTemplateInsertionModes.Pop();
                // Push "in table" onto the stack of template insertion modes so that it is the new current template insertion mode.
                stackOfTemplateInsertionModes.Push(InsertionMode.InTable);
                // Switch the insertion mode to "in table", and reprocess the token.   
                insertionMode = InsertionMode.InTable;
                ReprocessTheToken();
                break;
            case StartTag { name: "col" }:
                // Pop the current template insertion mode off the stack of template insertion modes.
                stackOfTemplateInsertionModes.Pop();
                // Push "in column group" onto the stack of template insertion modes so that it is the new current template insertion mode.
                stackOfTemplateInsertionModes.Push(InsertionMode.InColumnGroup);
                // Switch the insertion mode to "in column group", and reprocess the token. 
                insertionMode = InsertionMode.InColumnGroup;
                ReprocessTheToken();
                break;
            case StartTag { name: "tr" }:
                // Pop the current template insertion mode off the stack of template insertion modes.
                stackOfTemplateInsertionModes.Pop();
                // Push "in table body" onto the stack of template insertion modes so that it is the new current template insertion mode.
                stackOfTemplateInsertionModes.Push(InsertionMode.InTableBody);
                // Switch the insertion mode to "in table body", and reprocess the token.
                insertionMode = InsertionMode.InTableBody;
                ReprocessTheToken();
                break;
            case StartTag { name: "td" or "th" }:
                // Pop the current template insertion mode off the stack of template insertion modes.
                stackOfTemplateInsertionModes.Pop();
                // Push "in row" onto the stack of template insertion modes so that it is the new current template insertion mode.
                stackOfTemplateInsertionModes.Push(InsertionMode.InRow);
                // Switch the insertion mode to "in row", and reprocess the token.
                insertionMode = InsertionMode.InRow;
                ReprocessTheToken();
                break;
            case StartTag:
                // Pop the current template insertion mode off the stack of template insertion modes.
                stackOfTemplateInsertionModes.Pop();
                // Push "in body" onto the stack of template insertion modes so that it is the new current template insertion mode.
                stackOfTemplateInsertionModes.Push(InsertionMode.InBody);
                // Switch the insertion mode to "in body", and reprocess the token.
                insertionMode = InsertionMode.InBody;
                ReprocessTheToken();
                break;
            case EndTag:
                // Parse error. Ignore the token.
                AddParseError("in-template-unexpected-end-tag-ignored");
                break;
            case EndOfFile:
                // If there is no template element on the stack of open elements, then stop parsing. (fragment case)
                if (!stackOfOpenElements.Any(item => item.localName == "template")) {
                    StopParsing();
                    break;
                } else {
                    // Otherwise, this is a parse error.
                    AddParseError("in-template-eof");
                    // Pop elements from the stack of open elements until a template element has been popped from the stack.
                    while (stackOfOpenElements.Pop() is not Element { localName: "template" }) { }
                    // Clear the list of active formatting elements up to the last marker.
                    ClearTheListOfACtiveFormattingElementsUpToTheLastMarker();
                    // Pop the current template insertion mode off the stack of template insertion modes.
                    stackOfTemplateInsertionModes.Pop();
                    // Reset the insertion mode appropriately.
                    ResetTheInsertionModeAppropriately();
                    // Reprocess the token.
                    ReprocessTheToken();
                }
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-afterbody

    private void InsertionModeAfterBody() {
        switch (token) {
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case Tokenizer.Comment comment:
                // Insert a comment as the last child of the first element in the stack of open elements (the html element).
                InsertAComment(comment, (stackOfOpenElements[0], stackOfOpenElements[0].childNodes.Count));
                break;
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("after-body-unexpected-doctype-ignored");
                break;
            case StartTag { name: "html" }:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case EndTag { name: "html" }:
                // If the parser was created as part of the HTML fragment parsing algorithm, this is a parse error; ignore the token. (fragment case)
                // todo parse error
                // Otherwise, switch the insertion mode to "after after body".
                insertionMode = InsertionMode.AfterAfterBody;
                break;
            case EndOfFile:
                StopParsing();
                break;
            default:
                // Parse error. Switch the insertion mode to "in body" and reprocess the token.
                AddParseError("After Body - unexpected tag");
                insertionMode = InsertionMode.InBody;
                ReprocessTheToken();
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-inframeset
    private void InsertionModeInFrameset() {
        switch (token) {
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' } cToken:
                // Insert the character.
                InsertACharacter(cToken);
                break;
            case Tokenizer.Comment comment:
                // Insert a comment.
                InsertAComment(comment);
                break;
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("in-frameset-unexpected-doctype");
                break;
            case StartTag { name: "html" }:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case StartTag { name: "frameset" } tagToken:
                // Insert an HTML element for the token.
                InsertAnHTMLElement(tagToken);
                break;
            case EndTag { name: "frameset" }:
                // If the current node is the root html element, then this is a parse error; ignore the token. (fragment case)
                if (currentNode.localName == "html") {
                    AddParseError("In frameset root html");
                } else {
                    // Otherwise, pop the current node from the stack of open elements.
                    stackOfOpenElements.Pop();
                }
                // If the parser was not created as part of the HTML fragment parsing algorithm (fragment case), and the current node is no longer a frameset element, then switch the insertion mode to "after frameset".
                if (currentNode.localName != "frameset") {
                    // todo add fragment case 
                    insertionMode = InsertionMode.AfterFrameset;
                }
                break;
            case StartTag { name: "frame" } tagToken:
                // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
                InsertAnHTMLElement(tagToken);
                stackOfOpenElements.Pop();
                // Acknowledge the token's self-closing flag, if it is set.
                // todo
                break;
            case StartTag { name: "noframes" }:
                // Process the token using the rules for the "in head" insertion mode.
                InsertionModeInHead();
                break;
            case EndOfFile:
                // If the current node is not the root html element, then this is a parse error.
                if (currentNode.localName != "html") {
                    AddParseError("in-frameset-eof-not-html");
                }
                // Note: The current node can only be the root html element in the fragment case.
                // Stop parsing.
                StopParsing();
                break;
            default:
                // Parse error. Ignore the token.
                AddParseError("in-frameset-unexpected-token");
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#parsing-main-afterframeset
    private void InsertionModeAfterFrameset() {
        switch (token) {
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' } cToken:
                // Insert the character.
                InsertACharacter(cToken);
                break;
            case Tokenizer.Comment comment:
                // Insert a comment.
                InsertAComment(comment);
                break;
            case DOCTYPE:
                // Parse error. Ignore the token.
                AddParseError("after-frameset-unexpected-doctype-ignored");
                break;
            case StartTag { name: "html" }:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case EndTag { name: "html" }:
                // Switch the insertion mode to "after after frameset".
                insertionMode = InsertionMode.AfterAfterFrameset;
                break;
            case StartTag { name: "noframes" }:
                // Process the token using the rules for the "in head" insertion mode.
                InsertionModeInHead();
                break;
            case EndOfFile:
                // Stop parsing.
                StopParsing();
                break;
            default:
                // Parse error. Ignore the token.
                AddParseError("after-frameset-unexpected-token");
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#the-after-after-body-insertion-mode

    private void InsertionModeAfterAfterBody() {
        switch (token) {
            case Tokenizer.Comment comment:
                // Insert a comment as the last child of the Document object.
                InsertAComment(comment, (document, document.childNodes.Count));
                break;
            case DOCTYPE:
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
            case StartTag { name: "html" }:
                InsertionModeInBody();
                break;
            case EndOfFile:
                StopParsing();
                break;
            default:
                //Parse error. Switch the insertion mode to "in body" and reprocess the token.
                AddParseError("expected-eof-but-got-start-tag");
                insertionMode = InsertionMode.InBody;
                ReprocessTheToken();
                break;
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#the-after-after-frameset-insertion-mode
    private void InsertionModeAfterAfterFrameset() {
        switch (token) {
            case Tokenizer.Comment comment:
                // Insert a comment as the last child of the Document object.
                InsertAComment(comment, (document, document.childNodes.Count));
                break;
            case DOCTYPE:
            case Character { data: '\t' or '\n' or '\f' or '\r' or ' ' }:
            case StartTag { name: "html" }:
                // Process the token using the rules for the "in body" insertion mode.
                InsertionModeInBody();
                break;
            case EndOfFile:
                StopParsing();
                break;
            case StartTag { name: "noframes" }:
                // Process the token using the rules for the "in head" insertion mode.
                InsertionModeInHead();
                break;
            default:
                // Parse error. Ignore the token.
                AddParseError("after-after-frameset-unexpected-token-ignored");
                break;
        }
    }

    // 13.2.4.2 The stack of open elements
    private static readonly string[] specialListElements = ["address", "applet", "area", "article", "aside", "base", "basefont", "bgsound", "blockquote", "body", "br", "button", "caption", "center", "col", "colgroup",
        "dd", "details", "dir", "div", "dl", "dt", "embed", "fieldset", "figcaption", "figure", "footer", "form", "frame", "frameset", "h1", "h2", "h3", "h4", "h5", "h6",
         "head", "header", "hgroup", "hr", "html", "iframe", "img", "input", "keygen", "li", "link", "listing", "main", "marquee", "menu", "meta", "nav", "noembed", "noframes", "noscript", "object", "ol", "p",
          "param", "plaintext", "pre", "script", "search", "section", "select", "source", "style", "summary", "table", "tbody", "td", "template", "textarea", "tfoot", "th", "thead", "title", "tr",
        "track", "ul", "wbr", "xmp"]; // todo MathML mi, MathML mo, MathML mn, MathML ms, MathML mtext, and MathML annotation-xml; and SVG foreignObject, SVG desc, and SVG title.];

    // 13.2.4.1 The insertion mode
    // https://html.spec.whatwg.org/multipage/parsing.html#reset-the-insertion-mode-appropriately
    private void ResetTheInsertionModeAppropriately() {
        // 1. Let last be false.
        var last = false;
        // 2. Let node be the last node in the stack of open elements.
        var index = stackOfOpenElements.Count - 1;
        var node = stackOfOpenElements[index];
        // 3. Loop: If node is the first node in the stack of open elements, then set last to true, and, if the parser was created as part of the HTML fragment parsing algorithm (fragment case), set node to the context element passed to that algorithm.
        while (true) {
            if (index == 0) {
                last = true;
                // todo
            }
            switch (node) {
                // 4. If node is a select element, run these substeps:
                case Element { localName: "select" }:
                    // 1. If last is true, jump to the step below labeled done.
                    // todo
                    // 2. Let ancestor be node.
                    // todo
                    // 3. Loop: If ancestor is the first node in the stack of open elements, jump to the step below labeled done.
                    // todo
                    // 4. Let ancestor be the node before ancestor in the stack of open elements.
                    // todo
                    // 5. If ancestor is a template node, jump to the step below labeled done.
                    // todo
                    // 6. If ancestor is a table node, switch the insertion mode to "in select in table" and return.
                    // todo
                    // 7. Jump back to the step labeled loop.
                    // todo
                    // 8. Done: Switch the insertion mode to "in select" and return.
                    // todo
                    break;

                // 5. If node is a td or th element and last is false, then switch the insertion mode to "in cell" and return.
                case Element { localName: "td" or "th" }:
                    insertionMode = InsertionMode.InCell;
                    return;
                // 6. If node is a tr element, then switch the insertion mode to "in row" and return.
                case Element { localName: "tr" }:
                    insertionMode = InsertionMode.InRow;
                    return;
                // 7. If node is a tbody, thead, or tfoot element, then switch the insertion mode to "in table body" and return.
                case Element { localName: "tbody" or "thead" or "tfoot" }:
                    insertionMode = InsertionMode.InTableBody;
                    return;
                // 8. If node is a caption element, then switch the insertion mode to "in caption" and return.
                case Element { localName: "caption" }:
                    insertionMode = InsertionMode.InCaption;
                    return;
                // 9. If node is a colgroup element, then switch the insertion mode to "in column group" and return.
                case Element { localName: "colgroup" }:
                    insertionMode = InsertionMode.InColumnGroup;
                    return;
                // 10. If node is a table element, then switch the insertion mode to "in table" and return.
                case Element { localName: "table" }:
                    insertionMode = InsertionMode.InTable;
                    return;
                // 11. If node is a template element, then switch the insertion mode to the current template insertion mode and return.
                case Element { localName: "template" }:
                    insertionMode = stackOfTemplateInsertionModes[^1];
                    break;
                // 12. If node is a head element and last is false, then switch the insertion mode to "in head" and return.
                case Element { localName: "head" } when last is false:
                    insertionMode = InsertionMode.InColumnGroup;
                    return;
                // 13. If node is a body element, then switch the insertion mode to "in body" and return.
                case Element { localName: "body" }:
                    insertionMode = InsertionMode.InBody;
                    return;
                // 14. If node is a frameset element, then switch the insertion mode to "in frameset" and return. (fragment case)
                case Element { localName: "frameset" }:
                    throw new NotImplementedException();
                // 15. If node is an html element, run these substeps:
                case Element { localName: "html" }:
                    // 1. If the head element pointer is null, switch the insertion mode to "before head" and return. (fragment case)
                    if (headElementPointer is null) {
                        insertionMode = InsertionMode.BeforeHead;
                        return;
                    } else {
                        // 2. Otherwise, the head element pointer is not null, switch the insertion mode to "after head" and return.
                        insertionMode = InsertionMode.AfterHead;
                        return;
                    }
            }
            // 16. If last is true, then switch the insertion mode to "in body" and return. (fragment case)
            if (last) {
                throw new NotImplementedException();
            }
            // 17. Let node now be the node before node in the stack of open elements.
            node = stackOfOpenElements[--index];
            // 18. Return to the step labeled loop.
        }
    }

    // 13.2.4.3
    // https://html.spec.whatwg.org/multipage/parsing.html#push-onto-the-list-of-active-formatting-elements
    private void PushOntoTheListOfActiveFormattingElements(Element element) {
        // If there are already three elements in the list of active formatting elements after the last marker, if any, or anywhere in the list if there are no markers, that have the same tag name, namespace, and attributes as element, then remove the earliest such element from the list of active formatting elements. For these purposes, the attributes must be compared as they were when the elements were created by the parser; two elements have the same attributes if all their parsed attributes can be paired such that the two attributes in each pair have identical names, namespaces, and values (the order of the attributes does not matter).
        // todo
        // Add element to the list of active formatting elements.
        ListOfActiveFormattingElements.Add(element);
    }

    // 13.2.4.3
    // https://html.spec.whatwg.org/multipage/parsing.html#reconstruct-the-active-formatting-elements
    private void ReconstructTheActiveFormattingElements() {
        // 1. If there are no entries in the list of active formatting elements, then there is nothing to reconstruct; stop this algorithm.
        if (ListOfActiveFormattingElements.Count == 0) return;
        // 2. If the last (most recently added) entry in the list of active formatting elements is a marker, or if it is an element that is in the stack of open elements, then there is nothing to reconstruct; stop this algorithm.        
        if (ListOfActiveFormattingElements[^1] == null || stackOfOpenElements.Contains(ListOfActiveFormattingElements[^1]!)) return;
        // 3. Let entry be the last (most recently added) element in the list of active formatting elements.
        var index = ListOfActiveFormattingElements.Count - 1;
        var entry = ListOfActiveFormattingElements[index];
    // 4. Rewind: If there are no entries before entry in the list of active formatting elements, then jump to the step labeled create.
    rewind:
        if (index == 0)
            goto create;
        // 5. Let entry be the entry one earlier than entry in the list of active formatting elements.
        entry = ListOfActiveFormattingElements[--index];
        // 6. If entry is neither a marker nor an element that is also in the stack of open elements, go to the step labeled rewind.
        if (entry != null && !stackOfOpenElements.Contains(entry)) {
            goto rewind;
        }
    // 7. Advance: Let entry be the element one later than entry in the list of active formatting elements.
    advance:
        entry = ListOfActiveFormattingElements[++index];
    // 8. Create: Insert an HTML element for the token for which the element entry was created, to obtain new element.
    create:
        var elem = InsertAnHTMLElement(new StartTag(entry.localName)); // todo this is wrong we should have the token in the list of elements
        // 9. Replace the entry for entry in the list with an entry for new element.
        ListOfActiveFormattingElements[index] = elem;
        // 10. If the entry for new element in the list of active formatting elements is not the last entry in the list, return to the step labeled advance.
        if (index != ListOfActiveFormattingElements.Count - 1)
            goto advance;
        // This has the effect of reopening all the formatting elements that were opened in the current body, cell, or caption (whichever is youngest) that haven't been explicitly closed.
        // NOTE: The way this specification is written, the list of active formatting elements always consists of elements in chronological order with the least recently added element first and the most recently added element last (except for while steps 7 to 10 of the above algorithm are being executed, of course).
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#generate-implied-end-tags
    private void GenerateImpliedEndTags(string? except = null) {
        // When the steps below require the UA to generate implied end tags, then, while the current node is a dd element, a dt element, an li element, an optgroup element,
        // an option element, a p element, an rb element, an rp element, an rt element, or an rtc element, the UA must pop the current node off the stack of open elements.
        List<string> list = ["dd", "dt", "li", "optgroup", "option", "p", "rb", "rt", "rtc"];
        if (except != null) {
            list.Remove(except);
        }
        while (true) {
            if (list.Contains(stackOfOpenElements.Peek().localName)) {
                stackOfOpenElements.Pop();
            } else {
                return;
            }
        }
    }

    // 13.2.7 the End
    // https://html.spec.whatwg.org/multipage/parsing.html#stop-parsing
    private void StopParsing() {
        finishedParsing = true;
        //todo     
    }


    // https://html.spec.whatwg.org/multipage/parsing.html#has-an-element-in-the-specific-scope
    private bool HasAnElementInTheSpecificScope(ReadOnlySpan<string> elementsName, List<string> list) {
        // 1. initialize node to be the current node (the bottommost node of the stack).
        var index = stackOfOpenElements.Count - 1;
        var node = stackOfOpenElements[index];
        while (true) {
            // 2. If node is target node, terminate in a match state.
            if (elementsName.Contains(node.localName)) {
                return true;
            }
            // 3. Otherwise, if node is one of the element types in list, terminate in a failure state.
            if (list.Contains(node.localName)) {
                return false;
            }
            // 4. Otherwise, set node to the previous entry in the stack of open elements and return to step 2.
            node = stackOfOpenElements[--index];
            //    (This will never fail, since the loop will always terminate in the previous step if the top of the stack — an html element — is reached.)
        }
        throw new InvalidOperationException();
    }
    // https://html.spec.whatwg.org/multipage/parsing.html#has-an-element-in-scope
    private static List<string> ElementInScopeSpecialList = ["applet", "caption", "html", "table", "td", "th", "marquee", "object", "template"];
    // todo these elements should also be part of the list: They have a different namespace: "MathML mi" , "MathML mo" , "MathML mn" , "MathML ms" , "MathML mtext" , "MathML annotation-xml" , "SVG foreignObject" , "SVG desc" , "SVG title"

    //https://html.spec.whatwg.org/multipage/parsing.html#has-an-element-in-scope
    private bool HasAElementInScope(params ReadOnlySpan<string> elementsName) {
        return HasAnElementInTheSpecificScope(elementsName, ElementInScopeSpecialList);
    }
    // https://html.spec.whatwg.org/multipage/parsing.html#has-an-element-in-list-item-scope
    private bool HasAElementInListItemScope(params ReadOnlySpan<string> elementsName) {
        return HasAnElementInTheSpecificScope(elementsName, ["ol", "ul", .. ElementInScopeSpecialList]);
    }
    // https://html.spec.whatwg.org/multipage/parsing.html#has-an-element-in-button-scope
    private bool HasAElementInButtonScope(params ReadOnlySpan<string> elementsName) {
        return HasAnElementInTheSpecificScope(elementsName, ["button", .. ElementInScopeSpecialList]);
    }
    // https://html.spec.whatwg.org/multipage/parsing.html#has-an-element-in-table-scope
    private bool HasAElementInTableScope(params ReadOnlySpan<string> elementsName) {
        return HasAnElementInTheSpecificScope(elementsName, ["html", "table", "template"]);
    }
    // https://html.spec.whatwg.org/multipage/parsing.html#has-an-element-in-select-scope
    private bool HasAElementInSelectScope(params ReadOnlySpan<string> elementsName) {
        List<string> list = ["optgroup", "option"];
        // 1. initialize node to be the current node (the bottommost node of the stack).
        var index = stackOfOpenElements.Count - 1;
        var node = stackOfOpenElements[index];
        while (true) {
            // 2. If node is target node, terminate in a match state.
            if (elementsName.Contains(node.localName)) {
                return true;
            }
            // 3. Otherwise, if node is one of the element types in list, terminate in a failure state.
            // NOTE: this is special for select: consisting of all element types EXCEPT the following:
            if (!list.Contains(node.localName)) {
                return false;
            }
            // 4. Otherwise, set node to the previous entry in the stack of open elements and return to step 2.
            node = stackOfOpenElements[--index];
            //    (This will never fail, since the loop will always terminate in the previous step if the top of the stack — an html element — is reached.)
        }
        throw new InvalidOperationException();
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-a-character
    private void InsertACharacter(Character token) {
        // Let the adjusted insertion location be the appropriate place for inserting a node.
        var adjustedInsertionLocation = AppropriatePlaceForInsertingANode();
        // If the adjusted insertion location is in a Document node, then return.
        if (adjustedInsertionLocation.elem is Document) {
            return;
        }
        // If there is a Text node immediately before the adjusted insertion location, then append data to that Text node's data.
        if (adjustedInsertionLocation.elem.childNodes.Count > 0 && adjustedInsertionLocation.elem.childNodes[adjustedInsertionLocation.childPos - 1] is Text lastChild) {
            lastChild.data += token.data;
        } else {
            // Otherwise, create a new Text node whose data is data and whose node document is the same as that of the element in which the adjusted insertion location finds itself, and insert the newly created node at the adjusted insertion location.                
            InsertNode(adjustedInsertionLocation, new Text(adjustedInsertionLocation.elem.ownerDocument, token.data.ToString()));
        }
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-a-foreign-element
    private Element InsertAForeignElement(Tag token, string @namespace, bool onlyAddToELementStack) {
        // Let the adjustedInsertionLocation be the appropriate place for inserting a node.
        var adjustedInsertionLocation = AppropriatePlaceForInsertingANode();
        // Let element be the result of creating an element for the token given token, namespace, and the element in which the adjustedInsertionLocation finds itself.
        var element = CreateAnElementForAToken(token, @namespace, adjustedInsertionLocation.elem);
        // If onlyAddToElementStack is false, then run insert an element at the adjusted insertion location with element.
        if (!onlyAddToELementStack) {
            InsertAnElementAtTheAdjustedInsertionLocation(element);
        }
        // Push element onto the stack of open elements so that it is the new current node.
        stackOfOpenElements.Push(element);
        //Return element.
        return element;
    }

    // https://html.spec.whatwg.org/multipage/parsing.html#insert-an-element-at-the-adjusted-insertion-location
    private void InsertAnElementAtTheAdjustedInsertionLocation(Element element) {
        // 1. Let the adjusted insertion location be the appropriate place for inserting a node.
        var adjustedInsertionLocation = AppropriatePlaceForInsertingANode();
        // 2. If it is not possible to insert element at the adjusted insertion location, abort these steps.

        // 3. If the parser was not created as part of the HTML fragment parsing algorithm, then push a new element queue onto element's relevant agent's custom element reactions stack.

        // 4. Insert element at the adjusted insertion location.
        InsertNode(adjustedInsertionLocation, element);
        // 5. If the parser was not created as part of the HTML fragment parsing algorithm, then pop the element queue from element's relevant agent's custom element reactions stack, and invoke custom element reactions in that queue.
        // NOTE: If the adjusted insertion location cannot accept more elements, e.g., because it's a Document that already has an element child, then element is dropped on the floor.
    }
    // https://html.spec.whatwg.org/multipage/parsing.html#insert-an-html-element
    private Element InsertAnHTMLElement(Tag token) {
        // To insert an HTML element given a token token: insert a foreign element given token, the HTML namespace, and false.
        return InsertAForeignElement(token, Namespaces.HTML, false);
    }

    #region 13.2.6.1 Creating and inserting nodes
    // https://html.spec.whatwg.org/multipage/parsing.html#appropriate-place-for-inserting-a-node
    private (Node elem, int childPos) AppropriatePlaceForInsertingANode(Element? overrideTarget = null) {
        // 1. If there was an override target specified, then let target be the override target.
        // Otherwise, let target be the current node.
        var target = overrideTarget ?? currentNode;
        // 2. Determine the adjusted insertion location using the first matching steps from the following list:
        var adjustedInsertionLocation = DetermineTheAdjustedInsertionLocation(target);
        // 3. If the adjusted insertion location is inside a template element, let it instead be inside the template element's template contents, after its last child (if any).
        // todo
        // 4. Return the adjusted insertion location.
        return adjustedInsertionLocation;

        (Node, int) DetermineTheAdjustedInsertionLocation(Element target) {
            // If foster parenting is enabled and target is a table, tbody, tfoot, thead, or tr element
            // NOTE: Foster parenting happens when content is misnested in tables.
            if (fosterParenting && target is Element { localName: "table" or "tbody" or "tfoot" or "thead" or "tr" }) {
                // Run these substeps:
                // 1. Let last template be the last template element in the stack of open elements, if any.
                var lastTemplateIndex = stackOfOpenElements.FindLastIndex((element) => element.localName == "template");
                var lastTempalte = lastTemplateIndex != -1 ? stackOfOpenElements[lastTemplateIndex] : null;
                // 2. Let last table be the last table element in the stack of open elements, if any.
                var lastTableIndex = stackOfOpenElements.FindLastIndex((element) => element.localName == "template");
                var lastTable = lastTableIndex != -1 ? stackOfOpenElements[lastTableIndex] : null;
                // 3. If there is a last template and either there is no last table, or there is one, but last template is lower (more recently added) than last table in the stack of open elements, 
                // then: let adjusted insertion location be inside last template's template contents, after its last child (if any), and abort these steps.
                if (lastTempalte is not null)
                    if (lastTable is null || lastTemplateIndex < lastTableIndex) {
                        return adjustedInsertionLocation = (lastTempalte, lastTempalte.childNodes.Count);
                    }
                // 4. If there is no last table, then let adjusted insertion location be inside the first element in the stack of open elements (the html element),
                // after its last child (if any), and abort these steps. (fragment case)
                if (lastTable is null) {
                    return (stackOfOpenElements[0], stackOfOpenElements[0].childNodes.Count);
                }
                // 5. If last table has a parent node, then let adjusted insertion location be inside last table's parent node, immediately before last table, and abort these steps.
                if (lastTable.parent is not null) {
                    var index = lastTable.parent.childNodes.FindIndex((item) => item == lastTable);
                    return (lastTable.parent, index);
                }
                // 6. Let previous element be the element immediately above last table in the stack of open elements.
                var previousElement = stackOfOpenElements[lastTableIndex - 1];
                // 7. Let adjusted insertion location be inside previous element, after its last child (if any).
                return (previousElement, previousElement.childNodes.Count);
                // NOTE: These steps are involved in part because it's possible for elements, the table element in this case in particular, to have been moved by a script around in the DOM,
                // or indeed removed from the DOM entirely, after the element was inserted by the parser.
            } else {
                // Otherwise
                // Let adjusted insertion location be inside target, after its last child (if any).
                return (target, target.childNodes.Count);
            }
        }
    }
    #endregion
    // https://html.spec.whatwg.org/multipage/parsing.html#create-an-element-for-the-token
    private Element CreateAnElementForAToken(Tag token, string @namespace, Node intendedParent) {
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
        // todo this is not how this should be done: https://dom.spec.whatwg.org/#concept-element-attributes-append
        token.Attributes.ForEach((attr) => {
            element.attributes.Add(attr.name, attr.value);
        });
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

    private static void InsertNode((Node target, int childPos) insertPosition, Node child) {
        child.parent?.childNodes.Remove(child);

        insertPosition.target.childNodes.Insert(insertPosition.childPos, child);
        child.parent = insertPosition.target;
    }

    private static void AppendNode(Node target, Node child) {
        target.childNodes.Add(child);
        child.parent = target;
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
            if (node is Element element && element.attributes.Count > 0) {
                var attributeIndentation = "|" + new string(' ', (depth + 1) * 2 - 1);
                foreach (var attr in element.attributes) {
                    Console.WriteLine($"{attributeIndentation}{attr.Key}=\"{attr.Value}\"");
                }
            }
            foreach (var child in Enumerable.Reverse(node.childNodes)) {
                stack.Push((child, depth + 1));
            }
        }
    }

}


public static class ListExtensions {
    public static T Pop<T>(this List<T> list) {
        if (list == null) {
            throw new ArgumentNullException(nameof(list));
        }
        if (list.Count == 0) {
            throw new InvalidOperationException("The list is empty. Cannot pop from an empty List.");
        }
        var elem = list[^1];
        list.RemoveAt(list.Count - 1);
        return elem;
    }
    public static void Put<T>(this List<T> list, T elem) {
        if (list == null) {
            throw new ArgumentNullException(nameof(list));
        }
        list.Add(elem);
    }

    public static void Push<T>(this List<T> list, T elem) {
        Put(list, elem);
    }

    public static T Peek<T>(this List<T> list) {
        if (list == null) {
            throw new ArgumentNullException(nameof(list));
        }
        if (list.Count == 0) {
            throw new InvalidOperationException("The list is empty. Cannot peek from an empty List.");
        }
        return list[^1];
    }
}

