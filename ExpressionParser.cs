using System;
using System.Collections.Generic;
using Parser.AST;

namespace Parser
{
    /*
    Grammar：
    
    primaryExpression := ID | NUM | STRING | '(' expression ')'

    postfixExpression := primaryExpression ( (argList?) | [expression] | .ID ) *

    multiplicativeExpression := primaryExpression (*|/|% primaryExpression)*

    addtiveExpression := multiplicativeExpression (+|- multiplicativeExpression)*                     

    */
    class ExpressionParser 
    {

        private static Dictionary<TokenKind, SyntaxKind> _table = new Dictionary<TokenKind, SyntaxKind> 
        {
            {TokenKind.PLUS, SyntaxKind.PlusExpression},
            {TokenKind.MINUS, SyntaxKind.MinusExpression},
            {TokenKind.MUL, SyntaxKind.MulExpression},
            {TokenKind.DIV, SyntaxKind.DivExpression},
            {TokenKind.PERCENT, SyntaxKind.PercentExpression},
            {TokenKind.NULL_COALESCE, SyntaxKind.NullCoalescExpression},

            {TokenKind.NOT, SyntaxKind.NotExpression},
            {TokenKind.DOUBLE_EQUAL, SyntaxKind.EqualExpression},
            {TokenKind.NOT_EQUAL, SyntaxKind.NotEqualExpression},
            {TokenKind.MORE_THAN, SyntaxKind.GreaterThanExpression},
            {TokenKind.MORE_OR_EQUAL, SyntaxKind.GreaterOrEqualExpression},
            {TokenKind.LESS_THAN, SyntaxKind.LessThanExpression},
            {TokenKind.LESS_OR_EQUAl, SyntaxKind.LessOrEqualExpression},

            {TokenKind.SINGLE_AND, SyntaxKind.BitAndExpression},
            {TokenKind.XOR, SyntaxKind.BitXorExpression},

            {TokenKind.DOUBLE_AND, SyntaxKind.LogicalAndExpression},
            {TokenKind.DOUBLE_VERTICAL_LINE, SyntaxKind.LogicalOrExpression},
        };

        private ExpressionLexer _lexer;
        public ExpressionParser() 
        {
        }

        public SyntaxNode ParseExpression(string expression) 
        {
            _lexer = new ExpressionLexer(expression);

            parseExpression = genParserForBinOp(parsePostfixExpression, new []{TokenKind.XOR}, true);
            parseExpression = genParserForUniOp(parseExpression, new []{TokenKind.NOT, TokenKind.PLUS, TokenKind.MINUS});
            // binary ops
            parseExpression = genParserForBinOp(parseExpression, new []{TokenKind.MUL, TokenKind.DIV, TokenKind.PERCENT});
            parseExpression = genParserForBinOp(parseExpression, new []{TokenKind.PLUS, TokenKind.MINUS});
            parseExpression = genParserForBinOp(parseExpression, new []{TokenKind.DOUBLE_EQUAL, TokenKind.NOT_EQUAL});
            parseExpression = genParserForBinOp(parseExpression, new []{TokenKind.SINGLE_AND});
            parseExpression = genParserForBinOp(parseExpression, new []{TokenKind.LESS_THAN, TokenKind.LESS_OR_EQUAl, TokenKind.MORE_THAN, TokenKind.MORE_OR_EQUAL});
            parseExpression = genParserForBinOp(parseExpression, new []{TokenKind.DOUBLE_AND});
            parseExpression = genParserForBinOp(parseExpression, new []{TokenKind.DOUBLE_VERTICAL_LINE});
            parseExpression = genParserForBinOp(parseExpression, new []{TokenKind.NULL_COALESCE});

            var exp = parseExpression();
            if (nextToken().Kind == TokenKind.EOF)
            {
                return exp;
            }
            throw new Exception($"Unable to parse all input, stop at {nextToken().Kind}");
        }

        private Func<SyntaxNode> parseExpression = null;

