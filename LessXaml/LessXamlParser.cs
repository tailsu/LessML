using System;
using System.Linq;
using System.Xml.Linq;
using LessML;

namespace LessXaml
{
    public class LessXamlParser
    {
        public static XElement Translate(string lessXamlProgram)
        {
            var rules = new VampRules();
            rules.DefaultOp = ":";
            rules.Operators.Clear();
            rules.Operators.AddRange(new[] { "=", "->", ":=", ":" });
            var ast = VampParser.Parse(lessXamlProgram, rules);

            var stub = new XElement("stub");
            Transform(ast.First(), stub);
            var result = stub.Elements().First();

            result.Add(new XAttribute("xmlns", XamlPresentationNs));
            result.Add(new XAttribute(XNamespace.Xmlns + "x", XamlNs));
            stub.RemoveAll();
            return result;
        }

        private static string FormatValue(string value)
        {
            if (value.StartsWith("**"))
            {
                value = value.Substring(2);
                value = "{DynamicResource " + value + "}";
            }
            else if (value.StartsWith("*"))
            {
                value = value.Substring(1);
                value = "{StaticResource " + value + "}";
            }
            return value;
        }

        public const string XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";
        public const string XamlPresentationNs = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        private static void Transform(VampNode node, XElement parent)
        {
            //XElement thisElement = null;
            //switch (node.Operator.Snippet)
            //{
            //    case ":":
            //        thisElement = new XElement(XName.Get(node.Key, XamlPresentationNs));
            //        if (!String.IsNullOrEmpty(node.Value))
            //            thisElement.Add(node.Value);
            //        parent.Add(thisElement);
            //        break;

            //    case "->":
            //        {
            //            if (node.Key.StartsWith("."))
            //            {
            //                var valueElement = new XElement(XName.Get("Setter.Value", XamlPresentationNs));
            //                var setter = new XElement(XName.Get("Setter", XamlPresentationNs),
            //                    new XAttribute("Property", node.Key.Substring(1)),
            //                    valueElement);
            //                parent.Add(setter);
            //                thisElement = new XElement(XName.Get(node.Value, XamlPresentationNs));
            //                valueElement.Add(thisElement);
            //            }
            //            else
            //            {
            //                var propertyElement = new XElement(XName.Get(parent.Name.LocalName + "." + node.Key, XamlPresentationNs));
            //                parent.Add(propertyElement);
            //                thisElement = new XElement(XName.Get(node.Value, XamlPresentationNs));
            //                propertyElement.Add(thisElement);
            //            }
            //        }
            //        break;

            //    case "=":
            //        {
            //            if (node.Key.StartsWith("."))
            //            {
            //                var setter = new XElement(XName.Get("Setter", XamlPresentationNs),
            //                    new XAttribute("Property", node.Key.Substring(1)),
            //                    new XAttribute("Value", FormatValue(node.Value)));
            //                parent.Add(setter);
            //            }
            //            else
            //            {
            //                var key = node.Key;
            //                string nsp = "";
            //                if (key.StartsWith("$"))
            //                {
            //                    key = key.Substring(1);
            //                    nsp = XamlNs;
            //                }
            //                var propertyElement = new XAttribute(XName.Get(key, nsp), FormatValue(node.Value));
            //                parent.Add(propertyElement);
            //            }
            //        }
            //        break;

            //    case ":=":
            //        {
            //            var thisParent = parent;
            //            if (node.Key.StartsWith("."))
            //            {
            //                var valueElement = new XElement(XName.Get("Setter.Value", XamlPresentationNs));
            //                var setter = new XElement(XName.Get("Setter", XamlPresentationNs),
            //                    new XAttribute("Property", node.Key.Substring(1)),
            //                    valueElement);
            //                parent.Add(setter);
            //                thisParent = valueElement;
            //                valueElement.Add(thisElement);
            //            }
            //            else
            //            {
            //                var propertyElement = new XElement(XName.Get(parent.Name.LocalName + "." + node.Key, XamlPresentationNs));
            //                parent.Add(propertyElement);
            //                thisParent = propertyElement;
            //            }

            //            thisElement = new XElement(XName.Get("Binding", XamlPresentationNs), new XAttribute("Path", node.Value));
            //            thisParent.Add(thisElement);
            //        }
            //        break;
            //}

            //if (thisElement == null)
            //    return;
            //foreach (var child in node.Children)
            //    Transform(child, thisElement);
        }
    }
}