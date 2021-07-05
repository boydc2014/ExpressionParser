﻿using System;

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
                 "_test+5.5",
                "hello+world",
                "1+2",
                "a+1",
                "a+b*1+2",
                "(a+b)*1+2",
                "(1)+2",
                "(1/2"
            };

            TestParser(expressions);
        }
        
        static void printToken(Token t)
        {
            Console.WriteLine($"  <{(t.Kind)}/{t.Text}>");
        }

        static void TestParser(string[] expressions)
        {
            foreach (var e in expressions)
            {
                var parser = new ExpressionParser();
                try 
                {
                    Console.WriteLine(e);
                    var exp = parser.ParseExpression(e);
                    Console.WriteLine(exp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        static void TestLexer(string[] expressions)
        {
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
    }
}