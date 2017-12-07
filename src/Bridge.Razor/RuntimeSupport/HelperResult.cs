using System;
using System.IO;
using System.Threading.Tasks;

namespace Bridge.Razor.RuntimeSupport
{
    public class HelperResult : IHtmlContent
    {
        private readonly Action<TextWriter> _cb;

        public HelperResult(Action<TextWriter> cb)
        {
            _cb = cb;
        }

        public void WriteTo(TextWriter writer)
        {
            _cb(writer);
        }
    }
}