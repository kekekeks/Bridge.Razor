using Bridge.Razor.React.RuntimeSupport;
using Bridge.React;

namespace Bridge.Razor.React
{
    public class RazorReactStatelessComponent<TProps> : StatelessComponent<TProps>
    {
        public RazorReactStatelessComponent(TProps props, params Union<ReactElement, string>[] children) : base(props, children)
        {
        }

        protected RazorReactStatelessComponent() : this(default(TProps), null)
        {
            
        }

        protected virtual void RenderRazor(IDomBuilder builder)
        {
            
        }
        
        public override ReactElement Render()
        {
            var builder = new ReactDomBuilder();
            RenderRazor(builder);
            return builder.Create();
        }
    }
}