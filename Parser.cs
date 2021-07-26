using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Parser.AST;

namespace Parser
{
    class Parser 
    {
        public delegate ParserResult<T> IParser<T>(InputReader input);

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
            var parsePrimaryExpression = Debug(ParsePrimaryExpression(), "ParsePrimaryExpression");
            var parsePostfixExpression = ParsePostfixExpression(parsePrimaryExpression);
           
            parseExpression = ChainR(parsePostfixExpression, Debug(BinaryOps("^"), "Looking OP ^"));
            parseExpression = ParseUniaryExpression(parseExpression, UniaryOps("+", "-", "!"));

            parseExpression = ChainL(parseExpression, Debug(BinaryOps("*", "/", "%"), "Looking OP *, /, %"));
            parseExpression = ChainL(parseExpression, Debug(BinaryOps("+", "-"), "Looking OP +, -"));
            parseExpression = ChainL(parseExpression, Debug(BinaryOps("==", "!="), "Looking OP ==, !="));
            parseExpression = ChainL(parseExpression, Debug(BinaryOps(">=", ">", "<=", "<"), "Looking OP <=, <, >=, >"));
            parseExpression = ChainL(parseExpression, Debug(BinaryOps("&&"), "Looking OP &&"));
            parseExpression = ChainL(parseExpression, Debug(BinaryOps("||"), "Looking OP ||"));
            parseExpression = ChainL(parseExpression, Debug(BinaryOps("??"), "Looking OP ??"));
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
            {"??", SyntaxKind.NullCoalescExpression},
            {"!", SyntaxKind.NotExpression}
        };


        private IParser<SyntaxNode> ParsePrimaryExpression()
        {   
            var ID = Concat(Opt(Or(All("@@"), Str(Any("_#$@%")))), Str(Any(Letter())), Many(Any(Letter() + Digit() + "_")));
            
            var manyDigits = Many1(Any(Digit()));
            var NUM = Concat(manyDigits, Opt(Concat(Str(Char('.')), manyDigits)));

            var escapeChar = Concat(Str(Char('\\')), Str(Not(""))); 

            var stringLiteral1 = Concat(Str(Char('"')), Concat(Many(Or(Many1(Not("\\\"")), escapeChar))), Str(Char('"')));
            var stringLiteral2 = Concat(Str(Char('\'')), Concat(Many(Or(Many1(Not("\\'")), escapeChar))), Str(Char('\'')));
            //var stringLiteral = Or(stringLiteral1, stringLiteral2);

            parseID = Terminal(ID, SyntaxKind.IdentiferToken);
            var parseNUM = Terminal(NUM, SyntaxKind.NumToken);
            //var parseSTRING = Terminal(stringLiteral, SyntaxKind.StringToken);

            var parseArgList = SquareBracketed(SepBy(Spaced(LazyParseExpression()), Char(',')));
            var parseArrayCreation = Select(parseArgList, argList => { return new SyntaxNode(SyntaxKind.ArrayCreationExpression, argList.ToArray());});

            var lookAhead = LookAhead();

            var brackedExpression = Bracketed(Spaced(LazyParseExpression()));

            return (input) =>
            {
                var next = lookAhead(input);
                if (!next.IsSuccess)
                {
                    return ParserResult<SyntaxNode>.Failure(next.ErrorMessage);
                }
                
                switch (next.Value)
                {
                    case '(':
                        return brackedExpression(input);
                    case '[':
                        return parseArrayCreation(input);
                    case '"':
                        return Terminal(stringLiteral1, SyntaxKind.StringToken)(input);
                    case '\'':
                        return Terminal(stringLiteral2, SyntaxKind.StringToken)(input);
                    default:
                        if (Digit().Contains(next.Value))
                        {
                            return parseNUM(input);
                        }
                        if ((Letter()+"$#_@%").Contains(next.Value))
                        {
                            return parseID(input);
                        }
                        break;
                }
                return ParserResult<SyntaxNode>.Failure($"Can't parse out a primary expression, unexpected char {next.Value}");
            };
        }


        private IParser<char> LookAhead()
        {
            return (input) => 
            {
                var pos = input.GetPosition();
                var result = Any()(input);
                input.Seek(pos);
                return result;
            };
        }


