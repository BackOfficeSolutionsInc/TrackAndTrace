using RadialReview.Exceptions;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public bool Error { get; set; }

        public static JsonObject Success= new JsonObject(false, "Success");

        public JsonObject(Boolean error,String message)
        {
            Error = error;
            Message = message;
        }

        public JsonObject(RedirectException e)
        {
            Error = true;
            Message=e.Message;
        }

        public JsonObject(Exception e)
        {
            Error = true;
            if(e is RedirectException)
                Message = e.Message;
            else
                Message = ExceptionStrings.AnErrorOccured;
        }
    }
}