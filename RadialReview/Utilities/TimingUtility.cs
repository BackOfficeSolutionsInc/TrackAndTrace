using RadialReview.Exceptions;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;

namespace RadialReview.Utilities
{
    public class TimingUtility
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



		public static List<L10MeetingVM.WeekVM> GetWeeks(DayOfWeek weekStart, DateTime now, DateTime? meetingStart, List<ScoreModel> scores)
	    {
			var ordered = scores.Select(x => x.DateDue).OrderBy(x => x).ToList();
			var StartDate = ordered.FirstOrDefault().NotNull(x => now);
			var EndDate = ordered.LastOrDefault().NotNull(x => now).AddDays(7);

			//var s = StartDate.StartOfWeek(weekStart).AddDays(-7 * 4);
			//var e = EndDate.StartOfWeek(weekStart).AddDays(7 * 4);

			var s = (meetingStart ?? now).StartOfWeek(weekStart).AddDays(-7*13);
			var e = (meetingStart ?? now).StartOfWeek(weekStart);

			e = Math2.Max(now.StartOfWeek(weekStart), e);
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
					DisplayDate = s.StartOfWeek(weekStart),
					ForWeek = s.AddDays(7).StartOfWeek(DayOfWeek.Sunday),
					IsCurrentWeek = currWeek,
				});

				s = next;
				if (s > e)
					break;
			}
			return weeks;
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
    }
}