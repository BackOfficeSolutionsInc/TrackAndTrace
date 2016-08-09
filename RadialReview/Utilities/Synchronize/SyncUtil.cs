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

namespace RadialReview.Utilities.Synchronize
{

	public class SyncUtil
	{
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

		public static bool EnsureStrictlyAfter(UserOrganizationModel caller, ISession s, SyncAction action,bool noSyncException=false)
		{
			try{
				var now = DateTime.UtcNow;
				var after = now.Subtract(Buffer);
				var actionStr = action.ToString();
				var clientTimestamp = caller._ClientTimestamp;

				if (clientTimestamp == null){
					throw new SyncException(null);
				}
				var syncs = s.QueryOver<Sync>()
					.Where(x => x.DeleteTime == null && x.CreateTime >= after && x.Action == actionStr && x.UserId==caller.Id)
					.Select(x => x.Timestamp,x=>x.CreateTime)
					.List<object[]>()
					.Select(x=>new {ClientTimestamp = (long)x[0]- clientTimestamp, ServerTimestamp = ((DateTime)x[1]).ToJavascriptMilliseconds()})
					.ToList();

				s.Save(new Sync(){
					CreateTime = now,
					Action = actionStr,
					Timestamp = clientTimestamp.Value,
					UserId = caller.Id,
				});

				if (!syncs.All(x => x.ClientTimestamp < 0)){
					s.Transaction.Commit();
					s.Flush();
					throw new SyncException(clientTimestamp);
				}
                return true;
			}
			catch (SyncException){
				if (!noSyncException)
					throw;
			}catch (Exception e){
				throw new SyncException("Sync Exception: "+e.Message,null);
			}
            return false;
		}

	}
}