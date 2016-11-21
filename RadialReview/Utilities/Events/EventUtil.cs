using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Hooks;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Enums;
using RadialReview.Models.Events;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;
using RadialReview.Models.Tasks;
using RadialReview.Models.UserModels;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RadialReview.Utilities {


	public class EventUtil {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public class TriggerOptions {

			public ISession S { get; protected set; }
			public EventType Type { get; protected set; }
			public ResponsibilityGroupModel Caller { get; protected set; }
			public long? OrgId { get; protected set; }
			public ForModel Model { get; protected set; }
			public String Message { get; protected set; }
			public decimal? Arg1 { get; protected set; }
			public DateTime? Now;

			public TriggerOptions Create(ISession s, EventType type, UserOrganizationModel caller, ILongIdentifiable model, String message = null, decimal? arg1 = null) {
				return Create(s, type, caller, caller.Organization.Id, ForModel.Create(model), message, arg1);
			}
			public TriggerOptions Create(ISession s, EventType type, ResponsibilityGroupModel caller, long orgId, ForModel model = null, String message = null, decimal? arg1 = null) {
				S = s;
				Type = type;
				Caller = caller;
				OrgId = orgId;
				Model = model;
				Message = message;
				Arg1 = arg1;

				return this;
			}
			public TriggerOptions() { }
		}
		public static void Trigger(Action<TriggerOptions> options) {
			try {
				var o = new TriggerOptions();
				options(o);
				var evt = new AccountEvent() {
					Argument1 = o.Arg1,
					ForModel = o.Model,
					Message = o.Message,
					OrgId = o.OrgId ?? o.Caller.NotNull(x => x.Organization.NotNull(y=>y.Id)),
					TriggeredBy = o.Caller.NotNull(x => x.Id),
					Type = o.Type,
				};
				evt.CreateTime = o.Now ?? evt.CreateTime;
				o.S.Save(evt);
				HooksRegistry.Each<IAccountEvent>(x => x.CreateEvent(o.S, evt));
			} catch (Exception e) {
				log.Error("Error triggering event:", e);
			}

		}

		public static void GenerateAccountAgeEvents(ISession s, long orgId, DateTime startTime) {

			var events = new[] {
				EventType.AccountAge_1d,EventType.AccountAge_2d,EventType.AccountAge_3d,EventType.AccountAge_4d,
				EventType.AccountAge_5d,EventType.AccountAge_6d,EventType.AccountAge_1w,EventType.AccountAge_2w,
				EventType.AccountAge_3w,EventType.AccountAge_monthly
			};

			//foreach (var evt in events) {
			//var offset = EventDaysOffset(EventType.AccountAge_monthly, startTime);

			var fire = EventDaysOffset(EventType.AccountAge_monthly, startTime);

			var st = new ScheduledTask() {
				Fire = fire.Item1,
				FirstFire = fire.Item1,
				NextSchedule = TimespanExtensions.OneMonth(),
				EmailOnException = true,
				MaxException = 2,
				TaskName = ApplicationAccessor.ACCOUNT_AGE,
				Url = "/Scheduler/Trigger/" + orgId + "?event=" + EventType.AccountAge_monthly,

			};

			s.Save(st);
			st.OriginalTaskId = st.Id;
			s.Update(st);
			//}


		}
		public static void GenerateAllDailyEvents(ISession s, DateTime now) {
			var orgsQ = s.QueryOver<OrganizationModel>()
							.Where(x => x.DeleteTime == null &&
								x.AccountType != AccountType.Cancelled &&
								x.AccountType != AccountType.Dormant && x.AccountType != AccountType.Other
							).Future();


			var orgLookupsQ = s.QueryOver<OrganizationLookup>().Future();
			var recursQ = s.QueryOver<L10Recurrence>().Where(x => x.DeleteTime == null).Select(x => x.Id, x => x.TeamType, x => x.CreateTime, x => x.OrganizationId).Future<object[]>();
			var meetingsQ = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null).Select(x => x.Id, x => x.L10RecurrenceId, x => x.StartTime, x => x.CompleteTime, x => x.OrganizationId).Future<object[]>();
			var reviewsQ = s.QueryOver<ReviewsModel>().Where(x => x.DeleteTime == null).Select(x => x.Id, x => x.DateCreated, x => x.OrganizationId).Future<object[]>();
			var eventsQ = s.QueryOver<AccountEvent>().Where(x => x.DeleteTime == null).Select(x => x.Id, x => x.CreateTime, x => x.Type, x => x.OrgId).Future<object[]>();

			var orgs = orgsQ.ToList();

			var orgLookups = orgLookupsQ.ToList();
			var recurs = recursQ.Select(x => new { Id = (long)x[0], TeamType = (L10TeamType)x[1], CreateTime = (DateTime)x[2], OrgId = (long)x[3] }).ToList();
			var meetings = meetingsQ.Select(x => new { Id = (long)x[0], RecurId = (long)x[1], StartTime = (DateTime?)x[2], CompleteTime = (DateTime?)x[3], OrgId = (long)x[4] }).ToList();
			var reviews = reviewsQ.Select(x => new { Id = (long)x[0], CreateTime = (DateTime)x[1], OrgId = (long)x[2] }).ToList();
			var events = eventsQ.Select(x => new TinyEvent { Id = (long)x[0], CreateTime = (DateTime)x[1], Type = (EventType)x[2], OrgId = (long)x[3] }).ToList();

			var potentialEvents = new List<Tuple<long, EventType?>>();

			foreach (var o in orgs) {
				if (o.Settings.EnableReview) {
					var reviewTimes = reviews.Where(x => x.OrgId == o.Id).Select(x => x.CreateTime);
					var evt = _MaxDurEvent(now, o.CreationTime, EventType.NoReview_3m, reviewTimes);
					potentialEvents.Add(Tuple.Create(o.Id, evt));
				}

				if (o.Settings.EnableL10) {
					{
						var l10Times = meetings.Where(x => x.OrgId == o.Id && x.StartTime != null).Select(x => x.StartTime.Value);
						var evt = _MaxDurEvent(now, o.CreationTime, EventType.NoMeeting_1w, l10Times);
						potentialEvents.Add(Tuple.Create(o.Id, evt));
					}

					var myRecurs = recurs.Where(x => x.OrgId == o.Id);

					if (!myRecurs.Any(x => x.TeamType == L10TeamType.LeadershipTeam)) {
						var evt = _MaxDurEvent(now, o.CreationTime, EventType.NoLeadershipMeetingCreated_1w);
						potentialEvents.Add(Tuple.Create(o.Id, evt));
					}

					if (!myRecurs.Any(x => x.TeamType == L10TeamType.DepartmentalTeam)) {
						var evt = _MaxDurEvent(now, o.CreationTime, EventType.NoDepartmentMeetingCreated_2w);
						potentialEvents.Add(Tuple.Create(o.Id, evt));
					}
				}

				var ol = orgLookups.Where(x => x.OrgId == o.Id).FirstOrDefault();
				if (ol != null) {
					var evt = _MaxDurEvent(now, ol.LastUserLoginTime, EventType.NoLogins_1w);
					potentialEvents.Add(Tuple.Create(o.Id, evt));

					evt = _MaxDurEvent(now, ol.CreateTime, EventType.AccountAge_1d);
					potentialEvents.Add(Tuple.Create(o.Id, evt));

				}
			}

			foreach (var e in potentialEvents) {
				if (e.Item2 != null)
					AddIfNew(s, e.Item1, e.Item2.Value, now, events);
			}
		}
		
		public static EventType? _MaxDurEvent(DateTime now, DateTime orgStart, EventType likeType, IEnumerable<DateTime> list) {
			var minTimes = GetEventOffsets(likeType).ToList().OrderByDescending(x => x.Value).Where(x => x.Value < (now - orgStart)).ToList();
			foreach (var m in minTimes) {
				if (!list.Where(x => x > now.Subtract(m.Value)).Any())
					return m.Key;
			}
			return null;
		}
		public static EventType? _MaxDurEvent(DateTime now, DateTime orgStart, EventType likeType) {
			var minTimes = GetEventOffsets(likeType).ToList().OrderByDescending(x => x.Value).Where(x => x.Value < (now - orgStart)).ToList();
			return minTimes.FirstOrDefault().NotNull(x => x.Key);
		}

		public static TimeSpan GetTimespanFromEvent(string evt) {
			var split = ("" + evt).Split('_');
			if (("" + evt).EndsWith("_monthly"))
				return TimespanExtensions.OneMonth();
			if (split.Length > 1) {
				var dayPortion = split.Last();
				var resultString = Regex.Match(dayPortion, @"\d+").Value;
				var num = Int32.Parse(resultString);
				if (dayPortion.EndsWith("d"))
					return TimeSpan.FromDays(num);
				if (dayPortion.EndsWith("w"))
					return TimeSpan.FromDays(num * 7);
			}
			return TimeSpan.FromDays(365 * 1000);
		}
		public static TimeSpan GetTimespanFromEvent(EventType evt) {
			return GetTimespanFromEvent("" + evt);
		}

		protected class TinyEvent {
			public long Id { get; set; }
			public DateTime CreateTime { get; set; }
			public EventType Type { get; set; }
			public long OrgId { get; set; }
		}

		protected static void AddIfNew(ISession s, long orgId, EventType type, DateTime now, List<TinyEvent> events) {
			if (ShouldAdd(orgId, type, events)) {
				Trigger(x => {
					x.Create(s, type, null, orgId, ForModel.Create<OrganizationModel>(orgId));
					x.Now = now;
				});
				events.Add(new TinyEvent() {
					CreateTime = now,
					Type = type,
					OrgId = orgId,
					Id = -1
				});
			}
		}

		protected static bool ShouldAdd(long orgId, EventType type, List<TinyEvent> events) {
			var ts = GetTimespanFromEvent(type);
			var relavent = events.Where(x => x.OrgId == orgId)
				  .OrderByDescending(x => x.CreateTime)
				  .Where(x => x.Type.SameKind(type))
				  .TakeWhile(x => GetTimespanFromEvent(x.Type) <= ts);
			if (!relavent.Any(x => x.Type == type)) {
				return true;
			}
			return false;
		}
		protected static Tuple<DateTime, TimeSpan?> EventDaysOffset(EventType evt, DateTime startTime) {
			if (("" + evt).EndsWith("_monthly")) {
				return Tuple.Create(startTime.Add(TimespanExtensions.OneMonth()), (TimeSpan?)TimespanExtensions.OneMonth());
			}
			var span = GetTimespanFromEvent(evt);
			return Tuple.Create(startTime.Add(span), (TimeSpan?)null);

		}
		protected static List<EventType> FindEventsLike(EventType evt) {
			var o = new List<EventType>();
			foreach (EventType e in Enum.GetValues(typeof(EventType))) {
				if (e.SameKind(evt))
					o.Add(e);
			}
			return o;
		}
		protected static Dictionary<EventType, TimeSpan> GetEventOffsets(EventType eventLike) {
			return FindEventsLike(eventLike).ToDictionary(x => x, x => GetTimespanFromEvent(x));
		}
	}
}