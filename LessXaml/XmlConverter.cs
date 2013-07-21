using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using LessML.Strings;
using LessML.Vamp;

namespace LessML
{
    public class XmlConverter
    {
        public const string AttributeOp = "=";
        public const string ElementOp = ":";

        public static VampRules MakeRules()
        {
            var rules = new VampRules();
            rules.Operators.Add(AttributeOp);
            rules.Operators.Add(ElementOp);
            rules.DefaultOp = ElementOp;
            return rules;
        }

        public static XDocument ToXml(IEnumerable<VampNode> roots)
        {
            var stub = new XElement("_");

            var resolver = new NamespaceResolver();

            foreach (var node in roots)
                Transform(node, stub, resolver);

            var subnodes = stub.Nodes().ToList();
            stub.RemoveAll();

            return new XDocument(subnodes);
        }

        public static IEnumerable<VampNode> FromXml(XObject xml)
        {
            var resolver = new ReverseNamespaceResolver();
            return FromXml(xml, resolver);
        }
        
        private static IEnumerable<VampNode> FromXml(XObject xml, ReverseNamespaceResolver resolver)
        {
            var asText = xml as XText;
            if (asText != null)
            {
                var node = new VampNode();
                node.SetValue(asText.Value);
                return new[] { node };
            }

            var asAttribute = xml as XAttribute;
            if (asAttribute != null)
            {
                var node = new VampNode
                {
                    Key = resolver.StringFromXName(asAttribute.Name, true),
                    Operator = new QuotedString(AttributeOp),
                };
                node.SetValue(asAttribute.Value);
                return new[] {node};
            }

            var asDoc = xml as XDocument;
            if (asDoc != null)
            {
                return asDoc.Nodes().SelectMany(n => FromXml(n, resolver)).ToList();
            }

            var asElement = xml as XElement;
            if (asElement != null)
            {
                resolver.PushLevelAndRegisterAliases(asElement);

                var node = new VampNode
                {
                    Key = resolver.StringFromXName(asElement.Name, false),
                    Operator = new QuotedString(ElementOp),
                };

                node.Children.AddRange(asElement.Attributes().SelectMany(a => FromXml(a, resolver)));
                node.Children.AddRange(asElement.Nodes().SelectMany(n => FromXml(n, resolver)));

                resolver.PopLevel();
                return new[] {node};
            }

            throw new NotSupportedException("TODO: " + xml);
        }

        private static void Transform(VampNode node, XElement parent, NamespaceResolver resolver)
        {
            if (node.IsBareValue)
            {
                foreach (var v in node.Value)
                {
                    //TODO: add generic quotation support
                    if (v.Quotation.Start == "%C(")
                    {
                        parent.Add(new XComment(v.Snippet));
                    }
                    else
                    {
                        parent.Add(new XText(v.Snippet));
                    }
                }

                if (node.Children.Count != 0)
                    throw new Exception("TODO: can't have children under element value");

                return;
            }

            var key = node.Key.Snippet;
            var op = node.Operator.Snippet;
            var value = node.JoinValue();
            switch (op)
            {
                case "=":
                    if (!resolver.RegisterIfAlias(key, value, parent))
                    {
                        var name = resolver.ResolveName(key);
                        if (name.Namespace == parent.Name.Namespace)
                            name = name.LocalName;
                        var attr = new XAttribute(name, value);
                        parent.Add(attr);
                    }

                    break;

                case ":":
                    Func<VampNode, bool> isXmlnsDecl = c =>
                        (c.Operator != null && c.Operator.Snippet == "=")
                            && (c.Key.Snippet == "xmlns" || c.Key.Snippet.StartsWith("xmlns:"));

                    resolver.PushLevel();

                    XElement element;
                    {
                        var xmlnsCollector = new XElement("_");
                        foreach (var child in node.Children.Where(isXmlnsDecl))
                            Transform(child, xmlnsCollector, resolver);

                        element = new XElement(resolver.ResolveName(key));

                        var xmlnsAttrs = xmlnsCollector.Attributes().ToList();
                        xmlnsCollector.RemoveAll();
                        foreach (var child in xmlnsAttrs)
                            element.Add(child);
                    }

                    parent.Add(element);
                    if (!String.IsNullOrEmpty(value))
                        element.Add(new XText(value));
                    foreach (var child in node.Children.Where(c => !isXmlnsDecl(c)))
                        Transform(child, element, resolver);

                    resolver.PopLevel();
                    break;
            }
        }

        private class NamespaceResolver
        {
            private List<Dictionary<string, string>> stack = new List<Dictionary<string, string>>();

            public void PushLevel()
            {
                stack.Add(new Dictionary<string, string>());
            }

            public void PopLevel()
            {
                stack.RemoveAt(stack.Count - 1);
            }

            public bool RegisterIfAlias(string attrName, string value, XContainer container)
            {
                if (attrName == "xmlns")
                {
                    stack.Last().Add("", value);
                    container.Add(new XAttribute("xmlns", value));
                    return true;
                }

                if (attrName.StartsWith("xmlns:"))
                {
                    var alias = attrName.Substring("xmlns:".Length);
                    stack.Last().Add(alias, value);
                    container.Add(new XAttribute(XNamespace.Xmlns + alias, value));
                    return true;
                }

                return false;
            }

            public XNamespace ResolveNamespace(string alias)
            {
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    string @namespace;
                    if (stack[i].TryGetValue(alias, out @namespace))
                        return @namespace;
                }

                if (alias == "")
                    return XNamespace.None;

                throw new Exception("TODO: unmapped alias");
            }

            public XName ResolveName(string name)
            {
                var aliasDelimiter = name.IndexOf(':');
                var alias = aliasDelimiter == -1 ? "" : name.Substring(0, aliasDelimiter);
                var localName = name.Substring(aliasDelimiter + 1);
                return this.ResolveNamespace(alias) + localName;
            }
        }

        private class ReverseNamespaceResolver
        {
            private readonly List<Dictionary<string, string>> stack = new List<Dictionary<string, string>>();

            public void PushLevelAndRegisterAliases(XElement element)
            {
                var last = new Dictionary<string, string>();
                stack.Add(last);

                foreach (var attr in element.Attributes())
                {
                    if (attr.Name == "xmlns")
                    {
                        last.Add(attr.Value, "");
                    }
                    else if (attr.IsNamespaceDeclaration)
                    {
                        last.Add(attr.Value, attr.Name.LocalName);
                    }
                }
            }

            public void PopLevel()
            {
                stack.RemoveAt(stack.Count - 1);
            }

            public QuotedString StringFromXName(XName name, bool isAttribute)
            {
                if (isAttribute)
                {
                    if (name.Namespace == XNamespace.Xmlns)
                        return new QuotedString("xmlns:" + name.LocalName);
                    if (name.Namespace == XNamespace.None && name.LocalName == "xmlns")
                        return new QuotedString("xmlns");
                }

                var ns = name.NamespaceName;
                string alias = null;
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    if (stack[i].TryGetValue(ns, out alias))
                        break;
                }

                return new QuotedString((String.IsNullOrEmpty(alias) ? "" : alias + ":") + name.LocalName);
            }
        }
    }
}
