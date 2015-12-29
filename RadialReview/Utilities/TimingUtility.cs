using Moq;
using RadialReview.Exceptions;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;

namespace RadialReview.Utilities
{
    public static class TimingUtility
    {
        public static TimeSpan ExcludeLongerThan = TimeSpan.FromMinutes(10);
        public static double? ReviewDurationMinutes(List<AnswerModel> answers, TimeSpan excludeLongerThan)
        {
            var ordered = answers.ToListAlive().OrderBy(x => x.CompleteTime).GroupBy(x=>x.CompleteTime).Select(x=>new {time=x.FirstOrDefault().CompleteTime, count = x.Count()});

            TimeSpan total = new TimeSpan(0);
            DateTime? last = null;
            double counted = 0;
            double skipped = 0;

            foreach (var o in ordered)
            {
                if (last != null)
                {
                    TimeSpan duration = o.time.Value - last.Value;
                    if (duration < excludeLongerThan){
                        total = total.Add(duration);
                        counted += o.count;
                    }else{
                        skipped += o.count;
                    }
                }
                last = o.time;
            }
            var minutes = total.TotalMinutes;

            if (counted == 0)
                return null;
            return minutes * (counted + skipped) / counted;
        }


	   /* public class WeekRange
		{
			public DateTime StartTime { get; set; }
			public DateTime EndTime { get; set; }

			public List<Week> Weeks { get; set; }

	    }*/

		public static TimeSpan ApproxDurationOfPeriod(ScorecardPeriod periods)
		{
			switch (periods)
			{
				case ScorecardPeriod.Weekly: return TimeSpan.FromDays(7);
				case ScorecardPeriod.Monthly: return TimeSpan.FromDays(30.436875);
				case ScorecardPeriod.Quarterly: return TimeSpan.FromDays(13*7);
				default: return TimeSpan.FromDays(7);
			}

		}

	    public static DateTime AddDateOffset(this DateTime time, DateOffset offset)
	    {
		    switch(offset){
			    case DateOffset.Invalid: return time;
			    case DateOffset.FirstOfMonth: return new DateTime(time.Year,time.Month,1);
				case DateOffset.FirstMondayOfTheMonth: return new DateTime(time.Year, time.Month, 1).AddDays(6).StartOfWeek(DayOfWeek.Monday);
				case DateOffset.FirstSundayOfTheMonth: return new DateTime(time.Year, time.Month, 1).AddDays(6).StartOfWeek(DayOfWeek.Sunday);
			    default: throw new ArgumentOutOfRangeException("offset");
		    }
	    }
	    public static DateTime ScorecardRangeEnd(ScorecardPeriod scorecardType, DateTime current)
	    {
		    switch (scorecardType)
		    {
			    case ScorecardPeriod.Weekly: return current.AddDays(6);
			    case ScorecardPeriod.Monthly: return new DateTime(current.Year, current.Month, 1).AddMonths(1).Subtract(TimeSpan.FromDays(1));
				case ScorecardPeriod.Quarterly: return new DateTime(current.Year, current.Month, 1).AddMonths(3).Subtract(TimeSpan.FromDays(1));
			    default: throw new ArgumentOutOfRangeException();
		    }
	    }

		public static string ScorecardFormat1(ScorecardPeriod scorecardType)
		{
			switch (scorecardType)
			{
				case ScorecardPeriod.Weekly: return "MMM d";
				case ScorecardPeriod.Monthly: return "MMM";
				case ScorecardPeriod.Quarterly: return "MMM";
				default: return "MMM";
			}
		}
		public static string ScorecardFormat2(ScorecardPeriod scorecardType)
		{
			switch (scorecardType)
			{
				case ScorecardPeriod.Weekly: return "MMM d";
				case ScorecardPeriod.Monthly: return "yyyy";
				case ScorecardPeriod.Quarterly: return "yyyy";
				default: return "yyyy";
			}
		}

