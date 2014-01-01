using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class AboutTypeExtensions
    {
        public static AboutType Invert(this AboutType self)
        {
            switch (self)
            {
                case AboutType.Manager: return AboutType.Subordinate;
                case AboutType.NoRelationship: return AboutType.NoRelationship;
                case AboutType.Peer: return AboutType.Peer;
                case AboutType.Self: return AboutType.Self;
                case AboutType.Subordinate: return AboutType.Manager;
                case AboutType.Teammate: return AboutType.Teammate;
                default:throw new ArgumentException("Unknown about type ("+self+")");
            }
        }
    }
}