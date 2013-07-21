using LessML;
using LessML.Xaml;
using LessXaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VampTests
{
    [TestClass]
    public class LessXamlParserTests
    {
        [TestMethod]
        public void BlaTest()
        {
            const string Program = "Window:\n\tButton\n\tCheckBox:\n\t\tTextBlock";
            var xaml = LessXamlParser.Translate(Program);
        }
    }
}