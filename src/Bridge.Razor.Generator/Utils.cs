using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bridge.Razor.Generator
{
    
    public class TypeReferenceInfo
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public List<string> GenericTypeArguments { get; set; } = new List<string>();

        public TypeReferenceInfo(string fullClassName)
        {
            
            var genericStart = fullClassName.IndexOf("<");
            var nsi = genericStart == -1
                ? fullClassName.LastIndexOf('.')
                : fullClassName.LastIndexOf(fullClassName.Substring(0, genericStart), '.');
            Namespace = fullClassName.Substring(0, nsi);
            Name = fullClassName.Substring(nsi + 1);
            if (genericStart != -1)
            {
                GenericTypeArguments = fullClassName.Substring(genericStart).Trim('<', '>').Split(',')
                    .Select(x => x.Trim()).ToList();
            }

        }
    }

    public static class Utils
    {

        
        public static string EscapePath(string path)
        {
            return Regex.Replace(path, "[^A-Za-z-0-9._]", match => { return "-" + (int) match.Value[0] + "-"; });
        }
    }
}