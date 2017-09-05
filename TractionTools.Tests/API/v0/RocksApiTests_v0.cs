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
using TractionTools.Tests.Properties;

namespace TractionTools.Tests.Api {
    [TestClass]
    public class RocksApiTests_v0 : BaseTest {
        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetRocksMilestones() {
            var c = await Ctx.Build();
            RadialReview.Api.V0.RocksController rocksController = new RadialReview.Api.V0.RocksController();
            rocksController.MockUser(c.E1);

            var _recurrence =await L10Accessor.CreateBlankRecurrence(c.E1, c.E1.Organization.Id);

            var rock = new RockModel() {
                OrganizationId = c.E1.Organization.Id,
                ForUserId = c.E1.Id,
            };

            MockHttpContext();

            await L10Accessor.CreateRock(c.E1, _recurrence.Id, AddRockVm.CreateRock(_recurrence.Id, rock, true));

            var getRocks = RockAccessor.GetRocks(c.E1, c.E1.Id);

            Assert.AreEqual(1, getRocks.Count);

            var addRocksMilestones = rocksController.AddRocksMilestones(getRocks.FirstOrDefault().Id, "TestMilestone", DateTime.Now.AddDays(7));
            var getRocksMilestones = rocksController.GetRocksMilestones(getRocks.FirstOrDefault().Id);
            CompareModelProperties(APIResult.RocksApiTests_v0_TestGetRocksMilestones, getRocksMilestones);
            Assert.AreEqual(1, getRocksMilestones.Count());
        }


        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestAddRocksMilestones() {
            var c = await Ctx.Build();
            RadialReview.Api.V0.RocksController rocksController = new RadialReview.Api.V0.RocksController();
            rocksController.MockUser(c.E1);

            var _recurrence =await L10Accessor.CreateBlankRecurrence(c.E1, c.E1.Organization.Id);

            var rock = new RockModel() {
                OrganizationId = c.E1.Organization.Id,
                ForUserId = c.E1.Id,
            };

            await L10Accessor.CreateRock(c.E1, _recurrence.Id, AddRockVm.CreateRock(_recurrence.Id, rock, true));
            var getRocks = RockAccessor.GetRocks(c.E1, c.E1.Id);

            string name = "TestMilestone";
            DateTime date = DateTime.UtcNow.AddDays(7);
            var addRocksMilestones = rocksController.AddRocksMilestones(getRocks.FirstOrDefault().Id, name, date);

            Assert.AreEqual(name, addRocksMilestones.Name);
            Assert.AreEqual(date, addRocksMilestones.DueDate);

            Assert.AreEqual(1, getRocks.Count());
        }

    }
}
