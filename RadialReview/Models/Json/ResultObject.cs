using System.Net;
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
        @Default
    }

	public class ResultObject
    {
	    public ResultObject NoRefresh(){
		    Refresh = false;
		    return this;
	    }
		public ResultObject ForceRefresh()
		{
			Refresh = true;
			return this;
		}
		public StatusType Status { get; set; }
		private bool _Error { get; set; }
        public bool Error {
	        get { return _Error; }
	        set{
		        _Error = value;
		        try{
			        if (Error)
				        System.Web.HttpContext.Current.Response.StatusCode = (int) HttpStatusCode.BadRequest;
			        else
				        System.Web.HttpContext.Current.Response.StatusCode = (int) HttpStatusCode.OK;
		        }
		        catch (Exception e){
			        
		        }
	        }
        }

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
                   // case StatusType.SilentSuccess: return "Success!";
                    default: throw new ArgumentOutOfRangeException("Unknown message type");
                }
            }
        }

        public Dictionary<string, string> Data { get; set; }
        public String Html { get;set;}

        public object Object { get; set; }
		public String Message { get; set; }
		public String Trace { get; set; }
		public String TraceMessage { get; set; }
		private bool? _Refresh { get; set; }

		public bool Refresh {
			get
			{
                try{
                    if (System.Web.HttpContext.Current != null)
                    {
                        var requestRefresh = System.Web.HttpContext.Current.Request.Params["refresh"];
                        if (requestRefresh != null && requestRefresh.ToLower() == "true")
                            return true;
                        if (requestRefresh != null && requestRefresh.ToLower() == "false")
                            return false;
                    }
				}
				catch (Exception e){
					var ops = true;
				}
				if (_Refresh != null)
					return _Refresh.Value;
				
				return false;
			}
			set{_Refresh = value;}
		}

        public string Redirect { get; set; }

		private bool? _Silent { get; set; }

		public bool Silent{
			get{
				//Show By Default
				if (_Silent != null)
					return _Silent.Value;
				try
				{
					var requestSilent = System.Web.HttpContext.Current.Request.Params["silent"];

					//If Url says not to silence, then show..
					if (requestSilent != null){
						if (requestSilent.ToLower() == "false")
							return false;
						if (requestSilent.ToLower() == "true")
							return true;
					}
				}catch (Exception e){
					var ops = true;
				}
				//Assume Noisy
				return false;
			}
			set { _Silent = value; }
		}
        public static ResultObject Success(String message)
        {
            return new ResultObject(false, message) { Status=StatusType.Success };
        }

        protected ResultObject(){
            Status = StatusType.Success;
        }

        public static ResultObject SilentSuccess(object obj=null)
        {
            return new ResultObject()
            {
                Object = obj,
                Error = false,
                Message = "Success",
                Status = StatusType.Success,
				Silent=true,
            };
        }

		public static ResultObject CreateError(String message,object obj = null)
		{
			return new ResultObject()
			{
				Object = obj,
				Error = true,
				Message = Capitalize(message),
				Status = StatusType.Danger
			};
		}
        public static ResultObject Create(object obj, String message = "Success",StatusType status = StatusType.Success,bool error=false)
        {
            return new ResultObject()
            {
                Object = obj,
				Error = error,
                Message = message,
                Status = status
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

        private static String Capitalize(String message)
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
                Message = Capitalize(ExceptionStrings.AnErrorOccured);
#if(DEBUG)
			TraceMessage = Capitalize(e.Message);
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
                Status = StatusType.Success
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

        public static ResultObject CreateHtml(string html, Dictionary<string, string> data=null) {
            return new ResultObject() {
                Error = false,
                Message = "Success",
                Object = null,
                Status = StatusType.Success,
                Html = html,
                Data=data,
                Silent=true
            };
        }

		public ResultObject ForceSilent(){
			Silent = true;
			return this;
		}

        public static ResultObject CreateRedirect(string url,String message=null)
        {
            return new ResultObject() {
                Error = false,
                Message = message,
                Object = null,
                Status = StatusType.Success,
                Silent = (message == null),
                Redirect = url,
            };
        }

    }
}