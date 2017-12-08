using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Bridge.Razor.Generator
{
    class Program
    {
        private static string ClassName =
            "BridgeRazorEngineIntermediateResultDontUseInYourViewsOrTheyWillBeBrokenThankYou" + Guid.NewGuid();

        private static string DefaultNamespace = "Bridge.Razor.Generated";
        private static string TempNamespace = Guid.NewGuid().ToString().Replace("-", "");
        
        static int Main(string[] args)
        {
            var baseDirectory = Path.GetFullPath(args[0]);
            var outputDirectory = Path.GetFullPath(args[1]);
            if(Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
            Directory.CreateDirectory(outputDirectory);



            CustomWriterPhase customWriter = null;
            var inheritsDirective = new CustomInheritsDirective();
            var reactCtorPhase = new GenerateReactComponentCtorFeature();
            var engine = RazorEngine.Create(builder =>
            {
                builder
                    .SetNamespace(TempNamespace)
                    .SetBaseType("Bridge.Razor.BaseView")
                    .ConfigureClass((document, @class) =>
                    {
                        @class.ClassName = ClassName;
                        @class.Modifiers.Clear();
                        @class.Modifiers.Add("public");
                    });
                var defaultCSharpLoweringPhase = builder.Phases.OfType<IRazorCSharpLoweringPhase>().Single();
                builder.Phases.Remove(defaultCSharpLoweringPhase);
                builder.Phases.Add(customWriter = new CustomWriterPhase(defaultCSharpLoweringPhase));
                
                inheritsDirective.Register(builder);
                Microsoft.AspNetCore.Razor.Language.Extensions.FunctionsDirective.Register(builder);
                builder.Features.Add(reactCtorPhase);

                
            });
            
            var prj = RazorProject.Create(baseDirectory);
            var tengine = new RazorTemplateEngine(engine, prj);

            var initCode = "";

            var cshtmlFiles = prj.EnumerateItems("/");

            foreach (var item in cshtmlFiles)
            {
                Console.WriteLine($"Generating code file for view {item.CombinedPath}");
                var path = (item.CombinedPath).Replace("\\", "/");
                var className = Regex.Replace(path.Replace("/", "_"),
                                    "[^A-Za-z-0-9]+", "") + "_" +
                                Guid.NewGuid().ToString().Replace("-", "");
                var outputName = Utils.EscapePath(path);
                var ns = DefaultNamespace;

                var suppressRegistration = false;
                inheritsDirective.PartialClassMode = false;
                reactCtorPhase.Enabled = false;
                Func<string, string> postProcess = _ => _;
                
                var lines = File.ReadAllLines(item.PhysicalPath);
                foreach (var l in lines)
                {
                    var m = Regex.Match(l, @"^\s*@\*(.*)\*@\s*$");
                    if(!m.Success && !string.IsNullOrWhiteSpace(l))
                        break;
                    var directive = m.Groups[1].Value.Trim().Split(new[] {'='}, 2).Select(x => x.Trim()).ToArray();
                    var key = directive[0];
                    var value = directive.Length > 1 ? directive[1] : null;
                    if (key == "type" && value == "partial")
                    {
                        inheritsDirective.PartialClassMode = true;
                        suppressRegistration = true;
                    }

                    if (key == "classname")
                    {
                        var parsed = new TypeReferenceInfo(value);
                        ns = parsed.Namespace;
                        className = parsed.Name;
                        suppressRegistration = true;
                    }
                    if (key == "type" && value == "react-component")
                    {
                        reactCtorPhase.Enabled = true;
                    }
                }
                customWriter.Writer = new RazorDomWriter();
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
                    
                    
                    var code = cSharpDocument.GeneratedCode.Replace(ClassName, className);
                    code = postProcess(code.Replace(
                            "public async override global::System.Threading.Tasks.Task ExecuteAsync(",
                            "protected override void RenderRazor(Bridge.Razor.IDomBuilder Builder")
                        .Replace(TempNamespace, ns)
                    );
                    
                    File.WriteAllText(Path.Combine(outputDirectory, outputName + ".generated.cs"), code);
                    if (!suppressRegistration)
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
        
        
        

    }
}