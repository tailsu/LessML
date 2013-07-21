using System.IO;
using LessML;
using LessML.Xaml;

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
