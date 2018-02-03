using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace RadialReview.Exceptions
{
    public class RedirectException : Exception
    {
        public String RedirectUrl { get; set; }

		public bool? Silent { get; set; }
		public bool ForceReload { get; set; }
		public bool DisableStacktrace { get; set; }

		public bool NoErrorReport { get; set; }

        public HttpStatusCode? StatusCodeOverride { get; set; }

        public RedirectException(String message) : base(message) {
        }
        
    }
}