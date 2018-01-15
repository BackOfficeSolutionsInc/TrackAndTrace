using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using RadialReview.Utilities;
using RadialReview;
using RadialReview.Models.Synchronize;
using System.Threading;
using RadialReview.Models.Enums;

namespace TractionTools.Tests.Codebase {
    [TestClass]
    public class TestSqliteDbTime {
        [TestMethod]
        [TestCategory("Codebase")]
        public void EnsureDbTimeWorks_SQLite() {

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    Assert.IsTrue(HibernateSession.GetDbTime(s).ToJsMs() > 10000);
                }
            }
        }

        [TestMethod]
        [TestCategory("Sync")]
        public void EnsureSyncWorks_MySql() {
            using (HibernateSession.SetDatabaseEnv_TestOnly(Env.local_mysql)) {
                SyncLock sync = null;
                SyncLock sync2 = null;
                using (var s = HibernateSession.GetCurrentSession()) {
                    using (var tx = s.BeginTransaction()) {
                        sync = new SyncLock() {
                            Id = "Test EnsureSyncWorks_MySql" + Guid.NewGuid()
                        };
                        var now = DateTime.Now;
                        s.Save(sync);
                        s.Flush();
                        var dbTS = sync.LastUpdateDb;
                        var diff = (dbTS - now);
                        Console.WriteLine("Clock-vs-DB Diff: " + diff.TotalMilliseconds + "ms");
                        Assert.IsTrue(TimeSpan.FromSeconds(-1) < diff);
                        Assert.IsTrue(diff < TimeSpan.FromSeconds(1));
                        tx.Commit();
                        s.Flush();
                    }
                }
                using (var s = HibernateSession.GetCurrentSession()) {
                    using (var tx = s.BeginTransaction()) {

                        Thread.Sleep(1010);
                        sync2 = new SyncLock() {
                            Id = "Test EnsureSyncWorks_MySql" + Guid.NewGuid()
                        };
                        s.Save(sync2);
                        s.Flush();
                        var diff2 = (sync.LastUpdateDb - sync2.LastUpdateDb);
                        Console.WriteLine("Db-vs-Db clock: " + diff2.TotalMilliseconds + "ms");
                        Assert.IsTrue(sync.LastUpdateDb <= sync2.LastUpdateDb);

                        var oldSync = s.Get<SyncLock>(sync.Id);
                        Assert.IsTrue(oldSync.LastUpdateDb == sync.LastUpdateDb);
                        oldSync.UpdateCount += 1;
                        tx.Commit();
                        s.Flush();
                    }
                }
                using (var s = HibernateSession.GetCurrentSession()) {
                    using (var tx = s.BeginTransaction()) {
                        var oldSync = s.Get<SyncLock>(sync.Id);
                        Assert.IsTrue(oldSync.LastUpdateDb != sync.LastUpdateDb);
                    }
                }
            }
        }
    }
}