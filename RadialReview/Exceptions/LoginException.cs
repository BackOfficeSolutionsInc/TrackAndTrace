using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
    public class LoginException : RedirectException
    {
        public LoginException(String message)
            : base(message)
        {
        }
        public LoginException() : base(ExceptionStrings.DefaultLoginException)
        {
        }
    }
}