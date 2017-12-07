using System;
using Bridge;
using Bridge.Razor.React;
using Bridge.React;

namespace SimpleExample.Views
{
    public partial class SimpleReactComponent 
        : RazorComponent<SimpleReactComponent.Props, SimpleReactComponent.State>
    {
        public class Props
        {
            public string Label { get; set; }
            public Action<string> OnSave;
        }
        
        public class State
        {
            public string Value { get; set; }
        }
        
        
        public SimpleReactComponent(Props props, params Union<ReactElement, string>[] children)
            : base(props, children)
        {
        }

        // Make Rider happy
        protected SimpleReactComponent() : this(null)
        {
            
        }
    }
}