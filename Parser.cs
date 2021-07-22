using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Parser.AST;

namespace Parser
{
    class Parser 
    {
        public class InputReader 
        {
            private string _input;
            private int _offset;

            public InputReader(string input)
            {
                _input = input;
                _offset = 0;
            }

            public char Read()
            {
                if (_offset >= _input.Length)
                {
                    return '\0';
                }
                return _input[_offset++];
            }

            public int GetPosition()
            {
                return _offset;
            }

            public void Seek(int position)
            {
                _offset = position;
            }

            public bool EOF()
            {
                return _offset >= _input.Length;
            }

        }
        
        public class ParserResult<T>
        {
            public bool IsSuccess {get; set;}
            public T Value {get; set;}
            public string ErrorMessage { get; set;}

            public static ParserResult<T> Success(T value)
            {
                var result = new ParserResult<T>();
                result.IsSuccess = true;
                result.Value = value;
                return result;
            }

            public static ParserResult<T> Failure(string message)
            {
                var result = new ParserResult<T>();
                result.IsSuccess = false;
                result.ErrorMessage = message;
                return result;
            }

        }

        public Parser()
        {
            // var ID = Concat(Opt(Or(All("@@"), Str(Any("_#$@%")))), Str(Any(Letter())), Many(Any(Letter() + Digit() + "_")));
            
            // var manyDigits = Many1(Any(Digit()));
            // var NUM = Concat(manyDigits, Opt(Concat(Str(Char('.')), manyDigits)));

            // var escapeChar = Concat(Str(Char('\\')), Str(Not(""))); 

            // var stringLiteral1 = Concat(Str(Char('"')), Concat(Many(Or(Many1(Not("\\\"")), escapeChar))), Str(Char('"')));
            // var stringLiteral2 = Concat(Str(Char('\'')), Concat(Many(Or(Many1(Not("\\'")), escapeChar))), Str(Char('\'')));
            // var stringLiteral = Or(stringLiteral1, stringLiteral2);

            // parseID = Terminal(ID, SyntaxKind.IdentiferToken);
            // var parseNUM = Terminal(NUM, SyntaxKind.NumToken);
            // var parseSTRING = Terminal(stringLiteral, SyntaxKind.StringToken);

            // parseStringLiteral2 = Terminal(stringLiteral1, SyntaxKind.StringToken);

            // var parsePrimaryExpression = Between(Or(parseID, parseNUM, parseSTRING), Many(Any(Space())));
            
            var parsePrimaryExpression = ParsePrimaryExpression();
            var parsePostfixExpression = ParsePostfixExpression(parsePrimaryExpression);
           
            parseExpression = ChainR(parsePostfixExpression, BinaryOps("^"));
            parseExpression = ChainL(parseExpression, BinaryOps("*", "/", "%"));
            parseExpression = ChainL(parseExpression, BinaryOps("+", "-"));
            parseExpression = ChainL(parseExpression, BinaryOps("==", "!="));
            parseExpression = ChainL(parseExpression, BinaryOps(">=", ">", "<=", "<"));
            parseExpression = ChainL(parseExpression, BinaryOps("&&"));
            parseExpression = ChainL(parseExpression, BinaryOps("||"));
            parseExpression = ChainL(parseExpression, BinaryOps("??"));
        }

        private static Dictionary<string, SyntaxKind> opTable = new Dictionary<string, SyntaxKind>()
        {
            {"+", SyntaxKind.PlusExpression},
            {"-", SyntaxKind.MinusExpression},
            {"*", SyntaxKind.MulExpression},
            {"/", SyntaxKind.DivExpression},
            {"%", SyntaxKind.PercentExpression},
            {"^", SyntaxKind.BitXorExpression},
            {"==", SyntaxKind.EqualExpression},
            {"!=", SyntaxKind.NotEqualExpression},
            {">", SyntaxKind.GreaterThanExpression},
            {">=", SyntaxKind.GreaterOrEqualExpression},
            {"<", SyntaxKind.LessThanExpression},
            {"<=", SyntaxKind.LessOrEqualExpression},
            {"&&", SyntaxKind.LogicalAndExpression},
            {"||", SyntaxKind.LogicalOrExpression},
            {"??", SyntaxKind.NullCoalescExpression}
        };


