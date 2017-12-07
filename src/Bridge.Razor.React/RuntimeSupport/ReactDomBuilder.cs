using System;
using System.Collections.Generic;
using System.Linq;
using Bridge.React;

namespace Bridge.Razor.React.RuntimeSupport
{
    public class ReactDomBuilder : IDomBuilder
    {
        [Template("React.createElement({name}, Bridge.React.fixAttr({properties}), {*children})")]
        public static extern ReactElement CreateElement(string name, object properties, object[] children);
        
        [Template("React.createElement({name}, Bridge.React.fixAttr({properties}))")]
        public static extern ReactElement CreateElement(string name, object properties);
        public ReactDomBuilder()
        {
            _currentElement = new DomElement() {Name = "component"};
        }

        interface IChild
        {
            object Build();
        }
        
        class TextDomElement : IChild
        {
            private readonly string _text;

            public TextDomElement(string text)
            {
                _text = text;
            }

            public object Build() => _text;
        }
        
        class DomElement : IChild
        {
            public string Name { get; set; }
            public List<IChild> Children { get; set; } = new List<IChild>();
            public dynamic Attributes = new object();

            public object Build()
            {
                if (Children.Count == 0)
                    return CreateElement(Name, Attributes);
                return CreateElement(Name, Attributes, Children.Select(c => c.Build()).ToArray());
            }
        }

        private DomElement _currentElement;
        private Stack<DomElement> _stack = new Stack<DomElement>();
        
        public void StartElement(string name)
        {
            var el = new DomElement() {Name = name};
            _currentElement.Children.Add(el);
            _stack.Push(_currentElement);
            _currentElement = el;
        }

        public void AppendText(string text)
        {
            _currentElement.Children.Add(new TextDomElement(text));
        }
        
        public void EndElement()
        {
            _currentElement = _stack.Pop();
        }

        public void SetAttributeValue(string name, object value)
        {
            Script.Set(_currentElement.Attributes, name, value);
        }
        
        public ReactElement Create()
        {
            if (_stack.Count != 0)
                throw new InvalidOperationException($"{_currentElement.Name} is not closed");
            return (ReactElement) _currentElement.Build();
        }
    }
}