using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;

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
using RadialReview.Models.Issues;
using RadialReview.Models.Angular.Issues;

namespace TractionTools.Tests.API.v0
{
    [TestClass]
    public class IssueApiTests_v0 : BaseTest
    {
        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetMineIssues(){
            var c = await Ctx.Build();
            var issue = new IssueModel(){
                Message = "Issue for Test Method",
            };
            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
            var result = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E1.Id, issue);
            IssueController iss = new IssueController();
            iss.MockUser(c.E1);
            await c.Org.RegisterUser(c.E1);
            var _model = iss.GetMineIssues();
            Assert.AreEqual(1, _model.Count());
            Assert.AreEqual(issue.Message, _model.FirstOrDefault().Name);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestCreateIssue(){
            var c = await Ctx.Build();
            var issue = new IssueModel(){
                Message = "Issue for Test Method",
            };

            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
            IssueController iss = new IssueController();
            iss.MockUser(c.E1);
            var result = await iss.CreateIssue(_recurrence.Id,issue.Message,null,null);
            AngularIssue _angularIssue = iss.Get(result.Id);
            Assert.IsNotNull(_angularIssue);
            Assert.AreEqual(_angularIssue.Name, issue.Message);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetIssue() {
			var c = await Ctx.Build();
			var issue = new IssueModel(){
                Message = "Issue for Test Method",
            };

            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
            var result = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E1.Id, issue);
            IssueController iss = new IssueController();
            iss.MockUser(c.E1);
            var _todo = iss.Get(issue.Id);
            Assert.IsNotNull(_todo);
            Assert.AreEqual(issue.Message, _todo.Name);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetUserIssues() {
			var c = await Ctx.Build();
			var issue = new IssueModel(){
				Message = "Issue for Test Method",
            };

            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
            var result = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E1.Id, issue);
            var issue1 = new IssueModel(){
                Message = "Issue for Test Method",
            };

            // creating issue with different owner
            var result1 = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E2.Id, issue1);
            IssueController iss = new IssueController();
            iss.MockUser(c.E1);
            var _model = iss.GetUserIssues(c.E1.Id, _recurrence.Id);
            Assert.AreEqual(1, _model.Count());
            Assert.AreEqual(c.E1.Id, _model.First().Owner.Id);
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestGetRecurrenceIssues() {
			var c = await Ctx.Build();
			var issue = new IssueModel(){
                Message = "Issue for Test Method",
            };
            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
            var result = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E1.Id, issue);
            var issue1 = new IssueModel(){
                Message = "Issue for Test Method",
            };
            // creating issue with different owner
            var result1 = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E2.Id, issue1);
            IssueController iss = new IssueController();
            iss.MockUser(c.E1);
            var _model = iss.GetRecurrenceIssues(_recurrence.Id);
            Assert.AreEqual(2, _model.Count());
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestEditIssue() {
			var c = await Ctx.Build();
			var issue = new IssueModel(){
                Message = "Issue for Test Method",
            };
            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
            var result = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E1.Id, issue);
            IssueController iss = new IssueController();
            iss.MockUser(c.E1);
			var newMessage = "New Issue message for Test Method.";
            iss.EditIssue(issue.Id, newMessage);
            var _issue = iss.Get(issue.Id);
            Assert.AreNotEqual(issue.Message, _issue.Name);
            Assert.AreEqual(newMessage, _issue.Name);
        }
    }
}
