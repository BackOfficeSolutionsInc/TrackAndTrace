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

namespace RadialReview.Utilities.Synchronize {

	public class SyncUtil {
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
			public long DbTimestamp { get; set; }
			public long Id { get; set; }
		}

		public static String NO_SYNC_EXCEPTION = "noSyncException";

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
				throw new SyncException("Sync Exception: " + e.Message, null);
			}
			return false;
		}

		public static bool IsStrictlyAfter(ISession s, string actionStr, long? clientTimestamp, long callerId, DateTime serverTime, TimeSpan buffer) {
			if (clientTimestamp == null) {
				return false;
			}

			var newSync = new Sync() {
				CreateTime = serverTime,
				Action = actionStr,
				Timestamp = clientTimestamp.Value,
				UserId = callerId,
			};
			s.Save(newSync);

			var newSyncDbTimestamp = newSync.DbTimestamp;
			return IsStrictlyAfter(s, actionStr, clientTimestamp.Value, callerId, newSync, newSyncDbTimestamp, buffer);
		}
		
		private static bool IsStrictlyAfter(ISession s, string actionStr, long clientTimestamp, long callerId, Sync newSync, DateTime newSyncDbTimestamp,TimeSpan buffer) {
			var after = newSyncDbTimestamp.Subtract(buffer);

			var syncs = s.QueryOver<Sync>()
				.Where(x => x.DeleteTime == null && x.DbTimestamp >= after && x.Action == actionStr && x.UserId == callerId)
				.Select(x => x.Timestamp, x => x.DbTimestamp, x => x.Id)
				.List<object[]>()
				.Select(x => new SyncTiny {
					Id = (long)x[2],
					ClientTimestamp = (long)x[0],
					DbTimestamp = ((DateTime)x[1]).ToJavascriptMilliseconds()
				}).Where(x => x.Id != newSync.Id)
				.ToList();

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