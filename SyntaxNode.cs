namespace Parser
{
    class SyntaxNode 
    {
        public Token Op {get; private set;}
        private SyntaxNode[] _children;

        public SyntaxNode(Token op, params SyntaxNode[] children) 
        {
            _children = children;
            Op = op;
        }
        
        public bool IsTerminal()
        {
            return _children.Length == 0;
        }

        public override string ToString()
        {
            if (_children.Length == 0)
            {
                return Op.Text;
            }
            else 
            {
                return "(" + Op.Kind.ToString() + "," + string.Join(",", (object[])_children) + ")";
            }
        }
    }
}