namespace FunWithHtml.es262;


public class TokenInput {
    class EmptyToken: Token { }

    private readonly Token EMPTY = new EmptyToken();


    // Simple white space and single-line comments are discarded and do not appear in the stream of input elements for the syntactic grammar. 
    // A MultiLineComment (that is, a comment of the form /*…*/ regardless of whether it spans more than one line) is likewise simply discarded if it contains no line terminator;
    // but if a MultiLineComment contains one or more line terminators, then it is replaced by a single line terminator, which becomes part of the stream of input elements for the syntactic grammar.
    // todo multiline comments
    public List<Token> Tokens { get; private set; }
    public Token CurrentToken { get; private set; }

    public int Index { get; private set; } = 0;

    public TokenInput(List<Token> tokens) {
        Tokens = [.. tokens.Where(item => item is not WhiteSpace and not Comment)];
        CurrentToken = Tokens[0]; // todo handle 0 tokens
    }

    public Token ConsumeToken() {
        ++Index;
        CurrentToken = Tokens.Count > Index ? Tokens[Index] : EMPTY;
        return CurrentToken;
    }

    public int SavePosition() => Index;
    public void RestorePosition(int savedIndex) {
        Index = savedIndex;
        CurrentToken = Tokens.Count > Index ? Tokens[Index] : EMPTY;
    }

    public T? WithBacktrack<T>(Func<T> parseAttempt) {
        int savedPos = SavePosition();
        T result = parseAttempt();

        if (result == null) {
            RestorePosition(savedPos);
        }
        return result;
    }
}



class ParseNode {
    public List<ParseNode> childNodes = [];
    public ParseNode? parent = null;
    public void AppendChild(ParseNode child) {
        child.parent = this;
        childNodes.Add(child);
    }

    public static void PrintTree(ParseNode node) {
        var stack = new Stack<(int, ParseNode)> { };
        stack.Push((0, node));
        while (stack.Count > 0) {
            var (depth, el) = stack.Pop();
            Console.WriteLine($"{new string(' ', depth * 2)}{el}");
            foreach (var item in Enumerable.Reverse(el.childNodes)) {
                stack.Push((depth + 1, item));
            }
        }

    }
}


class ASTNode: ParseNode { }

class Block: ASTNode { }
class ExpressionStatement: ASTNode { }
class VariableDeclaration: ASTNode { }
class Initializer: ASTNode { }
class VariableStatement: ASTNode { }
class StatementList: ASTNode { }
class AssignmentExpression: ASTNode {
    public ParseNode lhs { get; private set; }
    public ParseNode rhs { get; private set; }

    public AssignmentExpression(ParseNode lhs, ParseNode rhs) {
        this.lhs = lhs;
        this.rhs = rhs;
        AppendChild(lhs);
        AppendChild(rhs);
    }
};


public class Parser() {


    class ParseNode1(Token token): ParseNode {
        public Token Token { get; private set; } = token;

        public override string ToString() {
            return $"{Token}";
        }
    }

    class ParseNode2(ParseNode n1, ParseNode n2): ParseNode {
        public ParseNode Node1 { get; private set; } = n1;
        public ParseNode Node2 { get; private set; } = n2;

        public override string ToString() {
            return $"{Node1} + {Node2}";
        }
    }



    public static void Parse(TokenInput input) {
        Console.WriteLine("=========");
        Console.WriteLine($"Index: {input.Index} Token count: {input.Tokens.Count} // {input.CurrentToken}");
        var node = Script(input);
        Console.WriteLine($"Node: {node}");
        if (node is not null)
            ParseNode.PrintTree(node);
        Console.WriteLine($"Index: {input.Index} Token count: {input.Tokens.Count} // {input.CurrentToken}");
    }

    // https://tc39.es/ecma262/#prod-Script
    private static ParseNode? Script(TokenInput input) {
        return ScriptBody(input);
    }

    // https://tc39.es/ecma262/#prod-ScriptBody    
    private static ParseNode? ScriptBody(TokenInput input) {
        return StatementList(input);
    }

    // https://tc39.es/ecma262/#prod-StatementList
    private static ParseNode? StatementList(TokenInput input) {
        var list = new StatementList();
        while (true) {
            var item = StatementItem(input);
            if (item is null) break;
            list.AppendChild(item);
        }
        if (list.childNodes.Count == 0) return null;
        return list;
    }

    // https://tc39.es/ecma262/#prod-StatementListItem
    private static ParseNode? StatementItem(TokenInput input) {
        return Statement(input);
        // Declaration(input); // todo
    }

