using System;
using System.Runtime.Serialization;

namespace LessML
{
    [Serializable]
    public class VampParseException : Exception
    {
        public VampParseException() { }
        public VampParseException(string message) : base(message) { }
        public VampParseException(string message, Exception inner) : base(message, inner) { }
        protected VampParseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}