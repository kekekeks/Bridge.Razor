using System;
using Bridge.Html5;
using Bridge.Razor.React.RuntimeSupport;
using Bridge.React;

namespace Bridge.Razor.React
{
    public class RazorComponent : Component<object, object> 
    {
        /*public RazorComponent(TProps props, params Union<ReactElement, string>[] children) : base(props, children)
        {
        }

        static TProps Throw()
        {
            throw new InvalidOperationException("This constuctor is here to make ReSharper/Rider happy, don't use");
        }*/

        public RazorComponent() : base(new object())
        {
            
        }
        
        protected ReactDomBuilder Builder { get; private set; }

        protected virtual void RenderCore()
        {
            
        }
        
        public override ReactElement Render()
        {
            Builder = new ReactDomBuilder();
            RenderCore();
            return Builder.Create();
            
        }

        public object Event(Action<SyntheticEvent<HTMLElement>> cb) => cb;
    }
}