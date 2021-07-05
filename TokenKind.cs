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

        EOF,

        TEXT // The default token
    }
}