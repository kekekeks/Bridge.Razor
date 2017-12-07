using System;
using System.Collections.Generic;

namespace Bridge.Razor.RuntimeSupport
{
    public static class ViewRegistry
    {
        static readonly Dictionary<string, Func<object>> Registered = new Dictionary<string, Func<object>>();
        public static void Register(string path, Func<object> factory)
        {
            Registered[path] = factory;
        }

        public static object CreateInstance(string path) => Registered[path]();
    }
}