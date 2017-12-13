using RadialReview;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using System.Linq;
using System.Collections.Generic;
using RadialReview.Utilities.DataTypes;
using NHibernate;

namespace TractionTools.Tests.Utilities {
	[TestClass]
	public class TimingUtilityTests {
		[TestMethod]
		public void TestRangeWeekly() {
			var d = new DateTime(2016, 2, 17);
			var yearStart = new YearStart() { Month = Month.January, Offset = DateOffset.FirstOfMonth };

			//Basic Test
			var range = TimingUtility.GetLocalRange(DayOfWeek.Sunday, 0, d, ScorecardPeriod.Weekly, yearStart);
			Assert.IsTrue(range.StartTime.DayOfWeek == DayOfWeek.Sunday, "StartTime DayOfWeek not Sunday");
			Assert.IsTrue(range.EndTime.DayOfWeek == DayOfWeek.Sunday, "EndTime DayOfWeek not Sunday");
			Assert.IsTrue(range.StartTime.TimeOfDay == TimeSpan.FromTicks(0), "StartTime not start of day");
			Assert.IsTrue(range.EndTime.TimeOfDay == TimeSpan.FromTicks(0), "EndTime not start of day");
			Assert.IsTrue(range.EndTime - range.StartTime == TimeSpan.FromDays(7), "Day difference incorrect");
			Assert.IsTrue(range.StartTime == new DateTime(2016, 2, 14), "StartTime incorrect");
			Assert.IsTrue(range.EndTime == new DateTime(2016, 2, 21), "EndTime incorrect");

			//Change Offset -360
			range = TimingUtility.GetLocalRange(DayOfWeek.Sunday, -360, d, ScorecardPeriod.Weekly, yearStart);
			Assert.IsTrue(range.StartTime.DayOfWeek == DayOfWeek.Sunday, "StartTime DayOfWeek not Sunday");
			Assert.IsTrue(range.EndTime.DayOfWeek == DayOfWeek.Sunday, "EndTime DayOfWeek not Sunday");
			Assert.IsTrue(range.StartTime.TimeOfDay == TimeSpan.FromHours(6), "StartTime not start of day");
			Assert.IsTrue(range.EndTime.TimeOfDay == TimeSpan.FromHours(6), "EndTime not start of day");
			Assert.IsTrue(range.EndTime - range.StartTime == TimeSpan.FromDays(7), "Day difference incorrect");
			Assert.IsTrue(range.StartTime == new DateTime(2016, 2, 14, 6, 0, 0), "StartTime incorrect");
			Assert.IsTrue(range.EndTime == new DateTime(2016, 2, 21, 6, 0, 0), "EndTime incorrect");

			//Change Offset +360
			range = TimingUtility.GetLocalRange(DayOfWeek.Sunday, 360, d, ScorecardPeriod.Weekly, yearStart);
			Assert.IsTrue(range.StartTime.DayOfWeek == DayOfWeek.Saturday, "StartTime DayOfWeek not Sunday");
			Assert.IsTrue(range.EndTime.DayOfWeek == DayOfWeek.Saturday, "EndTime DayOfWeek not Sunday");
			Assert.IsTrue(range.StartTime.TimeOfDay == TimeSpan.FromHours(18), "StartTime not start of day");
			Assert.IsTrue(range.EndTime.TimeOfDay == TimeSpan.FromHours(18), "EndTime not start of day");
			Assert.IsTrue(range.EndTime - range.StartTime == TimeSpan.FromDays(7), "Day difference incorrect");
			Assert.IsTrue(range.StartTime == new DateTime(2016, 2, 13, 18, 0, 0), "StartTime incorrect");
			Assert.IsTrue(range.EndTime == new DateTime(2016, 2, 20, 18, 0, 0), "EndTime incorrect");

			//Change Start of week
			for (var i = 0; i < 7; i++) {
				var dow = (DayOfWeek)i;
				range = TimingUtility.GetLocalRange(dow, 0, d, ScorecardPeriod.Weekly, yearStart);
				Assert.IsTrue(range.StartTime.DayOfWeek == dow, "StartTime DayOfWeek not " + dow);
				Assert.IsTrue(range.EndTime.DayOfWeek == dow, "EndTime DayOfWeek not " + dow);
				Assert.IsTrue(range.StartTime.TimeOfDay == TimeSpan.FromTicks(0), "StartTime not start of day");
				Assert.IsTrue(range.EndTime.TimeOfDay == TimeSpan.FromTicks(0), "EndTime not start of day");
				Assert.IsTrue(range.EndTime - range.StartTime == TimeSpan.FromDays(7), "Day difference incorrect");
				Assert.IsTrue(range.StartTime == new DateTime(2016, 2, 14 + i), "StartTime incorrect");
				Assert.IsTrue(range.EndTime == new DateTime(2016, 2, 21 + i), "EndTime incorrect");
			}
		}

