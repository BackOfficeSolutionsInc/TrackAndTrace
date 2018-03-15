using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.Enums;
using RadialReview.Crosscutting.EventAnalyzers.Events;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Models.L10;
using RadialReview.Utilities.DataTypes;
using System.Threading.Tasks;

namespace TractionTools.Tests.Crosscutting.Events {

    public static class EvtBuilder {
        public static List<IEvent> Append(this List<IEvent> self, decimal d, TimeSpan? after = null) {
            var time = new DateTime(2017, 1, 1);
            if (self.Any()) {
                time = self.Last().Time.Add(after ?? TimeSpan.FromHours(1));
            }

            self.Add(new Evt {
                Metric = d,
                Time = time
            });

            return self;
        }

        public static List<IEvent> Appends(this List<IEvent> self, params decimal[] d) {
            foreach (var a in d) {
                self = self.Append(a);
            }
            return self;
        }
    }

    public class Evt : IEvent {
        public decimal Metric { get; set; }
        public DateTime Time { get; set; }
    }

    [TestClass]
    public class EventProcessorTests {

        private class TestThresh : IThreshold {
            public LessGreater Direction { get; set; }
            public decimal Threshold { get; set; }
        }


        [TestMethod]
        public void TestEventProcessor() {
            var N = 1;
            var P = 0;
            var thresh = new TestThresh {
                Direction = LessGreater.GreaterThan,
                Threshold = 1,
            };

            {
                var evts = new List<IEvent>();
                evts.Appends(P, P, P, N, N, P, N, N, P, P, N, N);
                //should end with a trigger
                Assert.IsTrue(EventProcessor.ShouldTrigger(evts, thresh, 2, 2));
                //should have run already
                Assert.IsFalse(EventProcessor.ShouldTrigger(evts, thresh, 2, 1));

                var lastCheck = evts.ElementAt(evts.Count - 3 - 1).Time;
                Assert.IsTrue(EventProcessor.ShouldTrigger(evts, thresh, 2, 1, lastCheck));
            }
            {
                var evts = new List<IEvent>();
                evts.Appends(N, N, P, P, P, N, P, N, N, P, N);
                //should end with a trigger
                Assert.IsFalse(EventProcessor.ShouldTrigger(evts, thresh, 2, 2));
                //should have run already
                Assert.IsFalse(EventProcessor.ShouldTrigger(evts, thresh, 2, 1));
                Assert.IsTrue(EventProcessor.ShouldTrigger(evts, thresh, 1, 1));

                var lastCheck = evts.ElementAt(evts.Count - 2 - 1).Time;
                Assert.IsFalse(EventProcessor.ShouldTrigger(evts, thresh, 2, 2, lastCheck));
                lastCheck = evts.ElementAt(evts.Count - 3 - 1).Time;
                Assert.IsTrue(EventProcessor.ShouldTrigger(evts, thresh, 2, 2, lastCheck));
                lastCheck = evts.ElementAt(evts.Count - 4 - 1).Time;
                Assert.IsTrue(EventProcessor.ShouldTrigger(evts, thresh, 2, 2, lastCheck));
            }
        }

