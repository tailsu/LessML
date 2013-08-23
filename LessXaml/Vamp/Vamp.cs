using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LessML.Strings;

namespace LessML.Vamp
{
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
            public enum TokenType
            {
                Indentation,
                Key,
                Operator,
                Value,
                ElementValue,
            }

            public readonly TokenType Type;
            public readonly object Value;

            public Token(TokenType type, object value)
            {
                this.Type = type;
                this.Value = value;
            }

            public int ValueOfIndentation
            {
                get
                {
                    this.CheckType(TokenType.Indentation);
                    return (int) this.Value;
                }
            }

            public QuotedString ValueOfKey
            {
                get
                {
                    this.CheckType(TokenType.Key);
                    return (QuotedString) this.Value;
                }
            }

            public QuotedString ValueOfOperator
            {
                get
                {
                    this.CheckType(TokenType.Operator);
                    return (QuotedString) this.Value;
                }
            }

            public List<QuotedString> ValueOfValue
            {
                get
                {
                    this.CheckType(TokenType.Value);
                    return (List<QuotedString>) this.Value;
                }
            }

            public QuotedString ValueOfElementValue
            {
                get
                {
                    this.CheckType(TokenType.ElementValue);
                    return (QuotedString) this.Value;
                }
            }

            public bool Equals(Token other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                if (this.Type != other.Type)
                    return false;

                var thisEnum = this.Value as IEnumerable;
                var otherEnum = other.Value as IEnumerable;
                if (thisEnum != null && otherEnum != null)
                    return thisEnum.Cast<object>().SequenceEqual(otherEnum.Cast<object>());

                return Equals(this.Value, other.Value);
            }

            public override bool Equals(object obj)
            {
                var other = obj as Token;
                return this.Equals(other);
            }

