﻿using System;
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
using TractionTools.Tests.Properties;

namespace TractionTools.Tests.API.v0 {
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
            CompareModelProperties(APIResult.PositionApiTests_v0_TestGetMinePosition, getMinePosition);
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
			var result = await positionController.AddPositionRoles(createPosition.Id, "Test Position");
            CompareModelProperties(APIResult.PositionApiTests_v0_TestUpdatePositionRoles, result);
            var getRole = RoleAccessor.GetRoleById(c.E1, result.Id);
			Assert.AreEqual(result.Id, getRole.Id);
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
            CompareModelProperties(APIResult.PositionApiTests_v0_TestCreatePosition, createPosition);
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
            CompareModelProperties(APIResult.PositionApiTests_v0_TestGetPositions, getPosition);
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
            CompareModelProperties(APIResult.PositionApiTests_v0_TestUpdatePositions, UpdatePosition);
            var getPosition = positionController.GetPositions(createPosition.Id);
			Assert.AreEqual(nameUpdated, getPosition.Name);
		}
	}
}