        [TestMethod]
        public void TestEventHistogram() {
            {
                var dates = new List<DateTime>() {
                    new DateTime(2017,1,1),
                    new DateTime(2017,1,1),
                    new DateTime(2017,1,1),
                    new DateTime(2017,1,1),
                    new DateTime(2017,1,2),
                };
                {
                    var daily = EventHelper.ToHistogram(EventFrequency.Daily, dates, x => x);
                    Assert.AreEqual(2, daily.Count);

                    Assert.AreEqual(4, daily[0].Metric);
                    Assert.AreEqual(1, daily[1].Metric);
                }
                {
                    var weekly = EventHelper.ToHistogram(EventFrequency.Weekly, dates, x => x);
                    Assert.AreEqual(1, weekly.Count);

                    Assert.AreEqual(5, weekly[0].Metric);
                }
            }
            {
                var dates2 = new List<DateTime>() {
                    new DateTime(2017,1,1),
                    new DateTime(2017,1,1),
                    new DateTime(2017,1,1),
                    new DateTime(2017,1,1),
                    new DateTime(2018,1,1),
                };
                {
                    var weekly = EventHelper.ToHistogram(EventFrequency.Weekly, dates2, x => x);
                    Assert.AreEqual(53, weekly.Count);
                    Assert.AreEqual(4, weekly[0].Metric);
                    for (var i = 1; i < weekly.Count - 1; i++) {
                        Assert.AreEqual(0, weekly[i].Metric);
                    }
                    Assert.AreEqual(1, weekly[weekly.Count - 1].Metric);
                }
                {
                    var biweekly = EventHelper.ToHistogram(EventFrequency.Biweekly, dates2, x => x);
                    Assert.AreEqual(27, biweekly.Count);
                    Assert.AreEqual(4, biweekly[0].Metric);
                    for (var i = 1; i < biweekly.Count - 1; i++) {
                        Assert.AreEqual(0, biweekly[i].Metric);
                    }
                    Assert.AreEqual(1, biweekly[biweekly.Count - 1].Metric);
                }
                {
                    var monthly = EventHelper.ToHistogram(EventFrequency.Monthly, dates2, x => x);
                    Assert.AreEqual(13, monthly.Count);
                    Assert.AreEqual(4, monthly[0].Metric);
                    for (var i = 1; i < monthly.Count - 1; i++) {
                        Assert.AreEqual(0, monthly[i].Metric);
                    }
                    Assert.AreEqual(1, monthly[monthly.Count - 1].Metric);
                }
                {
                    var yearly = EventHelper.ToHistogram(EventFrequency.Yearly, dates2, x => x);
                    Assert.AreEqual(2, yearly.Count);
                    Assert.AreEqual(4, yearly[0].Metric);
                    Assert.AreEqual(1, yearly[1].Metric);
                }

            }
        }


		[TestMethod]
		public async Task TestDaysWithoutL10() {

			var evtGen = new DaysWithoutL10(1);

			Assert.AreEqual(15, evtGen.GetNumberOfFailsToTrigger(null));
			Assert.AreEqual(1, evtGen.GetNumberOfPassesToReset(null));
			Assert.IsTrue(evtGen.IsEnabled(null));
			Assert.AreEqual(1, evtGen.GetFireThreshold(null).Threshold);
			Assert.AreEqual(LessGreater.LessThan, evtGen.GetFireThreshold(null).Direction);

			var settings = new BaseEventSettings(null, 0, new DateTime(2017, 1, 14));
			var meetings = new List<L10Meeting>() {
				new L10Meeting() { StartTime = new DateTime(2017,1,1) },
				new L10Meeting() { StartTime = new DateTime(2017,1,2) },
				new L10Meeting() { StartTime = new DateTime(2017,1,8) },
				new L10Meeting() { StartTime = new DateTime(2017,1,15) },
				new L10Meeting() { StartTime = new DateTime(2017,2,15) },
			};
			settings.SetLookup(new SearchRealL10Meeting(0), settings, meetings);
			settings.SetLookup(new SearchLeadershipL10RecurrenceIds(), settings, new List<long> { 0 });


			Assert.IsTrue(await EventProcessor.ShouldTrigger(settings, evtGen));

			settings = new BaseEventSettings(null, 0, new DateTime(2017, 2, 14));
			settings.SetLookup(new SearchRealL10Meeting(0), settings, meetings);
			settings.SetLookup(new SearchLeadershipL10RecurrenceIds(), settings, new List<long> { 0 });
			Assert.IsFalse(await EventProcessor.ShouldTrigger(settings, evtGen));
		}



