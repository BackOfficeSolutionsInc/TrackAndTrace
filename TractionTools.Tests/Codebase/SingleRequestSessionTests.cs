using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities;
using TractionTools.Tests.TestUtils;
using RadialReview.Utilities.NHibernate;
using System.Threading;
using System.Threading.Tasks;

namespace TractionTools.Tests.Codebase {
	[TestClass]
	public class SingleRequestSessionTests : BaseTest {

		[TestMethod]
		[TestCategory("Codebase")]
		public void TestDispose() {
			MockHttpContext(false);


			//Do not run, never committed
			{
				var s = HibernateSession.GetCurrentSession();
				Assert.IsInstanceOfType(s, typeof(SingleRequestSession));
				var ss = (SingleRequestSession)s;

				bool wasRun = false;
				bool onlyOnCommit = true;
				ss.RunAfterDispose(new SingleRequestSession.OnDisposedModel(async (ses, tx) => {
					wasRun = true;
					await Task.Delay(0);
				}, onlyOnCommit));
				Assert.IsFalse(wasRun);
				ss.Dispose();
				Assert.IsFalse(wasRun);
			}

			//Should run, flag is false
			{
				var s = HibernateSession.GetCurrentSession();
				var ss = (SingleRequestSession)s;
				bool wasRun = false;
				bool onlyOnCommit = false;
				ss.RunAfterDispose(new SingleRequestSession.OnDisposedModel(async (_s, _tx) => {
					wasRun = true;
					await Task.Delay(0);
				}, onlyOnCommit));
				Assert.IsFalse(wasRun);
				var tx = ss.BeginTransaction();
				//tx.Commit();
				ss.Dispose();
				Assert.IsTrue(wasRun);
			}

			//Should run because it was committed
			{
				var s = HibernateSession.GetCurrentSession();
				var ss = (SingleRequestSession)s;
				bool wasRun = false;
				bool onlyOnCommit = true;
				ss.RunAfterDispose(new SingleRequestSession.OnDisposedModel(async (_s, _tx) => {
					wasRun = true;
					await Task.Delay(0);
				}, onlyOnCommit));
				Assert.IsFalse(wasRun);
				using (var tx = ss.BeginTransaction()) {
					tx.Commit();
				}
				ss.Dispose();
				Assert.IsTrue(wasRun);
			}

			//Shouldn't run, rolledback
			{
				var s = HibernateSession.GetCurrentSession();
				var ss = (SingleRequestSession)s;
				bool wasRun = false;
				bool onlyOnCommit = true;
				ss.RunAfterDispose(new SingleRequestSession.OnDisposedModel(async (_s, _tx) => {
					wasRun = true;
					await Task.Delay(0);
				}, onlyOnCommit));
				Assert.IsFalse(wasRun);
				using (var tx = ss.BeginTransaction()) {
					tx.Rollback();
				}
				ss.Dispose();
				Assert.IsFalse(wasRun);
			}
			//Should run, even though rolledback
			{
				var s = HibernateSession.GetCurrentSession();
				var ss = (SingleRequestSession)s;
				bool wasRun = false;
				bool onlyOnCommit = false;
				ss.RunAfterDispose(new SingleRequestSession.OnDisposedModel(async (_s, _tx) => {
					wasRun = true;
					await Task.Delay(0);
				}, onlyOnCommit));
				Assert.IsFalse(wasRun);
				using (var tx = ss.BeginTransaction()) {
					tx.Rollback();
				}
				ss.Dispose();
				Assert.IsTrue(wasRun);
			}
			
			//Should run, even on exception..
			{
				bool wasRun = false;
				bool onlyOnCommit = false;
				try {
					using (var s = (SingleRequestSession)HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							s.RunAfterDispose(new SingleRequestSession.OnDisposedModel(async (_s, _tx) => {
								wasRun = true;
								await Task.Delay(0);
							}, onlyOnCommit));
							Assert.IsFalse(wasRun);
							throw new Exception();
						}
					}
				} catch (Exception) {
					Assert.IsTrue(wasRun);
				}
				Assert.IsTrue(wasRun);
			}

			//Should not run on exception..
			{
				bool wasRun = false;
				bool onlyOnCommit = true;
				try {
					using (var s = (SingleRequestSession)HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							s.RunAfterDispose(new SingleRequestSession.OnDisposedModel(async (_s, _tx) => {
								wasRun = true;
								await Task.Delay(0);
							}, onlyOnCommit));
							Assert.IsFalse(wasRun);
							throw new Exception();
						}
					}
				} catch (Exception) {
					Assert.IsFalse(wasRun);
				}
				Assert.IsFalse(wasRun);
			}
		}
	}
}
