using System;

namespace Parser
{
    /*
    Grammar：
    
    primaryExpression:= ID | NUM | '(' expression ')'

    multiplicativeExpression := primaryExpression (*|/ primaryExpression)*

    expression:= multiplicativeExpression (+|- multiplicativeExpression)*                     

    */
    class ExpressionParser 
    {

        private ExpressionLexer _lexer;
        public ExpressionParser() 
        {
        }

        public Expression ParseExpression(string expression) 
        {
            _lexer = new ExpressionLexer(expression);
            parseExpression = genParserForBinOp(parsePrimaryExpression, TokenKind.MUL, TokenKind.DIV);
            parseExpression = genParserForBinOp(parseExpression, TokenKind.PLUS, TokenKind.MINUS);

            var exp = parseExpression();
            if (_lexer.NextToken().Kind == TokenKind.EOF)
            {
                return exp;
            }
            throw new Exception($"Unable to parse all input, stop at {_lexer.NextToken().Kind}");
        }

        private Func<Expression> parseExpression = null;

        private Expression parsePrimaryExpression()
        {
            var token = _lexer.NextToken();
            Expression exp = null;
            switch (token.Kind)
            {
                case TokenKind.ID:
                    exp = new Identifier(token.Text);
                    _lexer.EatToken();
                    return exp;
                case TokenKind.NUM:
                    exp = new Identifier(token.Text);
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
        private Func<Expression> genParserForBinOp(Func<Expression> parse, params TokenKind[] kinds)
        {
            Func<Expression> gened = null;
            gened = () => 
            {
                var result = parse();
                var next = _lexer.NextToken();
                if (Array.IndexOf(kinds, next.Kind) >= 0)
                {
                    _lexer.EatToken();
                    result = new Expression(next, result, gened());
                }
                return result;
            };
            return gened;
        }
    }
}