		[TestMethod]
		public void TestRangeMonthly() {
			var d = new DateTime(2016, 2, 17);
			var yearStart = new YearStart() { Month = Month.January, Offset = DateOffset.FirstOfMonth };

			//Basic Test
			var range = TimingUtility.GetLocalRange(DayOfWeek.Sunday, 0, d, ScorecardPeriod.Monthly, yearStart);
			Assert.IsTrue(range.StartTime.Day == 1, "StartTime Day not the first");
			Assert.IsTrue(range.EndTime.Day == 1, "EndTime Day not the first");
			Assert.IsTrue(range.StartTime.TimeOfDay == TimeSpan.FromTicks(0), "StartTime not start of day");
			Assert.IsTrue(range.EndTime.TimeOfDay == TimeSpan.FromTicks(0), "EndTime not start of day");
			Assert.IsTrue((range.EndTime - range.StartTime).TotalDays == 29.0, "Day difference incorrect");
			Assert.IsTrue(range.StartTime == new DateTime(2016, 2, 1), "StartTime incorrect");
			Assert.IsTrue(range.EndTime == new DateTime(2016, 3, 1), "EndTime incorrect");

			//Change Offset -360
			range = TimingUtility.GetLocalRange(DayOfWeek.Sunday, -360, d, ScorecardPeriod.Monthly, yearStart);
			Assert.IsTrue(range.StartTime.Day == 1, "StartTime Day not the first");
			Assert.IsTrue(range.EndTime.Day == 1, "EndTime Day not the first");
			Assert.IsTrue(range.StartTime.TimeOfDay == TimeSpan.FromHours(6), "StartTime not start of day");
			Assert.IsTrue(range.EndTime.TimeOfDay == TimeSpan.FromHours(6), "EndTime not start of day");
			Assert.IsTrue(range.EndTime - range.StartTime == TimeSpan.FromDays(29), "Day difference incorrect");
			Assert.IsTrue(range.StartTime == new DateTime(2016, 2, 1, 6, 0, 0), "StartTime incorrect");
			Assert.IsTrue(range.EndTime == new DateTime(2016, 3, 1, 6, 0, 0), "EndTime incorrect");

			//Change Offset +360
			range = TimingUtility.GetLocalRange(DayOfWeek.Sunday, 360, d, ScorecardPeriod.Monthly, yearStart);
			Assert.IsTrue(range.StartTime.Day == 31, "StartTime Day not last of month");
			Assert.IsTrue(range.EndTime.Day == 29, "EndTime Day not last of month");
			Assert.IsTrue(range.StartTime.TimeOfDay == TimeSpan.FromHours(18), "StartTime not start of day");
			Assert.IsTrue(range.EndTime.TimeOfDay == TimeSpan.FromHours(18), "EndTime not start of day");
			Assert.IsTrue(range.EndTime - range.StartTime == TimeSpan.FromDays(29), "Day difference incorrect");
			Assert.IsTrue(range.StartTime == new DateTime(2016, 1, 31, 18, 0, 0), "StartTime incorrect");
			Assert.IsTrue(range.EndTime == new DateTime(2016, 2, 29, 18, 0, 0), "EndTime incorrect");

			//Change Start of week
			for (var i = 0; i < 7; i++) {
				var dow = (DayOfWeek)i;
				range = TimingUtility.GetLocalRange(dow, 0, d, ScorecardPeriod.Monthly, yearStart);

				//Nothing should happen..
				Assert.IsTrue(range.StartTime.Day == 1, "StartTime Day not the first");
				Assert.IsTrue(range.EndTime.Day == 1, "EndTime Day not the first");
				Assert.IsTrue(range.StartTime.TimeOfDay == TimeSpan.FromTicks(0), "StartTime not start of day");
				Assert.IsTrue(range.EndTime.TimeOfDay == TimeSpan.FromTicks(0), "EndTime not start of day");
				Assert.IsTrue((range.EndTime - range.StartTime).TotalDays == 29.0, "Day difference incorrect");
				Assert.IsTrue(range.StartTime == new DateTime(2016, 2, 1), "StartTime incorrect");
				Assert.IsTrue(range.EndTime == new DateTime(2016, 3, 1), "EndTime incorrect");
			}
		}

