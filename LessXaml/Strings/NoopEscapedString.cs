namespace LessML.Strings
{
    public class NoopEscapedString : IEscapedString
    {
        public static readonly NoopEscapedString Instance = new NoopEscapedString();

        public string Unescape(string s)
        {
            return s;
        }
    }
}