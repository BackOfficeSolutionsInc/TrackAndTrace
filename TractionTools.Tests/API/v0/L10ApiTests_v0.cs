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
using RadialReview.Models.Issues;
using TractionTools.Tests.Properties;
using RadialReview.Models.Angular.Meeting;

namespace TractionTools.Tests.API.v0 {
	[TestClass]
	public class L10ApiTests_v0 : BaseTest {
		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestCreateL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			string l10Name = "Test L10";
			var recurrenceId = await L10.CreateL10(l10Name);
			var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E1, c.Org.Id);

            Assert.AreEqual(recurrenceId, getAllL10RecurrenceAtOrganization.FirstOrDefault().Id);
			Assert.AreEqual(l10Name, getAllL10RecurrenceAtOrganization.FirstOrDefault().Name);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestEditL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);

			var name = "Test L10 Updated";
			var recurrenceId = await L10.CreateL10("Test L10");
            await L10.EditL10(recurrenceId, name);

			var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E1, c.Org.Id);

			Assert.AreEqual(name, getAllL10RecurrenceAtOrganization.FirstOrDefault().Name);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestAttachMeasurableL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			var recurrenceId = await L10.CreateL10("Test L10");
			var m1 = new MeasurableModel() {
				AccountableUser = c.E1,
				AccountableUserId = c.E1.Id,
				AdminUserId = c.E1.Id,
				Title = "Meas1",
				OrganizationId = c.Org.Organization.Id
			};
			//	var measurable = AddMeasurableVm.CreateNewMeasurable(recurrenceId, m1, true);
			MockHttpContext();
			await ScorecardAccessor.CreateMeasurable(c.E1, m1, false);

			var getMeasurablesForRecurrence = L10Accessor.GetScorecardDataForRecurrence(c.E1, recurrenceId);
			Assert.AreEqual(0, getMeasurablesForRecurrence.MeasurablesAndDividers.Count());

			await L10.AttachMeasurableL10(recurrenceId, m1.Id);
			getMeasurablesForRecurrence = L10Accessor.GetScorecardDataForRecurrence(c.E1, recurrenceId);
			Assert.AreEqual(1, getMeasurablesForRecurrence.MeasurablesAndDividers.Count());
			Assert.AreEqual(m1.Title, getMeasurablesForRecurrence.MeasurablesAndDividers.FirstOrDefault().Measurable.Title);
		}



		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestRemoveMeasurableL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();
			L10.MockUser(c.E1);

			var recurrenceId = await L10.CreateL10("Test L10");
			var m1 = new MeasurableModel() {
				AccountableUserId = c.E1.Id,
				AdminUserId = c.E1.Id,
				Title = "Meas1",
				OrganizationId = c.Org.Organization.Id
			};

			var measurable = AddMeasurableVm.CreateNewMeasurable(recurrenceId, m1, true);
			MockHttpContext();
			await L10Accessor.CreateMeasurable(c.E1, recurrenceId, measurable);
			//await L10.AttachMeasurableL10(recurrenceId, measurable.Measurables.FirstOrDefault().Id);

			var getMeasurablesForRecurrence = L10Accessor.GetScorecardDataForRecurrence(c.E1, recurrenceId);
			Assert.AreEqual(1, getMeasurablesForRecurrence.MeasurablesAndDividers.Count());

			await L10.RemoveMeasurableL10(recurrenceId, measurable.Measurables.FirstOrDefault().Id);
			getMeasurablesForRecurrence = L10Accessor.GetScorecardDataForRecurrence(c.E1, recurrenceId);
			Assert.AreEqual(0, getMeasurablesForRecurrence.MeasurablesAndDividers.Count());

		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestAttachRockMeetingL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			var recurrenceId = await L10.CreateL10("Test L10");
			var rock = new RockModel() {
				OrganizationId = c.E1.Organization.Id,
				ForUserId = c.E1.Id,
			};
			MockHttpContext();
			var rockModel = AddRockVm.CreateRock(recurrenceId, rock, true);
			await L10Accessor.CreateRock(c.E1, recurrenceId, rockModel);

            await L10Accessor.CreateRock(c.E1, recurrenceId, rockModel);

            await L10.AttachRockMeetingL10(recurrenceId, rock.Id);

			await L10.AttachRockMeetingL10(recurrenceId, rock.Id);
			var getRocksForRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, recurrenceId);

			Assert.AreEqual(2, getRocksForRecurrence.Count());

		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestRemoveRockL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			var otherRecurrenceId = await L10.CreateL10("Other L10");
			var recurrenceId = await L10.CreateL10("Test L10");
			var rock = new RockModel() {
				OrganizationId = c.E1.Organization.Id,
				ForUserId = c.E1.Id,
			};
			MockHttpContext();
			var rockModel = AddRockVm.CreateRock(otherRecurrenceId, rock, true);
			//Create and add to other
			await L10Accessor.CreateRock(c.E1, otherRecurrenceId, rockModel);
			var getRocksForRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, recurrenceId);
			Assert.AreEqual(0, getRocksForRecurrence.Count());
			var getRocksForOtherRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, otherRecurrenceId);
			Assert.AreEqual(1, getRocksForOtherRecurrence.Count());

			//Attach to primary l10
			await L10.AttachRockMeetingL10(recurrenceId, rock.Id);
			getRocksForRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, recurrenceId);
			Assert.AreEqual(1, getRocksForRecurrence.Count());
			getRocksForOtherRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, otherRecurrenceId);
			Assert.AreEqual(1, getRocksForOtherRecurrence.Count());

            //Remove from primary
            await L10.RemoveRockL10(recurrenceId, rock.Id);
			getRocksForRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, recurrenceId);
			Assert.AreEqual(0, getRocksForRecurrence.Count());
			getRocksForOtherRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, otherRecurrenceId);
			Assert.AreEqual(1, getRocksForOtherRecurrence.Count());

            //Remove from other
            await L10.RemoveRockL10(otherRecurrenceId, rock.Id);
			getRocksForRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, recurrenceId);
			Assert.AreEqual(0, getRocksForRecurrence.Count());
			getRocksForOtherRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, otherRecurrenceId);
			Assert.AreEqual(0, getRocksForOtherRecurrence.Count());
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetMeetingsL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			var recurrenceId = await L10.CreateL10("Test L10");
            AngularRecurrence getMeetingsL10 = L10.GetL10(recurrenceId);

            CompareModelProperties(APIResult.L10ApiTests_v0_TestGetMeetingsL10, getMeetingsL10);
            Assert.AreEqual(recurrenceId, getMeetingsL10.Id);

		}



		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetL10Attendess() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			var recurrenceId = await L10.CreateL10("Test L10");

			var GetL10Attendess = L10.GetL10Attendees(recurrenceId);
			Assert.AreEqual(0, GetL10Attendess.Count());

			await L10Accessor.AddAttendee(c.E1, recurrenceId, c.E1.Id);
			GetL10Attendess = L10.GetL10Attendees(recurrenceId);
            CompareModelProperties(APIResult.L10ApiTests_v0_TestGetL10Attendess, GetL10Attendess);

            Assert.AreEqual(1, GetL10Attendess.Count());

		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestAttachHeadlineMeetingL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			var recurrenceId = await L10.CreateL10("Test L10");
			var headlineModel = new PeopleHeadline() {
				Message = "Test Head Line",
				OrganizationId = c.Org.Id,
				RecurrenceId = recurrenceId,
				_Details = "Test details"
			};
			var getAttachHeadline = L10Accessor.GetHeadlinesForMeeting(c.E1, recurrenceId);
			Assert.AreEqual(0, getAttachHeadline.Count());

			var getHeadline = await L10.CreateHeadlineL10(headlineModel.RecurrenceId, headlineModel.Message, null, headlineModel._Details);

            CompareModelProperties(APIResult.L10ApiTests_v0_TestAttachHeadlineMeetingL10, getHeadline);
            getAttachHeadline = L10Accessor.GetHeadlinesForMeeting(c.E1, recurrenceId);
			Assert.AreEqual(1, getAttachHeadline.Count());
			Assert.AreEqual("Test Head Line", getAttachHeadline.First().Message);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestRemoveHeadlineMeetingL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();
			L10.MockUser(c.E1);
			var recurrenceId = await L10.CreateL10("Test L10");
			var headlineModel = new PeopleHeadline() {
				Message = "Test Head Line",
				OrganizationId = c.Org.Id,
				RecurrenceId = recurrenceId,
				_Details = "Test details",
			};

			//create headline
			var headline = await L10.CreateHeadlineL10(headlineModel.RecurrenceId, headlineModel.Message, null, headlineModel._Details);

			//Get headlines
			var getAttachHeadline = L10Accessor.GetHeadlinesForMeeting(c.E1, recurrenceId);
			Assert.AreEqual(1, getAttachHeadline.Count());

            //Remove headline
            await L10.RemoveHeadlineL10(recurrenceId, headline.Id);
			//Get headlines
			getAttachHeadline = L10Accessor.GetHeadlinesForMeeting(c.E1, recurrenceId);
			Assert.AreEqual(0, getAttachHeadline.Count());

		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestAttachtodoMeetingL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			var reccurenceId = await L10.CreateL10("Test L10");
			var name = "Test To Do Meeting";

			await L10.AttachTodoL10(reccurenceId, name, c.E1.Id);

			var getToDoList = L10Accessor.GetAllTodosForRecurrence(c.E1, reccurenceId);

			Assert.AreEqual(1, getToDoList.Count());
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetList() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			var recurrenceId = await L10.CreateL10("Test L10");
			var m1 = new MeasurableModel() {
				AccountableUserId = c.E1.Id,
				AdminUserId = c.E1.Id,
				Title = "Meas1",
				OrganizationId = c.Org.Organization.Id
			};
			MockHttpContext();
			var measurable = AddMeasurableVm.CreateNewMeasurable(recurrenceId, m1, true);
			//await L10Accessor.CreateMeasurable(c.E1, recurrenceId, measurable);
			var getlist = L10.GetL10List();
			Assert.AreEqual(1, getlist.Count());
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestAttachIssueMeetingL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			var recurrenceId = await L10.CreateL10("Test L10");
			var name = "Test Name For Issue Meeting L10";

			var details = "Test detail For Issue Meeting L10";

			var result= await L10.CreateIssueL10(recurrenceId, name, c.E1.Id, details);
            CompareModelProperties(APIResult.L10ApiTests_v0_TestAttachIssueMeetingL10, result);
            var getIssueMeetingL10 = L10Accessor.GetIssuesForRecurrence(c.E1, recurrenceId, false);

			Assert.AreEqual(1, getIssueMeetingL10.Count());

		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestRemoveIssueL10() {
			var c = await Ctx.Build();
			L10_Controller L10 = new L10_Controller();

			L10.MockUser(c.E1);
			var recurrenceId = await L10.CreateL10("Test L10");
			var name = "Test Name For Issue Meeting L10";

			var details = "Test detail For Issue Meeting L10";

			await L10.CreateIssueL10(recurrenceId, name, c.E1.Id, details);

			var getIssueMeetingL10 = L10Accessor.GetIssuesForRecurrence(c.E1, recurrenceId, false);

            await L10.RemoveIssueL10(recurrenceId, getIssueMeetingL10.FirstOrDefault().Id);

			var getIssueMeetingList = L10Accessor.GetIssuesForRecurrence(c.E1, recurrenceId, false);

			Assert.AreEqual(0, getIssueMeetingList.Count());

		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestGetUserIssues() {
			var c = await Ctx.Build();
			var issue = new IssueModel() {
				Message = "Issue for Test Method",
			};

			var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
			var result = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E1.Id, issue);
			var issue1 = new IssueModel() {
				Message = "Issue for Test Method",
			};

			// creating issue with different owner
			var result1 = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E2.Id, issue1);
			L10_Controller iss = new L10_Controller();
			iss.MockUser(c.E1);
			var _model = iss.GetUserIssues(c.E1.Id, _recurrence.Id);
            CompareModelProperties(APIResult.L10ApiTests_v0_TestGetUserIssues, _model);
            Assert.AreEqual(1, _model.Count());
			Assert.AreEqual(c.E1.Id, _model.First().Owner.Id);
		}
	}
}
