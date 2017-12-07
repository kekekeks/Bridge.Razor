using System;
// Origin: Microsoft.Extensions.RazorViews.AttributeValue
namespace Bridge.Razor.RuntimeSupport
{
    public class AttributeValue
    {
        public AttributeValue(string prefix, object value, bool literal)
        {
            this.Prefix = prefix;
            this.Value = value;
            this.Literal = literal;
        }

        public string Prefix { get; }

        public object Value { get; }

        public bool Literal { get; }

        public static AttributeValue FromTuple(Tuple<string, object, bool> value)
        {
            return new AttributeValue(value.Item1, value.Item2, value.Item3);
        }

        public static AttributeValue FromTuple(Tuple<string, string, bool> value)
        {
            return new AttributeValue(value.Item1, (object) value.Item2, value.Item3);
        }

        public static implicit operator AttributeValue(Tuple<string, object, bool> value)
        {
            return AttributeValue.FromTuple(value);
        }
    }
}