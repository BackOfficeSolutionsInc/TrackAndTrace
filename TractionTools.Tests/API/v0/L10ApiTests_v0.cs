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

namespace TractionTools.Tests.Api
{
    [TestClass]
    public class L10ApiTests_v0 : BaseTest
    {
        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestCreateL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var recurrenceId = L10.CreateL10("Test L10");

            var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E1, c.Org.Id);

            Assert.AreEqual(recurrenceId, getAllL10RecurrenceAtOrganization.FirstOrDefault().Id);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestEditL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var name = "Test L10 Updated";

            var recurrenceId = L10.CreateL10("Test L10");

            L10.EditL10(recurrenceId, name);

            var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E1, c.Org.Id);

            Assert.AreEqual(name, getAllL10RecurrenceAtOrganization.FirstOrDefault().Name);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestAttachMeasurableL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var recurrenceId = L10.CreateL10("Test L10");
            var m1 = new MeasurableModel()
            {
                AccountableUserId = c.E1.Id,
                AdminUserId = c.E1.Id,
                Title = "Meas1",
                OrganizationId = c.Org.Organization.Id
            };

            var measurable = AddMeasurableVm.CreateNewMeasurable(recurrenceId, m1, true);

            L10Accessor.CreateMeasurable(c.E1, recurrenceId, measurable);

            L10.AttachMeasurableL10(recurrenceId, m1.Id);

            var getMeasurablesForRecurrence = L10Accessor.GetScoresAndMeasurablesForRecurrence(c.E1, recurrenceId);

            Assert.AreEqual(2, getMeasurablesForRecurrence.MeasurablesAndDividers.Count());

        }



        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestRemoveMeasurableL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var recurrenceId = L10.CreateL10("Test L10");
            var m1 = new MeasurableModel()
            {
                AccountableUserId = c.E1.Id,
                AdminUserId = c.E1.Id,
                Title = "Meas1",
                OrganizationId = c.Org.Organization.Id
            };

            var measurable = AddMeasurableVm.CreateNewMeasurable(recurrenceId, m1, true);

            L10Accessor.CreateMeasurable(c.E1, recurrenceId, measurable);

            L10.AttachMeasurableL10(recurrenceId, measurable.Measurables.FirstOrDefault().Id);

            L10.RemoveMeasurableL10(recurrenceId, measurable.Measurables.FirstOrDefault().Id);

            var getMeasurablesForRecurrence = L10Accessor.GetScoresAndMeasurablesForRecurrence(c.E1, recurrenceId);

            Assert.AreEqual(0, getMeasurablesForRecurrence.MeasurablesAndDividers.Count());

        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestAttachRockMeetingL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var recurrenceId = L10.CreateL10("Test L10");
            var rock = new RockModel()
            {
                OrganizationId = c.E1.Organization.Id,
                ForUserId = c.E1.Id,
            };

            var rockModel = AddRockVm.CreateRock(recurrenceId, rock, true);

            L10Accessor.CreateRock(c.E1, recurrenceId, rockModel);

            L10.AttachRockMeetingL10(recurrenceId, rock.Id);

            var getRocksForRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, recurrenceId);

            Assert.AreEqual(2, getRocksForRecurrence.Count());

        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestRemoveRockL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var reccurenceId = L10.CreateL10("Test L10");
            var rock = new RockModel()
            {
                OrganizationId = c.E1.Organization.Id,
                ForUserId = c.E1.Id,
            };

            var rockModel = AddRockVm.CreateRock(reccurenceId, rock, true);

            L10Accessor.CreateRock(c.E1, reccurenceId, rockModel);

            L10.AttachRockMeetingL10(reccurenceId, rock.Id);

            L10.RemoveRockL10(reccurenceId, rock.Id);

            var getRocksForRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, reccurenceId);

            Assert.AreEqual(0, getRocksForRecurrence.Count());

        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestGetMeetingsL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var recurrenceId = L10.CreateL10("Test L10");

            var getMeetingsL10 = L10.GetL10(recurrenceId);

            Assert.AreEqual(recurrenceId, getMeetingsL10.Id);

        }



        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestGetL10Attendess()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var recurrenceId = L10.CreateL10("Test L10");

            L10Accessor.AddAttendee(c.E1, recurrenceId, c.E1.Id);

            var GetL10Attendess = L10.GetL10Attendees(recurrenceId);

            Assert.AreEqual(1, GetL10Attendess.Count());

        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestAttachHeadlineMeetingL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var recurrenceId = L10.CreateL10("Test L10");

            var headlineModel = new PeopleHeadline()
            {
                Message = "Test Head Line",
                OrganizationId = c.Org.Id,
                RecurrenceId = recurrenceId,
                _Details = "Test details"
            };

            var getHeadline = await L10.AttachHeadlineL10(headlineModel);

            var getAttachHeadline = L10Accessor.GetHeadlinesForMeeting(c.E1, recurrenceId);

            Assert.AreEqual(1, getAttachHeadline.Count());

        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestRemoveHeadlineMeetingL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var recurrenceId = L10.CreateL10("Test L10");

            var headlineModel = new PeopleHeadline()
            {
                Message = "Test Head Line",
                OrganizationId = c.Org.Id,
                RecurrenceId = recurrenceId,
                _Details = "Test details"
            };

            //create headline
            await L10.AttachHeadlineL10(headlineModel);

            L10.RemoveHeadlineL10(recurrenceId, headlineModel.Id);

            var getAttachHeadline = L10Accessor.GetHeadlinesForMeeting(c.E1, recurrenceId);

            Assert.AreEqual(0, getAttachHeadline.Count());

        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestAttachtodoMeetingL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var reccurenceId = L10.CreateL10("Test L10");

            var name = "Test To Do Meeting";

            await L10.AttachTodoL10(reccurenceId, name, c.E1.Id);

            var getToDoList = L10Accessor.GetAllTodosForRecurrence(c.E1, reccurenceId);

            Assert.AreEqual(1, getToDoList.Count());
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestGetList()
        {
            var c = new Ctx();
            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);
            var recurrenceId = L10.CreateL10("Test L10");
            var m1 = new MeasurableModel()
            {
                AccountableUserId = c.E1.Id,
                AdminUserId = c.E1.Id,
                Title = "Meas1",
                OrganizationId = c.Org.Organization.Id
            };

            var measurable = AddMeasurableVm.CreateNewMeasurable(recurrenceId, m1, true);
            L10Accessor.CreateMeasurable(c.E1, recurrenceId, measurable);

            var getlist = L10.GetL10List();
            Assert.AreEqual(1, getlist.Count());
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestAttachIssueMeetingL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var recurrenceId = L10.CreateL10("Test L10");

            var name = "Test Name For Issue Meeting L10";

            var details = "Test detail For Issue Meeting L10";

            await L10.AttachIssueL10(recurrenceId, name, c.E1.Id, details);

            var getIssueMeetingL10 = L10Accessor.GetIssuesForRecurrence(c.E1, recurrenceId, false);

            Assert.AreEqual(1, getIssueMeetingL10.Count());

        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestRemoveIssueL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var recurrenceId = L10.CreateL10("Test L10");

            var name = "Test Name For Issue Meeting L10";

            var details = "Test detail For Issue Meeting L10";

            await L10.AttachIssueL10(recurrenceId, name, c.E1.Id, details);

            var getIssueMeetingL10 = L10Accessor.GetIssuesForRecurrence(c.E1, recurrenceId, false);

            L10.RemoveIssueL10(recurrenceId, getIssueMeetingL10.FirstOrDefault().Id);

            var getIssueMeetingList = L10Accessor.GetIssuesForRecurrence(c.E1, recurrenceId, false);

            Assert.AreEqual(0, getIssueMeetingList.Count());

        }

    }
}
