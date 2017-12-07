#addin "nuget:?package=NuGet.Core&version=2.14.0"
#tool "nuget:?package=NuGet.CommandLine&version=4.3.0"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var version = Argument("nuget-version", "0.0.1-debug");

Task("Clean").Does(()=>
{
    CleanDirectories("build");
    CreateDirectory("build");
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./Bridge.Razor.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() => {
    MSBuild("./src/Bridge.Razor/Bridge.Razor.csproj", settings => settings.SetConfiguration(configuration));
    MSBuild("./src/Bridge.Razor.React/Bridge.Razor.React.csproj", settings => settings.SetConfiguration(configuration));
    MSBuild("./src/Bridge.Razor.Generator/Bridge.Razor.Generator.csproj", settings => settings.SetConfiguration(configuration));
});

void Pack(string id, NuSpecContent[] content, string description)
{
     var settings = new NuGetPackSettings()
     {
         Id = id,
         Files = content,
         BasePath = Directory("."),
         OutputDirectory = "build",
         Version = version,
         Title                   = id,
         Authors                 = new[] {"Nikita Tsukanov"},
         Owners                  = new[] {"kekekeks"},
         Description             = description,
         NoPackageAnalysis       = true,
         ProjectUrl              = new Uri("https://github.com/kekekeks/Bridge.Razor") 
    };
    NuGetPack(settings);
}
    
Task("CreatePackage")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .Does(() => {
    var gb = "src/Bridge.Razor.Generator/bin/" + configuration + "/";
    
    var content = new[] {
        new NuSpecContent{
            Source = gb + "Bridge.Razor.Generator.exe", 
            Target = "tools/gen"
        },
        new NuSpecContent{
            Source = gb + "Microsoft.AspNetCore.Razor.Language.dll", 
            Target = "tools/gen"
        }, 
        new NuSpecContent{
            Source = gb + "Microsoft.AspNetCore.Razor.dll", 
            Target = "tools/gen"
        },
        new NuSpecContent{
            Source = "src/Bridge.Razor/Bridge.Razor.targets",
            Target = "build"
        },
        new NuSpecContent{
            Source = "src/Bridge.Razor/bin/"  + configuration + "/Bridge.Razor.dll",
            Target = "lib/net40"
        }
    };
    Pack("Bridge.Razor", content, "Razor support for Bridge");
    Pack("Bridge.Razor.React", new []{
        new NuSpecContent{
            Source = "src/Bridge.Razor.React/bin/"  + configuration + "/Bridge.Razor.React.dll",
            Target = "lib/net40"
        }
    }, "Razor support for Bridge.React");
});

Task("Default").IsDependentOn("CreatePackage");

RunTarget(target);

