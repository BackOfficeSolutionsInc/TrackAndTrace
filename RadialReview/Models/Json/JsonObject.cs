using RadialReview.Exceptions;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace RadialReview.Models.Json
{
    public class JsonObject<T>
    {
        public String ErrorMessage { get; set; }
        public bool Error { get; set; }
        public T Data { get;set;}
    }

    public class JsonObject
    {
        public String Message { get; set; }
        public String Trace { get; set; }
        public bool Error { get; set; }

        public static JsonObject Success= new JsonObject(false, "Success");

        public JsonObject(Boolean error,String message)
        {
            Error = error;
            Message = Capitalize(message);
        }

        public JsonObject(RedirectException e)
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

        public JsonObject(Exception e)
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