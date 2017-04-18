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

namespace TractionTools.Tests.Api
{
    [TestClass]
    public class MilestonesApiTests_v0 : BaseTest
    {
        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestGetMilestones()
        {
            var c = new Ctx();
            MilestonesController milestonesController = new MilestonesController();
            milestonesController.MockUser(c.E1);

            var _recurrence = L10Accessor.CreateBlankRecurrence(c.E1, c.E1.Organization.Id);

            var rock = new RockModel()
            {
                OrganizationId = c.E1.Organization.Id,
                ForUserId = c.E1.Id,
            };
            L10Accessor.CreateRock(c.E1, _recurrence.Id, AddRockVm.CreateRock(_recurrence.Id, rock, true));
            var getRocks = RockAccessor.GetRocks(c.E1, c.E1.Id);

            var addRocksMilestones = RockAccessor.AddMilestone(c.E1, getRocks.FirstOrDefault().Id, "TestMilestone", DateTime.Now.AddDays(7));
            var getRocksMilestones = milestonesController.GetMilestones(addRocksMilestones.Id);

            Assert.AreEqual(addRocksMilestones.Id, getRocksMilestones.Id);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestUpdateMilestones()
        {
            var c = new Ctx();
            MilestonesController milestonesController = new MilestonesController();
            milestonesController.MockUser(c.E1);

            var _recurrence = L10Accessor.CreateBlankRecurrence(c.E1, c.E1.Organization.Id);

            var rock = new RockModel()
            {
                OrganizationId = c.E1.Organization.Id,
                ForUserId = c.E1.Id,
            };

            var name = "TestMilestone_updated";
            L10Accessor.CreateRock(c.E1, _recurrence.Id, AddRockVm.CreateRock(_recurrence.Id, rock, true));
            var getRocks = RockAccessor.GetRocks(c.E1, c.E1.Id);

            var addRocksMilestones = RockAccessor.AddMilestone(c.E1, getRocks.FirstOrDefault().Id, "TestMilestone", DateTime.Now.AddDays(7));

            //Update Milestone
            milestonesController.UpdateMilestones(addRocksMilestones.Id, name);

            var getRocksMilestones = milestonesController.GetMilestones(addRocksMilestones.Id);

            Assert.AreEqual(name, getRocksMilestones.Name);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestRemoveMilestones()
        {
            var c = new Ctx();
            MilestonesController milestonesController = new MilestonesController();
            milestonesController.MockUser(c.E1);

            var _recurrence = L10Accessor.CreateBlankRecurrence(c.E1, c.E1.Organization.Id);

            var rock = new RockModel()
            {
                OrganizationId = c.E1.Organization.Id,
                ForUserId = c.E1.Id,
            };

            L10Accessor.CreateRock(c.E1, _recurrence.Id, AddRockVm.CreateRock(_recurrence.Id, rock, true));
            var getRocks = RockAccessor.GetRocks(c.E1, c.E1.Id);

            var addRocksMilestones = RockAccessor.AddMilestone(c.E1, getRocks.FirstOrDefault().Id, "TestMilestone", DateTime.Now.AddDays(7));

            //remove milestone
            milestonesController.RemoveMilestones(addRocksMilestones.Id);
            
            var getRocksMilestones = RockAccessor.GetMilestone(c.E1, addRocksMilestones.Id);

            Assert.IsNotNull(getRocksMilestones.DeleteTime);
        }

    }
}
