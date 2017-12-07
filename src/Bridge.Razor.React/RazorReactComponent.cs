using System;
using Bridge.Html5;
using Bridge.Razor.React.RuntimeSupport;
using Bridge.React;

namespace Bridge.Razor.React
{
    public abstract class RazorComponent<TProps, TState> : Component<TProps, TState> 
    {
        public RazorComponent(TProps props, params Union<ReactElement, string>[] children) : base(props, children)
        {
        }

        
        protected ReactDomBuilder Builder { get; private set; }

        protected virtual void RenderRazor()
        {
            
        }
        
        public override ReactElement Render()
        {
            Builder = new ReactDomBuilder();
            RenderRazor();
            return Builder.Create();
            
        }

        public object Event(Action<SyntheticEvent<HTMLElement>> cb) => cb;
    }
}