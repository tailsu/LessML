using System.Collections.Generic;
using System.Diagnostics;
using LessML.Strings;

namespace LessML
{
    [DebuggerDisplay("{Key} {Operator} {Value}")]
    public class VampNode
    {
        public QuotedString Key { get; set; }
        public QuotedString Operator { get; set; }
        public IList<QuotedString> Value { get; set; }
        public VampNode Parent { get; set; }

        public readonly List<VampNode> Children = new List<VampNode>();
    }
}