	    private static List<L10MeetingVM.WeekVM> GetWeeks(DayOfWeek weekStart, int timezoneOffset, DateTime now, DateTime? meetingStart, List<ScoreModel> scores, bool includeNextWeek)
	    {

			var ordered = scores.Select(x => x.DateDue).OrderBy(x => x).ToList();
			var StartDate = ordered.FirstOrDefault().NotNull(x => now);
			var EndDate = ordered.LastOrDefault().NotNull(x => now).AddDays(7);

			//var s = StartDate.StartOfWeek(weekStart).AddDays(-7 * 4);
			//var e = EndDate.StartOfWeek(weekStart).AddDays(7 * 4);

			var s = (meetingStart ?? now).StartOfWeek(DayOfWeek.Sunday).AddDays(-7 * 13);
			var e = (meetingStart ?? now).StartOfWeek(DayOfWeek.Sunday);

			DateTime arg;

			arg = now.StartOfWeek(DayOfWeek.Sunday);
			if (includeNextWeek)
				arg = arg.AddDays(7);

			e = Math2.Max(arg, e);
			if (StartDate >= EndDate)
				throw new PermissionsException("Date ordering incorrect");
			var weeks = new List<L10MeetingVM.WeekVM>();
			while (true)
			{
				var currWeek = false;
				var next = s.AddDays(7);
				var s1 = s;
				if (meetingStart.NotNull(x => s1 <= x.Value && x.Value < next))
					currWeek = true;
				//var j = s.AddDays(-7);

				weeks.Add(new L10MeetingVM.WeekVM()
				{
					DisplayDate = s.AddDays(-7).AddDays(6).StartOfWeek(weekStart).AddMinutes(-timezoneOffset),
					ForWeek = s.StartOfWeek(DayOfWeek.Sunday),
					IsCurrentWeek = currWeek,
				});

				s = next;
				if (s > e)
					break;
			}
		    return weeks;
	    }

		private static List<L10MeetingVM.WeekVM> GetMonths(DayOfWeek weekStart, int timezoneOffset, DateTime now, DateTime? meetingStart, List<ScoreModel> scores, bool includeNextWeek)
		{

			var ordered = scores.Select(x => x.DateDue).OrderBy(x => x).ToList();
			var StartDate = ordered.FirstOrDefault().NotNull(x => now);
			var EndDate = ordered.LastOrDefault().NotNull(x => now).AddDays(7);

			//var s = StartDate.StartOfWeek(weekStart).AddDays(-7 * 4);
			//var e = EndDate.StartOfWeek(weekStart).AddDays(7 * 4);

			//var s = (meetingStart ?? now).StartOfWeek(DayOfWeek.Sunday).AddDays(-7 * 13);
			//var e = (meetingStart ?? now).StartOfWeek(DayOfWeek.Sunday);

			var ed = (meetingStart ?? now);
			var sd = ed.AddMonths(-13);

			var e = new DateTime(ed.Year, ed.Month, 1).AddDays(6).StartOfWeek(DayOfWeek.Sunday);
			var s = new DateTime(sd.Year, sd.Month, 1).AddDays(6).StartOfWeek(DayOfWeek.Sunday);

			DateTime arg;

			arg = now.StartOfWeek(DayOfWeek.Sunday);
			if (includeNextWeek)
				arg = arg.AddDays(7);

			e = Math2.Max(arg, e);
			if (StartDate >= EndDate)
				throw new PermissionsException("Date ordering incorrect");
			var weeks = new List<L10MeetingVM.WeekVM>();
			while (true)
			{
				var currWeek = false;
				var n = s.AddMonths(1);
				var next = new DateTime(n.Year, n.Month, 1).AddDays(6).StartOfWeek(DayOfWeek.Sunday);
				var s1 = s;
				if (meetingStart.NotNull(x => s1 <= x.Value && x.Value < next))
					currWeek = true;
				//var j = s.AddDays(-7);

				weeks.Add(new L10MeetingVM.WeekVM()
				{
					DisplayDate = s.AddDays(6).StartOfWeek(weekStart).AddMinutes(-timezoneOffset),
					ForWeek = s.StartOfWeek(DayOfWeek.Sunday),
					IsCurrentWeek = currWeek,
				});

				s = next;
				if (s > e)
					break;
			}
			return weeks;
		}


		private static DateTime GetQuarterStart(DateTime time, YearStart yearStart)
	    {
		    var curYear = new DateTime(time.Year, 1, 1);
		    var yearStart_temp = curYear.AddMonths(Math.Max(0, (int) yearStart.Month - 1));
			var yearStartDate = yearStart_temp.AddDateOffset(yearStart.Offset);
			var nextYearStart = yearStartDate.AddDays(52*7*2);

			var i = nextYearStart;
			while (true){
				i = i.AddDays(-7*13);
				if (time >= i)
					return i;
			}
	    }

