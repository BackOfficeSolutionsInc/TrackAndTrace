using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class DateTimeExtensions
    {
        public static bool IsAfter(this DateTime self, DateTime other)
        {
            return self > other;
        }
        public static bool IsBefore(this DateTime self, DateTime other)
        {
            return self < other;
        }
    }
}