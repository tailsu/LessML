using System;
using System.Linq;
using System.Xml.Linq;
using LessML;
using LessML.Vamp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LessXaml.Tests
{
    [TestClass]
    public class XmlConverterTests
    {
        private static void RunTest(string program, string expectedXml, bool skipFromXml = false)
        {
            var parsed = VampParser.Parse(program, XmlConverter.MakeRules());
            var doc = XmlConverter.ToXml(parsed);

            var str = doc.ToString(SaveOptions.DisableFormatting);
            Assert.AreEqual(expectedXml, str);

            if (!skipFromXml)
            {
                var fromXml = XmlConverter.FromXml(XDocument.Parse(str)).ToList();

                Assert.AreEqual(parsed.Children.Count, fromXml.Count);
                for (int i = 0; i < parsed.Children.Count; ++i)
                    Assert.IsTrue(parsed.Children[i].IsSemanticallyEquivalent(fromXml[i]));
            }
        }

        [TestMethod]
        public void ToXml_SingleBaseElement()
        {
            RunTest("A", "<A />");
        }

        [TestMethod]
        public void ToXml_SingleAttribute()
        {
            RunTest("A\n\tB = C", "<A B=\"C\" />");
        }

        [TestMethod]
        public void ToXml_SingleElementValue()
        {
            RunTest("A\n\t'This is a value'", "<A>This is a value</A>");
        }

        [TestMethod]
        public void ToXml_SingleInlineElementValue()
        {
            RunTest("A: This is a value", "<A>This is a value</A>", true);
        }

        [TestMethod]
        public void ToXml_InlineAndAdditionalElementValue()
        {
            RunTest("A: This is a value\n\t'Another value'", "<A>This is a valueAnother value</A>", true);
        }

        [TestMethod]
        public void ToXml_DefaultNamespace()
        {
            RunTest("A\n\txmlns = http://example.org",
                "<A xmlns=\"http://example.org\" />");
        }
        
        [TestMethod]
        public void ToXml_AliasedNamespace()
        {
            RunTest("telerik:RadButton\n\txmlns:telerik = clr-namespace: Telerik.Windows.Controls",
                "<telerik:RadButton xmlns:telerik=\"clr-namespace: Telerik.Windows.Controls\" />");
        }

        [TestMethod]
        public void ToXml_AttributeInNamespaceBeforeDeclarations()
        {
            RunTest(
@"RadButton
    telerik:Theming.Enabled = True
    xmlns = default
    xmlns:telerik = clr-namespace: Telerik.Windows.Controls",
                "<RadButton xmlns=\"default\" xmlns:telerik=\"clr-namespace: Telerik.Windows.Controls\" telerik:Theming.Enabled=\"True\" />", true);

        }

        [TestMethod]
        public void ToXml_AttributeInNamespaceAfterDeclarations()
        {
            RunTest(
@"RadButton
    xmlns = default
    xmlns:telerik = clr-namespace: Telerik.Windows.Controls
    telerik:Theming.Enabled = True
",
                "<RadButton xmlns=\"default\" xmlns:telerik=\"clr-namespace: Telerik.Windows.Controls\" telerik:Theming.Enabled=\"True\" />");

        }

        [TestMethod]
        public void ToXml_ReusedDefaultNamespace()
        {
            RunTest(
@"A
    xmlns = NS1
    B
        xmlns = NS2",
                    "<A xmlns=\"NS1\"><B xmlns=\"NS2\" /></A>");
        }

        [TestMethod]
        public void ToXml_ReusedNamespaceAlias()
        {
            RunTest(
@"n:A
    xmlns:n = NS1
    n:B
        xmlns:n = NS2",
                    "<n:A xmlns:n=\"NS1\"><n:B xmlns:n=\"NS2\" /></n:A>");
        }

        [TestMethod]
        public void ToXml_MultipleElementValues()
        {
            RunTest(
@"TextBlock
    'a very '
    Bold
        'bold'
    ' proposition'",
                   "<TextBlock>a very <Bold>bold</Bold> proposition</TextBlock>");
        }
    }
}