		private static List<L10MeetingVM.WeekVM> GetQuarters(DayOfWeek weekStart, int timezoneOffset, DateTime now, DateTime? meetingStart, List<ScoreModel> scores, bool includeNextWeek, YearStart yearStart)
		{

			var ordered = scores.Select(x => x.DateDue).OrderBy(x => x).ToList();
			var StartDate = ordered.FirstOrDefault().NotNull(x => now);
			var EndDate = ordered.LastOrDefault().NotNull(x => now).AddDays(7);

			//var s = StartDate.StartOfWeek(weekStart).AddDays(-7 * 4);
			//var e = EndDate.StartOfWeek(weekStart).AddDays(7 * 4);

			//var s = (meetingStart ?? now).StartOfWeek(DayOfWeek.Sunday).AddDays(-7 * 13);
			//var e = (meetingStart ?? now).StartOfWeek(DayOfWeek.Sunday);




			var ed = GetQuarterStart(meetingStart ?? now, yearStart);
			var sd = ed.AddDays(-13*7*4*3);

			var e = ed.AddDays(6).StartOfWeek(DayOfWeek.Sunday);
			var s = sd.AddDays(6).StartOfWeek(DayOfWeek.Sunday);

			DateTime arg;

			arg = now.StartOfWeek(DayOfWeek.Sunday);
			if (includeNextWeek)
				arg = arg.AddDays(7);

			e = Math2.Max(arg, e);
			if (StartDate >= EndDate)
				throw new PermissionsException("Date ordering incorrect");
			var weeks = new List<L10MeetingVM.WeekVM>();
			while (true)
			{
				var currWeek = false;
				var n = s.AddDays(13*7);
				var next = n.AddDays(6).StartOfWeek(DayOfWeek.Sunday);
				var s1 = s;
				if (meetingStart.NotNull(x => s1 <= x.Value && x.Value < next))
					currWeek = true;
				//var j = s.AddDays(-7);

				weeks.Add(new L10MeetingVM.WeekVM()
				{
					DisplayDate = s.AddDays(6).StartOfWeek(weekStart).AddMinutes(-timezoneOffset),
					ForWeek = s.StartOfWeek(DayOfWeek.Sunday),
					IsCurrentWeek = currWeek,
				});

				s = next;
				if (s > e)
					break;
			}
			return weeks;
		}

		public static List<L10MeetingVM.WeekVM> GetPeriods(DayOfWeek weekStart,int timezoneOffset, DateTime now, DateTime? meetingStart, List<ScoreModel> scores,
			bool includeNextWeek,ScorecardPeriod scorecardPeriod,YearStart yearStart )
		{

			scores = scores ?? new List<ScoreModel>();

			switch(scorecardPeriod){
				case ScorecardPeriod.Weekly:
					return GetWeeks(weekStart, timezoneOffset, now, meetingStart, scores, includeNextWeek);
				case ScorecardPeriod.Monthly:
					return GetMonths(weekStart, timezoneOffset, now, meetingStart, scores, includeNextWeek);
				case ScorecardPeriod.Quarterly:
					return GetQuarters(weekStart, timezoneOffset, now, meetingStart, scores, includeNextWeek, yearStart);
				default:
					throw new ArgumentOutOfRangeException("scorecardPeriod");
			}
	    }

	    public static long GetWeekSinceEpoch(DateTime day)
	    {
			var span = day.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).StartOfWeek(DayOfWeek.Sunday));
		    return (long)Math.Floor(span.TotalDays/7);
	    }

	    public static DateTime GetDateSinceEpoch(long week)
	    {
			return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).StartOfWeek(DayOfWeek.Sunday).AddDays(week * 7);
	    }

        /*
        public static double? ReviewDuration(List<AnswerModel> answers)
        {
            return answers.Where(x => x.DurationMinutes != null && x.CompleteTime != null).Sum(x => x.DurationMinutes);
            /
            TimeSpan sum=TimeSpan.Zero;
            foreach(var t in times){
                sum+=t;
            }
            return sum.TotalMinutes;*
        }*/

		public static DateTime PeriodsAgo(DateTime startTime, int periods, ScorecardPeriod scorecardPeriod)
		{
			switch (scorecardPeriod)
			{
				case ScorecardPeriod.Weekly: return startTime.AddDays(-7 * periods);
				case ScorecardPeriod.Monthly:return new DateTime(startTime.Year, startTime.Month, 1).AddMonths(-periods);
				case ScorecardPeriod.Quarterly: return new DateTime(startTime.Year, startTime.Month, 1).AddMonths(-periods*3);
				default: throw new ArgumentOutOfRangeException("scorecardPeriod");
			}
		}
	}
}