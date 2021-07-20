using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

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
            var ID = Concat(Opt(Or(All("@@"), Str(Any("_#$@%")))), Str(Any(Letter())), Many(Any(Letter() + Digit() + "_")));
            
            var manyDigits = Many1(Any(Digit()));
            var NUM = Concat(manyDigits, Opt(Concat(Str(Char('.')), manyDigits)));

            var escapeChar = Concat(Str(Char('\\')), Str(Not(""))); 

            var stringLiteral1 = Concat(Str(Char('"')), Concat(Many(Or(Many1(Not("\\\"")), escapeChar))), Str(Char('"')));
            var stringLiteral2 = Concat(Str(Char('\'')), Concat(Many(Or(Many1(Not("\\'")), escapeChar))), Str(Char('\'')));
            var stringLiteral = Or(stringLiteral1, stringLiteral2);

            var parseID = Tokenize(ID, TokenKind.ID);
            var parseNUM = Tokenize(NUM, TokenKind.NUM);
            var parseSTRING = Tokenize(stringLiteral, TokenKind.STRING);

            parseStringLiteral2 = Tokenize(stringLiteral1, TokenKind.NUM);
            parseExpression = Or(parseID, parseNUM, parseSTRING);
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

        private Func<InputReader, ParserResult<T>> Where<T>(Func<InputReader, ParserResult<T>> parser, Func<T, bool> predicate)
        {
            return (input) =>
            {
                var result = parser(input);
                if (!result.IsSuccess || !predicate(result.Value))
                {
                    return ParserResult<T>.Failure(result.ErrorMessage + " or condition is not meet");
                }
                return ParserResult<T>.Success(result.Value);
            };
        }

        private Func<InputReader, ParserResult<char>> Char(char c) 
        {
            return Where(Any(), ch => ch == c);
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
            return Where(Any(), c => !str.Contains(c));
        }

        private Func<InputReader, ParserResult<char>> Any(string str) 
        {
            return Where(Any(), c => str.Contains(c));
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
                foreach (var parser in parsers)
                {
                    var result = Try<T>(parser)(input);
                    if (result.IsSuccess)
                    {
                        return result;
                    }
                }
                return ParserResult<T>.Failure("Parse fail");
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

        private Func<InputReader, ParserResult<string>> Concat(params Func<InputReader, ParserResult<string>>[] parsers)
        {
            return Aggregate(parsers, (x, y) => x + y);
        }

        private Func<InputReader, ParserResult<string>> Concat(Func<InputReader, ParserResult<IEnumerable<string>>> parser)
        {
            return Select<IEnumerable<string>, string>(parser, x => x.Aggregate(string.Empty, (a, b) => a + b));
        }


        private Func<InputReader, ParserResult<SyntaxNode>> Tokenize(Func<InputReader, ParserResult<string>> parser, TokenKind kind)
        {
            return Select(parser, str => new SyntaxNode(new Token(kind, str)));
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
        private Func<InputReader, ParserResult<SyntaxNode>> parseExpression;
        public Func<InputReader, ParserResult<SyntaxNode>> parseStringLiteral2;
    }
}