﻿using System;
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
using RadialReview.Models.Enums;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using RadialReview.Utilities.NHibernate;

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

        public static string UserActionKey(UserOrganizationModel caller, SyncAction action) {
            return (caller.GetClientRequestId()) + "_" + action.ToString();
        }

        public static async Task<bool> EnsureStrictlyAfter(UserOrganizationModel caller, SyncAction action, Func<IOrderedSession, Task> atomic, bool noSyncException = false) {
            var shouldThrowSyncException = !noSyncException;
            try {
                if (shouldThrowSyncException && HttpContext.Current != null && HttpContext.Current.Items != null && HttpContext.Current.Items.Contains(NO_SYNC_EXCEPTION) && (bool)HttpContext.Current.Items[NO_SYNC_EXCEPTION]) {
                    noSyncException = true;
                }
            } catch (Exception e) {
            }
            //Required again after all the short circuits
            shouldThrowSyncException = !noSyncException;

            var clientUpdateTime = caller._ClientTimestamp;

            var hasError = false;
            var hasWarning = false;

            if (clientUpdateTime == null) {
                hasWarning = true;
            }

            //Ensure only one...
            await SyncUtil.Lock(UserActionKey(caller, action), clientUpdateTime, async (s, lck) => {

                var canUpdate = lck.LastClientUpdateTimeMs == null;
                canUpdate = canUpdate || clientUpdateTime.Value - lck.LastClientUpdateTimeMs.Value > 0;
                canUpdate = canUpdate || lck.LastUpdateDb.Add(TimeSpan.FromMinutes(1)) < DateTime.UtcNow;

                if (lck.LastClientUpdateTimeMs == null || clientUpdateTime.Value - lck.LastClientUpdateTimeMs.Value > 0) {
                    await atomic(s);
                } else {
                    hasError = true;
                }
            });
            if (shouldThrowSyncException && hasError)
                throw new SyncException("Out of sync", clientUpdateTime);
            if (Config.IsLocal() && hasWarning)
                throw new SyncException("Client timestamp was null. Make sure timestamp is sent or you'll have issues.", clientUpdateTime);


            return !hasError && !hasWarning;
        }

        //Must happen atomically...
        /*[Obsolete("Doesnt work", true)]
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
        */
        //[Obsolete("Must call outside of a session")]
        //public static SyncLock GenerateSyncLock(string lockCategory,string id) {
        //    while (true) {
        //        using (var s = HibernateSession.GetCurrentSession()) {
        //            try {
        //                using (var tx = s.BeginTransaction(IsolationLevel.Serializable)) {
        //                    var lck = s.Get<SyncLock>(id);
        //                    if (lck == null) {
        //                        lck = new SyncLock() { Id = id };
        //                        s.Save(lck);
        //                    }
        //                    tx.Commit();
        //                    return lck;
        //                }
        //            } catch (GenericADOException ex) {

        //                // SQL-Server specific code for identifying deadlocks
        //                var sqlEx = ex.InnerException as SqlException;
        //                if (sqlEx == null || sqlEx.Number != 1205) {
        //                    Console.WriteLine("Unhandled SyncLock error");
        //                    throw;
        //                }
        //                Console.WriteLine("Handled Deadlock");
        //                // Deadlock, just try again by letting the loop go on (eventually log it).
        //            }
        //        }
        //    }
        //}

        public class TestHooks {
            public Action AfterLock { get; set; }
            public Action AfterUnlock { get; set; }
            public Action BeforeLock { get; set; }
            public Action BeforeUnlock { get; set; }
        }
        private static object TEST_LOCK = new object();
        private static object TEST_UNLOCK = new object();

        public static async Task Lock(string key, long? clientUpdateTimeMs, Func<ISession, SyncLock, Task> atomic, TestHooks testHooks = null) {
            var nil = Lock(key, clientUpdateTimeMs, async (s, lck) => { await atomic(s, lck); return false; }, testHooks);
        }

        public static async Task<T> Lock<T>(string key, long? clientUpdateTimeMs, Func<ISession, SyncLock, Task<T>> atomic, TestHooks testHooks = null) {
            if (clientUpdateTimeMs == null) {
                //probably want to make sure its not null...
                int a = 0;
            }

            //Make sure lock key exists
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    if (s is SingleRequestSession) {
                        var srs = (SingleRequestSession)s;
                        if (srs.GetCurrentContext().Depth != 0)
                            throw new Exception("Lock must be called outside of a session.");
                    }

                    var found = s.Get<SyncLock>(key, LockMode.Upgrade);
                    if (found == null) {
                        //Didnt exists. Lets atomically create it
                        //LockMode.Upgrade prevents creating simultaniously 
                        var createLock = s.Get<SyncLock>(SyncLock.CREATE_KEY, LockMode.Upgrade);
                        if (createLock == null)
                            throw new Exception("CreateLock doesnt exist. Call ApplicationAccessor.EnsureExists()");

                        //was it created in another thread while we were locked?
                        if (s.Get<SyncLock>(key, LockMode.Upgrade) != null) {
                            //was already created in another thread....
                        } else {
                            //doesn't exist. Lets create it..
                            s.Save(new SyncLock() {
                                Id = key,
                            });
                        }
                        s.Flush();
                        tx.Commit();
                    }
                }
            }
            T result = default(T);
            //Lets lock on the thing we just created..
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    //LOCK
                    SyncLock lck;
                    if (testHooks != null && testHooks.AfterLock != null) {
                        //If we are testing, we need BeforeLock and Get to be atomic
                        lock (TEST_LOCK) {
                            testHooks.BeforeLock();
                            lck = s.Get<SyncLock>(key, LockMode.Upgrade);
                            testHooks.AfterLock();
                            //Allowed to lock here since Get<> is atomic
                        }
                    } else {
                        lck = s.Get<SyncLock>(key, LockMode.Upgrade); //Actual db lock happens on this line.
                    }

                    var now = DateTime.UtcNow;
                    //atomic action here
                    result = await atomic(s, lck);

                   // lck.LastUpdate = now;
                    lck.LastClientUpdateTimeMs = clientUpdateTimeMs ?? lck.LastClientUpdateTimeMs;

                    lck.UpdateCount += 1;
                    s.Update(lck);


                    if (testHooks != null && testHooks.BeforeUnlock != null) {
                        //If we are testing, we need Commit and AfterLock to be atomic
                        lock (TEST_UNLOCK) {
                            testHooks.BeforeUnlock();
                            tx.Commit();
                            testHooks.AfterUnlock();
                            //Allowed to lock here since Commit is atomic
                        }
                    } else {
                        tx.Commit();//Actual db unlock happens on this line.
                    }

                    s.Flush();
                    //unlock
                }
            }
            return result;
        }


        //public static void Semaphore(ISession s, string category, string lockId) {
        //    var ctg = s.Get<SyncLock>(category);
        //    if (ctg == null) {
        //        s.Save()
        //       }

        //    s.Lock(ctg, LockMode.None);

        //    var lck = s.Get<SyncLock>(id, LockMode.Upgrade);


        //    if (lck == null) {
        //        //throw new Exception("Semephore does not exist. Call GenerateSyncLock first.");
        //        s.Transaction.

        //    }

        //}


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
        ///
        [Obsolete("Doesnt work", true)]
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
                                s2.Save(new SyncLock() { Id = lockId, });
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

        [Obsolete("Doesnt work", true)]
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
            var syncs = syncsUnfiltered.Where(x => x.Id != newSync.Id)
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

        [Obsolete("Doesnt work", true)]
        private static bool IsStrictlyAfter(long clientTimestamp, List<SyncTiny> existingSyncs) {
            return existingSyncs.All(x => x.ClientTimestamp - clientTimestamp <= 0);
        }
    }
}