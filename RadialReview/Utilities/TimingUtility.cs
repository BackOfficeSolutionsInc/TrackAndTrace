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
using RadialReview.Utilities.DataTypes;
using System.Globalization;
using System.Threading;
using log4net;
using System.Web.Mvc;

namespace RadialReview.Utilities {
	public static class TimingUtility {
		static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static TimeSpan ExcludeLongerThan = TimeSpan.FromMinutes(10);
		public static double? ReviewDurationMinutes(List<AnswerModel> answers, TimeSpan excludeLongerThan) {
			var ordered = answers.ToListAlive().OrderBy(x => x.CompleteTime).GroupBy(x => x.CompleteTime).Select(x => new { time = x.FirstOrDefault().CompleteTime, count = x.Count() });

			TimeSpan total = new TimeSpan(0);
			DateTime? last = null;
			double counted = 0;
			double skipped = 0;

			foreach (var o in ordered) {
				if (last != null) {
					TimeSpan duration = o.time.Value - last.Value;
					if (duration < excludeLongerThan) {
						total = total.Add(duration);
						counted += o.count;
					} else {
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

		public static TimeSpan ApproxDurationOfPeriod(ScorecardPeriod periods) {
			switch (periods) {
				case ScorecardPeriod.Weekly:
					return TimeSpan.FromDays(7);
				case ScorecardPeriod.Monthly:
					return TimeSpan.FromDays(30.436875);
				case ScorecardPeriod.Quarterly:
					return TimeSpan.FromDays(13 * 7);
				default:
					return TimeSpan.FromDays(7);
			}

		}

		public static DateTime AddDateOffset(this DateTime time, DateOffset offset) {
			switch (offset) {
				case DateOffset.Invalid:
					return time;
				case DateOffset.FirstOfMonth:
					return new DateTime(time.Year, time.Month, 1);
				case DateOffset.FirstMondayOfTheMonth:
					return new DateTime(time.Year, time.Month, 1).AddDays(6.9999).StartOfWeek(DayOfWeek.Monday);
				case DateOffset.FirstSundayOfTheMonth:
					return new DateTime(time.Year, time.Month, 1).AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday);
				case DateOffset.MondayOfFourthWeek: {
						var s = new DateTime(time.Year, time.Month, 1);
						if (s.DayOfWeek <= DayOfWeek.Monday)
							return s.AddDays(6.9999 * 4).StartOfWeek(DayOfWeek.Monday);
						return s.AddDays(6.9999 * 3).StartOfWeek(DayOfWeek.Monday);
					}
				default:
					throw new ArgumentOutOfRangeException("offset");
			}
		}

		public static DateTime ScorecardRangeStart(int clientOffset, ScorecardPeriod scorecardType, DateTime current) {
			return current.AddMinutes(clientOffset);
		}


		public static DateTime ScorecardRangeEnd(int clientOffset, ScorecardPeriod scorecardType, DateTime current, bool inclusive = false) {
			var extraDay = -1;
			if (inclusive)
				extraDay = 0;
			switch (scorecardType) {
				case ScorecardPeriod.Weekly:
					return current.AddDays(7 + extraDay).AddMinutes(clientOffset);
				case ScorecardPeriod.Monthly:
					return new DateTime(current.Year, current.Month, 1).AddMonths(1).Add(TimeSpan.FromDays(extraDay)).AddMinutes(clientOffset);
				case ScorecardPeriod.Quarterly:
					return new DateTime(current.Year, current.Month, 1).AddMonths(3).Add(TimeSpan.FromDays(extraDay)).AddMinutes(clientOffset);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static string ScorecardFormat1(ScorecardPeriod scorecardType) {
			switch (scorecardType) {
				case ScorecardPeriod.Weekly:
					return "MMM d";
				case ScorecardPeriod.Monthly:
					return "MMM";
				case ScorecardPeriod.Quarterly:
					return "MMM";
				default:
					return "MMM";
			}
		}
		public static string ScorecardFormat2(ScorecardPeriod scorecardType) {
			switch (scorecardType) {
				case ScorecardPeriod.Weekly:
					return "MMM d";
				case ScorecardPeriod.Monthly:
					return "yyyy";
				case ScorecardPeriod.Quarterly:
					return "yyyy";
				default:
					return "yyyy";
			}
		}

		private static List<L10MeetingVM.WeekVM> GetWeeks(TimeData settings, DateTime endDate, DateTime? highlightDate, bool includeNextWeek, bool useWeekstartForWeekNumber = false, DateRange range = null) {

            //var ordered = scores.Select(x => x.DateDue).OrderBy(x => x).ToList();
            //var StartDate = ordered.FirstOrDefault().NotNull(x => now);
            //var EndDate = ordered.LastOrDefault().NotNull(x => now).AddDays(7);

            //var s = StartDate.StartOfWeek(weekStart).AddDays(-7 * 4);
            //var e = EndDate.StartOfWeek(weekStart).AddDays(7 * 4);

            DateTime? localTimeMeetingStart = null;
            if (highlightDate != null) {
                localTimeMeetingStart = settings.ConvertFromServerTime(highlightDate.Value);
            }
                
            var weekStart = settings.WeekStart;
			var timezoneOffset = settings.TimezoneOffset;


			var weekNumber_StartOfWeek = DayOfWeek.Sunday;
			if (useWeekstartForWeekNumber)
				weekNumber_StartOfWeek = weekStart;


			var s = (localTimeMeetingStart ?? endDate).StartOfWeek(DayOfWeek.Sunday).AddDays(-7 * 13);
			var e = (localTimeMeetingStart ?? endDate).StartOfWeek(DayOfWeek.Sunday);

			if (range != null) {
				s = range.StartTime.StartOfWeek(DayOfWeek.Sunday);
				if (range.EndTime.StartOfWeek(DayOfWeek.Sunday) == range.EndTime) {
					e = range.EndTime.StartOfWeek(DayOfWeek.Sunday);
				} else {
					e = range.EndTime.AddDays(7).StartOfWeek(DayOfWeek.Sunday);

				}
			}


			DateTime arg;

			arg = endDate.StartOfWeek(DayOfWeek.Sunday);
			if (includeNextWeek)
				arg = arg.AddDays(7);

			//var offsetObj = Thread.GetData(Thread.GetNamedDataSlot("timeOffset"));
			var diff = timezoneOffset;
			//if (offsetObj != null) {
			//    diff = (int)Math.Round((double)offsetObj);
			//}


			e = Math2.Max(arg, e);
			//if (StartDate >= EndDate)
			//	throw new PermissionsException("Date ordering incorrect");
			var weeks = new List<L10MeetingVM.WeekVM>();
			while (true) {
				var currWeek = false;
				var next = s.AddDays(7);
				var s1 = s;
                var displayDate = s.AddDays(-7).AddDays(6.9999).StartOfWeek(weekStart).AddMinutes(-(diff - 60));
                var displayDateNext = displayDate.AddDays(7);
                if (localTimeMeetingStart.NotNull(x => displayDate <= x.Value && x.Value < displayDateNext))
					currWeek = true;
				//var j = s.AddDays(-7);
				weeks.Add(new L10MeetingVM.WeekVM() {
					DisplayDate = displayDate,
					StartDate = displayDate.AddMinutes(diff),
					ForWeek = s.StartOfWeek(DayOfWeek.Sunday),
					IsCurrentWeek = currWeek,
				});

				s = next;
				if (s > e)
					break;
			}
			return weeks;
		}

		private static List<L10MeetingVM.WeekVM> GetMonths(TimeData settings, DateTime endDate, DateTime? highlightDate,bool includeNextWeek, bool useWeekstartForWeekNumber = false, DateRange range = null) {

			log.Info("Called GetMonths with " + settings.WeekStart + "," + settings.TimezoneOffset);
			
			var weekNumber_StartOfWeek = DayOfWeek.Sunday;
			if (useWeekstartForWeekNumber)
				weekNumber_StartOfWeek = settings.WeekStart;


			var ed = (highlightDate ?? endDate);
			var sd = ed.AddMonths(-13);

			var e = new DateTime(ed.Year, ed.Month, 1).AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday);
			var s = new DateTime(sd.Year, sd.Month, 1).AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday);

			if (range != null) {
				s = new DateTime(range.StartTime.Year, range.StartTime.Month, 1).AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday);
				e = new DateTime(range.EndTime.Year, range.EndTime.Month, 1).AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday);
				if (e != range.EndTime) {
					e = e.AddMonths(1);
				}
			}

			DateTime arg;

			arg = endDate.StartOfWeek(DayOfWeek.Sunday);
			if (includeNextWeek)
				arg = arg.AddDays(7);

			e = Math2.Max(arg, e);
			//if (StartDate >= EndDate)
			//    throw new PermissionsException("Date ordering incorrect");
			var weeks = new List<L10MeetingVM.WeekVM>();
			while (true) {
				var currWeek = false;
				var n = s.AddMonths(1);
				var next = new DateTime(n.Year, n.Month, 1).AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday);
				var s1 = s;
				if (highlightDate.NotNull(x => s1.AddDays(7.0).StartOfWeek(weekNumber_StartOfWeek) <= x.Value && x.Value < next.AddDays(7.0).StartOfWeek(weekNumber_StartOfWeek)))
					currWeek = true;
				//var j = s.AddDays(-7);
				var display = s.AddDays(6.9999).StartOfWeek(settings.WeekStart).AddMinutes(-settings.TimezoneOffset);
				weeks.Add(new L10MeetingVM.WeekVM() {
					DisplayDate = display,
					StartDate = display.AddMinutes(-settings.TimezoneOffset),
					ForWeek = s.StartOfWeek(DayOfWeek.Sunday),
					IsCurrentWeek = currWeek,
				});

				s = next;
				if (s > e)
					break;
			}
			return weeks;
		}


