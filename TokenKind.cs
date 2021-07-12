namespace Parser
{
    public enum TokenKind
    {
        NONE,
        ID,
        NUM,
        STRING,

        WS, // WhiteSpace

        PLUS,
        MINUS,

        MUL,
        DIV,

        OPEN_BRACKET,
        CLOSE_BRACKET,

        OPEN_SQUARE_BRACKET,
        CLOSE_SQUARE_BRACKET,

        DOT,

        COMMA,

        EOF,

        TEXT // The default token
    }
}