namespace LessML.Strings
{
    public class QuotedString
    {
        public string Snippet;
        public StringQuotation Quotation;

        public QuotedString()
        {
        }

        public QuotedString(string snippet)
        {
            this.Snippet = snippet;
        }

        public bool Equals(QuotedString other)
        {
            return this.Snippet == other.Snippet && Equals(this.Quotation, other.Quotation);
        }

        public override bool Equals(object obj)
        {
            var other = obj as QuotedString;
            return this.Equals(other);
        }

        public override string ToString()
        {
            if (this.Quotation != null)
                return this.Quotation.Start + this.Snippet + this.Quotation.End;
            else
                return this.Snippet;
        }

        public static bool IsSemanticallyEquivalent(QuotedString a, QuotedString b)
        {
            if (a == null || b == null)
                return ReferenceEquals(a, b);

            return a.Snippet == b.Snippet
                && (a.Quotation == null || a.Quotation.Kind == QuoteKind.String)
                && (b.Quotation == null || b.Quotation.Kind == QuoteKind.String);
        }
    }
}