        private Func<InputReader, ParserResult<SyntaxNode>> ParsePrimaryExpression()
        {   
            var ID = Concat(Opt(Or(All("@@"), Str(Any("_#$@%")))), Str(Any(Letter())), Many(Any(Letter() + Digit() + "_")));
            
            var manyDigits = Many1(Any(Digit()));
            var NUM = Concat(manyDigits, Opt(Concat(Str(Char('.')), manyDigits)));

            var escapeChar = Concat(Str(Char('\\')), Str(Not(""))); 

            var stringLiteral1 = Concat(Str(Char('"')), Concat(Many(Or(Many1(Not("\\\"")), escapeChar))), Str(Char('"')));
            var stringLiteral2 = Concat(Str(Char('\'')), Concat(Many(Or(Many1(Not("\\'")), escapeChar))), Str(Char('\'')));
            var stringLiteral = Or(stringLiteral1, stringLiteral2);

            parseID = Terminal(ID, SyntaxKind.IdentiferToken);
            var parseNUM = Terminal(NUM, SyntaxKind.NumToken);
            var parseSTRING = Terminal(stringLiteral, SyntaxKind.StringToken);

            var parseArgList = SquareBracketed(SepBy(Spaced(LazyParseExpression()), Any(",")));
            var parseArrayCreation = Select(parseArgList, argList => { return new SyntaxNode(SyntaxKind.ArrayCreationExpression, argList.ToArray());});

            return Or(parseID, parseNUM, parseSTRING, Bracketed(Spaced(LazyParseExpression())), parseArrayCreation);
        }


        private Func<InputReader, ParserResult<SyntaxNode>> ParsePostfixExpression(Func<InputReader, ParserResult<SyntaxNode>> parser)
        {
            var parseProperty = Before(Any("."), LazyParseID());
            var parseIndex = Between(Spaced(LazyParseExpression()), Any("["), Any("]"));
            var parseArgList = Between(Spaced(SepBy(LazyParseExpression(), Spaced(Any(",")))), Any("("), Any(")"));

            return (input) =>
            {
                var primary = parser(input);
                if (!primary.IsSuccess)
                {
                    return ParserResult<SyntaxNode>.Failure(primary.ErrorMessage);
                }
                
                while(true)
                {
                    var property = Try(parseProperty)(input);
                    if (property.IsSuccess)
                    {
                        primary.Value = new SyntaxNode(SyntaxKind.AccessExpression, primary.Value, property.Value);
                        continue;
                    }

                    var index = Try(parseIndex)(input);
                    if (index.IsSuccess)
                    {
                        primary.Value = new SyntaxNode(SyntaxKind.ElementExpression, primary.Value, index.Value);
                        continue;
                    }

                    var argList = Try(parseArgList)(input);
                    if (argList.IsSuccess)
                    {
                        primary.Value = new SyntaxNode(SyntaxKind.InvokeExpression, primary.Value);
                        primary.Value.Children = primary.Value.Children.Concat(argList.Value).ToArray();
                        continue;
                    }

                    break;
                }
                
                return primary;
            };
        }

        private Func<InputReader, ParserResult<Func<SyntaxNode, SyntaxNode, SyntaxNode>>> BinaryOps(params string[] opStr)
        {
            var opParsers = opStr.Select(x => All(x)).ToArray();
            return Select<string, Func<SyntaxNode, SyntaxNode, SyntaxNode>>(Between(Or(opParsers), Many(Any(Space()))), op => 
                (left, right) => new SyntaxNode(opTable[op], left, right)
            );
        }

        private Func<InputReader, ParserResult<char>> Any() 
        {
            return (input) => 
            {
                if (input.EOF())
                {
                    return ParserResult<char>.Failure($"Unexpected end of file");
                }
                var ch = input.Read();
                return ParserResult<char>.Success(ch);                
            };
        }

