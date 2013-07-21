using System.Linq;
using LessXaml;
using LessXaml.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VampTests
{
    [TestClass]
    public class VampTokenizeTests
    {
        public static readonly StringQuotation SingleQuotes = new StringQuotation { Start = "'", End = "'", Escaping = NoopEscapedString.Instance, Kind = QuoteKind.String };
        public static readonly StringQuotation DoubleQuotes = new StringQuotation { Start = "\"", End = "\"", Escaping = NoopEscapedString.Instance, Kind = QuoteKind.String };

        private static void RunTest(string program, TokenFactory expectedTokens, string defaultOp, params string[] ops)
        {
            var rules = new VampRules {DefaultOp = defaultOp};
            rules.Operators.Clear();
            rules.Operators.AddRange(ops);
            var tokens = VampParser.Tokenize(program, rules).ToList();
            CollectionAssert.AreEqual(expectedTokens.ToList(), tokens);
        }

        [TestMethod]
        public void Tokenize_SingleRoot()
        {
            RunTest("A = B", new TokenFactory()
                .Indent(0).Key("A").Op("=").Value("B"),
                null, "=");
        }

        [TestMethod]
        public void Tokenize_DefaultRoot()
        {
            RunTest("A", new TokenFactory()
                .Indent(0).Key("A").Op("=").Value(""),
                "=", "=");
        }

        [TestMethod]
        public void Tokenize_MultipleOps()
        {
            RunTest("A = B\nC -> D", new TokenFactory()
                    .Indent(0).Key("A").Op("=").Value("B")
                    .Indent(0).Key("C").Op("->").Value("D"),
                null, "=", "->");
        }

        [TestMethod]
        public void Tokenize_IndentationPoppingUpALevel()
        {
            RunTest("A\n\tB\n\tC\nD", new TokenFactory()
                .Indent(0).Key("A").Op("=").Value("")
                .Indent(1).Key("B").Op("=").Value("")
                .Indent(1).Key("C").Op("=").Value("")
                .Indent(0).Key("D").Op("=").Value(""),
                "=", "=");
        }

        [TestMethod]
        public void Tokenize_IndentationPushingFourLevels()
        {
            RunTest("A\n\tB\n\t\tC\n\t\t\tD", new TokenFactory()
                .Indent(0).Key("A").Op("=").Value("")
                .Indent(1).Key("B").Op("=").Value("")
                .Indent(2).Key("C").Op("=").Value("")
                .Indent(3).Key("D").Op("=").Value(""),
                "=", "=");
        }

        [TestMethod]
        public void Tokenize_MixedTabsAndSpaces_ExceptionThrown()
        {
            const string Program = "A\n\tB\n    C";
            var ex = AssertEx.Throws<VampParseException>(() => RunTest(Program, null, "=", "="));
            Assert.AreEqual(ex.Message, VampParser.Exception_MixedTabsAndSpaces);
        }

        [TestMethod]
        public void Tokenize_QuotedString_Untrimmed()
        {
            const string Program1 = "  \"  A string  \"  ";
            const string Program2 = "  '  A string  '  ";

            RunTest(Program1, new TokenFactory().Indent(1).ElementValue("  A string  ", DoubleQuotes), "=", "=");
            RunTest(Program2, new TokenFactory().Indent(1).ElementValue("  A string  ", SingleQuotes), "=", "=");
        }

        [TestMethod]
        public void Tokenize_QuotedKeyAndOpAndValue()
        {
            RunTest("'A key' '=' 'A value'", new TokenFactory()
                .Indent(0).Key("A key", SingleQuotes).Op("=", SingleQuotes).Value("A value", SingleQuotes),
                null, "=");
        }

        [TestMethod]
        public void Tokenize_QuotedKeyAndRestIsUnquoted()
        {
            RunTest("'A key' = some value ", new TokenFactory()
                .Indent(0).Key("A key", SingleQuotes).Op("=").Value("some value"),
                null, "=");
        }

        [TestMethod]
        public void Tokenize_UnquotedValueString_OutputTrimmed()
        {
            RunTest("A:   the  quick  brown  fox  ", new TokenFactory()
                .Indent(0).Key("A").Op(":").Value("the  quick  brown  fox"),
                null, ":");
        }

        [TestMethod]
        public void Tokenize_OpAtEndOfKeyString()
        {
            RunTest("ui:Control: value text", new TokenFactory()
                .Indent(0).Key("ui:Control").Op(":").Value("value text"),
                null, ":");
        }

        [TestMethod]
        public void Tokenize_OperatorNotFirstInLine_OperatorPrependedToValue()
        {
            RunTest("ui:Control: = value text", new TokenFactory()
                .Indent(0).Key("ui:Control").Op(":").Value("= value text"),
                null, ":", "=");
        }

        [TestMethod]
        public void Tokenize_OpAtEndOfQuotedKeyString_OpAppendedToKey()
        {
            RunTest("'ui:Control:' = value text", new TokenFactory()
                .Indent(0).Key("ui:Control:", SingleQuotes).Op("=").Value("value text"),
                null, ":", "=");
        }
    }
}
