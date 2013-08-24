using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LessML.Strings;
using LessML.Vamp;

namespace LessML.Macros
{
    public class TemplateTransformer : IMacro
    {
        public static readonly StringQuotation TemplateApplication = new StringQuotation {Start = "`", End = "`", Kind = QuoteKind.String};

        public static void AppendRules(VampRules rules)
        {
            rules.Quotations.Add(TemplateApplication);
        }

        private class TemplateDefinition
        {
            public readonly List<string> Parameters;
            public readonly List<VampNode> Definition;

            public TemplateDefinition(List<VampNode> definition, List<string> parameters)
            {
                Parameters = parameters;
                Definition = definition;
            }
        }

        private readonly Dictionary<string, TemplateDefinition> myFunctions = new Dictionary<string, TemplateDefinition>();

        public MacroResult Transform(VampNode node)
        {
            return this.ParseTemplateInvocation(node);
        }

        private MacroResult ParseTemplateInvocation(VampNode node)
        {
            QuotedString val = null;
            if (node.Key != null && node.Value.Count == 0)
                val = node.Key;

            if (val == null || !ReferenceEquals(val.Quotation, TemplateApplication))
                return MacroResult.ContinueToChildren;

            string name;
            List<string> arguments;
            if (!ParseFunctionHeader(val.Snippet, out name, out arguments))
                throw new Exception("Looks wrong: " + val.Snippet);

            var index = node.Parent.Children.IndexOf(node);
            node.Parent.Children.RemoveAt(index);

            if (name.StartsWith("Template ", StringComparison.InvariantCultureIgnoreCase))
            {
                name = name.Substring("Template ".Length);
                var def = new TemplateDefinition(node.Children, arguments.Select(s => s.Trim()).ToList());
                myFunctions.Add(name, def);
            }
            else
            {
                TemplateDefinition funcTemplate;
                if (!myFunctions.TryGetValue(name, out funcTemplate))
                    throw new Exception("Unknown function specified in " + val.Snippet);

                if (funcTemplate.Parameters.Count != arguments.Count)
                    throw new Exception("Parameter count mismatch in " + val.Snippet);

                var children = funcTemplate.Definition.Select(c => c.Clone()).ToList();
                var keyValues = new Dictionary<string, string>();
                for (int i = 0; i < arguments.Count; ++i)
                    keyValues.Add(funcTemplate.Parameters[i], arguments[i].Trim());

                var expander = new TemplateParameterExpander(keyValues);
                foreach (var child in children)
                    MacroExpander.Transform(child, expander);

                node.Parent.Children.InsertRange(index, children);
            }

            return MacroResult.Break;
        }

        private static readonly Regex FunctionRegex = new Regex(@"(?<Name>.+?)\((?<Param>[^,]+,?)*\)");

        private static bool ParseFunctionHeader(string str, out string name, out List<string> parameters)
        {
            name = null;
            parameters = null;

            var match = FunctionRegex.Match(str);
            if (!match.Success)
                return false;

            name = match.Groups["Name"].Value;

            parameters = new List<string>();
            parameters.AddRange(match.Groups["Param"].Captures.Cast<Capture>().Select(c => c.Value.Replace(",", "")));

            return true;
        }

        private class TemplateParameterExpander : IMacro
        {
            private readonly Dictionary<string, string> myKeyValues;

            public TemplateParameterExpander(Dictionary<string, string> keyValues)
            {
                myKeyValues = keyValues;
            }

            public MacroResult Transform(VampNode node)
            {
                this.Expand(node.Key);
                this.Expand(node.Operator);
                foreach (var value in node.Value)
                    this.Expand(value);
                return MacroResult.ContinueToChildren;
            }

            private void Expand(QuotedString str)
            {
                if (str == null)
                    return;

                str.Snippet = this.myKeyValues.Aggregate(str.Snippet, (current, kvp) => current.Replace(kvp.Key, kvp.Value));
            }
        }
    }
}
