namespace Parser.AST
{
    class SyntaxNode
    {
        public SyntaxKind Kind {get; set;}
        public SyntaxNode[] Children {get; set;}

        public SyntaxNode(SyntaxKind kind, params SyntaxNode[] children)
        {
            Kind = kind;
            Children = children;
        }

        public override string ToString()
        {
            return "(" + Kind.ToString() + "," + string.Join(",", (object[])Children) + ")";
        }
    }
}