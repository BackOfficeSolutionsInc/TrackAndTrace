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

namespace TractionTools.Tests.Api {
	[TestClass]
	public class UsersApiTests_v0 : BaseTest {
		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestCreateUsers() {
			var c = await Ctx.Build();
			UsersController userController = new UsersController();
			userController.MockUser(c.E1);
			var firstName = "Test1";
			var lastName = "Test2";
			MockHttpContext();
			var getResult = await userController.CreateUser(firstName, lastName, "test@test.com", c.Org.Id);
			Assert.AreNotEqual(0, getResult.Id);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetUser() {
			var c = await Ctx.Build();
			UsersController userController = new UsersController();
			userController.MockUser(c.E1);
			var getUserDetails = userController.GetUser(c.E1.Id);

			Assert.AreEqual(c.E1.Id, getUserDetails.Id);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestDeleteUsers() {
			var c = await Ctx.Build();
			UsersController userController = new UsersController();
			userController.MockUser(c.Manager);
			var firstName = "Test1";
			var lastName = "Test2";
			MockHttpContext();
			var getResult = await userController.CreateUser(firstName, lastName, "test@test.com", c.Org.Id);
			var getDeletedUser = await userController.DeleteUsers(getResult.Id);
			var userModel = new UserAccessor().GetUserOrganization(c.Manager, getResult.Id, false, false);
			Assert.IsNotNull(userModel.DeleteTime);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetUserRoles() {
			var c = await Ctx.Build();
			UsersController userController = new UsersController();
			userController.MockUser(c.E1);
			var createRole = await AccountabilityAccessor.AddRole(c.E1, new Attach(AttachType.User, c.E1.Id));
			var getRoles = userController.GetUserRoles(c.E1.Id);
			Assert.AreEqual(1, getRoles.Count());
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetUserPositions() {
			var c = await Ctx.Build();
			UsersController userController = new UsersController();
			userController.MockUser(c.E1);
			OrganizationAccessor _accessor = new OrganizationAccessor();
			PositionAccessor posAccessor = new PositionAccessor();
			var createPosition = _accessor.EditOrganizationPosition(c.E1, 0, c.E1.Organization.Id, "TestPosition");
			MockHttpContext();
			posAccessor.AddPositionToUser(c.E1, c.E1.Id, createPosition.Id);
			var getPosition = userController.GetUserPositions(c.E1.Id);
			Assert.AreEqual(1, getPosition.Count());
		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetDirectReports() {
			var c = await Ctx.Build();
			UsersController userController = new UsersController();
			userController.MockUser(c.E1);
			var getDirectReport = userController.GetDirectReports(c.E1.Id);
			Assert.AreEqual(2, getDirectReport.Count());
		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetSupervisors() {
			var c = await Ctx.Build();
			UsersController userController = new UsersController();
			userController.MockUser(c.E1);
			var getSupervisors = userController.GetSupervisors(c.E1.Id);
			Assert.AreEqual(2, getSupervisors.Count());
		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetSeats() {
			var c = await Ctx.Build();
			UsersController userController = new UsersController();
			userController.MockUser(c.E1);
			var getSeats = userController.GetSeats(c.E1.Id);
			Assert.AreEqual(2, getSeats.Count());
		}
	}
}
