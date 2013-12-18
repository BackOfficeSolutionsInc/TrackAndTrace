using RadialReview.Exceptions;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.Json
{
    public class ResultObject<T>
    {
        public String ErrorMessage { get; set; }
        public bool Error { get; set; }
        public T Data { get;set;}
    }

    public class ResultObject
    {
        public object Object { get; set; }
        public String Message { get; set; }
        public String Trace { get; set; }
        public bool Error { get; set; }

        public readonly static ResultObject Success= new ResultObject(false, "Success");
        
        protected ResultObject()
        {

        }

        public static ResultObject Create(object obj,String message="Success")
        {
            return new ResultObject() { Object = obj, Error = false, Message = message };
        }

        public ResultObject(Boolean error,String message)
        {
            Error = error;
            Message = Capitalize(message);
        }

        public ResultObject(RedirectException e)
        {
            Error = true;
            Message = Capitalize(e.Message);
            #if(DEBUG)
            Trace = e.StackTrace;
            #endif
        }

        private String Capitalize(String message)
        {
            StringBuilder builder = new StringBuilder(message);
            if (builder.Length > 0)
                builder[0] = char.ToUpper(message[0]);
            return builder.ToString();
        }

        public ResultObject(Exception e)
        {
            Error = true;
            if(e is RedirectException)
                Message = Capitalize(e.Message);
            else
                Message = Capitalize(ExceptionStrings.AnErrorOccuredContactUs);
            #if(DEBUG)
            Trace = e.StackTrace;
            #endif
        }
    }
}