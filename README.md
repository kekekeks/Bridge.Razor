# Razor support for bridge.net

## Usage

1) Add nuget package (currently on myget https://www.myget.org/F/bridge-razor/api/v2 )
2) Create .cshtml file
3) 
```csharp
 Document.Body.AppendChild(RazorEngine.RenderDefaultView("/Views/SimpleView.cshtml", new SimpleViewModel()
{
  Foo = "bar"
}));
```


## How it works

Your .cshtml files get transpiled to .cs before Bridge.Net does its magic. You can find intermediate files in `bin/$(Configuration)/Bridge.Razor`
