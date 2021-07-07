using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Parser
{
    class ExpressionLexer
    {
        private int _index = 0;
        private string _text;
        private Token _curToken = null;

        // TokenKind => (SearchFunction, Firsts)
        // SearchFuntion is a function that search for such a token
        // Firsts is a string contains all possible chars to start this token
        private Dictionary<TokenKind, (Func<string>, string)> _lexerUnits = new Dictionary<TokenKind, (Func<string>, string)>();

        // Reverse map of _lexerUnits, first char => list of tokenizers
        // This is data structure that drives the final lexer's behavior when facing each char
        private Dictionary<char, List<Func<Token>>> _lexerMap;

        public ExpressionLexer(string text)
        {
            _text = text;

            _lexerUnits[TokenKind.PLUS] = (parseSingleChar('+'), "+");
            _lexerUnits[TokenKind.MINUS] = (parseSingleChar('-'), "-");
            _lexerUnits[TokenKind.MUL] = (parseSingleChar('*'), "*");
            _lexerUnits[TokenKind.DIV] = (parseSingleChar('/'), "/");
            _lexerUnits[TokenKind.OPEN_BRACKET] = (parseSingleChar('('), "(");
            _lexerUnits[TokenKind.CLOSE_BRACKET] = (parseSingleChar(')'), ")");
            _lexerUnits[TokenKind.WS] = (parseMultipleChars(" \t\r\n"), " \t\r\n");

            // TODO: it's possible to create another abstract that map certain firsts to certain sub parser and construct
            // an upper lever parser, so that we can split the pattern for ' and " to make it more efficent
            _lexerUnits[TokenKind.STRING] = (parseRegexPattern(@"\G(""([^""\\]|\\.)*"")|('([^'\\]|\\.)*')"), "'\"");

            _lexerUnits[TokenKind.ID] = (parseRegexPattern(@"\G(_|@|#|\*|\$|@@)?[a-zA-Z][a-zA-Z0-9_]*"), "_$#@" + letters());
            _lexerUnits[TokenKind.NUM] = (parseRegexPattern(@"\G[0-9]+(\.[0-9]+)?"), digits());

            buildLexerMap();
        }

        // Return the next token found, or EOF if hits the end
        // This function is "idempotent", means multiples calls will return the same result
        // Unless EatToken() is called to move the needle forward
        public Token NextToken()
        {
            if (_curToken != null)
            {
                return _curToken;
            }

            var token = findNextToken();
            _curToken = token;
            return token;
        }
        public void EatToken(TokenKind kind = TokenKind.NONE)
        {
            if (kind != TokenKind.NONE && NextToken().Kind != kind)
            {
                throw new System.Exception($"Unexpected token {NextToken().Kind}, expecting {kind}");
            }

            // Reset cur token
            _curToken = null;
        }

        // Actual execution of finding the next token and return
        private Token findNextToken()
        {
            if (_index >= _text.Length)
            {
                return new Token(TokenKind.EOF, "");
            }
            else
            {
                char curChar = _text[_index];
                if (_lexerMap.ContainsKey(curChar)) 
                {
                    // this char is registered with valid parsers
                    var result = new Token(TokenKind.EOF, "");
                    foreach (var parser in _lexerMap[curChar])
                    {
                        var tmp = parser();
                        if (tmp != null && tmp.Text.Length > result.Text.Length)
                        {
                            result = tmp;
                        }
                    }
                    if (result.Kind != TokenKind.EOF)
                    {
                        return result;
                    }
                }
                // Default token is TEXT
                _index++;
                return new Token(TokenKind.TEXT, curChar.ToString());
            }
        }

        // Build the reverse map _lexerMap based on _lexerUnits
        private void buildLexerMap()
        {
            _lexerMap = new Dictionary<char, List<Func<Token>>>();

            foreach (var kv in _lexerUnits)
            {   
                var firsts = kv.Value.Item2;

                foreach (char c in firsts)
                {
                    if (!_lexerMap.ContainsKey(c))
                    {
                        _lexerMap[c] = new List<Func<Token>>();
                    }
                    _lexerMap[c].Add(createTokenier(kv.Value.Item1, kv.Key));
                }
            }
        }

        // Create a tokenier by binding a certain search function with the expected token kind to return
        private Func<Token> createTokenier(Func<string> searchFunc, TokenKind kind)
        {
            return () => 
            {
                var result = searchFunc();
                if (result != null) 
                {
                    return new Token(kind, result);
                }
                return null;
            };
        }

        // Create a function that will look for a specific char in current position
        private Func<string> parseSingleChar(char c)
        {
            return () =>
            {
                if (_text[_index] == c)
                {
                    return _text[_index++].ToString();
                }
                return null;
            };
        }

        // Create a function that will look for a char in chars in current position
        private Func<string> parseMultipleChars(string chars)
        {
            return () => 
            {
                if (chars.Contains(_text[_index]))
                {
                    return _text[_index++].ToString();
                }
                return null;
            };
        }


        // Create a function that search for a specific path in current position
        private Func<string> parseRegexPattern(string pattern) 
        {
            return () => 
            {
                var reg = new Regex(pattern);
                var match = reg.Match(_text, _index);

                if (match.Success)
                {
                    _index = _index + match.Length;
                    return _text.Substring(_index-match.Length, match.Length);
                }
                return null;
            };
        }

        private string letters()
        {
            return "abcdefjhijklmnopqrstuvwxyzABCEDEFHIJKLMNOPQRSTUVWXYZ";
        }

        private string digits()
        {
            return "0123456789";
        }

    }
}