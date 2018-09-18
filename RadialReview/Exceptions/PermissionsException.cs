﻿using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
    public class PermissionsException : RedirectException
    {        
        public PermissionsException(String message,bool disableStacktrace=false): base(message)
        {
	        DisableStacktrace = disableStacktrace;
            StatusCodeOverride = System.Net.HttpStatusCode.Forbidden;
        }

        public PermissionsException() : base(ExceptionStrings.DefaultPermissionsException) {
            StatusCodeOverride = System.Net.HttpStatusCode.Forbidden;
        }
    }
}