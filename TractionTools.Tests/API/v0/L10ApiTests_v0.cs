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

            var recurrenceid = L10.CreateL10("Test L10");

            var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E1, c.Org.Id);

            Assert.AreEqual(recurrenceid, getAllL10RecurrenceAtOrganization.FirstOrDefault().Id);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestEditL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var name = "Test L10 Updated";
            
            var created = L10.CreateL10("Test L10");

            L10.EditL10(created, new L10Recurrence() { Name = name });

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

            var reccurenceId = L10.CreateL10("Test L10");
            var m1 = new MeasurableModel()
            {
                AccountableUserId = c.E1.Id,
                AdminUserId = c.E1.Id,
                Title = "Meas1",
                OrganizationId = c.Org.Organization.Id
            };

            var measurable = AddMeasurableVm.CreateNewMeasurable(reccurenceId, m1, true);

            L10Accessor.CreateMeasurable(c.E1, reccurenceId, measurable);

            L10.AttachMeasurableL10(reccurenceId, measurable.Measurables.FirstOrDefault().Id); //confirmation measurable id?

            var getMeasurablesForRecurrence = L10Accessor.GetScoresAndMeasurablesForRecurrence(c.E1, reccurenceId);

            Assert.AreEqual(2, getMeasurablesForRecurrence.MeasurablesAndDividers.Count());

        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestRemoveMeasurableL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var reccurenceId = L10.CreateL10("Test L10");
            var m1 = new MeasurableModel()
            {
                AccountableUserId = c.E1.Id,
                AdminUserId = c.E1.Id,
                Title = "Meas1",
                OrganizationId = c.Org.Organization.Id
            };

            var measurable = AddMeasurableVm.CreateNewMeasurable(reccurenceId, m1, true);

            L10Accessor.CreateMeasurable(c.E1, reccurenceId, measurable);

            L10.AttachMeasurableL10(reccurenceId, measurable.Measurables.FirstOrDefault().Id); //confirmation measurable id?

            L10.RemoveMeasurableL10(reccurenceId, measurable.Measurables.FirstOrDefault().Id);

            var removeMeasurablesForRecurrence = L10Accessor.GetScoresAndMeasurablesForRecurrence(c.E1, reccurenceId);

            Assert.AreEqual(0, removeMeasurablesForRecurrence.MeasurablesAndDividers.Count());

        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestAttachRockMeetingL10()
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

            var getRocksForRecurrence = L10Accessor.GetRocksForRecurrence(c.E1,reccurenceId);

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

            var removeRocksForRecurrence = L10Accessor.GetRocksForRecurrence(c.E1, reccurenceId);

            Assert.AreEqual(0, removeRocksForRecurrence.Count());

        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestGetMeetingsL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);
            
            var reccurenceId = L10.CreateL10("Test L10");

            var getMeetingsL10 = L10.GetMeetingsL10(reccurenceId);

            Assert.AreEqual(reccurenceId, getMeetingsL10.Id);

        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestGetL10Attendess()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var reccurenceId = L10.CreateL10("Test L10");

            L10Accessor.AddAttendee(c.E1, reccurenceId, c.E1.Id);

            var GetL10Attendess = L10.GetL10MeetingAttendees(reccurenceId);

            Assert.AreEqual(1, GetL10Attendess.Count());
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestAttachHeadlineMeetingL10()
        {
            var c = new Ctx();

            L10_Controller L10 = new L10_Controller();

            L10.MockUser(c.E1);

            var reccurenceId = L10.CreateL10("Test L10");

            var headlinemodel = new PeopleHeadline()
            {
                Message = "Test Head Line",
                OrganizationId = c.Org.Id,
                RecurrenceId = reccurenceId,
                _Details = "Test details"
            };

            var getheadline = await HeadlineAccessor.CreateHeadline(c.E1, headlinemodel);

            L10Accessor.AttachHeadline(c.E1, reccurenceId, headlinemodel.Id);

            var getattachheadline = L10Accessor.GetHeadlinesForMeeting(c.E1, reccurenceId);

            Assert.AreEqual(reccurenceId, getattachheadline.FirstOrDefault().RecurrenceId);

        }
    }
}
