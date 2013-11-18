using RadialReview.Models;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class OrganizationExtensions
    {
        public static string GetImage(this OrganizationModel organization)
        {
            if (organization.Image == null)
                return "/i/placeholder";
            return "/i/" + organization.Image.Id.ToString();
        }
    }
}