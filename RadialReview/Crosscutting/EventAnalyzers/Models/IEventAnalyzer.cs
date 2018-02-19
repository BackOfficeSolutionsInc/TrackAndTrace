using NHibernate;
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using RadialReview.Utilities;
using System.Threading.Tasks;
using RadialReview.Models.Frontend;
using RadialReview.Models;

namespace RadialReview.Crosscutting.EventAnalyzers.Interfaces {
	public enum EventFrequency {
		//[Obsolete("Hey these are expensive to run")]
		//Minutly = 1,
		[Obsolete("Hey these are expensive to run")]
		Hourly = 60,
		[Obsolete("Hey these are expensive to run")]
		Daily = 1440,
		Weekly = 10080,
		Biweekly = 20160,
		Monthly = 43800,
		Quarterly = 131040,
		Yearly = 525600,
	}


	public interface IEventAnalyzerGenerator {
		Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings);
		Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings);
		string EventType { get; }
		string Name { get; }
		string Description { get; }
		Task PreSaveOrUpdate(ISession s);
		EventFrequency GetExecutionFrequency();
	}

	public interface IEventAnalyzer {
		int GetNumberOfPassesToReset(IEventSettings settings);
		int GetNumberOfFailsToTrigger(IEventSettings settings);
		bool IsEnabled(IEventSettings settings);

		IThreshold GetFireThreshold(IEventSettings settings);
		Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings);


	}
	public interface IRecurrenceEventAnalyerGenerator {
		long RecurrenceId { get; }
	}

	public interface IEventTrigger {
		bool ShouldTrigger { get; }
	}

	public interface IEventGeneratorSettings {
		UserOrganizationModel Caller { get;  }
		PermissionsUtility Permissions { get;  }

		List<KeyValuePair<string, long>> VisibleRecurrences { get; }

		long OrganizationId { get; }
		ISession Session { get; }
	}

	public interface IEvent {
		DateTime Time { get; }
		decimal Metric { get; }
	}

	public interface IEventSettings {
		PermissionsUtility Admin { get; }
		long OrganizationId { get; }
		ISession Session { get; }
		DateTime RunTime { get; }
		IDataSource DataSearch { get; }

		Task<T> Lookup<T>(BaseSearch<T> search);
		void SetLookup<T>(BaseSearch<T> searcher, IEventSettings settings, T obj);
	}
	public interface IDataSource {
		Task<T> Lookup<T>(BaseSearch<T> search);
		void Set<T>(string key, T obj);
	}

	public abstract class BaseSearch<T> {
		public abstract Task<T> PerformSearch(IEventSettings settings);
		protected abstract IEnumerable<string> UniqueKeys(IEventSettings settings);
		public virtual string GetKey(IEventSettings settings) {
			var uniques = this.UniqueKeys(settings);
			var type = this.GetType().FullName;
			var end = string.Join("~", uniques);
			return type + "~" + end;
		}
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

		public class Bin<T> : IEnumerable<T> {
			public DateTime Date { get; set; }
			public List<T> Objects { get; set; }
			public Bin(DateTime date, List<T> objects) {
				Date = date;
				Objects = objects ?? new List<T>();
			}

			public IEnumerator<T> GetEnumerator() {
				return Objects.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return Objects.GetEnumerator();
			}
		}

		public static DateTime Add(this DateTime self, EventFrequency freq) {
			return self.AddMinutes((int)freq);
		}
		public static DateTime Subtract(this DateTime self, EventFrequency freq) {
			return self.AddMinutes(-(int)freq);
		}

		public static List<IEvent> ToBinnedEvents<T>(EventFrequency binSize, IEnumerable<T> items, Func<T, DateTime?> dateSelector, Func<Bin<T>, IEvent> toEvent, bool allowNullItems = false) {
			var bins = ToBins(binSize, items, dateSelector, allowNullItems);
			return bins.Select(x => toEvent(x)).Where(x => x != null).ToList();
		}

		public static List<IEvent> ToBinnedEventsFromRatio<T>(EventFrequency binSize, IEnumerable<T> items, Func<T, DateTime?> dateSelector, Func<T, Ratio> ratioSelector, bool allowNullItems = false) {
			return ToBinnedEvents(binSize, items, dateSelector, x => {
				var ratio = x.Aggregate(new Ratio(), (i, r) => {
					var binRatio = ratioSelector(r);
					if (binRatio != null)
						i.Add(binRatio);
					return i;
				});
				if (!ratio.IsValid())
					return null;
				return (IEvent)new BaseEvent(ratio.GetValue(null), x.Date);
			}, allowNullItems);
		}
		//public static List<IEvent> ToBinnedEventsAggregator<T, TAggreate>(EventFrequency binSize, IEnumerable<T> items, Func<T, DateTime?> dateSelector, TAggreate initialValue, Func<TAggreate,T, TAggreate> aggregator,Func<TAggreate,decimal?> aggregatorToMetric, bool allowNullItems = false) {          
		//    return ToBinnedEvents(binSize, items, dateSelector, bin=> {
		//        var agg = bin.Objects.Aggregate(initialValue, aggregator);
		//        if ()
		//        return new BaseEvent(aggregatorToMetric(agg),bin.Date);
		//    }, allowNullItems);
		//}

		public static List<Bin<T>> ToBins<T>(EventFrequency binSize, IEnumerable<T> items, Func<T, DateTime?> dateSelector, bool allowNullItems = false) {
			return ToBins(binSize, items, dateSelector, x => x, allowNullItems);
		}

		public static List<Bin<TPROP>> ToBins<T, TPROP>(EventFrequency binSize, IEnumerable<T> items, Func<T, DateTime?> dateSelector, Func<T, TPROP> propSelector, bool allowNullProp = false) {

			var orderedObj = items.Where(x => x != null && dateSelector(x) != null).OrderBy(dateSelector).ToList();
			var dates2 = orderedObj.Select(x => dateSelector(x).Value).ToList();
			if (!dates2.Any()) {
				return new List<Bin<TPROP>>();
			}

			var min = dates2.Min();
			var max = dates2.Max();
			var i = min.StartOfPeriod(binSize);
			var js = 0;

			var result = new List<Bin<TPROP>>();
			while (i <= max) {
				var next = i.Add(binSize);
				var count = 0;
				var objs = new List<TPROP>();
				for (var j = js; j < orderedObj.Count(); j++) {
					if (dates2[j] < next) {
						count += 1;
						var prop = propSelector(orderedObj[j]);
						if (prop != null || allowNullProp) {
							objs.Add(prop);
						}
					} else {
						js = j;
						break;
					}
				}

				//if (false) {
				//    var same = dates.Count(x => i <= x && x < next);
				//}
				result.Add(new Bin<TPROP>(i, objs));

				i = next;
			}

			return result;
		}


		public static List<IEvent> ToHistogram(EventFrequency binSize, IEnumerable<DateTime?> items) {
			return ToHistogram(binSize, items, x => x);
		}

		public static List<IEvent> ToHistogram<T>(EventFrequency binSize, IEnumerable<T> items, Func<T, DateTime?> dateSelector) {
			return ToBinnedEvents(binSize, items, dateSelector, x => new BaseEvent(x.Objects.Count, x.Date));
			///return bins.Select().ToList();

			//var dates = items.Select(dateSelector).Where(x => x != null).Select(x => x.Value).OrderBy(x => x).ToList();
			//if (!dates.Any()) {
			//	return new List<IEvent>();
			//}

			//var min = dates.Min();
			//var max = dates.Max();
			//var i = min;
			//var js = 0;

			//var result = new List<IEvent>();
			//while (i <= max) {
			//	var next = i.Add(binSize);
			//	var count = 0;
			//	for (var j = js; j < dates.Count(); j++) {
			//		if (dates[j] < next) {
			//			count += 1;
			//		} else {
			//			js = j;
			//			break;
			//		}
			//	}

			//	if (false) {
			//		var same = dates.Count(x => i <= x && x < next);
			//	}
			//	result.Add(new BaseEvent(count, i));

			//	i = next;
			//}

			//return result;
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

	public class BaseEventGeneratorSettings : IEventGeneratorSettings {
		public BaseEventGeneratorSettings(UserOrganizationModel caller, ISession session, PermissionsUtility permissions, long organizationId, IEnumerable<KeyValuePair<string, long>> visibleRecurrences) {
			Caller = caller;
			OrganizationId = organizationId;
			Permissions = permissions;
			VisibleRecurrences = visibleRecurrences.ToList();
			Session = session;
		}

		public UserOrganizationModel Caller { get; private set; }
		public long OrganizationId { get; private set; }
		public PermissionsUtility Permissions { get; private set; }
		public List<KeyValuePair<string, long>> VisibleRecurrences { get; private set; }
		public ISession Session { get; private set; }
	}

	public class BaseEventDataSource : IDataSource {
		public BaseEventDataSource(IEventSettings settings) {
			Settings = settings;
			LookupData = new Dictionary<string, object>();
		}

		private IEventSettings Settings { get; set; }
		private Dictionary<string, object> LookupData { get; set; }


		public async Task<T> Lookup<T>(BaseSearch<T> search) {
			var key = search.GetKey(Settings);
			if (!LookupData.ContainsKey(key))
				LookupData[key] = await search.PerformSearch(Settings);
			return (T)LookupData[key];
		}

		public void Set<T>(string key, T obj) {
			LookupData[key] = obj;
		}
	}

	public class BaseEventSettings : IEventSettings {
		public BaseEventSettings(ISession session, long organizationId, DateTime lastCheck) {
			RunTime = lastCheck;
			OrganizationId = organizationId;
			Session = session;
			DataSearch = new BaseEventDataSource(this);
			Admin= PermissionsUtility.CreateAdmin(Session);
		}

		public IDataSource DataSearch { get; private set; }
		public DateTime RunTime { get; private set; }
		public long OrganizationId { get; private set; }
		public ISession Session { get; private set; }

		public PermissionsUtility Admin { get; private set; }

		public async Task<T> Lookup<T>(BaseSearch<T> search) {
			return await DataSearch.Lookup(search);
		}

		public void SetLookup<T>(BaseSearch<T> searcher,IEventSettings settings, T obj) {
			DataSearch.Set(searcher.GetKey(settings), obj);
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

		public static async Task<bool> ShouldTrigger(IEventSettings settings, IEventAnalyzer analyzer) {
			return ShouldTrigger(await analyzer.GenerateEvents(settings), analyzer.GetFireThreshold(settings), analyzer.GetNumberOfPassesToReset(settings), analyzer.GetNumberOfFailsToTrigger(settings), settings.RunTime);
		}
	}
}