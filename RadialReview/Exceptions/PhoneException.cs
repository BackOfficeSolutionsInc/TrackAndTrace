using System.Web.Mvc;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
    public class PhoneException : Exception
    {        
        public PhoneException(String message): base(message)
        {
        }

		public PhoneException(): base("We're sorry, this service is unavailable at this time.")
        {
        }

	    public override String ToString()
	    {
		    return "<Response><Sms>"+Message+"</Sms></Response>";
	    }
    }
}