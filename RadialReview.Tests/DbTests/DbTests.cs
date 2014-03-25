using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models;
using System.Collections.Generic;
using RadialReview.Utilities;
using System.Diagnostics;
using System.Text;
using RadialReview.Tests.Utilities;

namespace RadialReview.Tests
{
    [TestClass]
    public class DbTests
    {
        [TestMethod]
        public void TestGetSubordinateModels()
        {
            var start = DateTime.UtcNow;
            var cur = start;
            var deepAcc = new DeepSubordianteAccessor();
            var forUsers = new List<long>();

            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    forUsers.AddRange(s.QueryOver<UserOrganizationModel>().List().ToListAlive().Select(x => x.Id));
                }
            }

            var r = Repeater.Repeate(10, forUsers, uid => deepAcc.GetSubordinatesAndSelfModels(UserOrganizationModel.ADMIN, uid).Count());
            Assert.IsTrue(r.IsBounded(5.0, 1.5));
        }

        [TestMethod]
        public void TestGetSubordinateIds()
        {
            var start = DateTime.UtcNow;
            var cur = start;
            var deepAcc = new DeepSubordianteAccessor();
            var forUsers = new List<long>();

            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    forUsers.AddRange(s.QueryOver<UserOrganizationModel>().List().ToListAlive().Select(x => x.Id));
                }
            }

            var r = Repeater.Repeate(10, forUsers, uid => deepAcc.GetSubordinatesAndSelf(UserOrganizationModel.ADMIN, uid).Count());
            Assert.IsTrue(r.IsBounded(2.1, 0.2));
        }
    }
}
