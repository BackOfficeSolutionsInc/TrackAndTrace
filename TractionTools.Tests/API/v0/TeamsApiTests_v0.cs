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
using TractionTools.Tests.Properties;
using RadialReview.Exceptions;

namespace TractionTools.Tests.Api {
    [TestClass]
    public class TeamsApiTests_v0 : BaseTest {
        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestCreateTeam() {
            var c = await Ctx.Build();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.E1);

            var name = "TestTeam";

            var getResult = teamController.AddTeam(name);

            CompareModelProperties(APIResult.TeamsApiTests_v0_TestCreateTeam, getResult);
            var team = teamController.GetTeams(getResult.Id);

            Assert.AreEqual(name, team.Name);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetTeams() {
            var c = await Ctx.Build();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.E1);

            var name = "TestTeam";

            var addTeam = teamController.AddTeam(name);
            var getTeams = teamController.GetTeams(addTeam.Id);
            CompareModelProperties(APIResult.TeamsApiTests_v0_TestGetTeams, getTeams);
            Assert.AreEqual(addTeam.Id, getTeams.Id);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestUpdateTeam() {
            var c = await Ctx.Build();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.E1);

            var name = "TestTeam";
            var updateName = "TestTeam_Update";
            var addTeam = teamController.AddTeam(name);

            var updateTeam = teamController.UpdateTeam(addTeam.Id, updateName);
            CompareModelProperties(APIResult.TeamsApiTests_v0_TestUpdateTeam, updateTeam);
            var getTeams = teamController.GetTeams(addTeam.Id);

            Assert.AreEqual(updateName, getTeams.Name);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetTeamMember() {
            var c = await Ctx.Build();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.Manager);

            var name = "TestTeam";

            var addTeam = teamController.AddTeam(name);

            MockHttpContext();
            var addMember = TeamAccessor.AddMember(c.Manager, addTeam.Id, c.E1.Id);

            var getTeamMember = teamController.GetTeamMembers(addTeam.Id);
            CompareModelProperties(APIResult.TeamsApiTests_v0_TestGetTeamMember, getTeamMember);
            Assert.AreEqual(2, getTeamMember.Count());
            
            Throws<PermissionsException>(() => TeamAccessor.AddMember(c.E1, addTeam.Id, c.E2.Id));
            //var add = TeamAccessor.AddMember(c.E1, addTeam.Id, c.E2.Id);
            //getTeamMember = teamController.GetTeamMembers(addTeam.Id);
            //CompareModelProperties(APIResult.TeamsApiTests_v0_TestUpdateTeam, getTeamMember);
            //Assert.AreEqual(2, getTeamMember.Count());
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestAddTeamMember() {
            var c = await Ctx.Build();
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
        public async Task TestRemoveTeamMember() {
            var c = await Ctx.Build();
            TeamsController teamController = new TeamsController();
            teamController.MockUser(c.Manager);

            var name = "TestTeam";
            var addTeam = teamController.AddTeam(name);
            var addMember = teamController.AddTeamMember(addTeam.Id, c.E1.Id);

            var getTeamMember = teamController.GetTeamMembers(addTeam.Id);
            Assert.AreEqual(2, getTeamMember.Count());

            var removeTeamMember = teamController.RemoveTeamMember(addTeam.Id, c.E1.Id);
            var member = teamController.GetTeamMembers(addTeam.Id);

            Assert.AreEqual(1, member.Count());
        }
    }
}
