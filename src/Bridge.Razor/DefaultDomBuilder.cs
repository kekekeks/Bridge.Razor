using System.Collections.Generic;
using Bridge.Html5;

namespace Bridge.Razor
{
    public class DefaultDomBuilder : IDomBuilder
    {       
        private HTMLElement _currentElement = Document.CreateElement("div");
        private Stack<HTMLElement> _stack = new Stack<HTMLElement>();
        
        public DefaultDomBuilder(HTMLElement root)
        {
            _currentElement = root;
        }
        
        public void StartElement(string name)
        {
            var element = Document.CreateElement(name);
            _currentElement.AppendChild(element);
            _stack.Push(_currentElement);
            _currentElement = element;

        }

        public void EndElement() => _currentElement = _stack.Pop();

        public void SetAttributeValue(string name, object value)
        {
            if (name.StartsWith("on"))
                Script.Set(_currentElement, name, value);
            else
                _currentElement.SetAttribute(name, value?.ToString());
        }

        public void AppendText(string text) 
            => _currentElement.AppendChild(Document.CreateTextNode(text));
    }
}