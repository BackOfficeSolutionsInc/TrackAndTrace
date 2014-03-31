using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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