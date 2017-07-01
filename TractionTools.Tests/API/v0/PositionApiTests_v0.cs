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

namespace TractionTools.Tests.Api {
	[TestClass]
	public class PositionApiTests_v0 : BaseTest {


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetMinePosition() {
			var c = await Ctx.Build();
			RadialReview.Api.V0.PositionController positionController = new RadialReview.Api.V0.PositionController();
			positionController.MockUser(c.E1);
			OrganizationAccessor _accessor = new OrganizationAccessor();
			PositionAccessor posAccessor = new PositionAccessor();
			var createPosition = _accessor.EditOrganizationPosition(c.E1, 0, c.E1.Organization.Id, "TestPosition");
			MockHttpContext();
			posAccessor.AddPositionToUser(c.E1, c.E1.Id, createPosition.Id);
			var getMinePosition = positionController.GetMinePosition();
			Assert.AreEqual(createPosition.Id, getMinePosition.FirstOrDefault().Id);
		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestUpdatePositionRoles() {
			var c = await Ctx.Build();
			RadialReview.Api.V0.PositionController positionController = new RadialReview.Api.V0.PositionController();
			positionController.MockUser(c.E1);
			OrganizationAccessor _accessor = new OrganizationAccessor();
			PositionAccessor posAccessor = new PositionAccessor();
			var createPosition = _accessor.EditOrganizationPosition(c.E1, 0, c.E1.Organization.Id, "TestPosition");
			var addRole = await positionController.AddPositionRoles(createPosition.Id, "Test Position");
			var getRole = RoleAccessor.GetRoleById(c.E1, addRole.Id);
			Assert.AreEqual(addRole.Id, getRole.Id);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestCreatePosition() {
			var c = await Ctx.Build();
			RadialReview.Api.V0.PositionController positionController = new RadialReview.Api.V0.PositionController();
			positionController.MockUser(c.E1);
			OrganizationAccessor _accessor = new OrganizationAccessor();
			PositionAccessor posAccessor = new PositionAccessor();
			var createPosition = positionController.CreatePosition("TestPosition");
			var getPosition = positionController.GetPositions(createPosition.Id);
			Assert.AreEqual(createPosition.Id, getPosition.Id);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetPositions() {
			var c = await Ctx.Build();
			RadialReview.Api.V0.PositionController positionController = new RadialReview.Api.V0.PositionController();
			positionController.MockUser(c.E1);
			OrganizationAccessor _accessor = new OrganizationAccessor();
			PositionAccessor posAccessor = new PositionAccessor();
			var createPosition = positionController.CreatePosition("TestPosition");
			var getPosition = positionController.GetPositions(createPosition.Id);
			Assert.AreEqual(createPosition.Id, getPosition.Id);
		}



		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestUpdatePositions() {
			var c = await Ctx.Build();
			RadialReview.Api.V0.PositionController positionController = new RadialReview.Api.V0.PositionController();
			positionController.MockUser(c.E1);
			OrganizationAccessor _accessor = new OrganizationAccessor();
			PositionAccessor posAccessor = new PositionAccessor();
			var nameUpdated = "TestPosition_Updated";
			var createPosition = positionController.CreatePosition("TestPosition");
			var UpdatePosition = positionController.UpdatePositions(nameUpdated, createPosition.Id);
			var getPosition = positionController.GetPositions(createPosition.Id);
			Assert.AreEqual(nameUpdated, getPosition.Name);
		}
	}
}
