﻿using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
    public class OrganizationIdException : RedirectException
    {


        public OrganizationIdException(String message): base(message)
        {
            RedirectUrl = "/Organization/ManageList";
        }
        public OrganizationIdException() : base(ExceptionStrings.DefaultOrganizationIdException)
        {
            RedirectUrl = "/Organization/ManageList";
        }
    }
}