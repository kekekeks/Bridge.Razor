using System.IO;
using System.Threading.Tasks;

namespace Bridge.Razor
{
    public interface IRazorView
    {
        Task ExecuteAsync(TextWriter writer, object model);
    }
}