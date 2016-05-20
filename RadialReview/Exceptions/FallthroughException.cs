using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
    public class FallthroughException : PermissionsException
    {
        public FallthroughException(String message, bool disableStacktrace = false): base(message, disableStacktrace)
        {
        }

        public FallthroughException(): base(ExceptionStrings.DefaultPermissionsException)
        {
        }
    }
}