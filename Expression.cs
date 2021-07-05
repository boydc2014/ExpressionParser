namespace Parser
{
    class Expression 
    {
        private Token _op;
        private Expression _child1;
        private Expression _child2;

        public Expression()
        {

        }
        public Expression(Token op, Expression child1, Expression child2 = null) 
        {
            _op = op;
            _child1 = child1;
            _child2 = child2 ?? null;
        }
        
        public override string ToString()
        {
            if (_child2 != null) 
            {
                return "(" + _op.Kind + "," + _child1.ToString() + "," + _child2.ToString() + ")";
                
            } 
            else 
            {
                return "(" + _op.Kind + "," + _child1.ToString() + ")";
            }
        }
    }
}