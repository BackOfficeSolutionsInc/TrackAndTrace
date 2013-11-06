using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
    public class UnknownException: RedirectException
    {
        public UnknownException(String message): base(message)
        {
        }
        public UnknownException()
            : base(ExceptionStrings.DefaultUnknownException)
        {
        }
    }
}