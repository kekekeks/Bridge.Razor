using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Bridge.Razor.Generator
{
    public class CustomWriterPhase : IRazorCSharpLoweringPhase
    {
        private readonly IRazorCSharpLoweringPhase _inner;

        public DocumentWriter Writer { get; set; }
        public CustomWriterPhase(IRazorCSharpLoweringPhase inner)
        {
            _inner = inner;
        }

        public void Execute(RazorCodeDocument codeDocument)
        {
            if (Writer != null)
            {
                var intermediateNode = codeDocument.GetDocumentIntermediateNode();
                RazorCSharpDocument csharp = Writer.WriteDocument(codeDocument, intermediateNode);
                codeDocument.SetCSharpDocument(csharp);
            }
            else
                _inner.Execute(codeDocument);
        }

        public RazorEngine Engine
        {
            get => _inner.Engine;
            set => _inner.Engine = value;
        }

    }
}