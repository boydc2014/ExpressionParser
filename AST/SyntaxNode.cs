namespace Parser.AST
{
    class SyntaxNode
    {
        public SyntaxKind Kind {get; set;}
        public SyntaxNode[] Children {get; set;}

        public SyntaxNode(SyntaxKind kind, SyntaxNode[] children = null)
        {
            Kind = kind;
            Children = children ?? new SyntaxNode[]{};
        }    
    }
}