		[TestMethod]
		public void TestRangeQuarterly_MondayOfFourthWeek() {
			//var d = new DateTime(2016, 2, 17);
			var yearStart = new YearStart() { Month = Month.February, Offset = DateOffset.MondayOfFourthWeek };

			var start = new DateTime(2014, 1, 1);
			var end = new DateTime(2019, 1, 1);
			var d = start;

			var quarters = new[]{
			   new DateTime(2014,02,17),new DateTime(2014,05,19),new DateTime(2014,08,18),new DateTime(2014,11,17),
			   new DateTime(2015,02,23),new DateTime(2015,05,18),new DateTime(2015,08,17),new DateTime(2015,11,23),
			   new DateTime(2016,02,22),new DateTime(2016,05,23),new DateTime(2016,08,22),new DateTime(2016,11,21),
			   new DateTime(2017,02,20),new DateTime(2017,05,22),new DateTime(2017,08,21),new DateTime(2017,11,20),
			   new DateTime(2018,02,19),new DateTime(2018,05,21),new DateTime(2018,08,20),new DateTime(2018,11,19),
			   new DateTime(2019,02,18),new DateTime(2019,05,20),new DateTime(2019,08,19),new DateTime(2019,11,18),
			   new DateTime(2020,02,17),new DateTime(2020,05,18),new DateTime(2020,08,17),new DateTime(2020,11,23),
			   new DateTime(2021,02,22),new DateTime(2021,05,17),new DateTime(2021,08,23),new DateTime(2021,11,22),
			   new DateTime(2022,02,21),new DateTime(2022,05,23),new DateTime(2022,08,22),new DateTime(2022,11,21),
			};
			/* for(int i=0;i<20;i++){
				 quarters.Add(yearStart.GetDate(2014, i));
				 Console.WriteLine(yearStart.GetDate(2014, i));
			 }*/


			for (var i = 0; i < quarters.Length/4; i++) {
				for (var j = 0; j < 4; j++) {
					Assert.AreEqual(quarters[i * 4 + j], yearStart.GetDate(2014 + i, j));// yearStart.Offset
				}
			}
		}
		
