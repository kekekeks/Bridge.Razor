// Large chunks of this file were originally taken from https://github.com/SteveSanderson/Blazor/blob/master/src/Blazor.Compiler/VirtualDomDocumentWriter.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Parser.Html;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Bridge.Razor.Generator
{
    class RazorDomWriter : DocumentWriter
    {
        private readonly static Regex _incompleteAttributeRegex = new Regex(@"\s(?<name>[a-z0-9\.\-:_]+)\s*\=\s*$");

        public override RazorCSharpDocument WriteDocument(RazorCodeDocument codeDocument, DocumentIntermediateNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var wr = new CodeWriter();
            var ctx = new DefaultCodeRenderingContext(wr, node.Target.CreateNodeWriter(), codeDocument, node,
                node.Options);
            
            var visitor = new Visitor(node.Target, ctx);
            ctx.Visitor = visitor;
            visitor.VisitDocument(node);
            return new ReactCSharpDocument(wr.GenerateCode(),
                new RazorDiagnostic[0], node.Options);
        }

        class ReactCSharpDocument : RazorCSharpDocument
        {
            public ReactCSharpDocument(string code, IReadOnlyList<RazorDiagnostic> diagnostics, RazorCodeGenerationOptions options)
            {
                GeneratedCode = code;
                Diagnostics = diagnostics;
                Options = options;
            }
            public override string GeneratedCode { get; }
            public override IReadOnlyList<SourceMapping> SourceMappings { get; } = new SourceMapping[0];
            public override IReadOnlyList<RazorDiagnostic> Diagnostics { get; }
            public override RazorCodeGenerationOptions Options { get; }
        }
        
        private class Visitor : IntermediateNodeVisitor
        {
            public CodeRenderingContext Context { get; }
            private readonly CodeTarget _target;
            private string _unconsumedHtml;

            private string _nextAttributeName;
            private Dictionary<string, object> _nextElementAttributes = new Dictionary<string, object>();
            private List<CSharpExpressionIntermediateNode> _nextElementAttributeExpressions = new List<CSharpExpressionIntermediateNode>(); // Attributes where the whole name-value pair isn't known until runtime

            private int _sourceSequence;

            public Visitor(CodeTarget target, CodeRenderingContext context)
            {
                Context = context;
                _target = target;
            }

            public void RenderChildren(IntermediateNode node) => Context.RenderChildren(node);

            public override void VisitDefault(IntermediateNode node) => Context.RenderChildren(node);

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
            {
                using (Context.CodeWriter.BuildNamespace(node.Content))
                {
                    Context.CodeWriter.WriteLine("#line hidden");
                    VisitDefault(node);
                }
            }
            
            public override void VisitUsingDirective(UsingDirectiveIntermediateNode node) 
                => Context.NodeWriter.WriteUsingDirective(Context, node);

            public override void VisitCSharpCode(CSharpCodeIntermediateNode node) 
                => Context.NodeWriter.WriteCSharpCode(Context, node);

            
            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                using(Context.CodeWriter.BuildClassDeclaration(node.Modifiers, node.ClassName, node.BaseType, node.Interfaces))
                    VisitDefault(node);
            }

            public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode node)
            {
                Context.CodeWriter.WriteLine("#pragma warning disable 1998");
                for (int index = 0; index < node.Modifiers.Count; ++index)
                {
                    Context.CodeWriter.Write(node.Modifiers[index]);
                    Context.CodeWriter.Write(" ");
                }
                Context.CodeWriter.Write(node.ReturnType).Write(" ").Write(node.MethodName).WriteLine("()");
                using (Context.CodeWriter.BuildScope())
                    VisitDefault(node);
                Context.CodeWriter.WriteLine("#pragma warning restore 1998");
            }

            public override void VisitExtension(ExtensionIntermediateNode node) => node.WriteNode(_target, Context);


            private CSharpExpressionIntermediateNode MakeCSharpExpressionIntermediateNode(IntermediateNode parent, string csharpExpressionContent)
            {
                var content = new CSharpExpressionIntermediateNode {Source = parent.Source };
                
                content.Children.Add(new IntermediateToken
                {
                    Kind = TokenKind.CSharp,
                    Source = parent.Source,
                    Content = csharpExpressionContent
                });
                return content;
            }


            private void OpenElement(string tagName)
            {
                Context.CodeWriter
                    .WriteStartMethodInvocation("Builder.StartElement")
                    .WriteStringLiteral(tagName)
                    .WriteEndMethodInvocation();
            }

            private void EndElement()
            {
                Context.CodeWriter
                    .WriteStartMethodInvocation("Builder.EndElement")
                    .WriteEndMethodInvocation();
            }

            public override void VisitCSharpExpression(CSharpExpressionIntermediateNode node)
            {
                if (!string.IsNullOrEmpty(_unconsumedHtml))
                {
                    // We're in the middle of writing out an element tag. This C# expression might represent an entire
                    // attribute (e.g., @onclick(...)), or it might represent the value of an attribute (e.g., something=@value).
                    // Differentiate based on whether the unconsumed HTML ends with " attribute=".
                    var incompleteAttributeMatch = _incompleteAttributeRegex.Match(_unconsumedHtml);
                    if (incompleteAttributeMatch.Success)
                    {
                        var wholeMatchText = incompleteAttributeMatch.Groups[0];
                        var attributeName = incompleteAttributeMatch.Groups["name"].Value;
                        _unconsumedHtml = _unconsumedHtml.Substring(0, _unconsumedHtml.Length - wholeMatchText.Length + 1);
                        _nextElementAttributes[attributeName] = node;
                    }
                    else
                    {
                        // There's no incomplete attribute, so the C# expression must represent an entire attribute
                        _nextElementAttributeExpressions.Add(node);
                    }
                }
                else
                {
                    // We're between tags, so treat it as an @someVar expression to be rendered as a text node
                    WriteContentExpression(++_sourceSequence, Context, node);
                }
            }

            public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
            {
                _nextAttributeName = node.AttributeName;
                RenderChildren(node);
            }

            private void WriteAttribute(string name, object value)
            {
                Context.CodeWriter.WriteStartInstanceMethodInvocation("Builder", "SetAttributeValue");
                Context.CodeWriter.WriteStringLiteral(name);
                Context.CodeWriter.WriteParameterSeparator();

                if (value is HtmlContentIntermediateNode)
                {
                    Context.CodeWriter.WriteStringLiteral(GetContent((HtmlContentIntermediateNode)value));
                }
                else if (value is CSharpExpressionIntermediateNode)
                {
                    WriteCSharpExpression((CSharpExpressionIntermediateNode)value);
                }
                else if ((value is IntermediateToken razorIRToken) && razorIRToken.IsCSharp)
                {
                    Context.CodeWriter.Write(razorIRToken.Content);
                }
                else
                {
                    throw new ArgumentException("value parameter is of unexpected type " + value.GetType().FullName);
                }

                Context.CodeWriter.WriteEndMethodInvocation();
            }

            private void WriteCSharpExpression(CSharpExpressionIntermediateNode node)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                    {
                        Context.CodeWriter.Write(token.Content);
                    }
                    else
                    {
                        // There may be something else inside the expression like a Template or another extension node.
                        Visit(node.Children[i]);
                    }
                }
            }
            
            public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
            {
                _nextElementAttributes[_nextAttributeName] = CreateHtmlContentIntermediateNode(GetContent(node), node.Source);
                _nextAttributeName = null;
            }

            public override void VisitCSharpExpressionAttributeValue(CSharpExpressionAttributeValueIntermediateNode node)
            {
                if (node.Children.Count > 1)
                {
                    throw new ArgumentException("Attribute values can't contain more than one code element");
                }

                var value = node.Children.Single();
                _nextElementAttributes[_nextAttributeName] = value;
                _nextAttributeName = null;
            }

            public override void VisitCSharpCodeAttributeValue(CSharpCodeAttributeValueIntermediateNode node)
            {
                if (node.Children.Count > 1)
                {
                    throw new ArgumentException("Attribute values can't contain more than one code element");
                }

                var value = node.Children.Single();

                // For syntax like <button onclick="@{ some C# statement }">...</button>,
                // we convert the statement into a lambda, as if you wrote onclick="@(() => { some C# statement })"
                // since that does what you'd want and is generally a good syntax for callbacks
                var innerCSharp = (IntermediateToken)value;
                var attributeValue = MakeCSharpExpressionIntermediateNode(node, $"_ => {{ {innerCSharp.Content} }}");
                _nextElementAttributes[_nextAttributeName] = attributeValue;
                _nextAttributeName = null;
            }

            public override void VisitHtml(HtmlContentIntermediateNode node)
            {
                var htmlToTokenize = GetContent(node);
                if (!string.IsNullOrEmpty(_unconsumedHtml))
                {
                    htmlToTokenize = _unconsumedHtml + htmlToTokenize;
                    _unconsumedHtml = null;
                }
                var tokenizer = new HtmlTokenizer(
                    new TextSource(htmlToTokenize),
                    HtmlEntityService.Resolver);
                HtmlToken nextToken;
                while ((nextToken = tokenizer.Get()).Type != HtmlTokenType.EndOfFile) {
                    switch (nextToken.Type)
                    {
                        case HtmlTokenType.EndTag:
                            EndElement();
                            break;
                        case HtmlTokenType.StartTag:

                            var nextTag = nextToken.AsTag();

                            OpenElement(nextTag.Data);

                            foreach (var attribute in nextTag.Attributes)
                            {
                                WriteAttribute(attribute.Key, CreateHtmlContentIntermediateNode(attribute.Value));
                            }
                            
                            

                            if (_nextElementAttributes.Count > 0)
                            {
                                foreach (var attributeKvp in _nextElementAttributes)
                                {
                                    WriteAttribute(attributeKvp.Key, attributeKvp.Value);
                                }
                                _nextElementAttributes.Clear();
                            }

                            if (_nextElementAttributeExpressions.Count > 0)
                            {
                                var wr = (RuntimeNodeWriter) Context.NodeWriter;
                                var oldMethod = wr.WriteCSharpExpressionMethod;
                                wr.WriteCSharpExpressionMethod = "Builder.AddAttribute";
                                foreach (var attributeExpression in _nextElementAttributeExpressions)
                                {
                                    wr.WriteCSharpExpression(Context, attributeExpression);
                                }

                                wr.WriteCSharpExpressionMethod = oldMethod;
                                _nextElementAttributeExpressions.Clear();
                            }
                            if(nextTag.IsSelfClosing)
                                EndElement();
                            break;

                        case HtmlTokenType.Character:
                            WriteHtmlContent(++_sourceSequence, Context, CreateHtmlContentIntermediateNode(nextToken.Data, node.Source));
                            break;
                    }
                }

                // If we got an EOF in the middle of an HTML element, it's probably because we're
                // about to receive some attribute name/value pairs. Store the unused HTML content
                // so we can prepend it to the part that comes after the attributes to make
                // complete valid markup.
                if (htmlToTokenize.Length > nextToken.Position.Position)
                {
                    _unconsumedHtml = htmlToTokenize.Substring(nextToken.Position.Position - 1);
                }
            }


            public static void WriteHtmlContent(int sourceSequence, CodeRenderingContext context, HtmlContentIntermediateNode node)
            {
                const int MaxStringLiteralLength = 1024;

                var charactersConsumed = 0;

                // Render the string in pieces to avoid Roslyn OOM exceptions at compile time: https://github.com/aspnet/External/issues/54
                var nodeContent = GetContent(node);
                while (charactersConsumed < nodeContent.Length)
                {
                    string textToRender;
                    if (nodeContent.Length <= MaxStringLiteralLength)
                    {
                        textToRender = nodeContent;
                    }
                    else
                    {
                        var charactersToSubstring = Math.Min(MaxStringLiteralLength, nodeContent.Length - charactersConsumed);
                        textToRender = nodeContent.Substring(charactersConsumed, charactersToSubstring);
                    }
                    
                    context.CodeWriter
                        .WriteStartMethodInvocation("Builder.AppendText")
                        .WriteStringLiteral(textToRender)
                        .WriteEndMethodInvocation();
                    
                    charactersConsumed += textToRender.Length;
                }
            }

            public static void WriteContentExpression(int sourceSequence, CodeRenderingContext context, CSharpExpressionIntermediateNode node)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (node == null)
                {
                    throw new ArgumentNullException(nameof(node));
                }

                context.CodeWriter.WriteStartMethodInvocation("Builder.AppendExpression");
                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                    {
                        context.CodeWriter.Write(token.Content);
                    }
                    else
                    {
                        // There may be something else inside the expression like a Template or another extension node.
                        throw new NotImplementedException("There may be something else inside the expression like a Template or another extension node.");
                    }
                }

                context.CodeWriter.WriteEndMethodInvocation();
            }

            private static HtmlContentIntermediateNode CreateHtmlContentIntermediateNode(string content, SourceSpan? source = null)
            {
                var result = new HtmlContentIntermediateNode
                {
                    Source = source
                };

                result.Children.Add(new IntermediateToken
                {
                    Kind = TokenKind.Html,
                    Content = content
                });

                return result;
            }

            private static string GetContent(HtmlContentIntermediateNode node)
            {
                var Builder = new StringBuilder();
                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsHtml)
                    {
                        Builder.Append(token.Content);
                    }
                }

                return Builder.ToString();
            }

            private static string GetContent(HtmlAttributeValueIntermediateNode node)
            {
                var Builder = new StringBuilder();
                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsHtml)
                    {
                        Builder.Append(token.Content);
                    }
                }

                return Builder.ToString();
            }
        }
    }
}