        private Func<InputReader, ParserResult<T>> Where<T>(Func<InputReader, ParserResult<T>> parser, Func<T, bool> predicate, Func<T, string> predicateFailMessage = null)
        {
            return (input) =>
            {
                var result = parser(input);
                if (!result.IsSuccess) 
                {
                    return ParserResult<T>.Failure(result.ErrorMessage);
                }

                if(!predicate(result.Value))
                {
                    var message = predicateFailMessage != null ? predicateFailMessage(result.Value) : "Condition not meet";
                    return ParserResult<T>.Failure(message);
                }
                return ParserResult<T>.Success(result.Value);
            };
        }

        private Func<InputReader, ParserResult<char>> Char(char c) 
        {
            return Where(Any(), ch => ch == c, ch => $"Input {ch} is expected value {c}.");
        }

        private Func<InputReader, ParserResult<string>> All(string str)
        {
            return (input) => 
            {
                foreach (char c in str)
                {
                    var ch = input.Read();
                    if (ch != c)
                    {
                        return ParserResult<string>.Failure($"Unexpected input, expecting {c}, actual {ch}");
                    }
                };
                return ParserResult<string>.Success(str);
            };
        }

        private Func<InputReader, ParserResult<char>> Not(string str) 
        {
            return Where(Any(), c => !str.Contains(c), c => $"Input {c} should not in {str}.");
        }

        private Func<InputReader, ParserResult<char>> Any(string str) 
        {
            return Where(Any(), c => str.Contains(c), c => $"Input {c} is not in {str}.");
        }

        private Func<InputReader, ParserResult<string>> Str<T>(Func<InputReader, ParserResult<T>> parser)
        {
            return Select<T, string>(parser, v => v.ToString());
        }

        private Func<InputReader, ParserResult<T>> Try<T>(Func<InputReader, ParserResult<T>> parser)
        {
            return (input) => 
            {
                var position = input.GetPosition();
                var result = parser(input);
                if (!result.IsSuccess)
                {
                    input.Seek(position);
                }
                return result;
            };
        }

          private Func<InputReader, ParserResult<T>> Or<T>(params Func<InputReader, ParserResult<T>>[] parsers)
        {
            return (input) => 
            {
                var messages = new List<string>();

                foreach (var parser in parsers)
                {
                    var result = Try<T>(parser)(input);
                    if (result.IsSuccess)
                    {
                        return result;
                    }
                    messages.Add(result.ErrorMessage);
                }
                return ParserResult<T>.Failure(string.Join("<AND>", messages));
            };
        }

        private Func<InputReader, ParserResult<T>> Opt<T>(Func<InputReader, ParserResult<T>> parser, T defaultValue = default(T))
        {
            return (input) => 
            {
                var result = Try<T>(parser)(input);
                return result.IsSuccess ? result : ParserResult<T>.Success(defaultValue);
            };
        }

        private Func<InputReader, ParserResult<U>> Select<T, U>(Func<InputReader, ParserResult<T>> parser, Func<T, U> func)
        {
            return (input) => 
            {
                var result = parser(input);
                if (result.IsSuccess)
                {
                    return ParserResult<U>.Success(func(result.Value));
                }
                else
                {
                    return ParserResult<U>.Failure(result.ErrorMessage);
                }
            };
        }

        private Func<InputReader, ParserResult<IEnumerable<T>>> Many<T>(Func<InputReader, ParserResult<T>> parser)
        {
            return (input) => 
            {
                var result = new List<T>(){};
                while(true) 
                {
                    var part = Try<T>(parser)(input);
                    if (part.IsSuccess)
                    {
                        result.Add(part.Value);
                    }
                    else 
                    {
                        break;
                    }
                }
                return ParserResult<IEnumerable<T>>.Success(result);
            };
        }

        private Func<InputReader, ParserResult<string>> Many(Func<InputReader, ParserResult<char>> parser)
        {
            return (input) => 
            {
                var builder = new StringBuilder();
                while(true) 
                {
                    var part = Try<char>(parser)(input);
                    if (part.IsSuccess)
                    {
                        builder.Append(part.Value);
                    }
                    else 
                    {
                        break;
                    }
                }
                return ParserResult<string>.Success(builder.ToString());
            };
        }