    // https://tc39.es/ecma262/#prod-Statement
    private static ParseNode? Statement(TokenInput input) {
        if (BlockStatement(input) is ParseNode bs) return bs;
        if (VariableStatement(input) is ParseNode vs) return vs;
        if (EmptyStatement(input) is ParseNode es) return es;
        if (ExpressionStatement(input) is ParseNode exps) return exps;
        return null;
    }

    // https://tc39.es/ecma262/#prod-ExpressionStatement
    private static ParseNode? ExpressionStatement(TokenInput input) {
        // todo lookahead
        var exp = Expression(input);
        if (exp is null) return null;
        if (input.CurrentToken is not Punctuator { Value: ";" }) return null;
        input.ConsumeToken();
        var expStmt = new ExpressionStatement();
        expStmt.AppendChild(exp);
        return expStmt;

    }

    // https://tc39.es/ecma262/#prod-Expression
    private static ParseNode? Expression(TokenInput input) {
        return AssignmentExpression(input);
    }

    // https://tc39.es/ecma262/#prod-EmptyStatement
    private static ParseNode? EmptyStatement(TokenInput input) {
        if (input.CurrentToken is Punctuator { Value: ";" }) {
            input.ConsumeToken();
            return new ParseNode();
        }
        return null;
    }

    // https://tc39.es/ecma262/#prod-BlockStatement
    private static ParseNode? BlockStatement(TokenInput input) {
        return Block(input);
    }

    // https://tc39.es/ecma262/#prod-Block
    private static ParseNode? Block(TokenInput input) {
        return input.WithBacktrack(() => {
            if (input.CurrentToken is not Punctuator { Value: "{" }) return null;
            input.ConsumeToken();
            var result = StatementList(input);
            if (result is null) return null;
            if (input.CurrentToken is not RightBracePunctuator) return null;
            input.ConsumeToken();
            var block = new Block();
            block.AppendChild(result);
            return block;
        });
    }

    // https://tc39.es/ecma262/#prod-VariableStatement
    private static ParseNode? VariableStatement(TokenInput input) {
        return input.WithBacktrack(() => {
            if (input.CurrentToken is not IdentifierName { Name: "var" }) return null;
            input.ConsumeToken();
            var result = VariableDeclarationList(input);
            if (result is null) return null;
            if (input.CurrentToken is not Punctuator { Value: ";" }) return null;
            input.ConsumeToken();
            var vs = new VariableStatement();
            vs.AppendChild(result);
            return vs;
        });
    }

    // https://tc39.es/ecma262/#prod-VariableDeclarationList
    private static ParseNode? VariableDeclarationList(TokenInput input) {
        return VariableDeclaration(input);
        // todo List , 
    }

    // https://tc39.es/ecma262/#prod-VariableDeclaration
    private static ParseNode? VariableDeclaration(TokenInput input) {
        var n1 = BindingIdentifier(input);
        var n2 = Initializer(input);
        var vd = new VariableDeclaration();
        if (n1 is not null)
            vd.AppendChild(n1);
        if (n2 is not null)
            vd.AppendChild(n2);
        return vd;
        // todo initializer optional
        // BindingPattern
        // initializer
    }

    // https://tc39.es/ecma262/#prod-Initializer
    private static ParseNode? Initializer(TokenInput input) {
        if (input.CurrentToken is not Punctuator { Value: "=" }) return null;
        input.ConsumeToken();


        var x = AssignmentExpression(input);
        if (x is null) return null;
        var ae = new Initializer();
        ae.AppendChild(x);
        return ae;
    }

    // https://tc39.es/ecma262/#prod-AssignmentExpression
    private static ParseNode? AssignmentExpression(TokenInput input) {
        if (input.WithBacktrack(() => {
            var lhe = LeftHandSideExpression(input);
            if (lhe is null) return null;
            if (input.CurrentToken is not Punctuator { Value: "=" }) return null;
            input.ConsumeToken();
            var ae = AssignmentExpression(input);
            if (ae is null) return null;
            return new AssignmentExpression(lhe, ae);
        }) is ParseNode lhse) return lhse;
        if (ConditionalExpression(input) is ParseNode ce) return ce;
        return null;
        // todo
    }

    // https://tc39.es/ecma262/#prod-ConditionalExpression
    private static ParseNode? ConditionalExpression(TokenInput input) {
        return ShortCircuitExpression(input);
        // todo
    }

    // https://tc39.es/ecma262/#prod-ShortCircuitExpression
    private static ParseNode? ShortCircuitExpression(TokenInput input) {
        return LogicalORExpression(input);
        // todo
    }

    // https://tc39.es/ecma262/#prod-LogicalORExpression
    private static ParseNode? LogicalORExpression(TokenInput input) {
        return LogicalANDExpression(input);
        // todo
    }

