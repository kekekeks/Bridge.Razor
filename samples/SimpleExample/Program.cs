using Bridge.Html5;
using Bridge.Razor;
using SimpleExample.ViewModels;

namespace SimpleExample
{
    public class Program
    {
        public static async void Main()
        {
            var content = await RazorEngine.ExecuteViewToStringAsync(
                "/Views/SimpleView.cshtml", new SimpleViewModel
            {
                Foo = "bar"
            });
            Document.Body.InnerHTML = content;
        }
    }
}