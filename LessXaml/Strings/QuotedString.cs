namespace LessML.Strings
{
    public class QuotedString
    {
        public string Snippet;
        public StringQuotation Quotation;

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
    }
}