    // https://tc39.es/ecma262/#prod-LogicalANDExpression
    private static ParseNode? LogicalANDExpression(TokenInput input) {
        return BitwiseOrExpression(input);
        // todo
    }

    // https://tc39.es/ecma262/#prod-BitwiseORExpression
    private static ParseNode? BitwiseOrExpression(TokenInput input) {
        return BitwiseXORExpression(input);
        // todo
    }

    // https://tc39.es/ecma262/#prod-BitwiseXORExpression
    private static ParseNode? BitwiseXORExpression(TokenInput input) {
        return BitwiseANDExpression(input);
        // todo
    }

    // https://tc39.es/ecma262/#prod-BitwiseANDExpression
    private static ParseNode? BitwiseANDExpression(TokenInput input) {
        return EqualityExpression(input);
        // todo
    }

    // https://tc39.es/ecma262/#prod-EqualityExpression
    private static ParseNode? EqualityExpression(TokenInput input) {
        return RelationalExpression(input);
        // todo
    }

    // https://tc39.es/ecma262/#prod-RelationalExpression
    private static ParseNode? RelationalExpression(TokenInput input) {
        return ShiftExpression(input);
    }

    // https://tc39.es/ecma262/#prod-ShiftExpression
    private static ParseNode? ShiftExpression(TokenInput input) {
        return AdditiveExpression(input);
    }

    // https://tc39.es/ecma262/#prod-AdditiveExpression
    private static ParseNode? AdditiveExpression(TokenInput input) {
        return MultiplicativeExpression(input);
    }

    // https://tc39.es/ecma262/#prod-MultiplicativeExpression
    private static ParseNode? MultiplicativeExpression(TokenInput input) {
        return ExponentiationExpression(input);
    }

    // https://tc39.es/ecma262/#prod-ExponentiationExpression
    private static ParseNode? ExponentiationExpression(TokenInput input) {
        return UnaryExpression(input);
    }

    // https://tc39.es/ecma262/#prod-UnaryExpression
    private static ParseNode? UnaryExpression(TokenInput input) {
        return UpdateExpression(input);
    }

    // https://tc39.es/ecma262/#prod-UpdateExpression
    private static ParseNode? UpdateExpression(TokenInput input) {
        return LeftHandSideExpression(input);
    }

    // https://tc39.es/ecma262/#prod-LeftHandSideExpression
    private static ParseNode? LeftHandSideExpression(TokenInput input) {
        return NewExpression(input);
    }

    // https://tc39.es/ecma262/#prod-NewExpression
    private static ParseNode? NewExpression(TokenInput input) {
        return MemberExpression(input);
    }

    // https://tc39.es/ecma262/#prod-MemberExpression
    private static ParseNode? MemberExpression(TokenInput input) {
        return PrimaryExpression(input);
    }

    // https://tc39.es/ecma262/#prod-PrimaryExpression
    private static ParseNode? PrimaryExpression(TokenInput input) {

        // todo
        if (IdentifierReference(input) is ParseNode ir) return ir;
        return Literal(input);
        // todo
    }

    // https://tc39.es/ecma262/#prod-IdentifierReference
    private static ParseNode? IdentifierReference(TokenInput input) {
        return Identifier(input);
    }

    // https://tc39.es/ecma262/#prod-Literal
    private static ParseNode? Literal(TokenInput input) {
        if (input.CurrentToken is NumericLiteral nl) {
            input.ConsumeToken();
            return new ParseNode1(nl);
        }
        return null;
    }

    // https://tc39.es/ecma262/#prod-BindingIdentifier
    private static ParseNode? BindingIdentifier(TokenInput input) {
        return Identifier(input);
    }

    // https://tc39.es/ecma262/#prod-Identifier
    private static ParseNode? Identifier(TokenInput input) {
        if (input.CurrentToken is IdentifierName idn && !IsReservedWord(idn.Name)) {
            input.ConsumeToken();
            return new ParseNode1(idn);
        }
        return null;
    }


    // https://tc39.es/ecma262/#prod-ReservedWord
    private static bool IsReservedWord(string word) {
        return word is "await" or "break" or "case" or "catch" or "class" or "const" or "continue" or "debugger" or "default" or "delete" or "do"
            or "else" or "enum" or "export" or "extends" or "false" or "finally" or "for" or "function" or "if" or "import" or "in" or "instanceof" or "new" or "null" or "return"
             or "super" or "switch" or "this" or "throw" or "true" or "try" or "typeof" or "var" or "void" or "while" or "with" or "yield";

    }
}