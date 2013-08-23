using System;
using System.Xml.Linq;
using LessML;
using LessML.Macros;
using LessML.Vamp;
using LessML.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LessXaml.Tests
{
    [TestClass]
    public class TemplateTests
    {
        [TestMethod]
        public void Template_Usage()
        {
            const string Program = @"
`Template Rectangle($X, $Y, $Width, $Height)`:
    Rectangle
        Canvas.X = $X
        Canvas.Y = $Y
        Width = $Width
        Height = $Height

Grid:
    `Rectangle(10, 20, 30, 40)`
";
            const string Expected = @"<Grid><Rectangle Canvas.X=""10"" Canvas.Y=""20"" Width=""30"" Height=""40"" /></Grid>";

            var rules = XamlTransformer.MakeRules();
            TemplateTransformer.AppendRules(rules);

            var root = VampParser.Parse(Program, rules);
            MacroExpander.Transform(root, new TemplateTransformer());

            var xml = XmlConverter.ToXml(root);
            var actual = xml.ToString(SaveOptions.DisableFormatting);
            Assert.AreEqual(Expected, actual);
        }
    }
}
