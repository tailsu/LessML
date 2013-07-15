using System.IO;
using LessXaml;

namespace LessXamlProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var source = File.ReadAllText(args[0]);
            var result = LessXamlParser.Translate(source);
            File.WriteAllText(args[1], result.ToString());
        }
    }
}