        private Func<InputReader, ParserResult<IEnumerable<T>>> Many1<T>(Func<InputReader, ParserResult<T>> parser)
        {
            return (input) => 
            {
                var result = new List<T>(){};
                var item0 = parser(input);
                if (item0.IsSuccess)
                {
                    result.Add(item0.Value);
                }
                else
                {
                    return ParserResult<IEnumerable<T>>.Failure(item0.ErrorMessage);
                }
                var rest = Many<T>(parser)(input);
                result.AddRange(rest.Value);
                return ParserResult<IEnumerable<T>>.Success(result);
            };
        }

        private Func<InputReader, ParserResult<string>> Many1(Func<InputReader, ParserResult<char>> parser)
        {
            return (input) => 
            {
                var item0 = parser(input);
                if (!item0.IsSuccess)
                {
                    return ParserResult<string>.Failure(item0.ErrorMessage);
                }
                var rest = Many(parser)(input);
                return ParserResult<string>.Success(item0.Value.ToString() + rest.Value);
            };
        }

        private Func<InputReader, ParserResult<IEnumerable<T>>> Seq<T>(params Func<InputReader, ParserResult<T>>[] parsers)
        {
            return (input) => 
            {
                var result = new List<T>(){};
                foreach (var parser in parsers)
                {
                    var part = parser(input);
                    if (part.IsSuccess)
                    {
                        result.Add(part.Value);
                    }
                    else 
                    {
                        return ParserResult<IEnumerable<T>>.Failure(part.ErrorMessage);
                    }
                }
                return ParserResult<IEnumerable<T>>.Success(result);
            };
        }

        private Func<InputReader, ParserResult<T>> Aggregate<T>(Func<InputReader, ParserResult<T>>[] parsers, Func<T, T, T> func)
        {
            return (input) =>
            {
                var result = Seq(parsers)(input);
                if (result.IsSuccess)
                {
                    var finalValue = result.Value.Aggregate(func);
                    return ParserResult<T>.Success(finalValue);
                }
                else
                {
                    return ParserResult<T>.Failure(result.ErrorMessage);
                }
            };
        }

        private Func<InputReader, ParserResult<IEnumerable<T>>> SepBy<T, U>(Func<InputReader, ParserResult<T>> parser, Func<InputReader, ParserResult<U>> sep)
        {
            return (input) => 
            {
                var result = new List<T>(){};
                var first = Try(parser)(input);
                if (first.IsSuccess)
                {
                    result.Add(first.Value);

                    var rest = Many(Before(sep, parser))(input);
                    result.AddRange(rest.Value);
                }

                return ParserResult<IEnumerable<T>>.Success(result);
            };
        }

        private Func<InputReader, ParserResult<string>> Concat(params Func<InputReader, ParserResult<string>>[] parsers)
        {
            return Aggregate(parsers, (x, y) => x + y);
        }

        private Func<InputReader, ParserResult<string>> Concat(Func<InputReader, ParserResult<IEnumerable<string>>> parser)
        {
            return Select<IEnumerable<string>, string>(parser, x => x.Aggregate(string.Empty, (a, b) => a + b));
        }


        private Func<InputReader, ParserResult<T>> Between<T, U>(Func<InputReader, ParserResult<T>> parser, Func<InputReader, ParserResult<U>> sep)
        {
            return After(Before(sep, parser), sep);
        }

        private Func<InputReader, ParserResult<T>> Between<T, U, V>(Func<InputReader, ParserResult<T>> parser, Func<InputReader, ParserResult<U>> sep1, Func<InputReader, ParserResult<V>> sep2)
        {
            return After(Before(sep1, parser), sep2);
        }
        private Func<InputReader, ParserResult<U>> Before<T, U>(Func<InputReader, ParserResult<T>> parser1, Func<InputReader, ParserResult<U>> parser2)
        {
            return (input) => 
            {
                var result1 = parser1(input);
                if (!result1.IsSuccess)
                {
                    return ParserResult<U>.Failure(result1.ErrorMessage);
                }
                return parser2(input);
            };
        }

