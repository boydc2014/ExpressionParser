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

        EOF,

        TEXT // The default token
    }
}