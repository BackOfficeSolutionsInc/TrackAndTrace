using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    public class TimingUtility
    {
        public static TimeSpan ReviewDuration(List<AnswerModel> answers, TimeSpan excludeLongerThan)
        {
            var ordered = answers.ToListAlive().OrderBy(x => x.CompleteTime);

            TimeSpan total = new TimeSpan(0);
            DateTime? last = null;

            foreach (var o in ordered)
            {
                if (last != null)
                {
                    TimeSpan duration = o.CompleteTime.Value - last.Value;
                    if (duration < excludeLongerThan)
                    {
                        total=total.Add(duration);
                    }
                }
                last = o.CompleteTime;
            }
            return total;
        }
    }
}