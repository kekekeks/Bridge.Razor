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

or use partial view like [here](https://github.com/kekekeks/Bridge.Razor/tree/master/samples/SimpleExample/Components):

![component](https://user-images.githubusercontent.com/1067584/33739761-5c3ad9f2-dbaf-11e7-8f45-29f58e704748.png)

![react](https://user-images.githubusercontent.com/1067584/33739791-75e3b7fc-dbaf-11e7-8c84-c13ede957833.gif)

## How it works

Your .cshtml files get transpiled to .cs before Bridge.Net does its magic. You can find intermediate files in `bin/$(Configuration)/Bridge.Razor`
