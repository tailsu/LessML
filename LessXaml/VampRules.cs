using System.Collections.Generic;
using LessML.Strings;

namespace LessML
{
    public class VampRules
    {
        public readonly List<StringQuotation> Quotations
            = new List<StringQuotation>
            {
                new StringQuotation {Start = "\"", End = "\""},
                new StringQuotation {Start = "'", End = "'"},
                new StringQuotation {Start = "/*", End = "*/", Kind = QuoteKind.Comment},
                new StringQuotation {Start = "#", Kind = QuoteKind.Comment},
            };

        public readonly List<string> Operators = new List<string>();
        public string DefaultOp;
    }
}