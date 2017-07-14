using log4net;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Hooks {
	public class HooksRegistry {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static HooksRegistry _Singleton { get; set; }
		private List<IHook> _Hooks { get; set; }
		private static object lck = new object();

		private HooksRegistry() {
			lock (lck) {
				_Hooks = new List<IHook>();
			}
		}

		public static void RegisterHook(IHook hook) {
			var hooks = GetSingleton();
			lock (lck) {
				hooks._Hooks.Add(hook);
			}
		}

		public static List<T> GetHooks<T>() where T : IHook {
			return GetSingleton()._Hooks.Where(x => x is T).Cast<T>().ToList();
		}

		public static async Task Each<T>(Func<T, Task> action) where T : IHook {
			var hooks = GetHooks<T>();
			foreach (var x in hooks) {
				try {
					await action(x);
				} catch (Exception e) {
					log.Error(e);
					if (Config.IsLocal())
						throw;
				}
			};
		}

		public static void Each<T>(Action<T> action) where T : IHook {
			GetHooks<T>().ForEach(x => {
				try {
					action(x);
				} catch (Exception e) {
					log.Error(e);
					if (Config.IsLocal())
						throw;
				}
			});
		}

		public static bool IsRegistered<T>() where T : IHook {
			return GetSingleton()._Hooks.Where(x => x is T).Any();
		}

		public static HooksRegistry GetSingleton() {
			if (_Singleton == null)
				_Singleton = new HooksRegistry();
			return _Singleton;
		}

		public static void Deregister() {
			_Singleton = null;
		}
	}
}