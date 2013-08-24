using System.Collections.Generic;
using LessML;
using LessML.Strings;
using LessML.Vamp;

namespace LessXaml.Tests
{
    internal class TokenFactory
    {
        private readonly List<VampParser.Token> myTokens = new List<VampParser.Token>();

        public TokenFactory Indent(int level)
        {
            myTokens.Add(new VampParser.Token(VampParser.Token.TokenType.Indentation, level));
            return this;
        }

        public TokenFactory Key(string key, StringQuotation quotation = null)
        {
            myTokens.Add(new VampParser.Token(VampParser.Token.TokenType.Key, new QuotedString { Snippet = key, Quotation = quotation }));
            return this;
        }


        public TokenFactory Op(string op, StringQuotation quotation = null)
        {
            var str = op != null ? new QuotedString {Snippet = op, Quotation = quotation} : null;
            myTokens.Add(new VampParser.Token(VampParser.Token.TokenType.Operator, str));
            return this;
        }

        public TokenFactory Value(string value, StringQuotation quotation = null)
        {
            var list = new List<QuotedString>();
            if (!string.IsNullOrEmpty(value) || quotation != null)
                list.Add(new QuotedString { Snippet = value, Quotation = quotation });

            myTokens.Add(new VampParser.Token(VampParser.Token.TokenType.Value, list));
            return this;
        }

        public List<VampParser.Token> ToList()
        {
            return myTokens;
        }
    }
}
