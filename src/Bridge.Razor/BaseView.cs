using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Bridge.Html5;
using Bridge.Razor.RuntimeSupport;

namespace Bridge.Razor
{
    interface IBaseView
    {
        IDomBuilder Builder { get; set; }
        object Model { get; set; }
        void Execute();
    }
    
    public abstract class BaseView : BaseView<dynamic>
    {
        
    }

    public abstract class BaseView<T> : IBaseView
    {
        public T Model { get; set; }

        object IBaseView.Model
        {
            get { return Model; }
            set { Model = (T) value; }
        }
        
        public IDomBuilder Builder { get; set; }

        public void Execute()
        {
            RenderRazor();
        }
        
        protected virtual void RenderRazor()
        {
            throw new NotImplementedException();
        }
        
    }
}