		[TestMethod]
		public void TestRangeQuarterly_FirstMondayOfMonth() {
			//var d = new DateTime(2016, 2, 17);
			var yearStart = new YearStart() { Month = Month.February, Offset = DateOffset.FirstMondayOfTheMonth };

			var start = new DateTime(2015, 1, 1);
			var end = new DateTime(2018, 1, 1);
			var d = start;

			var quarters = new[]{
			   new DateTime(2014,02,03),new DateTime(2014,05,05),new DateTime(2014,08,04),new DateTime(2014,11,03),
			   new DateTime(2015,02,02),new DateTime(2015,05,04),new DateTime(2015,08,03),new DateTime(2015,11,02),
			   new DateTime(2016,02,01),new DateTime(2016,05,02),new DateTime(2016,08,01),new DateTime(2016,11,07),
			   new DateTime(2017,02,06),new DateTime(2017,05,01),new DateTime(2017,08,07),new DateTime(2017,11,06),
			   new DateTime(2018,02,05),new DateTime(2018,05,07),new DateTime(2018,08,06),new DateTime(2018,11,05),
			};
			/* for(int i=0;i<20;i++){
				 quarters.Add(yearStart.GetDate(2014, i));
				 Console.WriteLine(yearStart.GetDate(2014, i));
			 }*/

			while (d <= end) {
				var shouldStartIndex = quarters.Where(q => d >= q).Count() - 1;
				var shouldStart = quarters[shouldStartIndex];
				var shouldEnd = quarters[shouldStartIndex + 1];
				//Basic Test

				for (var dow = 0; dow < 7; dow++)//Day of week has no effect
				{
					var range = TimingUtility.GetLocalRange((DayOfWeek)dow, 0, d, ScorecardPeriod.Quarterly, yearStart);
					Assert.AreEqual(shouldStart, range.StartTime, "start time: " + d);
					Assert.AreEqual(shouldEnd, range.EndTime, "end time: " + d);
				}
				d = d.AddDays(1);
			}

			//Change Offset -360
			d = start;
			while (d <= end) {
				var shouldStartIndex = quarters.Where(q => d >= q.AddHours(6)).Count() - 1;
				var shouldStart = quarters[shouldStartIndex].AddHours(6);
				var shouldEnd = quarters[shouldStartIndex + 1].AddHours(6);

				for (var dow = 0; dow < 7; dow++) {
					var range = TimingUtility.GetLocalRange((DayOfWeek)dow, -360, d, ScorecardPeriod.Quarterly, yearStart);

					Assert.AreEqual(shouldStart, range.StartTime, "start time: " + d);
					Assert.AreEqual(shouldEnd, range.EndTime, "end time: " + d);
				}
				d = d.AddDays(1);
			}
			//Change Offset +360
			d = start;
			while (d <= end) {
				var shouldStartIndex = quarters.Where(q => d >= q.AddHours(-6)).Count() - 1;
				var shouldStart = quarters[shouldStartIndex].AddHours(-6);
				var shouldEnd = quarters[shouldStartIndex + 1].AddHours(-6);
				for (var dow = 0; dow < 7; dow++) {
					var range = TimingUtility.GetLocalRange((DayOfWeek)dow, 360, d, ScorecardPeriod.Quarterly, yearStart);
					Assert.AreEqual(shouldStart, range.StartTime, "start time: " + d);
					Assert.AreEqual(shouldEnd, range.EndTime, "end time: " + d);
				}
				d = d.AddDays(1);
			}
		}
		
		[TestMethod]
		public void TestRangeQuarterly_FirstSundayOfTheMonth() {
			//var d = new DateTime(2016, 2, 17);
			var yearStart = new YearStart() { Month = Month.February, Offset = DateOffset.FirstSundayOfTheMonth };

			var start = new DateTime(2015, 1, 1);
			var end = new DateTime(2018, 1, 1);
			var d = start;

			var quarters = new[]{
				new DateTime(2014,2,2),new DateTime(2014,5,4),new DateTime(2014,8,3),new DateTime(2014,11,2),
				new DateTime(2015,2,1),new DateTime(2015,5,3),new DateTime(2015,8,2),new DateTime(2015,11,1),
				new DateTime(2016,2,7),new DateTime(2016,5,1),new DateTime(2016,8,7),new DateTime(2016,11,6),
				new DateTime(2017,2,5),new DateTime(2017,5,7),new DateTime(2017,8,6),new DateTime(2017,11,5),
				new DateTime(2018,2,4),new DateTime(2018,5,6),new DateTime(2018,8,5),new DateTime(2018,11,4),
			};
			//for (int i = 0; i < 20; i++)
			//{
			//    quarters.Add(yearStart.GetDate(2014, i));
			//    Console.WriteLine(yearStart.GetDate(2014, i));
			//}

			while (d <= end) {
				var shouldStartIndex = quarters.Where(q => d >= q).Count() - 1;
				var shouldStart = quarters[shouldStartIndex];
				var shouldEnd = quarters[shouldStartIndex + 1];
				//Basic Test

				for (var dow = 0; dow < 7; dow++)//Day of week has no effect
				{
					var range = TimingUtility.GetLocalRange((DayOfWeek)dow, 0, d, ScorecardPeriod.Quarterly, yearStart);
					Assert.AreEqual(shouldStart, range.StartTime, "start time: " + d);
					Assert.AreEqual(shouldEnd, range.EndTime, "end time: " + d);
				}
				d = d.AddDays(1);
			}

			//Change Offset -360
			d = start;
			while (d <= end) {
				var shouldStartIndex = quarters.Where(q => d >= q.AddHours(6)).Count() - 1;
				var shouldStart = quarters[shouldStartIndex].AddHours(6);
				var shouldEnd = quarters[shouldStartIndex + 1].AddHours(6);

				for (var dow = 0; dow < 7; dow++) {
					var range = TimingUtility.GetLocalRange((DayOfWeek)dow, -360, d, ScorecardPeriod.Quarterly, yearStart);

					Assert.AreEqual(shouldStart, range.StartTime, "start time: " + d);
					Assert.AreEqual(shouldEnd, range.EndTime, "end time: " + d);
				}
				d = d.AddDays(1);
			}
			//Change Offset +360
			d = start;
			while (d <= end) {
				var shouldStartIndex = quarters.Where(q => d >= q.AddHours(-6)).Count() - 1;
				var shouldStart = quarters[shouldStartIndex].AddHours(-6);
				var shouldEnd = quarters[shouldStartIndex + 1].AddHours(-6);
				for (var dow = 0; dow < 7; dow++) {
					var range = TimingUtility.GetLocalRange((DayOfWeek)dow, 360, d, ScorecardPeriod.Quarterly, yearStart);
					Assert.AreEqual(shouldStart, range.StartTime, "start time: " + d);
					Assert.AreEqual(shouldEnd, range.EndTime, "end time: " + d);
				}
				d = d.AddDays(1);
			}
		}

