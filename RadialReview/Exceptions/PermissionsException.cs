using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
    public class PermissionsException : RedirectException
    {        
        public PermissionsException(String message): base(message)
        {
        }

        public PermissionsException() : base(ExceptionStrings.DefaultPermissionsException)
        {
        }
    }
}