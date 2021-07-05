using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Parser
{
    class ExpressionLexer
    {
        private int _index = 0;
        private string _text;

        private Dictionary<TokenKind, Func<Token>> _tokenLexer = new Dictionary<TokenKind, Func<Token>>();
        private Dictionary<TokenKind, string> _tokenFirsts = new Dictionary<TokenKind, string>();

        // reverse map, first char => possible lexer(s)
        private Dictionary<char, List<Func<Token>>> _lexerMap;


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

        private Func<string> parseRegexPattern(string pattern) 
        {
            return () => 
            {
                var reg = new Regex(pattern);
                var match = reg.Match(_text, _index);

                if (match.Success)
                {
                    _index = _index + match.Length;
                    return new Token(kind, _text.Substring(_index-match.Length, match.Length));
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

        public ExpressionLexer(string text)
        {
            _text = text;
            _tokenLexer[TokenKind.PLUS] = parseSingleChar('+', TokenKind.PLUS);
            _tokenFirsts[TokenKind.PLUS] = "+";

            _tokenLexer[TokenKind.MINUS] = parseSingleChar('-', TokenKind.MINUS);
            _tokenFirsts[TokenKind.MINUS] = "-";

            _tokenLexer[TokenKind.MUL] = parseSingleChar('*', TokenKind.MUL);
            _tokenFirsts[TokenKind.MUL] = "*";
            
            _tokenLexer[TokenKind.ID] = parseRegexPattern(@"\G(_|@|#|\*|\$|@@)?[a-zA-Z][a-zA-Z0-9_]*", TokenKind.ID);
            _tokenFirsts[TokenKind.ID] = "_$#@" + letters();

            _tokenLexer[TokenKind.NUM] = parseRegexPattern(@"\G[0-9]+(\.[0-9]+)?", TokenKind.NUM);
            _tokenFirsts[TokenKind.NUM] = digits();

            buildLexerMap();
        }

        private void buildLexerMap()
        {
            _lexerMap = new Dictionary<char, List<Func<Token>>>();

            foreach (var kv in _tokenLexer)
            {
                if (!_tokenFirsts.ContainsKey(kv.Key))
                {
                    throw new Exception($"Undefined firsts for: {kv.Key}");
                }
                var firsts = _tokenFirsts[kv.Key];

                foreach (char c in firsts)
                {
                    if (!_lexerMap.ContainsKey(c))
                    {
                        _lexerMap[c] = new List<Func<Token>>();
                    }
                    _lexerMap[c].Add(kv.Value);
                }
            }
        }

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
    }
}