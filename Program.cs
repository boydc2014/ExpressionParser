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
            Console.WriteLine($"  <{(t.Kind)}/{t.Text}>");
        }
    }
}
