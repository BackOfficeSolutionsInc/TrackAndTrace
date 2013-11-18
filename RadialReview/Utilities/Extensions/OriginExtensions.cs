using RadialReview.Models;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class OriginExtensions
    {
        public static Origin GetOrigin(this IOrigin self)
        {
            return new Origin(self.GetOriginType(), self.Id);
        }
    }
}