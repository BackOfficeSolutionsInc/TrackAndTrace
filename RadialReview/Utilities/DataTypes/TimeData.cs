using RadialReview.Models.Application;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
    public class TimeData : TimeSettings {
        public DayOfWeek WeekStart { get; set; }
        public ScorecardPeriod Period { get; set; }
        public int TimezoneOffset { get; set; }
        public YearStart YearStart { get; set; }
        public DateTime Now { get; set; }
		public bool Descending { get; set; }

        public DateTime ConvertFromServerTime(DateTime serverTime) {
            return serverTime.AddMinutes(TimezoneOffset);
        }

        public DateTime ConvertToServerTime(DateTime localTime) {
            return localTime.AddMinutes(-TimezoneOffset);
        }

        public TimeData GetTimeSettings()
        {
            return this;
        }
    }

    public interface TimeSettings {
        TimeData GetTimeSettings();
    }
}