namespace Parser
{
    class Expression 
    {
        private string _op = "null";
        private Expression _child1 = null;
        private Expression _child2 = null;

        public Expression()
        {

        }
        public Expression(string op, Expression child1, Expression child2 = null) 
        {
            _op = op;
            _child1 = child1;
            _child2 = child2 ?? null;
        }
        
        public override string ToString()
        {
            if (_child2 != null) 
            {
                return "(" + _op + "," + _child1.ToString() + "," + _child2.ToString() + ")";
                
            } 
            else 
            {
                return "(" + _op + "," + _child1.ToString() + ")";
            }
        }
    }
}