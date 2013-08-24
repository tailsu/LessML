using System.Collections.Generic;
using LessML.Strings;

namespace LessML.Vamp
{
    public class VampRules
    {
        public static readonly StringQuotation SingleQuotes = new StringQuotation {Start = "'", End = "'"};
        public static readonly StringQuotation DoubleQuotes = new StringQuotation {Start = "\"", End = "\""};

        public readonly List<StringQuotation> Quotations
            = new List<StringQuotation>
            {
                SingleQuotes, DoubleQuotes,
                new StringQuotation {Start = "/*", End = "*/", Kind = QuoteKind.Comment},
                new StringQuotation {Start = "#", Kind = QuoteKind.Comment},
            };

        public readonly List<string> Operators = new List<string>();
    }
}