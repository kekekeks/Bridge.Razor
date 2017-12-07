using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Bridge.Razor.Generator
{
    public sealed class CustomInheritsDirective : IntermediateNodePassBase, IRazorDirectiveClassifierPass,
        IRazorEngineFeature
    {
        public bool PartialClassMode { get; set; }

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            ClassDeclarationIntermediateNode primaryClass = documentNode.FindPrimaryClass();
            if (primaryClass == null)
                return;

            string fullClassName = null;
            foreach (IntermediateNodeReference directiveReference in documentNode.FindDirectiveReferences(InheritsDirective.Directive))
            {
                DirectiveTokenIntermediateNode intermediateNode =
                    ((DirectiveIntermediateNode) directiveReference.Node).Tokens
                    .FirstOrDefault<DirectiveTokenIntermediateNode>();
                if (intermediateNode != null)
                {

                    fullClassName = intermediateNode.Content;
                    break;
                }
            }
            if(fullClassName == null)
                return;
                
                
            if (PartialClassMode)
            {
                var genericStart = fullClassName.IndexOf("<");
                var nsi = genericStart == -1
                    ? fullClassName.LastIndexOf('.')
                    : fullClassName.LastIndexOf(fullClassName.Substring(0, genericStart), '.');
                var ns = fullClassName.Substring(0, nsi);
                var className = fullClassName.Substring(nsi + 1);

                var pns = documentNode.FindPrimaryNamespace().Content = ns;
                primaryClass.BaseType = null;
                primaryClass.Modifiers.Add("partial");
                primaryClass.ClassName = className;
            }
            else
            {

                primaryClass.BaseType = fullClassName;

            }
        }
        public void Register(IRazorEngineBuilder builder)
        {
            builder.AddDirective(InheritsDirective.Directive);
            builder.Features.Add(this);
        }
    }
}