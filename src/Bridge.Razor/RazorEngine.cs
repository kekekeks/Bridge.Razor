using System.IO;
using System.Threading.Tasks;
using Bridge.Razor.RuntimeSupport;

namespace Bridge.Razor
{
    public static class RazorEngine
    {
        public static Task ExecuteViewAsync(string path, TextWriter output, object model) 
            => ((IRazorView)ViewRegistry.CreateInstance(path)).ExecuteAsync(output, model);

        public static async Task<string> ExecuteViewToStringAsync(string path, object model)
        {
            var sw = new StringWriter();
            await ExecuteViewAsync(path, sw, model);
            return sw.ToString();
        }

        public static object CreateView(string path) => ViewRegistry.CreateInstance(path);
    }
}