		[TestMethod]
        public async Task TestAverageMeetingRatingBelowForWeeksInARow() {
            var evtGen = new AverageMeetingRatingBelowForWeeksInARow(1);

            Assert.AreEqual(2, evtGen.GetNumberOfFailsToTrigger(null));
            Assert.AreEqual(1, evtGen.GetNumberOfPassesToReset(null));
            Assert.IsTrue(evtGen.IsEnabled(null));
            Assert.AreEqual(7, evtGen.GetFireThreshold(null).Threshold);
            Assert.AreEqual(LessGreater.LessThanOrEqual, evtGen.GetFireThreshold(null).Direction);

            var settings = new BaseEventSettings(null, 0, new DateTime(2017, 1, 14));
            var meetings = new List<L10Meeting>() {
                new L10Meeting() { StartTime = new DateTime(2017,1,1) , AverageMeetingRating = new Ratio(10,1)},
                new L10Meeting() { StartTime = new DateTime(2017,1,2) , AverageMeetingRating = new Ratio(10,1)},
                new L10Meeting() { StartTime = new DateTime(2017,1,8) , AverageMeetingRating = new Ratio(7,1)},
                new L10Meeting() { StartTime = new DateTime(2017,1,15) , AverageMeetingRating = new Ratio(7,1)},
                new L10Meeting() { StartTime = new DateTime(2017,2,15) , AverageMeetingRating = new Ratio(10,1)},
            };
            settings.SetLookup(new SearchRealL10Meeting(0), settings, meetings);
            settings.SetLookup(new SearchL10RecurrenceIds(), settings, new List<long> { 0 });


            Assert.IsTrue(await EventProcessor.ShouldTrigger(settings, evtGen));

            settings = new BaseEventSettings(null, 0, new DateTime(2017, 2, 14));
            settings.SetLookup(new SearchRealL10Meeting(0), settings, meetings);
            settings.SetLookup(new SearchL10RecurrenceIds(), settings, new List<long> { 0 });
            Assert.IsFalse(await EventProcessor.ShouldTrigger(settings, evtGen));
        }

        [TestMethod]
        public async Task TestTodoCompletionConsecutiveWeeks() {
            var evtGen = new TodoCompletionConsecutiveWeeks(1);

            Assert.AreEqual(2, evtGen.GetNumberOfFailsToTrigger(null));
            Assert.AreEqual(1, evtGen.GetNumberOfPassesToReset(null));
            Assert.IsTrue(evtGen.IsEnabled(null));
            Assert.AreEqual(.5m, evtGen.GetFireThreshold(null).Threshold);
            Assert.AreEqual(LessGreater.LessThanOrEqual, evtGen.GetFireThreshold(null).Direction);

            var settings = new BaseEventSettings(null, 0, new DateTime(2017, 1, 14));
            var meetings = new List<L10Meeting>() {
                new L10Meeting() { StartTime = new DateTime(2017,1,1) , TodoCompletion = new Ratio(.8m,1)},
                new L10Meeting() { StartTime = new DateTime(2017,1,2) , TodoCompletion = new Ratio(.8m,1)},
                new L10Meeting() { StartTime = new DateTime(2017,1,8) , TodoCompletion = new Ratio(.5m,1)},
                new L10Meeting() { StartTime = new DateTime(2017,1,15) , TodoCompletion= new Ratio(.5m,1)},
                new L10Meeting() { StartTime = new DateTime(2017,2,15) , TodoCompletion= new Ratio(.8m,1)},
            };
            settings.SetLookup(new SearchRealL10Meeting(0), settings, meetings);
            settings.SetLookup(new SearchL10RecurrenceIds(), settings, new List<long> { 0 });


            Assert.IsTrue(await EventProcessor.ShouldTrigger(settings, evtGen));

            settings = new BaseEventSettings(null, 0, new DateTime(2017, 2, 14));
            settings.SetLookup(new SearchRealL10Meeting(0), settings, meetings);
            settings.SetLookup(new SearchL10RecurrenceIds(), settings, new List<long> { 0 });
            Assert.IsFalse(await EventProcessor.ShouldTrigger(settings, evtGen));
        }

