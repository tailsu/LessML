using System.Linq;
using System.Xml.Linq;
using LessML.Macros;
using LessML.Vamp;

namespace LessML.Xaml
{
    public class LessXamlParser
    {
        public static XDocument Translate(string lessXamlProgram)
        {
            var rules = XamlTransformer.MakeRules();
            TemplateTransformer.AppendRules(rules);

            var ast = VampParser.Parse(lessXamlProgram, rules);
            MacroExpander.Transform(ast, new TemplateTransformer());
            var root = ast.Children.Single(n => n.Key != null);
            MacroExpander.Transform(root, new XamlTransformer());

            if (root.Children.FirstOrDefault(c => c.Key.Snippet == "xmlns" && c.Operator.Snippet == "=") == null)
                root.AddChild(MakeXmlnsDeclaration("xmlns", XamlPresentationNs));

            if (root.Children.FirstOrDefault(c => c.Key.Snippet == "xmlns:x" && c.Operator.Snippet == "=") == null)
                root.AddChild(MakeXmlnsDeclaration("xmlns:x", XamlNs));

            return XmlConverter.ToXml(ast);
        }

        private static VampNode MakeXmlnsDeclaration(string key, string ns)
        {
            var node = new VampNode
            {
                Key = key,
                Operator = "=",
            };
            node.SetValue(ns);
            return node;
        }

        public const string XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";
        public const string XamlPresentationNs = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    }
}