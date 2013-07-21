namespace LessML.Strings
{
    public class StringQuotation
    {
        public string Start;
        public string End;
        public QuoteKind Kind = QuoteKind.String;
        public IEscapedString Escaping = NoopEscapedString.Instance;

        public bool Equals(StringQuotation other)
        {
            return this.Start == other.Start
                && this.End == other.End
                && this.Kind == other.Kind
                && Equals(this.Escaping, other.Escaping);
        }

        public override bool Equals(object obj)
        {
            var other = obj as StringQuotation;
            return this.Equals(other);
        }
    }
}