using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class StringExtensions
    {
        public static bool EqualsInvariant(this String self, string other)
        {
            if (self == null && other == null)
                return true;
            if (self == null || other == null)
                return false;
            return self.ToLower().Equals(other.ToLower());
        }
		
        public static string Surround(this String self, string left, string right)
        {
            if (String.IsNullOrWhiteSpace(self))
                return self;
            else
                return left + self + right;
        }

        public static String Pluralize(this String self, double count, String plural = null)
        {
            if (count == 1) return self;
            else return plural ?? (self + "s");
        }
        public static String Possessive(this String self)
        {
            return self + "'s";
        }
    }
}