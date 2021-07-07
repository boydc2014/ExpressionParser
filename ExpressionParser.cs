using System;

namespace Parser
{
    /*
    Grammar：
    
    primaryExpression:= ID | NUM | STRING | '(' expression ')'

    multiplicativeExpression := primaryExpression (*|/ primaryExpression)*

    expression:= multiplicativeExpression (+|- multiplicativeExpression)*                     

    */
    class ExpressionParser 
    {

        private ExpressionLexer _lexer;
        public ExpressionParser() 
        {
        }

        public SyntaxNode ParseExpression(string expression) 
        {
            _lexer = new ExpressionLexer(expression);
            parseExpression = genParserForBinOp(parsePrimaryExpression, TokenKind.MUL, TokenKind.DIV);
            parseExpression = genParserForBinOp(parseExpression, TokenKind.PLUS, TokenKind.MINUS);

            var exp = parseExpression();
            if (nextToken().Kind == TokenKind.EOF)
            {
                return exp;
            }
            throw new Exception($"Unable to parse all input, stop at {nextToken().Kind}");
        }

        private Func<SyntaxNode> parseExpression = null;

        private SyntaxNode parsePrimaryExpression()
        {
            var token = nextToken();
            SyntaxNode exp = null;
            switch (token.Kind)
            {
                case TokenKind.ID:
                case TokenKind.NUM:
                case TokenKind.STRING:
                    exp = new SyntaxNode(token);
                    _lexer.EatToken();
                    return exp;
                case TokenKind.OPEN_BRACKET:
                    _lexer.EatToken();
                    exp = parseExpression();
                    _lexer.EatToken(TokenKind.CLOSE_BRACKET);
                    return exp;
                default:
                    throw new Exception($"Unexpected token {token.Kind}, expecting {TokenKind.ID}, {TokenKind.NUM} or {TokenKind.OPEN_BRACKET}");
            }
        }
    
        // Generate a parser with subParser and a binary op for the following grammer
        // S => A | A op A | A op A op A | ...
        // which is equivalent to 
        // S => A | A op S
        // which is equivalent to
        // S => A S1
        // S1 => e | op S
        private Func<SyntaxNode> genParserForBinOp(Func<SyntaxNode> parse, params TokenKind[] kinds)
        {
            Func<SyntaxNode> gened = null;
            gened = () => 
            {
                var result = parse();
                var next = nextToken();
                if (Array.IndexOf(kinds, next.Kind) >= 0)
                {
                    _lexer.EatToken();
                    result = new SyntaxNode(next, result, gened());
                }
                return result;
            };
            return gened;
        }

        // Wrapper function on lexer's NextToken() to support skip whitespace
        private Token nextToken(bool skipWS = true)
        {
            while (_lexer.NextToken().Kind == TokenKind.WS && skipWS)
            {
                _lexer.EatToken();
            }
            return _lexer.NextToken();
        }
    }
}