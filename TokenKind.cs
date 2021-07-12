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
        PERCENT,

        DOUBLE_EQUAL,
        NOT_EQUAL,

        SINGLE_AND,
        NOT,
        XOR,

        LESS_THAN,
        LESS_OR_EQUAl,
        MORE_THAN,
        MORE_OR_EQUAL,

        DOUBLE_AND,
        DOUBLE_VERTICAL_LINE,

        NULL_COALESCE,
        QUESTION_MARK,
        COLON,
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