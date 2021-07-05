using System;

/*
The grammer

primaryExpression :  IDENTIFIER
                     NUMBER
                     STRING_LITERAL
                     '(' expression ')'

postfixExpression: primaryExpression ( . IDENTIFER | [expression] (argList?))*

multiplicativeExpression: postfixExpression (*|/ postfixExpression)*

expression: multiplicativeExpression

*/

namespace Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] expressions = {
                "1+2",
                "a+1",
                "a+b*1+2",
                "(a+b)*1+2",
                "(1)+2",
                "(1/2"
            };


            foreach (var e in expressions)
            {
                var lexer = new ExpressionLexer(e);
                Console.WriteLine($"{e}:");
                Token token = null;
                do {
                    token = lexer.NextToken();
                    printToken(token);
                } while (token.Kind != TokenKind.EOF);
            }
        }
        
        static void printToken(Token t)
        {
            Console.WriteLine($"  <{printTokenKind(t.Kind)}/{t.Text}>");
        }

        static string printTokenKind(TokenKind k)
        {
            string tokenName = string.Empty;
            switch(k) 
            {
                case TokenKind.ID: 
                    tokenName = "ID";
                    break;
                case TokenKind.NUM:
                    tokenName = "NUM";
                    break;
                case TokenKind.PLUS:
                    tokenName = "PLUS";
                    break;
                case TokenKind.MINUS:
                    tokenName = "MINUS";
                    break;
                case TokenKind.EOF:
                    tokenName = "EOF";
                    break;
                default:
                    tokenName = "TEXT";
                    break;
            }
            return tokenName;
        }
    }
}
