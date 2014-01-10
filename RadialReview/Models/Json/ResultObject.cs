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
        public T Data { get; set; }
    }

    public enum StatusType
    {
        Success,
        Danger,
        Warning,
        Info,
        Primary,
        @Default,
        SilentSuccess
    }

    public class ResultObject
    {
        public StatusType Status { get; set; }

        public String MessageType { get { return Status.ToString(); } }
        public String Heading
        {
            get
            {
                switch (Status)
                {
                    case StatusType.Success: return "Success!";
                    case StatusType.Danger: return "Warning";
                    case StatusType.Warning: return "Warning";
                    case StatusType.Info: return "Info";
                    case StatusType.Primary: return "";
                    case StatusType.Default: return "";
                    case StatusType.SilentSuccess: return "Success!";
                    default: throw new ArgumentOutOfRangeException("Unknown message type");
                }
            }
        }

        public object Object { get; set; }
        public String Message { get; set; }
        public String Trace { get; set; }
        public bool Error { get; set; }

        public static ResultObject Success(String message)
        {
            return new ResultObject(false, message) { Status=StatusType.Success };
        }

        protected ResultObject()
        {
            Status = StatusType.SilentSuccess;
        }

        public static ResultObject SilentSuccess(object obj=null)
        {
            return new ResultObject()
            {
                Object = obj,
                Error = false,
                Message = "Success",
                Status = StatusType.SilentSuccess
            };
        }

        public static ResultObject Create(object obj, String message = "Success")
        {
            return new ResultObject()
            {
                Object = obj,
                Error = false,
                Message = message,
                Status = StatusType.Success
            };
        }
        
        public ResultObject(Boolean error, String message)
        {
            Error = error;
            Message = Capitalize(message);
            Status = StatusType.Danger;
        }

        public ResultObject(RedirectException e)
        {
            Error = true;
            Status = StatusType.Danger;
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
            Status = StatusType.Danger;
            if (e is RedirectException)
                Message = Capitalize(e.Message);
            else
                Message = Capitalize(ExceptionStrings.AnErrorOccuredContactUs);
#if(DEBUG)
            Trace = e.StackTrace;
#endif
        }

        public override string ToString()
        {
            return (Error ? "Error:" : "Success:") + Message ?? "";
        }

        public static ResultObject NoMessage()
        {
            return new ResultObject()
            {
                Error = false,
                Message = null,
                Object = null,
                Status = StatusType.SilentSuccess
            };
        }

        public static ResultObject CreateMessage(StatusType status, string message)
        {
            return new ResultObject()
            {
                Error = false,
                Message = message,
                Object = null,
                Status = status
            };
        }
    }
}