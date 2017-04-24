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
    public class TeamsApiTests_v0 : BaseTest
    {
        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestCreateTeam()
        {
            var c = new Ctx();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.E1);

            var name = "TestTeam";

            var getResult = teamController.AddTeam(name);

            var team = teamController.GetTeams(getResult.Id);

            Assert.AreEqual(name, team.Name);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestGetTeams()
        {
            var c = new Ctx();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.E1);

            var name = "TestTeam";

            var addTeam = teamController.AddTeam(name);
            var getTeams = teamController.GetTeams(addTeam.Id);
            Assert.AreEqual(addTeam.Id, getTeams.Id);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestUpdateTeam()
        {
            var c = new Ctx();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.E1);

            var name = "TestTeam";
            var updateName = "TestTeam_Update";
            var addTeam = teamController.AddTeam(name);

            var updateTeam = teamController.UpdateTeam(addTeam.Id, updateName);
            var getTeams = teamController.GetTeams(addTeam.Id);

            Assert.AreEqual(updateName, getTeams.Name);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestGetTeamMember()
        {
            var c = new Ctx();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.E1);

            var name = "TestTeam";

            var addTeam = teamController.AddTeam(name);

            var addMember = TeamAccessor.AddMember(c.E1, addTeam.Id, c.E1.Id);

            var getTeamMember = teamController.GetTeamMembers(addTeam.Id);

            Assert.AreEqual(1, getTeamMember.Count());
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestAddTeamMember()
        {
            var c = new Ctx();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.E1);

            var name = "TestTeam";
            var addTeam = teamController.AddTeam(name);
            var addMember = teamController.AddTeamMember(addTeam.Id, c.E1.Id);
            var getTeamMember = teamController.GetTeamMembers(addTeam.Id);

            Assert.AreEqual(1, getTeamMember.Count());
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestRemoveTeamMember()
        {
            var c = new Ctx();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.E1);

            var name = "TestTeam";
            var addTeam = teamController.AddTeam(name);
            var addMember = teamController.AddTeamMember(addTeam.Id, c.E1.Id);

            var getTeamMember = teamController.GetTeamMembers(addTeam.Id);
            Assert.AreEqual(1, getTeamMember.Count());

            var removeTeamMember = teamController.RemoveTeamMember(addTeam.Id, c.E1.Id);
            var member = teamController.GetTeamMembers(addTeam.Id);

            Assert.AreEqual(0, member.Count());
        }
    }
}