        private Func<InputReader, ParserResult<T>> After<T, U>(Func<InputReader, ParserResult<T>> parser1, Func<InputReader, ParserResult<U>> parser2)
        {
            return (input) => 
            {
                var result1 = parser1(input);
                if (!result1.IsSuccess)
                {
                    return ParserResult<T>.Failure(result1.ErrorMessage);
                }

                var result2 = parser2(input);
                if (!result2.IsSuccess)
                {
                    return ParserResult<T>.Failure(result2.ErrorMessage);
                }
                return result1;
            };
        }
        
        private Func<InputReader, ParserResult<T>> ChainL<T>(Func<InputReader, ParserResult<T>> parser, Func<InputReader, ParserResult<Func<T, T, T>>> op)
        {
            return (input) => 
            {
                var result = parser(input);
                if (!result.IsSuccess)
                {
                    return ParserResult<T>.Failure(result.ErrorMessage);
                }
                
                while (true)
                {
                    var opResult = Try(op)(input);
                    if (!opResult.IsSuccess)
                    {
                        break;
                    }
                    var result2 = parser(input);
                    if (!result2.IsSuccess)
                    {
                        return ParserResult<T>.Failure(result2.ErrorMessage);
                    }
                    result.Value = opResult.Value(result.Value, result2.Value);
                }
                return result;
            };
        }

        private Func<InputReader, ParserResult<T>> ChainR<T>(Func<InputReader, ParserResult<T>> parser, Func<InputReader, ParserResult<Func<T, T, T>>> op)
        {
            Func<InputReader, ParserResult<T>> right = null;

            right = (input) => 
            {
                var result = parser(input);
                if (!result.IsSuccess)
                {
                    return ParserResult<T>.Failure(result.ErrorMessage);
                }
                
                var opResult = Try(op)(input);
                if (opResult.IsSuccess)
                {
                    var rightResult = right(input);
                    if (!rightResult.IsSuccess)
                    {
                        return ParserResult<T>.Failure(rightResult.ErrorMessage);
                    }
                    result.Value = opResult.Value(result.Value, rightResult.Value);
                }
                return result;
            };

            return right;
        }

        private Func<InputReader, ParserResult<SyntaxNode>> Terminal(Func<InputReader, ParserResult<string>> parser, SyntaxKind kind)
        {
            return Select(parser, str => (SyntaxNode)new Terminal(kind, str));
        }

        private Func<InputReader, ParserResult<T>> Spaced<T>(Func<InputReader, ParserResult<T>> parser)
        {
            return Between(parser, Many(Any(Space())));
        }

        private Func<InputReader, ParserResult<T>> Bracketed<T>(Func<InputReader, ParserResult<T>> parser)
        {
            return Between(parser, Any("("), Any(")"));
        }

        private Func<InputReader, ParserResult<T>> SquareBracketed<T>(Func<InputReader, ParserResult<T>> parser)
        {
            return Between(parser, Any("["), Any("]"));
        }

        private Func<InputReader, ParserResult<T>> CurlyBracketed<T>(Func<InputReader, ParserResult<T>> parser)
        {
            return Between(parser, Any("["), Any("]"));
        }

        private string ConcatStrs(IEnumerable<string> strs)
        {
            return string.Join("", strs);
        }

        public ParserResult<SyntaxNode> Parse(string input)
        {
            var inputReader = new InputReader(input);
            return parseExpression(inputReader);
        }

        private string Letter()
        {
            return "abcdefghijklmnopqrstuvwxyzABCEDEFGHIJKLMNOPQRSTUVWXYZ";
        }

        private string Digit()
        {
            return "0123456789";
        }

        private string Space()
        {
            return " \t\r\n";
        }

        private Func<InputReader, ParserResult<SyntaxNode>> LazyParseExpression()
        {
            return (input) => 
            {
                return parseExpression(input);
            };
        }
        private Func<InputReader, ParserResult<SyntaxNode>> LazyParseID()
        {
            return (input) => 
            {
                return parseID(input);
            };
        }

        private Func<InputReader, ParserResult<SyntaxNode>> parseExpression;
        private Func<InputReader, ParserResult<SyntaxNode>> parseID;
        public Func<InputReader, ParserResult<SyntaxNode>> parseStringLiteral2;
    }
}