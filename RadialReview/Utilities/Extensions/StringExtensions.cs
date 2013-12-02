using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class StringExtensions
    {
        public static string Surround(this String self,string left,string right)
        {
            if (String.IsNullOrWhiteSpace(self))
                return self;
            else
                return left + self + right;
        }
    }
}