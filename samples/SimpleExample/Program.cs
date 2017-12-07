using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Bridge.Html5;
using Bridge.Razor;
using Bridge.Razor.React;
using Bridge.React;
using SimpleExample.ViewModels;
using SimpleExample.Views;

namespace SimpleExample
{
    public class Program
    {
        static Task LoadScript(string src)
        {
            var t = (HTMLScriptElement) Document.CreateElement("script");
            t.Type = "text/javascript";
            t.Src = src;
            t.Async = false;
            var tcs = new TaskCompletionSource<int>();
            t.OnLoad = _ => { tcs.SetResult(0); };
            Document.Head.AppendChild(t);
            return tcs.Task;
        }
        public static async void Main()
        {
            await LoadScript("react.js");
            await LoadScript("react-dom.js");
            React.Render(new SimpleReactComponent(new SimpleReactComponent.Props()
            {
                Label = "Input text",
                OnSave = t => Window.Alert(t)
            }), Document.Body);
            /*
            var content = await RazorEngine.ExecuteViewToStringAsync(
                "/Views/SimpleView.cshtml", new SimpleViewModel
            {
                Foo = "bar"
            });
            Document.Body.InnerHTML = content;
            var c = (RazorComponent) RazorEngine.CreateView("/Views/ReactComponent.cshtml");
            React.Render(c.Render(), Document.Body);*/
        }
    }
}