		[TestMethod]
		public void TestRangeQuarterly_FirstOfMonth() {
			//var d = new DateTime(2016, 2, 17);
			var yearStart = new YearStart() { Month = Month.February, Offset = DateOffset.FirstOfMonth };

			var start = new DateTime(2015, 1, 1);
			var end = new DateTime(2018, 1, 1);
			var d = start;

			var quarters = new[]{
				new DateTime(2014,2,1),new DateTime(2014,5,1),new DateTime(2014,8,1),new DateTime(2014,11,1),
				new DateTime(2015,2,1),new DateTime(2015,5,1),new DateTime(2015,8,1),new DateTime(2015,11,1),
				new DateTime(2016,2,1),new DateTime(2016,5,1),new DateTime(2016,8,1),new DateTime(2016,11,1),
				new DateTime(2017,2,1),new DateTime(2017,5,1),new DateTime(2017,8,1),new DateTime(2017,11,1),
				new DateTime(2018,2,1),new DateTime(2018,5,1),new DateTime(2018,8,1),new DateTime(2018,11,1),
			};
			while (d <= end) {
				var shouldStartIndex = quarters.Where(q => d >= q).Count() - 1;
				var shouldStart = quarters[shouldStartIndex];
				var shouldEnd = quarters[shouldStartIndex + 1];
				//Basic Test

				for (var dow = 0; dow < 7; dow++)//Day of week has no effect
				{
					var range = TimingUtility.GetLocalRange((DayOfWeek)dow, 0, d, ScorecardPeriod.Quarterly, yearStart);
					Assert.AreEqual(shouldStart, range.StartTime, "start time: " + d);
					Assert.AreEqual(shouldEnd, range.EndTime, "end time: " + d);
				}
				d = d.AddDays(1);
			}

			//Change Offset -360
			d = start;
			while (d <= end) {
				var shouldStartIndex = quarters.Where(q => d >= q.AddHours(6)).Count() - 1;
				var shouldStart = quarters[shouldStartIndex].AddHours(6);
				var shouldEnd = quarters[shouldStartIndex + 1].AddHours(6);

				for (var dow = 0; dow < 7; dow++) {
					var range = TimingUtility.GetLocalRange((DayOfWeek)dow, -360, d, ScorecardPeriod.Quarterly, yearStart);

					Assert.AreEqual(shouldStart, range.StartTime, "start time: " + d);
					Assert.AreEqual(shouldEnd, range.EndTime, "end time: " + d);
				}
				d = d.AddDays(1);
			}
			//Change Offset +360
			d = start;
			while (d <= end) {
				var shouldStartIndex = quarters.Where(q => d >= q.AddHours(-6)).Count() - 1;
				var shouldStart = quarters[shouldStartIndex].AddHours(-6);
				var shouldEnd = quarters[shouldStartIndex + 1].AddHours(-6);
				for (var dow = 0; dow < 7; dow++) {
					var range = TimingUtility.GetLocalRange((DayOfWeek)dow, 360, d, ScorecardPeriod.Quarterly, yearStart);
					Assert.AreEqual(shouldStart, range.StartTime, "start time: " + d);
					Assert.AreEqual(shouldEnd, range.EndTime, "end time: " + d);
				}
				d = d.AddDays(1);
			}

		}

