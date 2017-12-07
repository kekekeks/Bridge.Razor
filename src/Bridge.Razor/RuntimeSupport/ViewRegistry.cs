using System;
using System.Collections.Generic;

namespace Bridge.Razor.RuntimeSupport
{
    public static class ViewRegistry
    {
        static readonly Dictionary<string, Func<IRazorView>> Registered = new Dictionary<string, Func<IRazorView>>();
        public static void Register(string path, Func<IRazorView> factory)
        {
            Registered[path] = factory;
        }

        public static IRazorView CreateInstance(string path) => Registered[path]();
    }
}