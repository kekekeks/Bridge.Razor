using System;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Bridge.Razor.Generator
{
    public class GenerateReactComponentCtorFeature: IntermediateNodePassBase, IRazorOptimizationPass
    {
        public bool Enabled { get; set; }
        public override int Order => 2000;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if(!Enabled)
                return;
            var primary = documentNode.FindPrimaryClass();
            if (string.IsNullOrWhiteSpace(primary.BaseType) || !primary.BaseType.Contains("."))
                throw new InvalidOperationException("Invalid base type");
            var baseType = new TypeReferenceInfo(primary.BaseType);
            if (baseType.GenericTypeArguments.Count < 1)
                throw new InvalidOperationException("Invalid base type");

            var ctor = new StringBuilder();

            ctor.AppendLine($"public {primary.ClassName}({baseType.GenericTypeArguments[0]} props,");
            ctor.AppendLine("params Bridge.Union<Bridge.React.ReactElement, string>[] children) : ");
            ctor.AppendLine("base(props, children){}");


            primary.Children.Add(new CSharpCodeIntermediateNode()
            {
                Children =
                {
                    new IntermediateToken()
                    {
                        Kind = TokenKind.CSharp,
                        Content = ctor.ToString()
                    }
                }
            });

        }
    }
}