        private SyntaxNode parsePostfixExpression()
        {
            var result = parsePrimaryExpression();
            var t = nextToken(false);   // We can't allow white space in postfix expression
            while (t.Kind == TokenKind.OPEN_BRACKET || 
                   t.Kind == TokenKind.DOT ||
                   t.Kind == TokenKind.OPEN_SQUARE_BRACKET)
            {
                var op = eatToken();
                if (op.Kind == TokenKind.OPEN_BRACKET)
                {
                    var argList = new List<SyntaxNode>() { result };
                    if (nextToken().Kind != TokenKind.CLOSE_BRACKET)
                    {
                        var args = parseOneOrMore(parseExpression, TokenKind.COMMA);
                        argList.AddRange(args);
                    }
                    eatToken(TokenKind.CLOSE_BRACKET);
                    result = new SyntaxNode(SyntaxKind.InvokeExpression, argList.ToArray());
                }
                else if (op.Kind == TokenKind.DOT)
                {
                    var property = eatToken(TokenKind.ID);
                    result = new SyntaxNode(SyntaxKind.AccessExpression, result, new Terminal(SyntaxKind.IdentiferToken, property.Text));
                }
                else
                {
                    var index = parseExpression();
                    eatToken(TokenKind.CLOSE_SQUARE_BRACKET);
                    result = new SyntaxNode(SyntaxKind.ElementExpression, result, index);
                }   

                t = nextToken(false);  
            }
            return result;
        }

        private SyntaxNode parsePrimaryExpression()
        {
            var token = nextToken();
            SyntaxNode exp = null;
            switch (token.Kind)
            {
                case TokenKind.ID:
                case TokenKind.NUM:
                case TokenKind.STRING:
                    exp = new Terminal(SyntaxKind.StringToken, token.Text);
                    eatToken();
                    return exp;
                case TokenKind.OPEN_BRACKET:
                    eatToken();
                    exp = parseExpression();
                    eatToken(TokenKind.CLOSE_BRACKET);
                    return exp;
                case TokenKind.OPEN_SQUARE_BRACKET:
                    var op = eatToken();
                    var result = new SyntaxNode(SyntaxKind.ArrayCreationExpression);
                    if (nextToken().Kind != TokenKind.CLOSE_SQUARE_BRACKET)
                    {
                        var parts = parseOneOrMore(parseExpression, TokenKind.COMMA);
                        result.Children = parts.ToArray();
                    }
                    eatToken(TokenKind.CLOSE_SQUARE_BRACKET);
                    return result;
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
        private Func<SyntaxNode> genParserForBinOp(Func<SyntaxNode> parse, TokenKind[] kinds, bool rightAssociate = false)
        {
            Func<SyntaxNode> gened = null;
            gened = () => 
            {
                var result = parse();
                
                if (rightAssociate) 
                {
                    // right asscociate is simply, treat the whole right part as a whole
                    if (Array.IndexOf(kinds, nextToken().Kind) >= 0)
                    {
                        var op = nextToken();
                        eatToken();
                        result = new SyntaxNode(TokenKind2SyntaxKind(op.Kind), result, gened());
                    }
                }
                else 
                {
                    // left asscoicate will contruct the expression as soon as possible
                    while (Array.IndexOf(kinds, nextToken().Kind) >= 0)
                    {
                        var op = nextToken();
                        eatToken();
                        var nextPart = parse();
                        result = new SyntaxNode(TokenKind2SyntaxKind(op.Kind), result, nextPart);
                    }
                }

                return result;
            };
            return gened;
        }

        private Func<SyntaxNode> genParserForUniOp(Func<SyntaxNode> parse, TokenKind[] kinds)
        {
            Func<SyntaxNode> gened = null;
            gened = () => 
            {
                var t = nextToken();
                if (Array.IndexOf(kinds, t.Kind) >= 0)
                {
                    eatToken();
                    var result = gened();
                    return new SyntaxNode(TokenKind2SyntaxKind(t.Kind), result);
                }
                return parse();
            };
            return gened;
        }

        private List<SyntaxNode> parseOneOrMore(Func<SyntaxNode> parse, TokenKind seperator)
        {
            var result = new List<SyntaxNode>(){};
            for(;;) {
                result.Add(parse());
                if (nextToken().Kind != seperator)
                {
                    break;
                }
                eatToken();
            }
            return result;
        }

        // Wrapper function on lexer's NextToken() to support skip whitespace
        private Token nextToken(bool skipWS = true)
        {
            while (_lexer.NextToken().Kind == TokenKind.WS && skipWS)
            {
                eatToken();
            }
            return _lexer.NextToken();
        }

        private Token eatToken(TokenKind kind = TokenKind.NONE)
        {
            return _lexer.EatToken(kind);
        }

        private SyntaxKind TokenKind2SyntaxKind(TokenKind kind)
        {
            return _table[kind];
        }
    }
}