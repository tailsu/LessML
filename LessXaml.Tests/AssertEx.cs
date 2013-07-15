using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LessXaml.Tests
{
    internal static class AssertEx
    {
        public static T Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                Assert.Fail(String.Format("Expected exception '{0}' but '{1}' thrown", typeof(T), ex));
            }

            Assert.Fail(String.Format("Expected exception '{0}' but not exception thrown", typeof(T)));
            return null;
        }
    }
}