        private IParser<SyntaxNode> ParsePostfixExpression(IParser<SyntaxNode> parser)
        {
            var parseProperty = Before(Char('.'), LazyParseID());
            var parseIndex = SquareBracketed(Spaced(LazyParseExpression()));
            var parseArgList = Bracketed(SepBy(Spaced(LazyParseExpression()), Char(',')));
            var lookAhead = LookAhead();

            return (input) =>
            {
                var primary = parser(input);
                if (!primary.IsSuccess)
                {
                    return ParserResult<SyntaxNode>.Failure(primary.ErrorMessage);
                }
                
                while(true)
                {
                    var next = lookAhead(input);
                    if (!next.IsSuccess)
                    {
                        break;
                    }

                    var matched = true;
                    switch (next.Value)
                    {
                        case '.':
                            var property = parseProperty(input);
                            if (!property.IsSuccess)
                            {
                                return ParserResult<SyntaxNode>.Failure("Can't get an identifer after .");
                            }
                            primary.Value = new SyntaxNode(SyntaxKind.AccessExpression, primary.Value, property.Value);
                            break;
                        case '[':
                            var index = parseIndex(input);
                            if (!index.IsSuccess)
                            {
                                return ParserResult<SyntaxNode>.Failure("Can't get an expression after [");
                            }
                            primary.Value = new SyntaxNode(SyntaxKind.ElementExpression, primary.Value, index.Value);
                            break;
                        case '(':
                            var argList = parseArgList(input);
                            if (!argList.IsSuccess)
                            {
                                return ParserResult<SyntaxNode>.Failure("Can't get arg list after (");
                            }
                            primary.Value = new SyntaxNode(SyntaxKind.InvokeExpression, primary.Value);
                            primary.Value.Children = primary.Value.Children.Concat(argList.Value).ToArray();
                            break;
                        default:
                            matched = false;
                            break;
                    }

                    if (!matched)
                    {
                        break;
                    }
                }
                
                return primary;
            };
        }


        private IParser<SyntaxNode> ParseUniaryExpression(IParser<SyntaxNode> parser,IParser<Func<SyntaxNode, SyntaxNode>> op)
        {
            return (input) =>
            {
                var ops = Many(op)(input);
                var right = parser(input);
                if (!right.IsSuccess)
                {
                    return ParserResult<SyntaxNode>.Failure(right.ErrorMessage);
                }

                foreach (var op in ops.Value.Reverse())
                {
                    right.Value = op(right.Value);
                }
                return right;
            };
        }
        private IParser<Func<SyntaxNode, SyntaxNode, SyntaxNode>> BinaryOps(params string[] opStr)
        {
            var opParsers = opStr.Select(x => All(x)).ToArray();
            return Select<string, Func<SyntaxNode, SyntaxNode, SyntaxNode>>(Between(Or(opParsers), Spaces()), op => 
                (left, right) => new SyntaxNode(opTable[op], left, right)
            );
        }

        private IParser<Func<SyntaxNode, SyntaxNode>> UniaryOps(params string[] opStr)
        {
            var opParsers = opStr.Select(x => All(x)).ToArray();
            return Select<string, Func<SyntaxNode, SyntaxNode>>(Or(opParsers), op => 
                right => new SyntaxNode(opTable[op], right)
            );
        }

        private IParser<char> Any() 
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

        private IParser<T> Where<T>(IParser<T> parser, Func<T, bool> predicate, Func<T, string> predicateFailMessage = null)
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

        private IParser<char> Char(char c) 
        {
            return Where(Any(), ch => ch == c, ch => $"Input {ch} is expected value {c}.");
        }

        private IParser<string> All(string str)
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

        private IParser<char> Not(string str) 
        {
            return Where(Any(), c => !str.Contains(c), c => $"Input {c} should not in {str}.");
        }

        private IParser<char> Any(string str) 
        {
            return Where(Any(), c => str.Contains(c), c => $"Input {c} is not in {str}.");
        }

        private IParser<string> Str<T>(IParser<T> parser)
        {
            return Select<T, string>(parser, v => v.ToString());
        }

        private IParser<T> Try<T>(IParser<T> parser)
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

        private IParser<T> Or<T>(params IParser<T>[] parsers)
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

        private IParser<T> Opt<T>(IParser<T> parser, T defaultValue = default(T))
        {
            return (input) => 
            {
                var result = Try<T>(parser)(input);
                return result.IsSuccess ? result : ParserResult<T>.Success(defaultValue);
            };
        }

