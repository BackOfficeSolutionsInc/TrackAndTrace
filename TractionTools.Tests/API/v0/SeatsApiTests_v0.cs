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

namespace TractionTools.Tests.API.v0 {
	[TestClass]
	public class SeatsApiTests_v0 : BaseTest {
		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestAttachDirectReport() {
			var c = await Ctx.Build();

			SeatsController seatController = new SeatsController();
			seatController.MockUser(c.Manager);

			//var outParam = new UserOrganizationModel();
			var firstName = "Test1";
			var lastName = "Test2";
			var model = new CreateUserOrganizationViewModel() { FirstName = firstName, LastName = lastName, OrgId = c.E1.Organization.Id, Email = "test@test.com", SendEmail = c.E1.Organization.SendEmailImmediately };
			MockHttpContext();
			var result = await JoinOrganizationAccessor.CreateUserUnderManager(c.E1, model);

			var attachSeat = seatController.AttachDirectReport(c.Org.E1MiddleNode.Id, result.User.Id); // nodeId is seatId
			var getSeat = seatController.GetSeat(attachSeat.Id);
			Assert.AreEqual(getSeat.Id, attachSeat.Id);
		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetSeat() {
			var c = await Ctx.Build();
			SeatsController seatController = new SeatsController();
			seatController.MockUser(c.E1);
			var getResult = seatController.GetSeat(c.Org.E1MiddleNode.Id); // nodeId is seatId
			Assert.AreEqual(c.Org.E1MiddleNode.Id, getResult.Id);
		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestRemoveSeat() {
			var c = await Ctx.Build();
			SeatsController seatController = new SeatsController();
			seatController.MockUser(c.Manager);
			//var outParam = new UserOrganizationModel();
			var firstName = "Test1";
			var lastName = "Test2";
			var model = new CreateUserOrganizationViewModel() { FirstName = firstName, LastName = lastName, OrgId = c.E1.Organization.Id, Email = "test@test.com", SendEmail = c.E1.Organization.SendEmailImmediately };
			MockHttpContext();
			var result = await JoinOrganizationAccessor.CreateUserUnderManager(c.E1, model);
			var attachSeat = seatController.AttachDirectReport(c.Org.E1MiddleNode.Id, result.User.Id); // nodeId is seatId
			//remove seat
			seatController.RemoveSeat(attachSeat.Id);
			var getSeat = AccountabilityAccessor.GetNodeById(c.E1, attachSeat.Id, false);
			Assert.IsNotNull(getSeat.DeleteTime);
		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetPosition() {
			var c = await Ctx.Build();
			SeatsController seatController = new SeatsController();
			seatController.MockUser(c.E1);
			var getPosition = AccountabilityAccessor.GetNodeById(c.E1, c.Org.MiddleNode.Id);
			Assert.IsNotNull(getPosition);
		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestAttachPosition() // breaking
		{
			var c = await Ctx.Build();
			SeatsController seatController = new SeatsController();
			seatController.MockUser(c.Manager);

			OrganizationAccessor _accessor = new OrganizationAccessor();
			//var outParam = new UserOrganizationModel();
			var firstName = "Test1";
			var lastName = "Test2";
			var model = new CreateUserOrganizationViewModel() { FirstName = firstName, LastName = lastName, OrgId = c.E1.Organization.Id, Email = "test@test.com", SendEmail = c.E1.Organization.SendEmailImmediately };
			MockHttpContext();
			var result = await JoinOrganizationAccessor.CreateUserUnderManager(c.E1, model);
			MockHttpContext();
			var attachSeat = seatController.AttachDirectReport(c.Org.E1MiddleNode.Id, result.User.Id);

			var createPosition = _accessor.EditOrganizationPosition(c.E1, 0, result.User.Organization.Id, "TestPosition");

			// AttachPosition(attachSeat.Id, createPosition.Id);
			//attach position
			MockHttpContext();
			seatController.AttachPosition(attachSeat.Id, createPosition.Id);
			var getUserPosition = PositionAccessor.GetPositionModelForUser(c.E1, result.User.Id);
			Assert.AreEqual(createPosition.Id, getUserPosition.FirstOrDefault().Id);
		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestRemovePosition() {
			var c = await Ctx.Build();
			SeatsController seatController = new SeatsController();
			seatController.MockUser(c.Manager);
			OrganizationAccessor _accessor = new OrganizationAccessor();
			//var outParam = new UserOrganizationModel();
			var firstName = "Test1";
			var lastName = "Test2";
			var model = new CreateUserOrganizationViewModel() { FirstName = firstName, LastName = lastName, OrgId = c.E1.Organization.Id, Email = "test@test.com", SendEmail = c.E1.Organization.SendEmailImmediately };
			MockHttpContext();
			var result = await JoinOrganizationAccessor.CreateUserUnderManager(c.E1, model);
			var attachSeat = seatController.AttachDirectReport(c.Org.E1MiddleNode.Id, result.User.Id);
			var createPosition = _accessor.EditOrganizationPosition(c.E1, 0, result.User.Organization.Id, "TestPosition");
			//attach position
			seatController.AttachPosition(attachSeat.Id, createPosition.Id);

			//remove position
			seatController.RemovePosition(attachSeat.Id);
			var getUserPosition = PositionAccessor.GetPositionModelForUser(c.E1, result.User.Id);
			Assert.AreEqual(0, getUserPosition.Count());
		} // breaking

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetSeatUser() {
			var c = await Ctx.Build();
			SeatsController seatController = new SeatsController();
			seatController.MockUser(c.Manager);

			OrganizationAccessor _accessor = new OrganizationAccessor();
			//var outParam = new UserOrganizationModel();
			var firstName = "Test1";
			var lastName = "Test2";
			var model = new CreateUserOrganizationViewModel() { FirstName = firstName, LastName = lastName, OrgId = c.E1.Organization.Id, Email = "test@test.com", SendEmail = c.E1.Organization.SendEmailImmediately };
			MockHttpContext();
			var result =await JoinOrganizationAccessor.CreateUserUnderManager(c.E1, model);
			var attachSeat = seatController.AttachDirectReport(c.Org.E1MiddleNode.Id, result.User.Id);
			var getUser = seatController.GetSeatUser(attachSeat.Id);
			Assert.AreEqual(result.User.Id, getUser.Id);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestAttachUser() {
			var c = await Ctx.Build();
			SeatsController seatController = new SeatsController();
			seatController.MockUser(c.Manager);

			OrganizationAccessor _accessor = new OrganizationAccessor();
			//var outParam = new UserOrganizationModel();
			var firstName = "Test1";
			var lastName = "Test2";
			var model = new CreateUserOrganizationViewModel() { FirstName = firstName, LastName = lastName, OrgId = c.E1.Organization.Id, Email = "test@test.com", SendEmail = c.E1.Organization.SendEmailImmediately };
			MockHttpContext();
			var result = await JoinOrganizationAccessor.CreateUserUnderManager(c.E1, model);

			seatController.AttachUser(c.Org.E1MiddleNode.Id, result.User.Id);
			var getUser = seatController.GetSeatUser(c.Org.E1MiddleNode.Id);
			Assert.AreEqual(result.User.Id, getUser.Id);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestDetachUser() {
			var c = await Ctx.Build();
			SeatsController seatController = new SeatsController();
			seatController.MockUser(c.Manager);
			OrganizationAccessor _accessor = new OrganizationAccessor();
			//var outParam = new UserOrganizationModel();
			var firstName = "Test1";
			var lastName = "Test2";
			var model = new CreateUserOrganizationViewModel() { FirstName = firstName, LastName = lastName, OrgId = c.E1.Organization.Id, Email = "test@test.com", SendEmail = c.E1.Organization.SendEmailImmediately };
			MockHttpContext();
			var result = await JoinOrganizationAccessor.CreateUserUnderManager(c.E1, model);

			//attach user
			seatController.AttachUser(c.Org.E1MiddleNode.Id, result.User.Id);

			//remove user
			seatController.DetachUser(c.Org.E1MiddleNode.Id);
			var getUser = seatController.GetSeatUser(c.Org.E1MiddleNode.Id);
			Assert.AreNotEqual(result.User.Id, getUser.Id);
		}
	}
}
