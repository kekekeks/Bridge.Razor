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
        object Model { get; set; }
        void Execute(IDomBuilder builder);
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
        

        public void Execute(IDomBuilder builder)
        {
            RenderRazor(builder);
        }
        
        protected virtual void RenderRazor(IDomBuilder builder)
        {
            throw new NotImplementedException();
        }
        
    }
}