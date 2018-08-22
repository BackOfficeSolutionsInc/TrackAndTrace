using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.TestUtils;
using RadialReview.Models.Synchronize;
using RadialReview.Accessors;
using RadialReview;

namespace TractionTools.Tests {
    [TestClass]
    public class DeleteOldSyncLocks : BaseTest{
        [TestMethod]
        public void TestDeleted() {
            var guid = Guid.NewGuid().ToString();
            var sync = new SyncLock() {
                Id = guid,
                LastClientUpdateTimeMs = 100,
            };
            var guid2 = Guid.NewGuid().ToString();
            var sync2 = new SyncLock() {
                Id = guid2,
                LastClientUpdateTimeMs = DateTime.UtcNow.AddDays(1).ToJsMs(),
            };

            DbCommit(s => s.Save(sync));
            DbCommit(s => s.Save(sync2));
            DbQuery(x => Assert.IsNotNull(x.Get<SyncLock>(guid)));
            DbQuery(x => Assert.IsNotNull(x.Get<SyncLock>(guid2)));
            DbCommit(s =>TaskAccessor.DeleteOldSyncLocks(s));
            DbQuery(x => Assert.IsNull(x.Get<SyncLock>(guid)));
            DbQuery(x => Assert.IsNotNull(x.Get<SyncLock>(guid2)));

        }
    }
}
