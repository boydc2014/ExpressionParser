namespace Parser.AST
{
    class Terminal: SyntaxNode
    {
        public string Value {get; set; }
        public Terminal(SyntaxKind kind, string value): base(kind)
        {
            Value = value;
        }
        public override string ToString()
        {
            return Value;
        }
    }
}