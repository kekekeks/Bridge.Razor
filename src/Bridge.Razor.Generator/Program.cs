using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;

namespace Bridge.Razor.Generator
{
    class Program
    {
        private static string ClassName =
            "BridgeRazorEngineIntermediateResultDontUseInYourViewsOrTheyWillBeBrokenThankYou" + Guid.NewGuid();

        private static string Namespace = "Bridge.Razor.Generated";
        
        static int Main(string[] args)
        {
            var baseDirectory = Path.GetFullPath(args[0]);
            var outputDirectory = Path.GetFullPath(args[1]);
            if(Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
            Directory.CreateDirectory(outputDirectory);
            
            var engine = CreateRazorEngine("Bridge.Razor.Generated");
            var prj = RazorProject.Create(baseDirectory);
            var tengine = new RazorTemplateEngine(engine, prj);



            var initCode = "";

            var cshtmlFiles = prj.EnumerateItems("/");

            foreach (var item in cshtmlFiles)
            {
                Console.WriteLine($"Generating code file for view {item.CombinedPath}");

                var cSharpDocument = tengine.GenerateCode(item);
                if (cSharpDocument.Diagnostics.Any())
                {
                    var diagnostics = string.Join(Environment.NewLine, cSharpDocument.Diagnostics);
                    Console.Error.WriteLine(
                        $"One or more parse errors encountered. This will not prevent the generator from continuing: {Environment.NewLine}{diagnostics}.");
                    return 1;
                }
                else
                {
                    var path = (item.CombinedPath).Replace("\\", "/");
                    var className = Regex.Replace(path.Replace("/", "_"),
                                        "[^A-Za-z-0-9]+", "") + "_" +
                                    Guid.NewGuid().ToString().Replace("-", "");
                    var code = cSharpDocument.GeneratedCode.Replace(ClassName, className);
                    File.WriteAllText(Path.Combine(outputDirectory, className + ".generated.cs"), code);
                    initCode += "\nBridge.Razor.RuntimeSupport.ViewRegistry.Register(\""
                                + path.Replace("\\", "/")
                                + "\", () => new " + className + " ());";
                }
            }

            Console.WriteLine("Generating item registration...");

            initCode = "namespace Bridge.Razor.Generated\n{\nstatic class ViewRegistration\n{\n" +
                       "[Bridge.Init(Bridge.InitPosition.Bottom)]\n" +
                       "public static void Register(){\n" + initCode
                       + "\n}}}";
            File.WriteAllText(Path.Combine(outputDirectory, "Bridge.Razor.Generated.ViewRegistration.generated.cs"),
                initCode);
                    
            Console.WriteLine("Razor generation completed");

            return 0;
        }
        
        public static RazorEngine CreateRazorEngine(string ns, Action<IRazorEngineBuilder> configure = null)
        {
            var razorEngine = RazorEngine.Create(builder =>
            {
                builder
                    .SetNamespace(ns)
                    .SetBaseType("Bridge.Razor.BaseView")
                    .ConfigureClass((document, @class) =>
                    {
                        @class.ClassName = ClassName;
                        @class.Modifiers.Clear();
                        @class.Modifiers.Add("public");
                    });
                
                Microsoft.AspNetCore.Razor.Language.Extensions.InheritsDirective.Register(builder);
                Microsoft.AspNetCore.Razor.Language.Extensions.FunctionsDirective.Register(builder);
                configure?.Invoke(builder);
            });
            return razorEngine;
        }

    }
}