		private static DateTime GetQuarterStart(DateTime time, YearStart yearStart) {
			var curYear = new DateTime(time.Year, 1, 1);
			var yearStart_temp = curYear.AddMonths(Math.Max(0, (int)yearStart.Month - 1));
			var yearStartDate = yearStart_temp.AddDateOffset(yearStart.Offset);
			var nextYearStart = yearStartDate.AddDays(52 * 7 * 2);

			var i = nextYearStart;
			while (true) {
				i = i.AddDays(-7 * 13);
				if (time >= i)
					return i;
			}
		}

		private static List<L10MeetingVM.WeekVM> GetQuarters(TimeData settings, DateTime endDate, DateTime? hightlightDate, /*List<ScoreModel> scores,*/ bool includeNextWeek, bool useWeekstartForWeekNumber = false, DateRange range = null) {
			
			var weekNumber_StartOfWeek = DayOfWeek.Sunday;
			if (useWeekstartForWeekNumber)
				weekNumber_StartOfWeek = settings.WeekStart;



			var ed = GetQuarterStart(hightlightDate ?? endDate, settings.YearStart);
			var sd = ed.AddDays(-13 * 7 * 4 * 3);


			if (range != null) {
				sd = GetQuarterStart(range.StartTime, settings.YearStart);
				ed = GetQuarterStart(range.EndTime, settings.YearStart);
				if (ed != range.EndTime) {
					ed = ed.AddDays(7 * 13);
				}
			}
			var e = ed.AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday);
			var s = sd.AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday);

