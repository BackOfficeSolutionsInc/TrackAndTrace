using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
    public class OrganizationIdException : RedirectException
    {


        public OrganizationIdException(String message, String redirectUrl): base(message)
        {
            RedirectUrl = redirectUrl;
        }
        public OrganizationIdException(String redirectUrl = null) : this(ExceptionStrings.DefaultOrganizationIdException, redirectUrl ?? "/Account/Role")
        {
	        ForceReload = true;
        }

    }
}