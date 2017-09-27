using LambdaSerializer;
using log4net;
using NHibernate;
using RadialReview.Areas.CoreProcess.Models;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

		[Untested("RunAfterDispose","Does it run?")]
        public static async Task Each<T>(Expression<Func<ISession, T, Task>> action) where T : IHook {

            var hooks = GetHooks<T>();
            foreach (var x in hooks) {
				try {
					if (x.CanRunRemotely() && Config.IsSchedulerAction()) {
						await AmazonSQSUtility.SendMessage(MessageQueueModel.CreateHookRegistryAction(action, new SerializableHook() { lambda = action, type = action.GetType() }));
					} else {
						await HibernateSession.RunAfterSuccessfulDisposeOrNow(async (s, tx) => {
							await action.Compile()(s, x);
							tx.Commit();
							s.Flush();
						});
					}
				} catch (NotImplementedException e) {
					//just eat this one..
				} catch (Exception e) {
					log.Error(e);
					if (Config.IsLocal())
						throw;
				}
            };


        }

        public static async Task Each<T>(Expression<Func<T, Task>> action) where T : IHook {
            var hooks = GetHooks<T>();
            foreach (var x in hooks) {
                try {
                    if (x.CanRunRemotely() && Config.IsSchedulerAction()) {
                        await AmazonSQSUtility.SendMessage(MessageQueueModel.CreateHookRegistryAction(action, new SerializableHook() { lambda = action, type = action.GetType() }));
                    } else {
                        await action.Compile()(x);
                    }
                } catch (NotImplementedException e) {
					//just eat this one..
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