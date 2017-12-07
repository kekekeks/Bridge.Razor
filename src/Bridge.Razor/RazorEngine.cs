using System.IO;
using System.Threading.Tasks;
using Bridge.Razor.RuntimeSupport;

namespace Bridge.Razor
{
    public static class RazorEngine
    {
        public static object CreateView(string path) => ViewRegistry.CreateInstance(path);
    }
}