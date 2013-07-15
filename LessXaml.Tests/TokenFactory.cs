using System.Collections.Generic;

namespace LessXaml.Tests
{
    internal class TokenFactory
    {
        private List<VampParser.Token> myTokens = new List<VampParser.Token>();

        public TokenFactory Indent(int level)
        {
            myTokens.Add(new VampParser.Token { Type = VampParser.Token.TokenType.Indentation, Value = level });
            return this;
        }

        public TokenFactory Key(string key)
        {
            myTokens.Add(new VampParser.Token { Type = VampParser.Token.TokenType.Key, Value = key });
            return this;
        }


        public TokenFactory Op(string op)
        {
            myTokens.Add(new VampParser.Token { Type = VampParser.Token.TokenType.Operator, Value = op });
            return this;
        }

        public TokenFactory Value(string value)
        {
            myTokens.Add(new VampParser.Token { Type = VampParser.Token.TokenType.Value, Value = value });
            return this;
        }

        public TokenFactory ElementValue(string value)
        {
            myTokens.Add(new VampParser.Token { Type = VampParser.Token.TokenType.ElementValue, Value = value });
            return this;
        }

        public List<VampParser.Token> ToList()
        {
            return myTokens;
        }
    }
}
