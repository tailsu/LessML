using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LessML.Strings;

namespace LessML.Vamp
{
    [DebuggerDisplay("{Key} {Operator} {Value}")]
    public class VampNode
    {
        public QuotedString Key { get; set; }
        public QuotedString Operator { get; set; }
        public List<QuotedString> Value { get; private set; }
        public VampNode Parent { get; set; }

        public readonly List<VampNode> Children = new List<VampNode>();

        public VampNode()
        {
            this.Value = new List<QuotedString>();
        }

        public bool IsBareValue
        {
            get { return this.Key == null; }
        }

        public string JoinValue()
        {
            return String.Join("", this.Value.Select(q => q.Snippet).ToArray());
        }

        public void SetValue(string value)
        {
            this.Value = new List<QuotedString>();
            if (!String.IsNullOrEmpty(value))
                this.Value.Add(value);
        }

        public void SetValues(IEnumerable<QuotedString> values)
        {
            this.Value = new List<QuotedString>();
            this.Value.AddRange(values);
        }

        public void AddChild(VampNode child)
        {
            if (child.Parent != null)
                throw new Exception("TODO");
            this.Children.Add(child);
            child.Parent = this;
        }

        public void AddChildren(IEnumerable<VampNode> children)
        {
            foreach (var child in children)
                this.AddChild(child);
        }

        public void ClearChildren()
        {
            foreach (var child in this.Children)
                child.Parent = null;
            this.Children.Clear();
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
