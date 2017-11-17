using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using RadialReview.Utilities;
using RadialReview;
using RadialReview.Models.Synchronize;
using System.Threading;

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
		public void EnsureSyncWorks_SQLite() {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var sync = new Sync();
					var now = DateTime.UtcNow;
					s.Save(sync);
					var dbTS = sync.DbTimestamp;
					var diff = (dbTS - now);
					Console.WriteLine(diff.TotalMilliseconds + "ms");
					Assert.IsTrue(TimeSpan.FromSeconds(-1) < diff );
					Assert.IsTrue(diff < TimeSpan.FromSeconds(1));

					Thread.Sleep(1);
					var sync2 = new Sync();
					s.Save(sync2);

					var diff2 = (sync.DbTimestamp - sync2.DbTimestamp);
					Console.WriteLine(diff2.TotalMilliseconds + "ms");
					Assert.IsTrue(sync.DbTimestamp < sync2.DbTimestamp);



				}
			}
		}
	}
}
