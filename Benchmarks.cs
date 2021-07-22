using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace Parser
{
    public class Benchmarks
    {
        public static string[] expressions = {
            "1 -2",
            "1+2",
            "_abcd + bc",
            "hello",
            "a1",
            "1a",
            " a1234 ",
            " $myHello ",
            "__",
            "123",
            ".123",
            " 456.",
            "456.1234",
            "0.123456",
            "\"abc\"",
            "'abc'",
            "\"abc\\\"abc\"",
            "'\'\'\'\'\'\'\''",
            "\"hello world '' \""
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