using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using RadialReview.Utilities;
using RadialReview;

namespace TractionTools.Tests.Codebase {
	[TestClass]
	public class TestSqliteDbTime {
		[TestMethod]
		[TestCategory("Codebase")]
		public void EnsureDbTimeWorks_SQLite() {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					Assert.IsTrue(HibernateSession.GetDbTime(s).ToJsMs()>10000);
				}
			}
		}
	}
}
