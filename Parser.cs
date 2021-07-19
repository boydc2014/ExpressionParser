using System;
using System.Collections.Generic;

namespace Parser
{
    class Parser 
    {
        class InputReader 
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
               
        }

        private Func<InputReader, ParserResult<string>> PChar(char c) 
        {
            return (input) => 
            {
                var ch = input.Read();
                if (ch == c)
                {
                    return ParserResult<string>.Success(c.ToString());
                }
                return ParserResult<string>.Failure($"Unexpected input, expecting {c}, actual {ch}");
            };
        }

        private Func<InputReader, ParserResult<string>> PString(string str)
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

        private Func<InputReader, ParserResult<string>> AnyChar(string str) 
        {
            return (input) => 
            {
                var ch = input.Read();
                if (str.Contains(ch))
                {
                    return ParserResult<string>.Success(ch.ToString());
                }
                return ParserResult<string>.Failure($"Unexpected input, expecting any char in {str}, actual {ch}");
            };
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

        private Func<InputReader, ParserResult<T>> Opt<T>(Func<InputReader, ParserResult<T>> parser, T defaultValue)
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


        public ParserResult<SyntaxNode> Parse(string input)
        {
            var inputReader = new InputReader(input);
            var parseID = Select(Seq(Opt(Or(PString("@@"), AnyChar("_#$@%")), string.Empty),
                                     AnyChar(letters()),
                                     Select(Many(AnyChar(letters() + digits() + "_")), strs => string.Join("", strs))),
                                 strs => string.Join("", strs));
            
            parseExpression = Select(parseID, id => new SyntaxNode(new Token(TokenKind.ID, id)));

            return parseExpression(inputReader);
        }

        private string letters()
        {
            return "abcdefghijklmnopqrstuvwxyzABCEDEFGHIJKLMNOPQRSTUVWXYZ";
        }

        private string digits()
        {
            return "0123456789";
        }
        private Func<InputReader, ParserResult<SyntaxNode>> parseExpression;
    }
}