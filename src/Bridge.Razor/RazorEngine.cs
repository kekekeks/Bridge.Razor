using System.IO;
using System.Threading.Tasks;
using Bridge.Html5;
using Bridge.Razor.RuntimeSupport;

namespace Bridge.Razor
{
    public static class RazorEngine
    {
        public static object CreateView(string path) => ViewRegistry.CreateInstance(path);
        
        public static HTMLElement RenderDefaultView(string path, object model)
        {
            var v = (IBaseView) CreateView(path);
            v.Model = model;
            var root = Document.CreateElement("div");
            v.Builder = new DefaultDomBuilder(root);
            v.Execute();
            return root;
        }
    }
}