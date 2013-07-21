using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using LessML.Strings;

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
               return new[] { new VampNode { Value = new List<QuotedString> { new QuotedString(asText.Value) } } };

            var asAttribute = xml as XAttribute;
            if (asAttribute != null)
                return new[]
                {
                    new VampNode
                    {
                        Key = resolver.StringFromXName(asAttribute.Name, true),
                        Operator = new QuotedString(AttributeOp),
                        Value = new List<QuotedString> { new QuotedString(asAttribute.Value) }
                    }
                };

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
                    Value = new List<QuotedString>(),
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
                        var attr = new XAttribute(resolver.ResolveName(key), value);
                        parent.Add(attr);
                    }

                    break;

                case ":":
                    resolver.PushLevel();
                    
                    var stubElement = new XElement("_");
                    if (!String.IsNullOrEmpty(value))
                        stubElement.Add(new XText(value));

                    Func<VampNode, bool> isXmlnsDecl = c =>
                        (c.Operator != null && c.Operator.Snippet == "=")
                            && (c.Key.Snippet == "xmlns" || c.Key.Snippet.StartsWith("xmlns:"));

                    var xmlnsFirstChildren = node.Children.Where(isXmlnsDecl)
                        .Concat(node.Children.Where(c => !isXmlnsDecl(c)));

                    foreach (var child in xmlnsFirstChildren)
                        Transform(child, stubElement, resolver);

                    var element = new XElement(resolver.ResolveName(key));
                    resolver.PopLevel();
                    parent.Add(element);

                    var childrenNodes = stubElement.Nodes().ToList();
                    var childrenAttrs = stubElement.Attributes().ToList();
                    stubElement.RemoveAll();

                    foreach (var child in childrenAttrs)
                        element.Add(child);
                    foreach (var child in childrenNodes)
                        element.Add(child);
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
