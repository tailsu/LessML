using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LessXaml.Tests
{
    [TestClass]
    public class CommentTests
    {
        [TestMethod]
        public void RemoveComments_SingleLineComment()
        {
            const string Input = "--Foo";
            var result = VampParser.RemoveComments(Input);
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RemoveComments_EmptySingleLineCommentWithEol()
        {
            const string Input = "--\n";
            var result = VampParser.RemoveComments(Input);
            Assert.AreEqual("\n", result);
        }

        [TestMethod]
        public void RemoveComments_LineCommentAfterSomeText()
        {
            const string Input = "This is ok--Foo";
            var result = VampParser.RemoveComments(Input);
            Assert.AreEqual("This is ok", result);
        }

        [TestMethod]
        public void RemoveComments_LineCommentThenText()
        {
            const string Input = "--Foo\r\nFind this";
            var result = VampParser.RemoveComments(Input);
            Assert.AreEqual("\r\nFind this", result);
        }

        [TestMethod]
        public void RemoveComments_ComplexExample()
        {
            const string Input = "--Foo\r\nFind this--Bar\nThen this\n--end";
            var result = VampParser.RemoveComments(Input);
            Assert.AreEqual("\r\nFind this\nThen this\n", result);
        }

        [TestMethod]
        public void RemoveComment_BlockCommentOnly()
        {
            const string Input = "/**/";
            var result = VampParser.RemoveComments(Input);
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RemoveComment_BlockComment()
        {
            const string Input = "/* ignore this */";
            var result = VampParser.RemoveComments(Input);
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RemoveComment_BlockCommentMingled()
        {
            const string Input = "Something in front/* ignore this */ and in the back";
            var result = VampParser.RemoveComments(Input);
            Assert.AreEqual("Something in front and in the back", result);
        }

        [TestMethod]
        public void RemoveComment_BlockCommentSurroundsLineComment()
        {
            const string Input = "Stuff/* ignore --this\n */More stuff";
            var result = VampParser.RemoveComments(Input);
            Assert.AreEqual("StuffMore stuff", result);
        }

        [TestMethod]
        public void RemoveComment_BlockCommentNested()
        {
            const string Input = "Stuff/* /* /* */More stuff";
            var result = VampParser.RemoveComments(Input);
            Assert.AreEqual("StuffMore stuff", result);
        }
    }
}
