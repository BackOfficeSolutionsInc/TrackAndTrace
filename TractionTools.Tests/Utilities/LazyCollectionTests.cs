using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities.DataTypes;
using System.Collections;
using System.Collections.Generic;
using RadialReview;
using RadialReview.Accessors;
using Moq;
using RadialReview.Models.UserModels;
using NHibernate;
using System.Linq.Expressions;
using NHibernate.Criterion;
using NHibernate.Criterion.Lambda;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using RadialReview.Models.Askables;
using TractionTools.Tests.TestUtils;
using System.Dynamic;
using System.Linq;

namespace TractionTools.Tests.Utilities {
    [TestClass]
    public class LazyCollectionTests : BaseTest {
        private IEnumerable<T> GenLazyList<T>() where T : new() {
            yield return new T();
            yield return new T();
            yield return new T();

        }
        [TestMethod]
        public void EnsureNotResolved() {
            IEnumerable<object> list = GenLazyList<object>();
            var lazy = new LazyCollection<object>(list);
            Assert.IsFalse(lazy.IsResolved());
            var count = lazy.Count;
            Assert.AreEqual(3, count);
            Assert.IsTrue(lazy.IsResolved());
        }
        [TestMethod]
        public void ToLazyCollection() {
            IEnumerable<object> list = GenLazyList<object>();
            var lazy = list.ToLazyCollection();
            Assert.IsFalse(lazy.IsResolved());
            var count = lazy.Count;
            Assert.AreEqual(3, count);
            Assert.IsTrue(lazy.IsResolved());
        }
        //public Mock<IQueryOver<T,T>> GenMock<T>() where T:new(){
        //    var mock = new Mock<IQueryOver<T, T>>();
        //    mock.Setup(s => s.Where(It.IsAny<Expression<Func<T, bool>>>())).Returns(() => mock.Object);
        //    mock.Setup(s => s.Future()).Returns(() => GenLazyList<T>());
        //    return mock;
        //}

        [TestMethod]
        public void TestRoleLinksLazyResolved() {
            //var mockSession = new MockSession();
            //mockSession.AddQueryOver<TeamDurationModel>().AddFuture();
            //mockSession.AddQueryOver<PositionDurationModel>().AddFuture();
            //mockSession.AddQueryOver<RoleLink>().AddFuture();
            var org = OrgUtil.CreateOrganization("RoleLinks");
            using (CompareUtil.StaticComparer<RoleLink, int>("CtorCalls", x => x + 1)) {
                DbCommit(s => {
                    var roleLink = new RoleLink() {
                        OrganizationId = org.Id,
                        CreateTime = new DateTime(2017, 6, 13)
                    };
                    s.Save(roleLink);
                });
            }


            DbExecute(s => {
                using (var comp = CompareUtil.StaticComparer<RoleLink, int>("CtorCalls", x => x+1)) {
                    var model = RoleAccessor.GetRolesForOrganization_Unsafe(s, org.Id, new DateRange(new DateTime(2017, 6, 12), new DateTime(2017, 6, 14)));
                    comp.Assert(x => x);
                    var result = model.GetRoleDetailsForUser(1);
                }
            });
        }

    }
}
