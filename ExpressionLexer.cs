using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Parser
{
    class ExpressionLexer
    {
        private int _index = 0;
        private string _text;

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
            
            _lexerUnits[TokenKind.ID] = (parseRegexPattern(@"\G(_|@|#|\*|\$|@@)?[a-zA-Z][a-zA-Z0-9_]*"), "_$#@" + letters());
             _lexerUnits[TokenKind.NUM] = (parseRegexPattern(@"\G[0-9]+(\.[0-9]+)?"), digits());

            buildLexerMap();
        }

        // Return the next token found, or EOF if hits the end
        public Token NextToken()
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
        public void EatToken(string token = null)
        {
            if (token != null && NextToken().Text != token)
            {
                throw new System.Exception($"Unexpected token {NextToken().Text}, expecting {token}");
            }

            _index++;
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
                    _index++;
                    return c.ToString();
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