		[TestMethod]
		public void TestYearStart() {
			var yearStart = new YearStart() { Month = Month.February, Offset = DateOffset.FirstOfMonth };

			Assert.AreEqual(new DateTime(2014, 2, 1), yearStart.GetDate(2014));
			Assert.AreEqual(new DateTime(2014, 2, 1), yearStart.GetDate(2014, 0));
			Assert.AreEqual(new DateTime(2014, 5, 1), yearStart.GetDate(2014, 1));
			Assert.AreEqual(new DateTime(2014, 8, 1), yearStart.GetDate(2014, 2));
			Assert.AreEqual(new DateTime(2014, 11, 1), yearStart.GetDate(2014, 3));
			Assert.AreEqual(new DateTime(2015, 2, 1), yearStart.GetDate(2014, 4));
			Assert.AreEqual(new DateTime(2015, 5, 1), yearStart.GetDate(2014, 5));
			Assert.AreEqual(new DateTime(2016, 5, 1), yearStart.GetDate(2014, 9));
			Assert.AreEqual(new DateTime(2017, 2, 1), yearStart.GetDate(2014, 12));
			Assert.AreEqual(new DateTime(2013, 11, 1), yearStart.GetDate(2014, -1));
			Assert.AreEqual(new DateTime(2013, 8, 1), yearStart.GetDate(2014, -2));
			Assert.AreEqual(new DateTime(2013, 5, 1), yearStart.GetDate(2014, -3));
			Assert.AreEqual(new DateTime(2013, 2, 1), yearStart.GetDate(2014, -4));
			Assert.AreEqual(new DateTime(2012, 11, 1), yearStart.GetDate(2014, -5));
			Assert.AreEqual(new DateTime(2011, 11, 1), yearStart.GetDate(2014, -9));
		}

		[TestMethod]
		public void TestWeeksBetween() {

			for (var i = 17; i<= 21; i++) {
				var w = TimingUtility.GetWeeksBetween(new DateRange(new DateTime(2017, 10, 10), new DateTime(2017, 10, i)));
				SetUtility.AssertEqual(new[] { new DateTime(2017, 10, 8), new DateTime(2017, 10, 15), new DateTime(2017, 10, 22) }, w,"i="+i);
			}

			var weeks = TimingUtility.GetWeeksBetween(new DateRange(new DateTime(2017, 10, 10), new DateTime(2017, 10, 14)));
			SetUtility.AssertEqual(new[] { new DateTime(2017, 10, 8), new DateTime(2017, 10, 15) }, weeks, "ddd");



			for (var i = 10; i <= 14; i++) {
				var w = TimingUtility.GetWeeksBetween(new DateRange(new DateTime(2017, 10, i), new DateTime(2017, 10, 17)));
				SetUtility.AssertEqual(new[] { new DateTime(2017, 10, 8), new DateTime(2017, 10, 15), new DateTime(2017, 10, 22) }, w, "i=" + i);
			}

			var weeks2 = TimingUtility.GetWeeksBetween(new DateRange(new DateTime(2017, 10, 15), new DateTime(2017, 10, 17)));
			SetUtility.AssertEqual(new[] { new DateTime(2017, 10, 15), new DateTime(2017, 10, 22) }, weeks2,"eee");

		}


		[TestMethod]
		public void ServerTimeConvert() {
			var td = new TimeData() {
				TimezoneOffset = -420, //-7hrs = PST
			};

			var localTime = new DateTime(2017, 10, 22);
			var serverTime = td.ConvertToServerTime(localTime);
			Assert.AreEqual(new DateTime(2017, 10, 22, 7, 0, 0), serverTime);

			var backToLocal = td.ConvertFromServerTime(serverTime);
			Assert.AreEqual(new DateTime(2017, 10, 22, 0, 0, 0), backToLocal);
		}


		[TestMethod]
		public void SingleSourceOfTime_DatabaseFallback() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//No exceptions...
					var time = TimingUtility.GetDbTimestamp(s);
				}
			}
		}

	}
}
