using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.TestUtils;
using RadialReview.Api.V1;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using System.Threading.Tasks;
using RadialReview.Exceptions;
using RadialReview;

namespace TractionTools.Tests.API.v1 {
    /// <summary>
    /// Summary description for UserApiTests_v1
    /// </summary>
    [TestClass]
    public class UserApiTests_v1 : BaseApiTest {
		public UserApiTests_v1() : base(VERSION_1) {
        }
        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task GetUser() {
            var ctx = await Ctx.Build();
            var c = new Users_Controller();
            c.MockUser(ctx.E1);
            Throws<PermissionsException>(() => c.GetUser(ctx.OtherOrg.Employee.Id));

            var found = c.GetUser(ctx.E1.Id);
            CompareModelProperties(found);
        }
        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task GetMine() {
            var ctx = await Ctx.Build();
            var c = new Users_Controller();
            c.MockUser(ctx.E1);
            Throws<PermissionsException>(() => c.GetUser(ctx.OtherOrg.Employee.Id));

            var found = c.GetMineUser();
            CompareModelProperties(found);

            Assert.AreEqual(ctx.E1.GetImageUrl(), found.ImageUrl);
            Assert.AreEqual(ctx.E1.GetInitials(), found.Initials);
            Assert.AreEqual(ctx.E1.GetName(), found.Name);
            Assert.AreEqual(ctx.E1.GetImageUrl(), found.ImageUrl);
        }
    }
}
