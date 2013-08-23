using System.Linq;
using LessML.Macros;
using LessML.Vamp;

namespace LessML.Xaml
{
    public class XamlTransformer : IMacro
    {
        public static VampRules MakeRules()
        {
            var rules = XmlConverter.MakeRules();
            rules.Operators.AddRange(new[] {"->", ":="});
            return rules;
        }

        public MacroResult Transform(VampNode node)
        {
            if (node.Key.Snippet.StartsWith("."))
            {
                // .P ?? V
                //   other stuff

                // Setter:
                //   Property = P
                //   Value ?? V
                //     other stuff

                var propNode = new VampNode
                {
                    Key = "Property",
                    Operator = "=",
                };
                propNode.SetValue(node.Key.Snippet.Substring(1));

                var valueNode = new VampNode
                {
                    Key = "Value",
                    Operator = node.Operator,
                };
                valueNode.Value.AddRange(node.Value);
                var children = node.Children.ToList();
                node.ClearChildren();
                valueNode.AddChildren(children);

                node.Key = "Setter";
                node.Operator = ":";
                node.SetValue(null);
                node.AddChild(propNode);
                node.AddChild(valueNode);
                return MacroResult.ReapplyTransform;
            }

            switch (node.Operator.Snippet)
            {
                case "->":
                    {
                        // A:
                        //   a -> b
                        
                        // A:
                        //   A.a:
                        //     b

                        var valueNode = new VampNode
                        {
                            Key = node.JoinValue(),
                            Operator = ":",
                        };
                        var children = node.Children.ToList();
                        node.ClearChildren();
                        node.AddChild(valueNode);
                        valueNode.AddChildren(children);
                        
                        node.Key = node.Parent.Key.Snippet + "." + node.Key.Snippet;
                        node.Operator = ":";
                        node.SetValue(null);
                    }
                    return MacroResult.ReapplyTransform;

                case ":=":
                    {
                        // A := B
                        //   Converter = foo
                        
                        // A -> Binding
                        //  Converter = foo
                        //  Path = B

                        var pathNode = new VampNode()
                        {
                            Key = "Path",
                            Operator = "=",
                        };
                        pathNode.Value.AddRange(node.Value);
                        node.AddChild(pathNode);

                        node.Operator = "->";
                        node.SetValue("Binding");
                    }
                    return MacroResult.ReapplyTransform;
            }

            var val = node.JoinValue();
            if (val.StartsWith("**"))
            {
                node.SetValue("{DynamicResource " + val.Substring(2) + "}");
                return MacroResult.ContinueToChildren;
            }
            if (val.StartsWith("*"))
            {
                node.SetValue("{StaticResource " + val.Substring(1) + "}");
                return MacroResult.ContinueToChildren;
            }

            return MacroResult.ContinueToChildren;
        }
    }
}
