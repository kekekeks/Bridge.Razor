using System.IO;

namespace Bridge.Razor
{
    public interface IHtmlContent
    {
        void WriteTo(TextWriter writer);
    }
}