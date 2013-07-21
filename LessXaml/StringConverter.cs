using System.Collections.Generic;
using System.Text;
using LessML.Vamp;

namespace LessML
{
    public static class StringConverter
    {
        public static string ToString(IEnumerable<VampNode> roots)
        {
            return ToString(roots, "    ");
        }

        public static string ToString(IEnumerable<VampNode> roots, string indentation)
        {
            var sb = new StringBuilder();
            Append(sb, roots, 0, indentation);
            return sb.ToString();
        }

        private static void Append(StringBuilder sb, IEnumerable<VampNode> nodes, int indent, string indentation)
        {
            foreach (var node in nodes)
            {
                AppendIndent(sb, indent, indentation);
                Append(sb, node);
                Append(sb, node.Children, indent + 1, indentation);
            }
        }

        private static void Append(StringBuilder sb, VampNode node)
        {
            bool prependSpace = false;

            if (!node.IsBareValue)
            {
                sb.Append(node.Key);
                sb.Append(" ");
                sb.Append(node.Operator);
                prependSpace = true;
            }

            foreach (var value in node.Value)
            {
                if (prependSpace)
                {
                    sb.Append(" ");
                    prependSpace = false;
                }
                sb.Append(value);
            }

            sb.AppendLine();
        }

        private static void AppendIndent(StringBuilder sb, int indent, string indentation)
        {
            for (int i = 0; i < indent; ++i)
                sb.Append(indentation);
        }
    }
}
