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
            parseExpression = genParserForBinOp(parsePrimaryExpression, "*/");
            parseExpression = genParserForBinOp(parseExpression, "+-");

            return parseExpression();
        }

        private Func<Expression> parseExpression = null;

        private Expression parsePrimaryExpression()
        {
            if (_lexer.NextToken().Text == "a" || _lexer.NextToken().Text == "b")
            {
                var exp = new Identifier(_lexer.NextToken().Text);
                _lexer.EatToken();
                return exp;
            }
            else if (_lexer.NextToken().Text == "1" || _lexer.NextToken().Text == "2")
            {
                var exp = new Identifier(_lexer.NextToken().Text);
                _lexer.EatToken();
                return exp;
            }
            else if (_lexer.NextToken().Text == "(")
            {
                _lexer.EatToken();
                var exp = parseExpression();
                _lexer.EatToken(")");
                return exp;
            }
            else 
            {
                throw new Exception($"Unknown token {_lexer.NextToken().Text}");
            }
        }
    
        // Generate a parser with subParser and a binary op for the following grammer
        // S => A | A op A | A op A op A | ...
        // which is equivalent to 
        // S => A | A op S
        // which is equivalent to
        // S => A S1
        // S1 => e | op S
        private Func<Expression> genParserForBinOp(Func<Expression> parse, string ops)
        {
            Func<Expression> gened = null;
            gened = () => 
            {
                var result = parse();
                var next = _lexer.NextToken().Text;
                if (ops.Contains(next))
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