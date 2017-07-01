using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models.Todo;
using RadialReview.Utilities.DataTypes;
using System.Collections.Generic;
using RadialReview.Utilities;
using RadialReview.Models.Enums;
using TractionTools.Tests.TestUtils;
using RadialReview.Models.L10;
using RadialReview.Models;
using System.Linq;
using RadialReview.Api.V0;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Todos;
using RadialReview.Controllers;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Scorecard;
using static RadialReview.Controllers.L10Controller;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.ViewModels;

namespace TractionTools.Tests.Api
{
    [TestClass]
    public class RolesApiTests_v0 : BaseTest
    {

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetRoles() {
			var c = await Ctx.Build();

			RoleController roleController = new RoleController();
            roleController.MockUser(c.E1);
            OrganizationAccessor _accessor = new OrganizationAccessor();
            PositionAccessor posAccessor = new PositionAccessor();
            var createPosition = _accessor.EditOrganizationPosition(c.E1, 0, c.E1.Organization.Id, "TestPosition");

            var addRole = await AccountabilityAccessor.AddRole(c.E1, new Attach(AttachType.Position, createPosition.Id), "TestRole");

            var getRole = roleController.GetRoles(addRole.Id);

            Assert.AreEqual(addRole.Id, getRole.Id);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestUpdateRoles() {
			var c = await Ctx.Build();

			RoleController roleController = new RoleController();
            roleController.MockUser(c.E1);
            OrganizationAccessor _accessor = new OrganizationAccessor();
            PositionAccessor posAccessor = new PositionAccessor();

            var name = "TestRoleUpdated";

            var createPosition = _accessor.EditOrganizationPosition(c.E1, 0, c.E1.Organization.Id, "TestPosition"); // create wropper for this method with CreatePosition
            var addRole = await AccountabilityAccessor.AddRole(c.E1, new Attach(AttachType.Position, createPosition.Id), "TestRole");

            //Updated
            await roleController.UpdateRoles(name, addRole.Id);

            var getRole = roleController.GetRoles(addRole.Id);

            Assert.AreEqual(name, getRole.Name);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestRemoveRoles() {
			var c = await Ctx.Build();

			RoleController roleController = new RoleController();
            roleController.MockUser(c.E1);
            OrganizationAccessor _accessor = new OrganizationAccessor();
            PositionAccessor posAccessor = new PositionAccessor();

            var createPosition = _accessor.EditOrganizationPosition(c.E1, 0, c.E1.Organization.Id, "TestPosition");
            var addRole = await AccountabilityAccessor.AddRole(c.E1, new Attach(AttachType.Position, createPosition.Id), "TestRole");

            var roleList = PositionAccessor.GetPositionRoles(c.E1, createPosition.Id);

            Assert.AreEqual(1, roleList.Count);

            //remove role
            roleController.RemoveRoles(addRole.Id);

            var roles = PositionAccessor.GetPositionRoles(c.E1, createPosition.Id);
            
            Assert.AreEqual(0, roles.Count);
        }
    }
}
