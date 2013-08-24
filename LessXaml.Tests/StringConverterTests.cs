using System;
using LessML;
using LessML.Vamp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LessXaml.Tests
{
    [TestClass]
    public class StringConverterTests
    {
        private static void RunTest(string program, string expected = null)
        {
            var parsed = VampParser.Parse(program, XmlConverter.MakeRules());
            var backToString = StringConverter.ToString(parsed);

            Assert.AreEqual(expected ?? program, backToString.Trim());
        }

        [TestMethod]
        public void ToString_RootElement()
        {
            RunTest("A", "A");
        }
    }
}
