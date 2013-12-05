using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
    public class NoUserOrganizationException : RedirectException
    {        
        public NoUserOrganizationException(String message): base(message)
        {
            RedirectUrl = "/Home/Index";
        }
    public NoUserOrganizationException() : base("Not attached to any organizations")
        {
            RedirectUrl = "/Home/Index";
        }
    }
}