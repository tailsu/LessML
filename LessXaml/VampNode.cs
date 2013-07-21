using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LessML.Strings;

namespace LessML
{
    [DebuggerDisplay("{Key} {Operator} {Value}")]
    public class VampNode
    {
        public QuotedString Key { get; set; }
        public QuotedString Operator { get; set; }
        public IList<QuotedString> Value { get; set; }
        public VampNode Parent { get; set; }

        public readonly List<VampNode> Children = new List<VampNode>();

        public bool IsBareValue
        {
            get { return Key == null; }
        }

        public string JoinValue()
        {
            return String.Join("", this.Value.Select(q => q.Snippet).ToArray());
        }

        public bool IsSemanticallyEquivalent(VampNode other)
        {
            return QuotedString.IsSemanticallyEquivalent(this.Key, other.Key)
                && QuotedString.IsSemanticallyEquivalent(this.Operator, other.Operator)
                && this.Value.Count == other.Value.Count
                && (this.Value.Count == 0 || this.Value.Zip(other.Value, (a, b) => QuotedString.IsSemanticallyEquivalent(a, b)).All(_ => _))
                && this.Children.Count == other.Children.Count
                && (this.Children.Count == 0 || this.Children.Zip(other.Children, (a, b) => a.IsSemanticallyEquivalent(b)).All(_ => _));
        }
    }

    internal static class EnumerableExtensions
    {
        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(
            this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
        {
            using (IEnumerator<TFirst> iterator1 = first.GetEnumerator())
            using (IEnumerator<TSecond> iterator2 = second.GetEnumerator())
            {
                while (iterator1.MoveNext() && iterator2.MoveNext())
                {
                    yield return resultSelector(iterator1.Current, iterator2.Current);
                }
            }
        }
    }
}