            private void CheckType(TokenType expectedType)
            {
                if (this.Type != expectedType)
                    throw new Exception("TODO:");
            }
        }

        private class Indenter
        {
            private readonly Stack<string> myIndentationStrings = new Stack<string>();
            private char? myUniformWhitespaceChar;

            public Indenter()
            {
                this.myIndentationStrings.Push("");
            }

            public string Unindent(string line, out int indentationLevel)
            {
                int nonWhitespaceIndex = 0;
                foreach (char c in line)
                {
                    if (!Char.IsWhiteSpace(c))
                        break;
                    nonWhitespaceIndex++;
                    if (this.myUniformWhitespaceChar == null)
                        this.myUniformWhitespaceChar = c;
                    else if (this.myUniformWhitespaceChar.Value != c)
                        throw new VampParseException(Exception_MixedTabsAndSpaces);
                }

                string whitespaceStr = line.Substring(0, nonWhitespaceIndex);
                var lastIndent = this.myIndentationStrings.Peek();
                if (lastIndent.Length < whitespaceStr.Length)
                    this.myIndentationStrings.Push(whitespaceStr);
                else
                {
                    while (lastIndent.Length > whitespaceStr.Length)
                    {
                        this.myIndentationStrings.Pop();
                        lastIndent = this.myIndentationStrings.Peek();
                    }
                    if (lastIndent.Length != whitespaceStr.Length)
                        throw new VampParseException(Exception_WrongIndentation);
                }
                string assignmentStr = line.Substring(nonWhitespaceIndex);

                indentationLevel = this.myIndentationStrings.Count - 1;
                return assignmentStr;
            }
        }

        private class StringQuoter
        {
            private readonly IList<StringQuotation> myQuotations;

            public StringQuoter(IList<StringQuotation> quotations)
            {
                this.myQuotations = quotations;
            }

            public IEnumerable<QuotedString> GetStrings(string line, TextReader lineReader)
            {
                var result = new List<QuotedString>();
                while (true)
                {
                    var match = this.myQuotations.Select(q => new { Index = line.IndexOf(q.Start, StringComparison.Ordinal), q.Start, q.End, Quotation = q })
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
                        Snippet = match.Quotation.Escaping.Unescape(quotedString),
                        Quotation = match.Quotation
                    });
                }

                return result;
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
                this.myStrings = strings;
            }

            public QuotedString GetNextString()
            {
                if (String.IsNullOrEmpty(this.myStringToSplit))
                {
                    if (this.myStringIndex >= this.myStrings.Count)
                        return null;

                    var q = this.myStrings[this.myStringIndex++];
                    if (q.Quotation != null)
                        return q;
                    this.myStringToSplit = q.Snippet;
                }

                var tokenEnd = this.myStringToSplit.IndexOfAny(Whitespace);
                var result = this.myStringToSplit;
                if (tokenEnd == -1)
                {
                    this.myStringToSplit = null;
                }
                else
                {
                    result = result.Substring(0, tokenEnd);
                    this.myStringToSplit = this.myStringToSplit.Substring(tokenEnd + 1).Trim();
                }
                return new QuotedString { Snippet = result };
            }

            public List<QuotedString> JoinRest()
            {
                var result = new List<QuotedString>();
                if (this.myStringToSplit != null)
                {
                    var remainingStr = this.myStringToSplit.Trim();
                    if (!String.IsNullOrEmpty(remainingStr))
                        result.Add(new QuotedString { Snippet = remainingStr });
                }

                result.AddRange(this.myStrings.Skip(this.myStringIndex));
                return result;
            }
        }

        internal static IEnumerable<Token> Tokenize(string program, VampRules rules)
        {
            var reader = new StringReader(program);
            var indenter = new Indenter();
            var quoter = new StringQuoter(rules.Quotations);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim() == "")
                    continue;

                int indentationLevel;
                line = indenter.Unindent(line, out indentationLevel);

                var quotedStrings = quoter.GetStrings(line, reader)
                    .Where(q => q.Quotation == null || q.Quotation.Kind != QuoteKind.Comment)
                    .ToList();
                if (quotedStrings.Count == 0)
                    continue;
                Debug.Assert(quotedStrings.All(q => q.Snippet.Length > 0));

                yield return new Token(Token.TokenType.Indentation, indentationLevel);

                // case 1: single quoted string
                if (quotedStrings.Count == 1 && quotedStrings[0].Quotation != null)
                {
                    yield return new Token(Token.TokenType.ElementValue, quotedStrings[0]);
                }
                else // case 2: unquoted string or more than one strings - split into key-op-value triplet
                {
                    var splitter = new StringSplitter(quotedStrings);
                    QuotedString qOp = null;

                    var qKey = splitter.GetNextString();
                    if (qKey.Quotation == null)
                    {
                        //TODO: don't do this if an operator is not made of only punctuation
                        var key = qKey.Snippet;
                        var op = rules.Operators.FirstOrDefault(o => key.EndsWith(o));
                        if (op != null)
                        {
                            key = key.Substring(0, key.Length - op.Length);
                            qOp = new QuotedString { Snippet = op };
                        }
                        qKey.Snippet = key;
                    }

                    if (qOp == null)
                    {
                        qOp = splitter.GetNextString();
                    }

                    yield return new Token(Token.TokenType.Key, qKey);
                    yield return new Token(Token.TokenType.Operator, qOp ?? new QuotedString { Snippet = rules.DefaultOp });

                    var valueStrings = splitter.JoinRest();
                    yield return new Token(Token.TokenType.Value, valueStrings);
                }
            }
        }

        /// <returns>A pseudo-node acting like the parent to all top-level nodes</returns>
        public static VampNode Parse(string program, VampRules rules)
        {
            var tokens = Tokenize(program, rules);
            var rootNodes = new List<VampNode>();
            var stack = new Stack<VampNode>();
            foreach (var token in tokens)
            {
                bool nodeCompleted = false;
                switch (token.Type)
                {
                    case Token.TokenType.Indentation:
                        var node = new VampNode();
                        int thisIndentLevel = token.ValueOfIndentation;
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
                        stack.Peek().Key = token.ValueOfKey;
                        break;
                    case Token.TokenType.Operator:
                        stack.Peek().Operator = token.ValueOfOperator;
                        break;
                    case Token.TokenType.Value:
                        stack.Peek().SetValues(token.ValueOfValue);
                        nodeCompleted = true;
                        break;
                    case Token.TokenType.ElementValue:
                        stack.Peek().SetValues(new[]{token.ValueOfElementValue});
                        nodeCompleted = true;
                        break;
                }

                if (nodeCompleted)
                {
                    //TODO: callback
                }
            }

            var pseudoRoot = new VampNode();
            pseudoRoot.AddChildren(rootNodes);
            return pseudoRoot;
        }
    }
}
