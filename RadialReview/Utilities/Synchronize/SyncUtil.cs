using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Common.Logging.Configuration;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Synchronize;
using System.Threading;
using log4net;
using RadialReview.Utilities;
using NHibernate.Exceptions;

namespace RadialReview.Utilities.Synchronize {

	public class SyncUtil {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static TimeSpan Buffer = TimeSpan.FromSeconds(40);


		/*public static void EnsureStrictlyAfterForUser(UserOrganizationModel caller,ISession s,SyncAction action)
		{
			var now = DateTime.UtcNow;
			var after = now.Subtract(Buffer);
			var actionStr = action.ToString();
			var clientTimestamp = caller._ClientTimestamp;

			if (clientTimestamp == null){
				throw new SyncException(null);
			}

			var syncs = s.QueryOver<Sync>()
				.Where(x => x.DeleteTime == null && x.CreateTime >= after && x.Action == actionStr && caller.Id == x.UserId)
				.Select(x=>x.Timestamp)
				.List<long>()
				.ToList();

			s.Save(new Sync(){
				CreateTime = now,
				Action = actionStr,
				Timestamp = clientTimestamp.Value,
				UserId = caller.Id,
			});

			if (!syncs.All(x => x < clientTimestamp))
				throw new SyncException(clientTimestamp);
		}*/
		public class SyncTiny {
			public long? ClientTimestamp { get; set; }
			public DateTime DbTimestamp { get; set; }
			public long Id { get; set; }
		}

		public static String NO_SYNC_EXCEPTION = "noSyncException";

		///// <summary>
		///// Must be called atomically. do not call within a session, always call this first.
		///// </summary>
		///// <param name="caller"></param>
		///// <param name="action"></param>
		///// <param name="noSyncException"></param>
		///// <returns></returns>
		//public static bool EnsureStrictlyAfter(UserOrganizationModel caller, SyncAction action, bool noSyncException = false) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction(LockMode.)) {
		//		//	EnsureStrictlyAfter(caller, s, action, noSyncException);
		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//}

		//Must happen atomically...
		public static bool EnsureStrictlyAfter(UserOrganizationModel caller, ISession s, SyncAction action, bool noSyncException = false) {
			var shouldThrowSyncException = !noSyncException;
			try {
				if (shouldThrowSyncException) {
					if (HttpContext.Current != null && HttpContext.Current.Items != null && HttpContext.Current.Items.Contains(NO_SYNC_EXCEPTION)) {
						if ((bool)HttpContext.Current.Items[NO_SYNC_EXCEPTION])
							noSyncException = true;

					}
				}
			} catch (Exception e) {
				int a = 0;
			}


			//Required again after all the short circuits
			shouldThrowSyncException = !noSyncException;
			try {
				//var now = DateTime.UtcNow;
				//var after = now.Subtract(Buffer);
				var actionStr = action.ToString();
				var clientTimestamp = caller._ClientTimestamp;
				var callerId = caller.Id;

				var isAfter = IsStrictlyAfter(s, actionStr, clientTimestamp, callerId, DateTime.UtcNow, Buffer);
				if (isAfter == false) {
					throw new SyncException(clientTimestamp);
				}

				return isAfter;
			} catch (SyncException) {
				if (shouldThrowSyncException) {
					s.Transaction.Commit();//should we be rolling back?
					s.Flush();
					throw;
				}
			} catch (Exception e) {
				log.Error(e);
				throw new SyncException("Sync Exception: " + e.Message, null);
			}
			return false;
		}

		/// <summary>
		/// Must be called atomically, in its own session...
		/// </summary>
		/// <param name="s"></param>
		/// <param name="actionStr"></param>
		/// <param name="clientTimestamp"></param>
		/// <param name="callerId"></param>
		/// <param name="serverTime"></param>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public static bool IsStrictlyAfter(ISession s, string actionStr, long? clientTimestamp, long callerId, DateTime serverTime, TimeSpan buffer) {
			if (clientTimestamp == null) {
				return false;
			}
			
			//Concurrency Lock...
			var lockId = callerId + "@" + actionStr;
			long syncId;
			if (Config.GetEnv() == Models.Enums.Env.local_test_sqlite) {
				var ns = new Sync() {
					CreateTime = serverTime,
					Action = actionStr,
					Timestamp = clientTimestamp.Value,
					UserId = callerId,
				};
				s.Save(ns);
				syncId = ns.Id;
			} else {
				using (var s2 = HibernateSession.GetDatabaseSessionFactory().OpenSession()) {
					using (var tx = s.BeginTransaction()) {
						var syncLock = s2.Get<SyncLock>(lockId, LockMode.Upgrade);
						if (syncLock == null) {
							try {
								s2.Save(new SyncLock() { Id = lockId,});
								tx.Commit();
								s2.Flush();
							} catch (GenericADOException e) {
								//save a duplicate key i guess...
								tx.Rollback();
							}
						}
					}
					using (var tx = s.BeginTransaction()) {
						var syncLock = s2.Get<SyncLock>(lockId, LockMode.Upgrade);
						syncLock.LastUpdate = DateTime.UtcNow;
						syncLock.UpdateCount += 1;
						s2.Update(syncLock);
						var ns = new Sync() {
							CreateTime = serverTime,
							Action = actionStr,
							Timestamp = clientTimestamp.Value,
							UserId = callerId,
						};
						s.Save(ns);						
						syncId = ns.Id;
						tx.Commit();
						s2.Flush();
					}
				}
			}
			var newSync = s.Get<Sync>(syncId);			

			var newSyncDbTimestamp = newSync.DbTimestamp;
			return IsStrictlyAfter(s, actionStr, clientTimestamp.Value, callerId, newSync, newSyncDbTimestamp, buffer);
		}

		private static bool IsStrictlyAfter(ISession s, string actionStr, long clientTimestamp, long callerId, Sync newSync, DateTime newSyncDbTimestamp, TimeSpan buffer) {
			var after = newSyncDbTimestamp;
			if ((newSyncDbTimestamp - DateTime.MinValue) > buffer)
				after = newSyncDbTimestamp.Subtract(buffer);
			s.Flush();
			var syncsUnfiltered = s.QueryOver<Sync>()
				.Where(x => x.DeleteTime == null && x.DbTimestamp >= after && x.Action == actionStr && x.UserId == callerId)
				.Select(x => x.Timestamp, x => x.DbTimestamp, x => x.Id)
				.List<object[]>()
				.Select(x => new SyncTiny {
					Id = (long)x[2],
					ClientTimestamp = (long)x[0],
					DbTimestamp = (DateTime)x[1]
				});
			var syncs=syncsUnfiltered.Where(x => x.Id != newSync.Id)
				.ToList();

			///Debug info
			//var builder = "[" + clientTimestamp + "] Syncs:";
			//foreach (var a in syncs) {
			//	builder += a.Id+", ";
			//}
			//log.Info(builder);


			if (!IsStrictlyAfter(clientTimestamp, syncs)) {
				return false;
			}
			return true;
		}

		private static bool IsStrictlyAfter(long clientTimestamp, List<SyncTiny> existingSyncs) {
			return existingSyncs.All(x => x.ClientTimestamp - clientTimestamp <= 0);
		}
	}
}