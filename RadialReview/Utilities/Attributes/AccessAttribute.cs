using RadialReview.Utilities.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public class AccessAttribute : Attribute
    {
        public AccessLevel AccessLevel { get; set; }

        public AccessAttribute(AccessLevel level)
        {
            AccessLevel = level;
        }
    }
}