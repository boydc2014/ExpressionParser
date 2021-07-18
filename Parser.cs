using System;
using System.Linq;

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

        private Func<InputReader, ParserResult<char>> parseChar(char c) 
        {
            return (input) => 
            {
                var ch = input.Read();
                if (ch == c)
                {
                    return ParserResult<char>.Success(c);
                }
                return ParserResult<char>.Failure($"Unexpected input, expecting {c}, actual {ch}");
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

        public ParserResult<SyntaxNode> Parse(string input)
        {
            var inputReader = new InputReader(input);
            return parseExpression(inputReader);
        }

        private Func<InputReader, ParserResult<SyntaxNode>> parseExpression;
    }
}