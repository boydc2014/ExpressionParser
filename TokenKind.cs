namespace Parser
{
    public enum TokenKind
    {
        ID,
        NUM,
        STRING,

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