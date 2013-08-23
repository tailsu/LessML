using System.IO;
using System.Xml.Linq;
using LessML;
using LessML.Xaml;

namespace LessXamlProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            var source = File.ReadAllText(args[0]);
            var vamp = XmlConverter.FromXml(XDocument.Parse(source));
            File.WriteAllText(args[1], StringConverter.ToString(vamp, "\t"));
            //var result = LessXamlParser.Translate(source);
            //File.WriteAllText(args[1], result.ToString());
        }
    }
}