			DateTime arg;

			arg = endDate.StartOfWeek(DayOfWeek.Sunday);
			if (includeNextWeek)
				arg = arg.AddDays(7);

			e = Math2.Max(arg, e);
			//if (StartDate >= EndDate)
			//	throw new PermissionsException("Date ordering incorrect");
			var weeks = new List<L10MeetingVM.WeekVM>();
			while (true) {
				var currWeek = false;
				var n = s.AddDays(13 * 7);
				var next = n.AddDays(6.9999).StartOfWeek(DayOfWeek.Sunday);
				var s1 = s;
				if (hightlightDate.NotNull(x => s1.AddDays(7.0).StartOfWeek(weekNumber_StartOfWeek) <= x.Value && x.Value < next.AddDays(7.0).StartOfWeek(weekNumber_StartOfWeek)))
					currWeek = true;
				//var j = s.AddDays(-7);
				var display = s.AddDays(6.9999).StartOfWeek(settings.WeekStart).AddMinutes(-(settings.TimezoneOffset-60));
				weeks.Add(new L10MeetingVM.WeekVM() {
					DisplayDate = display,
					StartDate = display.AddMinutes(-settings.TimezoneOffset),
					ForWeek = s.StartOfWeek(DayOfWeek.Sunday),
					IsCurrentWeek = currWeek,
				});

				s = next;
				if (s > e)
					break;
			}
			return weeks;
		}
		public static DateRange GetRange(OrganizationModel org, DateTime forWeek) {
			var settings = org.NotNull(x => x.Settings) ?? new OrganizationModel.OrganizationSettings();

			return GetLocalRange(settings.WeekStart, settings.GetTimezoneOffset(),
				forWeek, settings.ScorecardPeriod, settings.YearStart);
		}

		public static DateRange GetLocalRange(DayOfWeek weekStart, int timezoneOffset, DateTime date, ScorecardPeriod scorecardPeriod, YearStart yearStart) {
			var w = date;
			// L10MeetingVM.WeekVM vm = null;

			//var periods = GetPeriods(weekStart, timezoneOffset, w, null, true, scorecardPeriod, yearStart);
			//DateRange o;
			switch (scorecardPeriod) {
				case ScorecardPeriod.Weekly:
					var sw = w.StartOfWeek(DayOfWeek.Sunday).AddDays(6.9999).StartOfWeek(weekStart).AddMinutes(-timezoneOffset);
					return new DateRange(sw, sw.AddDays(7));
				case ScorecardPeriod.Monthly:
					var s = new DateTime(w.Year, w.Month, 1).AddMinutes(-timezoneOffset);
					var e = s.AddMonths(1);
					return new DateRange(s, e);
				case ScorecardPeriod.Quarterly:
					var soy = yearStart.GetDate(w.Year);
					var year = w.Year;
					if (w < soy.AddMinutes(-(timezoneOffset))) {
						soy = yearStart.GetDate(w.Year - 1);
						year = year - 1;
					}
					for (var i = 1; i <= 4; i++) {
						var end = yearStart.GetDate(year, i).AddMinutes(-(timezoneOffset));
						//if (i==4)
						//    end = yearStart.GetDate(soy.Year + 1).AddMinutes(-timezoneOffset);
						if (w < end) {
							return new DateRange(
									yearStart.GetDate(year, i - 1).AddMinutes(-(timezoneOffset)),
									end
								);
						}
					}
					throw new ArgumentOutOfRangeException("Out of range");

				default:
					throw new ArgumentOutOfRangeException("scorecardPeriod");
			}

			//foreach (var p in periods.OrderBy(x => x.ForWeek))
			//{
			//    if (w <= p.ForWeek){
			//        vm = p;
			//        break;
			//    }
			//}
			//var start = vm.DisplayDate;            
			//var end   = ScorecardRangeEnd(scorecardPeriod, start, true);

			//return new DateRange(start, end);

		}
		
		public static List<L10MeetingVM.WeekVM> GetPeriods(TimeSettings ts, DateTime endDate, DateTime? highlightDate,bool includeNextWeek, bool useWeekstartForWeekNumber = false, DateRange range = null) {
			
			var settings = ts.GetTimeSettings();
			List<L10MeetingVM.WeekVM> output;
			switch (settings.Period) {
				case ScorecardPeriod.Weekly:
					output= GetWeeks(settings, endDate, highlightDate, includeNextWeek, useWeekstartForWeekNumber, range);
					break;
				case ScorecardPeriod.Monthly:
					output= GetMonths(settings, endDate, highlightDate, /*scores,*/ includeNextWeek, useWeekstartForWeekNumber, range);
					break;
				case ScorecardPeriod.Quarterly:
					output= GetQuarters(settings, endDate, highlightDate, /*scores,*/ includeNextWeek, useWeekstartForWeekNumber, range);
					break;
				default:
					throw new ArgumentOutOfRangeException("scorecardPeriod");
			}

			if (settings.Descending)
				output = output.OrderByDescending(x => x.ForWeek).ToList();
			
			return output;
		}

		public static long GetWeekSinceEpoch(DateTime day) {
			var span = day.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).StartOfWeek(DayOfWeek.Sunday));
			return (long)Math.Floor(span.TotalDays / 7);
		}

		public static DateTime GetDateSinceEpoch(long week) {
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

		public static DateTime PeriodsAgo(DateTime startTime, int periods, ScorecardPeriod scorecardPeriod) {
			switch (scorecardPeriod) {
				case ScorecardPeriod.Weekly:
					return startTime.AddDays(-7 * periods);
				case ScorecardPeriod.Monthly:
					return new DateTime(startTime.Year, startTime.Month, 1).AddMonths(-periods);
				case ScorecardPeriod.Quarterly:
					return new DateTime(startTime.Year, startTime.Month, 1).AddMonths(-periods * 3);
				default:
					throw new ArgumentOutOfRangeException("scorecardPeriod");
			}
		}
		private static IList<string> GetDateTimePatterns(CultureInfo culture) {
			var info = culture.DateTimeFormat;
			return new string[]
			{
				info.FullDateTimePattern,
				info.LongDatePattern,
				info.LongTimePattern,
				info.ShortDatePattern,
				info.ShortTimePattern,
				info.MonthDayPattern,
				info.ShortDatePattern + " " + info.LongTimePattern,
				info.ShortDatePattern + " " + info.ShortTimePattern,
				info.YearMonthPattern
                // Consider the sortable pattern, ISO-8601 etc
            };
		}
		public static IEnumerable<string> GuessPatterns(string text, CultureInfo culture = null) {
			DateTime ignored;
			culture = culture ?? new CultureInfo("en-US");
			return GetDateTimePatterns(culture).Where(pattern => DateTime.TryParseExact(text, pattern, culture, DateTimeStyles.None, out ignored));
		}

		private static List<DateTime> _FixOrderedDates_Year(List<DateTime> dates) {
			if (dates.Count == 0)
				return dates;
			var upcount = 0;
			var downcount = 0;
			if (dates.Count > 3) {
				for (var i = 1; i < dates.Count; i++) {
					if ((dates[i] - dates[i - 1]).Ticks > 0)
						upcount += 1;
					else {
						downcount += 1;
					}
				}
			}
			var reverse = false;
			if (upcount != 0 && downcount != 0 && downcount > upcount) {
				dates.Reverse();
				reverse = true;
			}


			//var curYear = dates.Last().Year;
			var newOut = new List<DateTime>();
			var last = DateTime.MaxValue;
			var subYears = 0;
			for (var i = dates.Count - 1; i >= 0; i--) {
				DateTime cur;
				while (true) {
					cur = new DateTime(dates[i].Year - subYears, dates[i].Month, dates[i].Day, dates[i].Hour, dates[i].Minute, dates[i].Second, dates[i].Millisecond);//.Subtract(TimeSpan.FromDays(52 * 7 * subYears));
					if (cur < last)
						break;
					subYears += 1;
				}
				last = cur;
				newOut.Add(cur);
			}
			if (!reverse)
				newOut.Reverse();
			return newOut;
		}
		public static List<DateTime> FixOrderedDates(List<String> dates, CultureInfo culture = null) {
			var c = culture ?? new CultureInfo("en-US");
			string format;
			List<DateTime> fixedDates;
			try {

				fixedDates = _FixOrderedDates(dates, c, out format);
				if (!format.Contains("y"))
					fixedDates = _FixOrderedDates_Year(fixedDates);
				return fixedDates;
			} catch (ArgumentOutOfRangeException) {
				var tempDates = dates.Select(x => {
					DateTime o = DateTime.MinValue;
					if (DateTime.TryParse(x, out o))
						return (DateTime?)o;
					else
						return null;
				}).ToList();
				var interp = 0;
				fixedDates = InterpolateDates(tempDates, out interp);
				fixedDates = _FixOrderedDates_Year(fixedDates);
				return fixedDates;
			}


		}
		private static List<DateTime> _FixOrderedDates(List<String> dates, CultureInfo culture, out string format) {
			var histogram = new Multimap<string, string>();
			foreach (var d in dates) {
				TimingUtility.GuessPatterns(d, culture).Where(x => x != null).ToList().ForEach(x => histogram.Add(x, x));
			}
			if (!histogram.Any()) {
				throw new ArgumentOutOfRangeException("date", "Date column could not be parsed");
			}

			#region comments
			//else if (histogram.AllKeys().Count()==1)
			//{
			//Exactly one possible format
			//format = histogram.AllKeys().First();
			//var theFormat = format;
			//if (histogram.Get(format).Count == dates.Count){//All found, all same
			//    return  dates.Select(x => { 
			//        DateTime o;
			//        if (DateTime.TryParseExact(x, theFormat, culture, DateTimeStyles.None, out o))
			//            return o;
			//        throw new PermissionsException("Should not get here"); 
			//    }).ToList();

			//}
			//else 
			//{
			//    // Non optimal, not all dates are available, try interpolate
			//    var found= dates.Select(x =>{
			//        DateTime o;
			//        if (DateTime.TryParseExact(x, theFormat, culture, DateTimeStyles.None, out o))
			//            return (DateTime?)o;
			//        return null;
			//    }).ToList();
			//    return TimingUtility.InterpolateDates(found);
			//    }
			//}else{
			#endregion

			var highestCountFormat = histogram.OrderByDescending(x => x.Value.Count).First().Key;
			format = highestCountFormat;
			var found = dates.Select(x => {
				DateTime o;
				if (DateTime.TryParseExact(x, highestCountFormat, culture, DateTimeStyles.None, out o))
					return (DateTime?)o;
				return null;
			}).ToList();
			int interp;
			var fixedDates = TimingUtility.InterpolateDates(found, out interp);
			return fixedDates;
		}

		public static IEnumerable<DateTime> GetWeeksBetween(DateRange range) {
			return GetWeeksBetween(range.StartTime, range.EndTime);
		}
		
		public static IEnumerable<DateTime> GetWeeksBetween(DateTime scorecardStart, DateTime scorecardEnd) {
			var s = Math2.Min(scorecardStart, scorecardEnd);
			var e = Math2.Max(scorecardStart, scorecardEnd);
			s = s.StartOfWeek(DayOfWeek.Sunday);
			e = e.AddDays(6.999).StartOfWeek(DayOfWeek.Sunday);

			var i = s;
			while (i <= e) {
				yield return i;
				i=i.AddDays(7);
			}
			yield break;
		}

		public static List<DateTime> InterpolateDates(List<DateTime?> dates, out int interpolated) {
			interpolated = 0;
			if (!dates.Any())
				return new List<DateTime>();
			if (dates.Count == 1 && dates[0] != null)
				return dates.Select(x => x.Value).ToList();

			if (dates.Count == 1 && dates[0] == null)
				return (DateTime.UtcNow).AsList();


			if (dates.Count(x => x != null) < 2) {
				if (dates.Count() > 0 && !dates.Any(x => x != null))
					throw new PermissionsException("Unreadable date format.");

				throw new PermissionsException("Not enough dates.");
			}

			var temp = new List<DateTime>();
			DateTime? last = null;
			int count = 0;
			int initCount = 0;
			var jumps = new List<TimeSpan>();
			var interpJumps = new List<TimeSpan>();
			DateTime curJump;
			DateTime? firstDate = null;
			//Inner dates
			foreach (var d in dates) {
				if (d == null && last == null) {
					initCount++;
				} else if (d == null) {
					count += 1;
				} else if (d != null) {
					if (firstDate == null)
						firstDate = d.Value;
					if (count == 0) {
						if (last.HasValue) {
							var diff = d.Value - last.Value;
							if (diff > TimeSpan.FromDays(-358.9))
								jumps.Add(diff);
						}
					} else {
						var jump = new TimeSpan((d.Value - last.Value).Ticks / (count + 1));
						curJump = last.Value;
						interpJumps.Add(jump);
						for (var i = 0; i < count; i++) {
							curJump = curJump.Add(jump);
							temp.Add(curJump);
							interpolated++;
						}
					}
					temp.Add(d.Value);
					last = d;
					count = 0;
				}
			}

			var allJumps = new List<TimeSpan>();
			allJumps.AddRange(jumps);
			allJumps.AddRange(interpJumps);
			var jumpAvg = new TimeSpan((long)allJumps.Average(x => x.Ticks));

			//foreach (var j in allJumps)
			//{
			//    if (j - jumpAvg > TimeSpan.FromHours(6))
			//        throw new PermissionsException("Unstable interpolation");
			//}
			//Add ending
			curJump = last.Value;
			for (var i = 0; i < count; i++) {
				curJump = curJump.Add(jumpAvg);
				temp.Add(curJump);
				interpolated++;
			}
			//Add begining
			curJump = firstDate.Value;
			for (var i = 0; i < initCount; i++) {
				curJump = curJump.Subtract(jumpAvg);
				temp.Insert(0, curJump);
				interpolated++;
			}

			return temp;
		}

		public static int NumberOfWeeks(UserOrganizationModel caller) {
			switch (caller.GetOrganizationSettings().ScorecardPeriod) {
				case Models.Scorecard.ScorecardPeriod.Weekly:
					return 13;
				case Models.Scorecard.ScorecardPeriod.Monthly:
					return 52;
				case Models.Scorecard.ScorecardPeriod.Quarterly:
					return 52;
			}
			return 13;
		}



		public static List<SelectListItem> GetPossibleTimes(int? selected) {

			var possibleTimes = new List<SelectListItem>();

			possibleTimes.Add(new SelectListItem() {
				Selected = (selected ?? -1) == -1,
				Text = "Do not send e-mail",
				Value = "-1"
			});

			for (int i = 0; i < 24; i++) {
				var name = " AM (GMT)";
				if (i == 0)
					name = "12" + name;
				else if (i < 12)
					name = "" + i + name;
				else if (i == 12)
					name = "12 PM (GMT)";
				else
					name = "" + (i - 12) + " PM (GMT)";

				possibleTimes.Add(new SelectListItem() {
					Selected = selected == i,
					Text = name,
					Value = "" + i
				});
			}
			return possibleTimes;

		}

	}
}