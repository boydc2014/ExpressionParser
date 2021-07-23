using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace Parser
{
    public class Benchmarks
    {
        public static string[] expressions = {
            "[1, 2].length + [].length",
            "a + +b",
            "f.g(x) != f(g(x))",
            "!!!!++a.b != 0",
            "-2 + a == 3 + 4",
            "a != b^2",
            "fun[a+2](a).b.c + c",
            "func(2).a.b + 3",
            "1 + exp(2, 3)",
            "a + div(3 + 2",
            "add(1, 2)",
            "\"hel lo\" + 'world'",
            "'hello' + 'world'",
            "_test + 5.5",
            "hello+world",
            "1+2",
            "a+1",
            "a+b*1+2",
            "(a+b)*1+2",
            "(1)+2",
            "(1/2"
        };

        private Regex creg = new Regex(@"\G(_|@|#|\*|\$|@@)?[a-zA-Z][a-zA-Z0-9_]*", RegexOptions.Compiled);

        [Benchmark]        
        public void TestDirectParser()
        {
            var parser = new Parser();
            foreach(var e in expressions)
            {
                parser.Parse(e);
            }
        }

        [Benchmark]
        public void TestTraditionalParser()
        {
            foreach (var e in expressions)
            {
                var parser = new ExpressionParser();
                try 
                {
                    var exp = parser.ParseExpression(e);
                }
                catch
                {
                    //System.Console.WriteLine(ex.Message);
                }
            }
        }

        // [Benchmark]
        // public void TestDirectRegex()
        // {
        //     foreach (var e in expressions)
        //     {
        //         var reg = new Regex(@"\G(_|@|#|\*|\$|@@)?[a-zA-Z][a-zA-Z0-9_]*");
        //         reg.Match(e);
        //     }
        // }

        // [Benchmark]
        // public void TestCompiledRegex()
        // {
        //     foreach (var e in expressions)
        //     {
        //         creg.Match(e);
        //     }
        // }
    }
}