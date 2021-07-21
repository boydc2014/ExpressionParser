namespace Parser.AST
{
    enum SyntaxKind
    {
        # region Arithmetic Opertaions
        PlusExpression,  // +
        MinusExpression,  // -
        MulExpression,  // *
        DivExpression,  // /
        PercentExpression, // %
        NullCoalescExpression, // ??
        # endregion

        # region Bit Operations
        BitAndExpression, // &
        BitOrExpression,  // |
        BitXorExpression, // ^
        # endregion

        # region Logical Operations
        NotExpression, // !
        LogicalAndExpression, // &&
        LogicalOrExpression, // ||
        EqualExpression, // ==
        NotEqualExpression, // !=
        GreaterThanExpression, // >
        GreaterOrEqualExpression, // >=
        LessThanExpression, // <
        LessOrEqualExpression, // <=
        # endregion

        # region Postfix Operations
        InvokeExpression, // a(b)
        AccessExpression, // a.b
        ElementExpression, // a[b]
        # endregion

        # region Creation Operations
        ArrayCreationExpression, // [a, b, c]
        # endregion

        # region Tokens
        IdentiferToken,
        StringToken,
        NumToken
        # endregion
    }
}