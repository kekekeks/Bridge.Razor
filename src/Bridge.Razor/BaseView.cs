﻿using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Bridge.Razor.RuntimeSupport;

namespace Bridge.Razor
{
    public abstract class BaseView : BaseView<dynamic>
    {
        
    }

    public abstract class BaseView<T> : IRazorView
    {
        public T Model { get; set; }
        
        public TextWriter Output { get; set; }

        public Task ExecuteAsync(TextWriter writer, object model)
        {
            Output = writer;
            Model = (T) model;
            return ExecuteAsync();
        }
        
        public virtual Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
        
        //TODO: optimize
        string HtmlEscape(string s)
        {
            return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#039;");
        }
        
        // Mostly copy-pasted from https://github.com/aspnet/Common/blob/dev/shared/Microsoft.Extensions.RazorViews.Sources/BaseView.cs
        #region Microsoft.Extensions.RazorViewsBaseView.cs
        /// <summary>
        /// Write the given value directly to the output
        /// </summary>
        /// <param name="value"></param>
        protected void WriteLiteral(string value)
        {
            WriteLiteralTo(Output, value);
        }

        /// <summary>
        /// Write the given value directly to the output
        /// </summary>
        /// <param name="value"></param>
        protected void WriteLiteral(object value)
        {
            WriteLiteralTo(Output, value);
        }

        private List<string> AttributeValues { get; set; }

        protected void WriteAttributeValue(string thingy, int startPostion, object value, int endValue, int dealyo, bool yesno)
        {
            if (AttributeValues == null)
            {
                AttributeValues = new List<string>();
            }

            AttributeValues.Add(value.ToString());
        }

        private string AttributeEnding { get; set; }

        protected void BeginWriteAttribute(string name, string begining, int startPosition, string ending, int endPosition, int thingy)
        {
            Debug.Assert(string.IsNullOrEmpty(AttributeEnding));

            Output.Write(begining);
            AttributeEnding = ending;
        }

        protected void EndWriteAttribute()
        {
            Debug.Assert(!string.IsNullOrEmpty(AttributeEnding));

            var attributes = string.Join(" ", AttributeValues);
            Output.Write(attributes);
            AttributeValues = null;

            Output.Write(AttributeEnding);
            AttributeEnding = null;
        }

        /// <summary>
        /// Writes the given attribute to the given writer
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="name">The name of the attribute to write</param>
        /// <param name="leader">The value of the prefix</param>
        /// <param name="trailer">The value of the suffix</param>
        /// <param name="values">The <see cref="AttributeValue"/>s to write.</param>
        protected void WriteAttributeTo(
            TextWriter writer,
            string name,
            string leader,
            string trailer,
            params AttributeValue[] values)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (leader == null)
            {
                throw new ArgumentNullException(nameof(leader));
            }

            if (trailer == null)
            {
                throw new ArgumentNullException(nameof(trailer));
            }


            WriteLiteralTo(writer, leader);
            foreach (var value in values)
            {
                WriteLiteralTo(writer, value.Prefix);

                // The special cases here are that the value we're writing might already be a string, or that the
                // value might be a bool. If the value is the bool 'true' we want to write the attribute name
                // instead of the string 'true'. If the value is the bool 'false' we don't want to write anything.
                // Otherwise the value is another object (perhaps an HtmlString) and we'll ask it to format itself.
                string stringValue;
                if (value.Value is bool)
                {
                    if ((bool)value.Value)
                    {
                        stringValue = name;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    stringValue = value.Value as string;
                }

                // Call the WriteTo(string) overload when possible
                if (value.Literal && stringValue != null)
                {
                    WriteLiteralTo(writer, stringValue);
                }
                else if (value.Literal)
                {
                    WriteLiteralTo(writer, value.Value);
                }
                else if (stringValue != null)
                {
                    WriteTo(writer, stringValue);
                }
                else
                {
                    WriteTo(writer, value.Value);
                }
            }
            WriteLiteralTo(writer, trailer);
        }

        /// <summary>
        /// Convert to string and html encode
        /// </summary>
        /// <param name="value"></param>
        protected void Write(object value)
        {
            WriteTo(Output, value);
        }

        /// <summary>
        /// Html encode and write
        /// </summary>
        /// <param name="value"></param>
        protected void Write(string value)
        {
            WriteTo(Output, value);
        }

        /// <summary>
        /// <see cref="HelperResult.WriteTo(TextWriter)"/> is invoked
        /// </summary>
        /// <param name="result">The <see cref="HelperResult"/> to invoke</param>
        protected void Write(HelperResult result)
        {
            WriteTo(Output, result);
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="object"/> to write.</param>
        /// <remarks>
        /// <see cref="HelperResult.WriteTo(TextWriter)"/> is invoked for <see cref="HelperResult"/> types.
        /// For all other types, the encoded result of <see cref="object.ToString"/> is written to the
        /// <paramref name="writer"/>.
        /// </remarks>
        protected void WriteTo(TextWriter writer, object value)
        {
            if (value != null)
            {
                var helperResult = value as HelperResult;
                if (helperResult != null)
                {
                    helperResult.WriteTo(writer);
                }
                else
                {
                    WriteTo(writer, Convert.ToString(value, CultureInfo.InvariantCulture));
                }
            }
        }
        
        /// <summary>
        /// Writes the specified <paramref name="value"/> with HTML encoding to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="string"/> to write.</param>
        protected void WriteTo(TextWriter writer, string value)
        {
            WriteLiteralTo(writer, HtmlEscape(value));
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> without HTML encoding to the <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="object"/> to write.</param>
        protected void WriteLiteralTo(TextWriter writer, object value)
        {
            WriteLiteralTo(writer, Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes the specified <paramref name="value"/> without HTML encoding to <see cref="Output"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> instance to write to.</param>
        /// <param name="value">The <see cref="string"/> to write.</param>
        protected void WriteLiteralTo(TextWriter writer, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.Write(value);
            }
        }

        protected string HtmlEncodeAndReplaceLineBreaks(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Split on line breaks before passing it through the encoder.
            return string.Join("<br />" + Environment.NewLine,
                input.Split(new[] { "\r\n" }, StringSplitOptions.None)
                .SelectMany(s => s.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
                .Select(HtmlEscape));
}
        
        #endregion
    }
}