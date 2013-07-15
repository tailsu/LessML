using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace LessXaml
{
    [DebuggerDisplay("{Key} {Operator} {Value}")]
    public class VampNode
    {
        public string Key { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public VampNode Parent { get; set; }

        public readonly List<VampNode> Children = new List<VampNode>();
    }

    public class VampParser
    {
        public const string Exception_MixedTabsAndSpaces = "don't mix different whitespace characters";
        public const string Exception_WrongIndentation = "invalid whitespace indentation";
        public const string Exception_UnfinishedString = "unfinished string";

        public const string LineCommentSymbol = "--";
        public const string BlockCommentStartSymbol = "/*";
        public const string BlockCommentEndSymbol = "*/";

        [DebuggerDisplay("{Type}: {Value}")]
        internal class Token
        {
            public override bool Equals(object otherObj)
            {
                var other = otherObj as Token;
                if (other == null)
                    return false;
                return this.Type == other.Type && Equals(this.Value, other.Value);
            }

            public enum TokenType
            {
                Indentation,
                Key,
                Operator,
                Value,
                ElementValue,
            }

            public TokenType Type;
            public object Value;
        }

        private class StringQuotation
        {
            public string Start;
            public string End;
            public QuoteKind Kind = QuoteKind.String;

            public virtual string Unquote(string s)
            {
                return s;
            }
        }

        private static List<StringQuotation> Quotations
            = new List<StringQuotation>
            {
                new StringQuotation {Start = "\"", End = "\""},
                new StringQuotation {Start = "'", End = "'"},
                new StringQuotation {Start = "/*", End = "*/", Kind = QuoteKind.Comment},
                new StringQuotation {Start = "#", Kind = QuoteKind.Comment},
            };

        private class Indenter
        {
            private Stack<string> myIndentationStrings = new Stack<string>();
            private char? myUniformWhitespaceChar = null;

            public Indenter()
            {
                myIndentationStrings.Push("");
            }

            public string Unindent(string line, out int indentationLevel)
            {
                int nonWhitespaceIndex = 0;
                for (int i = 0; i < line.Length; ++i)
                {
                    if (!Char.IsWhiteSpace(line[i]))
                        break;
                    nonWhitespaceIndex++;
                    if (this.myUniformWhitespaceChar == null)
                        this.myUniformWhitespaceChar = line[i];
                    else if (this.myUniformWhitespaceChar.Value != line[i])
                        throw new VampParseException(Exception_MixedTabsAndSpaces);
                }

                string whitespaceStr = line.Substring(0, nonWhitespaceIndex);
                var lastIndent = myIndentationStrings.Peek();
                if (lastIndent.Length < whitespaceStr.Length)
                    myIndentationStrings.Push(whitespaceStr);
                else
                {
                    while (lastIndent.Length > whitespaceStr.Length)
                    {
                        myIndentationStrings.Pop();
                        lastIndent = myIndentationStrings.Peek();
                    }
                    if (lastIndent.Length != whitespaceStr.Length)
                        throw new VampParseException(Exception_WrongIndentation);
                }
                string assignmentStr = line.Substring(nonWhitespaceIndex);

                indentationLevel = myIndentationStrings.Count - 1;
                return assignmentStr;
            }
        }

        private enum QuoteKind
        {
            String,
            Comment
        }

        private class QuotedString
        {
            public string Snippet;
            public StringQuotation Quotation;
        }

        private class StringQuoter
        {
            public static IEnumerable<QuotedString> GetStrings(string line, TextReader lineReader)
            {
                var result = new List<QuotedString>();
                while (true)
                {
                    var match = Quotations.Select(q => new { Index = line.IndexOf(q.Start, StringComparison.Ordinal), q.Start, q.End, Quotation = q })
                        .Where(tup => tup.Index != -1)
                        .OrderBy(tup => tup.Index)
                        .FirstOrDefault();

                    // find any sort of quotation used on this line. If there isn't any, we've finished with this line.
                    if (match == null)
                    {
                        var snippet = line.Trim();
                        if (snippet != "")
                            result.Add(new QuotedString { Snippet = line.Trim() });
                        break;
                    }

                    // extract the string preceding the quote and add it as an unquoted result.
                    var prefix = line.Substring(0, match.Index).Trim();
                    if (prefix != "")
                        result.Add(new QuotedString { Snippet = prefix });

                    var startIndex = match.Index + match.Start.Length;
                    string quotedString;
                    // End==null means this string simply runs to the end of the line
                    if (match.End == null)
                    {
                        quotedString = line.Substring(startIndex);
                    }
                    else
                    {
                        // build the quote
                        var quoteBuilder = new StringBuilder();
                        while (true)
                        {
                            var endIndex = line.IndexOf(match.End, startIndex, StringComparison.Ordinal);
                            if (endIndex == -1)
                            {
                                quoteBuilder.AppendLine(line.Substring(startIndex));
                                line = lineReader.ReadLine();
                                if (line == null)
                                    throw new VampParseException(Exception_UnfinishedString);
                                startIndex = 0;
                            }
                            else
                            {
                                quoteBuilder.Append(line.Substring(startIndex, endIndex - startIndex));
                                line = line.Substring(endIndex + match.End.Length);
                                break;
                            }
                        }
                        quotedString = quoteBuilder.ToString();
                    }

                    result.Add(new QuotedString
                    {
                        Snippet = match.Quotation.Unquote(quotedString),
                        Quotation = match.Quotation
                    });
                }

                return result;
            }

            public static IEnumerable<QuotedString> SplitOnWhitespace(IEnumerable<QuotedString> strings)
            {
                return strings
                    .SelectMany(q =>
                    {
                        if (q.Quotation != null)
                        {
                            return new[] { q };
                        }
                        else
                        {
                            return q.Snippet.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => new QuotedString { Snippet = s.Trim() });
                        }
                    });
            }
        }

        private static readonly char[] Whitespace = {' ', '\t'};

        private class StringSplitter
        {
            private readonly List<QuotedString> myStrings;
            private int myStringIndex;
            private string myStringToSplit;

            public StringSplitter(List<QuotedString> strings)
            {
                myStrings = strings;
            }

            public QuotedString GetNextString()
            {
                if (String.IsNullOrEmpty(myStringToSplit))
                {
                    if (myStringIndex >= myStrings.Count)
                        return null;

                    var q = myStrings[myStringIndex++];
                    if (q.Quotation != null)
                        return q;
                    myStringToSplit = q.Snippet;
                }

                var tokenEnd = myStringToSplit.IndexOfAny(Whitespace);
                var result = myStringToSplit;
                if (tokenEnd == -1)
                {
                    myStringToSplit = null;
                }
                else
                {
                    result = result.Substring(0, tokenEnd);
                    myStringToSplit = myStringToSplit.Substring(tokenEnd + 1).Trim();
                }
                return new QuotedString { Snippet = result };
            }

            public string JoinRest()
            {
                var result = myStringToSplit;
                result += String.Join("", myStrings.Skip(myStringIndex).Select(q => q.Snippet).ToArray());
                return result;
            }
        }

        internal static IEnumerable<Token> Tokenize(string program, IList<string> operators, string defaultOp)
        {
            var reader = new StringReader(program);
            var indenter = new Indenter();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim() == "")
                    continue;

                int indentationLevel;
                line = indenter.Unindent(line, out indentationLevel);

                yield return new Token { Type = Token.TokenType.Indentation, Value = indentationLevel };

                var quotedStrings = StringQuoter.GetStrings(line, reader)
                    .Where(q => q.Quotation == null || q.Quotation.Kind != QuoteKind.Comment)
                    .ToList();
                Debug.Assert(quotedStrings.Count > 0);
                Debug.Assert(quotedStrings.All(q => q.Snippet.Length > 0));

                // case 1: single quoted string
                if (quotedStrings.Count == 1 && quotedStrings[0].Quotation != null)
                {
                    yield return new Token { Type = Token.TokenType.ElementValue, Value = quotedStrings[0].Snippet };
                }
                else // case 2: unquoted string or more than one strings - split into key-op-value triplet
                {
                    var splitter = new StringSplitter(quotedStrings);
                    string key = null, op = null, value = null;

                    var qKey = splitter.GetNextString();
                    key = qKey.Snippet;
                    if (qKey.Quotation == null)
                    {
                        op = operators.FirstOrDefault(o => qKey.Snippet.EndsWith(o));
                        if (op != null)
                        {

                        }
                    }

                    yield return new Token { Type = Token.TokenType.Key, Value = key.Trim() };
                    yield return new Token { Type = Token.TokenType.Operator, Value = op ?? defaultOp };
                    yield return new Token { Type = Token.TokenType.Value, Value = value.Trim() };
                }
            }
        }

        public static IEnumerable<VampNode> Parse(string program, IList<string> operators, string defaultOperator)
        {
            //program = RemoveComments(program);

            var tokens = Tokenize(program, operators, defaultOperator);
            var rootNodes = new List<VampNode>();
            var stack = new Stack<VampNode>();
            foreach (var token in tokens)
            {
                VampNode node;
                switch (token.Type)
                {
                    case Token.TokenType.Indentation:
                        node = new VampNode();
                        int thisIndentLevel = (int)token.Value;
                        if (thisIndentLevel == 0)
                        {
                            rootNodes.Add(node);
                            stack.Clear();
                            stack.Push(node);
                        }
                        else
                        {
                            while (thisIndentLevel < stack.Count)
                                stack.Pop();
                            node.Parent = stack.Peek();
                            node.Parent.Children.Add(node);
                            stack.Push(node);
                        }
                        break;
                    case Token.TokenType.Key:
                        stack.Peek().Key = (string)token.Value;
                        break;
                    case Token.TokenType.Operator:
                        stack.Peek().Operator = (string)token.Value;
                        break;
                    case Token.TokenType.Value:
                        stack.Peek().Value = (string)token.Value;
                        break;
                }
            }

            return rootNodes;
        }

        internal static string RemoveComments(string contents)
        {
            var sb = new StringBuilder();

            // remove line comments
            int pointer = 0;
            while (true)
            {
                var hashIndex = contents.IndexOf(LineCommentSymbol, pointer, StringComparison.Ordinal);
                if (hashIndex == -1)
                    break;

                sb.Append(contents.Substring(pointer, hashIndex - pointer));
                var eolIndex = contents.IndexOfAny(new[] { '\r', '\n' }, hashIndex);
                if (eolIndex == -1)
                {
                    pointer = contents.Length;
                    break;
                }

                pointer = eolIndex;
            }

            if (pointer < contents.Length)
                sb.Append(contents.Substring(pointer));

            contents = sb.ToString();
            sb = new StringBuilder();

            // remove block comments
            pointer = 0;
            while (true)
            {
                var blockStart = contents.IndexOf(BlockCommentStartSymbol, pointer, StringComparison.Ordinal);
                if (blockStart == -1)
                    break;

                var blockEnd = contents.IndexOf(BlockCommentEndSymbol, blockStart + BlockCommentStartSymbol.Length, StringComparison.Ordinal);
                if (blockEnd == -1)
                    throw new VampParseException("Unfinished block comment");

                sb.Append(contents.Substring(pointer, blockStart - pointer));
                pointer = blockEnd + BlockCommentEndSymbol.Length;
            }

            if (pointer < contents.Length)
                sb.Append(contents.Substring(pointer));

            return sb.ToString();
        }
    }

    [Serializable]
    public class VampException : Exception
    {
        public VampException()
        {
        }

        public VampException(string message) : base(message)
        {
        }

        public VampException(string message, Exception inner) : base(message, inner)
        {
        }

        protected VampException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class VampParseException : VampException
    {
        public VampParseException()
        {
        }

        public VampParseException(string message) : base(message)
        {
        }

        public VampParseException(string message, Exception inner) : base(message, inner)
        {
        }

        protected VampParseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
