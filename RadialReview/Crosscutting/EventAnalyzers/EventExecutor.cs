using log4net;
using NHibernate;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Models;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Hooks;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Crosscutting.EventAnalyzers {
	public class EventExecutor {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


		//private static EventRegistry _Singleton { get; set; }
		//private List<IEventAnalyzer> _EventAnalyzers { get; set; }
		//private static object lck = new object();

		//private EventRegistry() {
		//	lock (lck) {
		//		_EventAnalyzers = new List<IEventAnalyzer>();
		//	}
		//}

		//public static void RegisterEventAnalyzer(IEventAnalyzer hook) {
		//	var hooks = GetSingleton();
		//	lock (lck) {
		//		hooks._EventAnalyzers.Add(hook);
		//	}
		//}

		//public static List<T> GetEventAnalyzers<T>() where T : IEventAnalyzer {
		//	return GetSingleton()._EventAnalyzers.Where(x => x is T).Cast<T>().ToList();
		//}

		//public static EventRegistry GetSingleton() {
		//	if (_Singleton == null)
		//		_Singleton = new EventRegistry();
		//	return _Singleton;
		//}

		public static async Task ExecuteAll(EventFrequency frequency) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					UserOrganizationModel subA = null;
					OrganizationModel orgA = null;
					var subs = s.QueryOver<EventSubscription>()
						.JoinAlias(x => x.Subscriber, () => subA)
						.JoinAlias(x => x.Org, () => orgA)
						.Where(x => x.Frequency == frequency && x.DeleteTime == null && subA.DeleteTime == null && orgA.DeleteTime == null)
						.List().ToList();


					tx.Commit();
					s.Flush();
				}
			}

		}


		public static async Task Execute(ISession s,long orgId, List<IEventAnalyzer> analyzers) {
			//var analyzers = GetSingleton()._EventAnalyzers;

			var eventLogs = s.QueryOver<EventLogModel>().Where(x => x.DeleteTime == null).List().ToList();

			var now = DateTime.UtcNow;

			//var orgIds = s.QueryOver<OrganizationModel>()
			//	.Where(x => x.DeleteTime == null)
			//	.Select(x => x.Id)
			//	.List<long>().ToList();

			foreach (var a in analyzers) {
				var f = a.GetExecutionFrequency();
				var after = now.Subtract(f);
				var type = ForModel.GetModelType(a.GetType());

				var log = eventLogs.SingleOrDefault(x => x.EventAnalyzerName == type);
				if (log == null) {
					s.Save(new EventLogModel() {
						EventAnalyzerName = type,
						Frequency = f,
						RunTime = now,
						OrgId = orgId,
					});
				}

				if (log.RunTime < after) {
					var anyExecuted = false;
					//foreach (var oId in orgIds) {
						IEventSettings settings = new BaseEventSettings(s,orgId, log.RunTime);

						if (a.IsEnabled(settings)) {
							var shouldTrigger = await EventProcessor.ShouldTrigger(settings, a);

                            if (shouldTrigger) {
                                anyExecuted = true;
                                await HooksRegistry.Each<IEventHook>((ses, x) => x.HandleEventTriggered(ses, a, settings));
                            }
							//Run the analyzer
						}
					//}
					if (anyExecuted) {
						log.RunTime = now;
						s.Update(log);
					}
				}
			}
		}
	}
}