        private IParser<U> Select<T, U>(IParser<T> parser, Func<T, U> func)
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

        private IParser<IEnumerable<T>> Many<T>(IParser<T> parser)
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

        private IParser<string> Many(IParser<char> parser)
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

        private IParser<IEnumerable<T>> Many1<T>(IParser<T> parser)
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

        private IParser<string> Many1(IParser<char> parser)
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

        private IParser<IEnumerable<T>> Seq<T>(params IParser<T>[] parsers)
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

        private IParser<T> Aggregate<T>(IParser<T>[] parsers, Func<T, T, T> func)
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

        private IParser<IEnumerable<T>> SepBy<T, U>(IParser<T> parser, IParser<U> sep)
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

        private IParser<string> Concat(params IParser<string>[] parsers)
        {
            return Aggregate(parsers, (x, y) => x + y);
        }

        private IParser<string> Concat(IParser<IEnumerable<string>> parser)
        {
            return Select<IEnumerable<string>, string>(parser, x => x.Aggregate(string.Empty, (a, b) => a + b));
        }


        private IParser<T> Between<T, U>(IParser<T> parser, IParser<U> sep)
        {
            return After(Before(sep, parser), sep);
        }

        private IParser<T> Between<T, U, V>(IParser<T> parser, IParser<U> sep1, IParser<V> sep2)
        {
            return After(Before(sep1, parser), sep2);
        }
        private IParser<U> Before<T, U>(IParser<T> parser1, IParser<U> parser2)
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

        private IParser<T> After<T, U>(IParser<T> parser1, IParser<U> parser2)
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
        
        private IParser<T> ChainL<T>(IParser<T> parser, IParser<Func<T, T, T>> op)
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

        private IParser<T> ChainR<T>(IParser<T> parser, IParser<Func<T, T, T>> op)
        {
            IParser<T> right = null;

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

        private IParser<SyntaxNode> Terminal(IParser<string> parser, SyntaxKind kind)
        {
            return Select(parser, str => (SyntaxNode)new Terminal(kind, str));
        }

        private IParser<T> Spaced<T>(IParser<T> parser)
        {
            return Between(parser, Spaces());
        }

        private IParser<T> Bracketed<T>(IParser<T> parser)
        {
            return Between(parser, Char('('), Char(')'));
        }

        private IParser<T> SquareBracketed<T>(IParser<T> parser)
        {
            return Between(parser, Char('['), Char(']'));
        }

        private IParser<T> CurlyBracketed<T>(IParser<T> parser)
        {
            return Between(parser, Char('{'), Char('}'));
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

        private IParser<string> Spaces()
        {
            return (input) => 
            {
                var builder = new StringBuilder();
                while (true)
                {
                    var c = input.Read();
                    if (!" \t\r\n".Contains(c))
                    {
                        input.Seek(input.GetPosition()-1);                        
                        break;
                    }
                    builder.Append(c);
                }
                return ParserResult<string>.Success(builder.ToString());
            };
        }

        private IParser<SyntaxNode> LazyParseExpression()
        {
            return (input) => 
            {
                return parseExpression(input);
            };
        }
        private IParser<SyntaxNode> LazyParseID()
        {
            return (input) => 
            {
                return parseID(input);
            };
        }

        private IParser<T> Debug<T>(IParser<T> parser, string label)
        {
            return (input) => 
            {
                if (debug)
                {
                    var indent = string.Concat(Enumerable.Repeat("  ", debugLevel));
                    Console.WriteLine($"{indent}Enter {label}. <POS:{input.GetPosition()}>");
                    debugLevel++;
                }

                var result = parser(input);

                if (debug) 
                {
                    debugLevel--;
                    var indent = string.Concat(Enumerable.Repeat("  ", debugLevel));
                    Console.Write($"{indent}Leave {label}. ");
                    Console.Write(result.IsSuccess ? "<Success>":"<Failure>");
                    Console.WriteLine($" <POS:{input.GetPosition()}>");
                }

                return result;
            };
        }
        private IParser<SyntaxNode> parseExpression;
        private IParser<SyntaxNode> parseID;
        private bool debug = false;
        private int debugLevel = 0;
    }
}