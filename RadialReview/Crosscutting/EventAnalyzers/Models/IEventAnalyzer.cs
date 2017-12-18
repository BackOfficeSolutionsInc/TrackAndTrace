using NHibernate;
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers.Interfaces {
	public enum EventFrequency {
		Minutly = 1,
		Hourly = 60,
		Daily = 1440,
		Weekly = 10080,
		Biweekly = 20160,
		Monthly = 43800,
		Yearly = 525600,
	}

	public interface IEventAnalyzer {
		EventFrequency GetFrequency();
		int GetNumberOfPassesToReset(IEventSettings settings);
		int GetNumberOfFailsToTrigger(IEventSettings settings);
		bool IsEnabled(IEventSettings settings);

		IThreshold GetFireThreshold(IEventSettings settings);

		IEnumerable<IEvent> GenerateEvents(IEventSettings settings);

	}

	public interface IEventTrigger {
		bool ShouldTrigger { get; }
	}


	public interface IEvent {
		DateTime Time { get; }
		decimal Metric { get; }
	}

	public interface IEventSettings {
		long OrganizationId { get; }
		ISession Session { get; }
		DateTime LastCheck { get; }
		IDataSource DataSearch { get; }

		T Lookup<T>(ISearch<T> search);
		void SetLookup<T>(string key, T obj);

	}
	public interface IDataSource {
		T Lookup<T>(ISearch<T> search);
		void Set<T>(string key, T obj);
	}
	public interface ISearch<T> {
		T PerformSearch(IEventSettings settings);
		string UniqueKey(IEventSettings settings);
	}

	public interface IThreshold {
		decimal Threshold { get; }
		LessGreater Direction { get; }
	}

	public class EventThreshold : IThreshold {
		public EventThreshold(LessGreater direction, decimal threshold) {
			Direction = direction;
			Threshold = threshold;
		}

		public LessGreater Direction { get; private set; }
		public decimal Threshold { get; private set; }
	}

	public static class EventHelper {

		public static DateTime Add(this DateTime self, EventFrequency freq) {
			return self.AddMinutes((int)freq);
		}
		public static DateTime Subtract(this DateTime self, EventFrequency freq) {
			return self.AddMinutes(-(int)freq);
		}

		public static List<IEvent> ToHistogram(EventFrequency binSize, IEnumerable<DateTime?> items) {
			return ToHistogram(binSize, items, x => x);
		}


		public static List<IEvent> ToHistogram<T>(EventFrequency binSize, IEnumerable<T> items, Func<T, DateTime?> dateSelector) {

			var dates = items.Select(dateSelector).Where(x => x != null).Select(x => x.Value).OrderBy(x => x).ToList();
			if (!dates.Any()) {
				return new List<IEvent>();
			}

			var min = dates.Min();
			var max = dates.Max();
			var i = min;
			var js = 0;

			var result = new List<IEvent>();
			while (i <= max) {
				var next = i.Add(binSize);
				var count = 0;
				for (var j = js; j < dates.Count(); j++) {
					if (dates[j] < next) {
						count += 1;
					} else {
						js = j;
						break;
					}
				}

				if (false) {
					var same = dates.Count(x => i <= x && x < next);
				}


				result.Add(new BaseEvent(count, i));

				i = next;
			}

			return result;
		}
	}

	public class BaseEvent : IEvent {
		public BaseEvent(decimal metric, DateTime time) {
			Metric = metric;
			Time = time;
		}
		public decimal Metric { get; private set; }
		public DateTime Time { get; private set; }
	}

	public class BaseEventDataSource : IDataSource {
		public BaseEventDataSource(IEventSettings settings) {
			Settings = settings;
			LookupData = new Dictionary<string, object>();
		}

		private IEventSettings Settings { get; set; }
		private Dictionary<string, object> LookupData { get; set; }

		public T Lookup<T>(ISearch<T> search) {
			var key = search.UniqueKey(Settings);
			if (!LookupData.ContainsKey(key))
				LookupData[key] = search.PerformSearch(Settings);
			return (T)LookupData[key];
		}

		public void Set<T>(string key, T obj) {
			LookupData[key] = obj;
		}
	}

	public class BaseEventSettings : IEventSettings {
		public BaseEventSettings(ISession session, long organizationId, DateTime lastCheck) {
			LastCheck = lastCheck;
			OrganizationId = organizationId;
			Session = session;
			DataSearch = new BaseEventDataSource(this);
		}

		public IDataSource DataSearch { get; private set; }
		public DateTime LastCheck { get; private set; }
		public long OrganizationId { get; private set; }
		public ISession Session { get; private set; }

		public T Lookup<T>(ISearch<T> search) {
			return DataSearch.Lookup(search);
		}

		public void SetLookup<T>(string key, T obj) {
			DataSearch.Set(key, obj);
		}
	}

	public class EventProcessor {

		protected static bool IsPositive(IEvent e, IThreshold thresh) {
			return !thresh.Direction.MeetGoal(thresh.Threshold, null, e.Metric);
		}

		/// <summary>
		/// When lastCheck is null, only trigger on the last event
		/// </summary>
		/// <param name="events"></param>
		/// <param name="threshold"></param>
		/// <param name="consecutivePositivesToReset"></param>
		/// <param name="consecutiveNegativesToTrigger"></param>
		/// <param name="lastCheck"></param>
		/// <returns></returns>
		public static bool ShouldTrigger(IEnumerable<IEvent> evts, IThreshold threshold,int consecutivePositivesToReset,int consecutiveNegativesToTrigger, DateTime? lastCheck=null) {
			int positives = 0;
			int negatives = 0;
			bool trigger = false;
			bool reset = false;

			var events = evts.ToList();
			 
			var ordered = events.OrderBy(x => x.Time).ToList();
			var first = ordered.FirstOrDefault();
			if (first != null) {
				reset = IsPositive(first, threshold);
			}

			foreach (var e in ordered) {
				var isPositive = IsPositive(e, threshold);
				trigger = false;

				if (isPositive) {
					negatives = 0;
					positives += 1;
					if (positives == consecutivePositivesToReset)
						reset = true;
				} else {
					positives = 0;
					negatives += 1;
					if (reset && negatives == consecutiveNegativesToTrigger) {
						trigger = true;
						reset = false;

						if (lastCheck != null && e.Time > lastCheck.Value) {
							return true;
						}
					}
				}
			}
			return trigger;
		}

		public static bool ShouldTrigger(IEventSettings settings, IEventAnalyzer analyzer) {
			return ShouldTrigger(analyzer.GenerateEvents(settings), analyzer.GetFireThreshold(settings), analyzer.GetNumberOfPassesToReset(settings), analyzer.GetNumberOfFailsToTrigger(settings), settings.LastCheck);
		}
	}
}