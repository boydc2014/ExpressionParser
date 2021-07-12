namespace Parser
{
    class SyntaxNode 
    {
        public Token Op {get; private set;}
        public SyntaxNode[] Children {get; set;}

        public SyntaxNode(Token op, params SyntaxNode[] children) 
        {
            Children = children;
            Op = op;
        }
        
        public bool IsTerminal()
        {
            return Children.Length == 0;
        }

        public override string ToString()
        {
            if (Children.Length == 0)
            {
                return Op.Text;
            }
            else 
            {
                return "(" + Op.Kind.ToString() + "," + string.Join(",", (object[])Children) + ")";
            }
        }
    }
}