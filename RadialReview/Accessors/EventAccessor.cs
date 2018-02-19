using Newtonsoft.Json;
using NHibernate;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Models;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Frontend;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors {
	public class EventAccessor {

		public static Type GetGeneratorTypeFromType(string eventType) {
			foreach (var g in GetDefaultAvailableAnalyzers()){
				if (g.EventType == eventType)
					return g.GetType();
			}
			throw new ArgumentOutOfRangeException("eventType");
		}

		public static IEventAnalyzerGenerator BuildFromJson(string json) {
			var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
			var generatorName = (string)info["eag"];
			return (IEventAnalyzerGenerator)JsonConvert.DeserializeObject(json, GetGeneratorTypeFromType(generatorName));
		}

		public static IEventAnalyzerGenerator BuildFromSubscription(EventSubscription sub) {
			var json = sub.EventSettings;
			return (IEventAnalyzerGenerator)JsonConvert.DeserializeObject(json, GetGeneratorTypeFromType(sub.EventType));
		}




		public static async Task<EventSubscription> GetEventSubscription(UserOrganizationModel caller, long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var sub = s.Get<EventSubscription>(id);
					perms.Self(sub.SubscriberId);
					return sub;
				}
			}
		}

		public static async Task<EditorForm> CreateForm(UserOrganizationModel caller, IEnumerable<IEventAnalyzerGenerator> generators) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					var visibleRecur = L10Accessor.GetVisibleL10Meetings_Tiny(caller, caller.Id, false, true)
						.Select(x => new KeyValuePair<string, long>(x.Name, x.Id))
						.ToList();

					var settings = new BaseEventGeneratorSettings(caller, s, perms, caller.Organization.Id, visibleRecur);

					var subform = new EditorSubForm("eventType","Event Type");

					foreach (var generator in generators) {
						var fields = (await generator.GetSettingsFields(settings)).ToList();
						var name = generator.EventType;
						fields.Add(EditorField.Hidden("eag", name));
						subform.AddSubForm(generator.Name, name, fields);
					}

					return new EditorForm() {
						fields = subform.AsList(),
					};
				}
			}
		}

		public static IEnumerable<IEventAnalyzerGenerator> GetDefaultAvailableAnalyzers() {
			var generators =  new IEventAnalyzerGenerator[] {
				new Crosscutting.EventAnalyzers.Events.AverageMeetingRatingBelowForWeeksInARow(0),
				new Crosscutting.EventAnalyzers.Events.ConsecutiveLateEnds(0),
				new Crosscutting.EventAnalyzers.Events.ConsecutiveLateStarts(0),
				new Crosscutting.EventAnalyzers.Events.DaysWithoutL10(0),
				new Crosscutting.EventAnalyzers.Events.MissL10PastQuarterGenerator(0),
				new Crosscutting.EventAnalyzers.Events.TodoCompletionConsecutiveWeeks(0),
				new Crosscutting.EventAnalyzers.Events.MemberwiseTodoCompletionGenerator(0),
			};

			return generators;
		}

		public static async Task<EventSubscription> SubscribeToEvent(UserOrganizationModel caller,long subscriberUserId, IEventAnalyzerGenerator analyzer) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.SubscribeToEvent(subscriberUserId,analyzer);

					var subscriber = s.Get<UserOrganizationModel>(subscriberUserId);
					await analyzer.PreSaveOrUpdate(s);
					var settings = JsonConvert.SerializeObject(analyzer);

					EventSubscription evt = new EventSubscription() {
						EventSettings = settings,
						EventType = analyzer.EventType,
						OrgId = subscriber.Organization.Id,
						SubscriberId = subscriber.Id,
						Frequency = analyzer.fre
					};
					s.Save(evt);
					tx.Commit();
					s.Flush();
					return evt;
				}
			}
		}

		public static async Task<EventSubscription> EditEvent(UserOrganizationModel caller, long id, IEventAnalyzerGenerator analyzer) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewEvent(id, analyzer);

					var evt  = s.Get<EventSubscription>(id);
					await analyzer.PreSaveOrUpdate(s);
					var settings = JsonConvert.SerializeObject(analyzer);

					evt.EventSettings = settings;
					evt.EventType = analyzer.EventType;

					s.Update(evt);

					tx.Commit();
					s.Flush();
					return evt;
				}
			}

		}

		public static async Task<List<EventSubscription>> GetEventSubscriptions(UserOrganizationModel caller, long subscriberId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(subscriberId);

					return s.QueryOver<EventSubscription>()
						.Where(x => x.DeleteTime == null && x.SubscriberId == subscriberId)
						.List().ToList();
				}
			}
		}



		public static async Task<bool> DeleteOrUndeleteEvent(UserOrganizationModel caller, long eventId, bool delete = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var evt = s.Get<EventSubscription>(eventId);
					perms.Self(evt.SubscriberId);
					evt.DeleteOrUndelete(s, delete);
					tx.Commit();
					s.Flush();
					return true;
				}
			}
		}

	}
}