using System;
using System.Collections.Generic;
using System.Linq;
using Bridge.React;

namespace Bridge.Razor.React.RuntimeSupport
{
    public class ReactDomBuilder : IDomBuilder
    {
        [Template("React.createElement.apply(null, {args})")]
        public static extern ReactElement CreateElementDyn(object[] args);
        
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

        class ComponentElement : IChild
        {
            private readonly dynamic _component;

            public ComponentElement(dynamic component)
            {
                _component = component;
            }

            public object Build()
            {
                return _component._reactElement;
            }
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
                var args = Children.Select(c => c.Build()).ToList();
                args.Insert(0, Attributes);
                args.Insert(0, Name);
                var arr = args.ToArray();
                return CreateElementDyn(arr);
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

        public void AppendExpression(dynamic expr)
        {
            if (expr == null)
                return;
            if (expr._reactElement != null)
                _currentElement.Children.Add(new ComponentElement(expr));
            else
                AppendText(((object) expr).ToString());
        }
    }
}