        [TestMethod]
        public async Task TestConsecutiveLateStarts() {
            var evtGen = new ConsecutiveLateStarts(1);

            Assert.AreEqual(2, evtGen.GetNumberOfFailsToTrigger(null));
            Assert.AreEqual(1, evtGen.GetNumberOfPassesToReset(null));
            Assert.IsTrue(evtGen.IsEnabled(null));
            Assert.AreEqual(5m, evtGen.GetFireThreshold(null).Threshold);
            Assert.AreEqual(LessGreater.GreaterThan, evtGen.GetFireThreshold(null).Direction);

            var settings = new BaseEventSettings(null, 0, new DateTime(2017, 1, 14));
            var meetings = new List<L10Meeting>() {
                new L10Meeting() { StartTime = new DateTime(2017,1,1,1,1,0)  },
                new L10Meeting() { StartTime = new DateTime(2017,1,8,0,58,0) },
                new L10Meeting() { StartTime = new DateTime(2017,1,15,1,6,0) },
                new L10Meeting() { StartTime = new DateTime(2017,1,22,1,7,0) },
                new L10Meeting() { StartTime = new DateTime(2017,2,22,1,1,0) },
            };
            settings.SetLookup(new SearchRealL10Meeting(0), settings, meetings);
            settings.SetLookup(new SearchL10RecurrenceIds(), settings, new List<long> { 0 });

            Assert.IsTrue(await EventProcessor.ShouldTrigger(settings, evtGen));

            settings = new BaseEventSettings(null, 0, new DateTime(2017, 2, 14));
            settings.SetLookup(new SearchRealL10Meeting(0), settings, meetings);
            settings.SetLookup(new SearchL10RecurrenceIds(), settings, new List<long> { 0 });
            Assert.IsFalse(await EventProcessor.ShouldTrigger(settings, evtGen));
        }

        [TestMethod]
        public async Task TestConsecutiveLateEnds() {
            var evtGen = new ConsecutiveLateEnds(1);

            Assert.AreEqual(2, evtGen.GetNumberOfFailsToTrigger(null));
            Assert.AreEqual(1, evtGen.GetNumberOfPassesToReset(null));
            Assert.IsTrue(evtGen.IsEnabled(null));
            Assert.AreEqual(15m, evtGen.GetFireThreshold(null).Threshold);
            Assert.AreEqual(LessGreater.GreaterThan, evtGen.GetFireThreshold(null).Direction);

            var settings = new BaseEventSettings(null, 0, new DateTime(2017, 1, 14));
            var meetings = new List<L10Meeting>() {
                new L10Meeting() { StartTime = new DateTime(2017,1,1)  , CompleteTime = new DateTime(2017,1,1,0,30,0)},
                new L10Meeting() { StartTime = new DateTime(2017,1,8)  , CompleteTime = new DateTime(2017,1,8,0,30,0)},
                new L10Meeting() { StartTime = new DateTime(2017,1,15) , CompleteTime = new DateTime(2017,1,15,0,50,0)},
                new L10Meeting() { StartTime = new DateTime(2017,1,22) , CompleteTime = new DateTime(2017,1,22,0,50,0)},
                new L10Meeting() { StartTime = new DateTime(2017,2,22) , CompleteTime = new DateTime(2017,2,22,0,30,0)},
                new L10Meeting() { StartTime = new DateTime(2017,2,22) , CompleteTime = null},
            };
            settings.SetLookup(new SearchRealL10Meeting(0), settings, meetings);
            settings.SetLookup(new SearchL10RecurrenceIds(), settings, new List<long> { 0 });
            settings.SetLookup(new SearchPageTimerSettings(0), settings, new List<L10Recurrence.L10Recurrence_Page> {
                new L10Recurrence.L10Recurrence_Page() {
                    Minutes = 30
                }
            });

            Assert.IsTrue(await EventProcessor.ShouldTrigger(settings, evtGen));

            settings = new BaseEventSettings(null, 0, new DateTime(2017, 2, 14));
            settings.SetLookup(new SearchRealL10Meeting(0), settings, meetings);
            settings.SetLookup(new SearchL10RecurrenceIds(), settings, new List<long> { 0 });
            settings.SetLookup(new SearchPageTimerSettings(0), settings, new List<L10Recurrence.L10Recurrence_Page> {
                new L10Recurrence.L10Recurrence_Page() {
                    Minutes = 30
                }
            });

            Assert.IsFalse(await EventProcessor.ShouldTrigger(settings, evtGen));
        }

    }
}
