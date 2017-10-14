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
using TractionTools.Tests.Properties;
using RadialReview.Api.V1;

namespace TractionTools.Tests.API.v1 {
    [TestClass]
    public class IssueApiTests_v1 : BaseApiTest {
		public IssueApiTests_v1() : base(VERSION_1) {
		}

		[TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestGetMineIssues() {
            var c = await Ctx.Build();
            //var issue = new IssueModel() {
            //    Message = "Issue for Test Method",
            //};
            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);

            await L10Accessor.AddAttendee(c.E1, _recurrence.Id, c.E1.Id);

			var creation = IssueCreation.CreateL10Issue("Issue for Test Method", null, c.E1.Id, _recurrence.Id);
			var result = await IssuesAccessor.CreateIssue(c.E1, creation);// _recurrence.Id, c.E1.Id, issue);
            IssuesController iss = new IssuesController();
            iss.MockUser(c.E1);
            await c.Org.RegisterUser(c.E1);
            var _model = iss.GetMineIssues();
            
            // this is to test model type with key names or property names
            CompareModelProperties(/*APIResult.IssueApiTests_v0_TestGetMineIssues,*/ _model);
            
            Assert.AreEqual(1, _model.Count());
            Assert.AreEqual(result.IssueModel.Message, _model.FirstOrDefault().Name);
        }

        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestCreateIssue() {
            var c = await Ctx.Build();
            var issue = new IssueModel() {
                Message = "Issue for Test Method",
            };

            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
            IssuesController iss = new IssuesController();
            iss.MockUser(c.E1);
            var result = await iss.CreateIssue(new IssuesController.CreateIssueModel {
				title = issue.Message,
				meetingId= _recurrence.Id

			});
            AngularIssue _angularIssue = iss.Get(result.Id);

            CompareModelProperties(/*APIResult.IssueApiTests_v0_TestCreateIssue,*/ _angularIssue);

            Assert.IsNotNull(_angularIssue);
            Assert.AreEqual(_angularIssue.Name, issue.Message);
        }

        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestGetIssue() {
            var c = await Ctx.Build();
            var issue = new IssueModel() {
                Message = "Issue for Test Method",
            };

            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);

			var creation = IssueCreation.CreateL10Issue("Issue for Test Method", null, c.E1.Id, _recurrence.Id);
			var result = await IssuesAccessor.CreateIssue(c.E1, creation);//_recurrence.Id, c.E1.Id, issue);
            IssuesController iss = new IssuesController();
            iss.MockUser(c.E1);
            AngularIssue _angularIssue = iss.Get(issue.Id);

            CompareModelProperties(/*APIResult.IssueApiTests_v0_TestGetIssue,*/ _angularIssue);
            Assert.IsNotNull(_angularIssue);
            Assert.AreEqual(issue.Message, _angularIssue.Name);
        }


        //shift to L10
        //     [TestMethod]
        //     [TestCategory("Api_V1")]
        //     public async Task TestGetUserIssues() {
        //var c = await Ctx.Build();
        //var issue = new IssueModel(){
        //	Message = "Issue for Test Method",
        //         };

        //         var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
        //         var result = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E1.Id, issue);
        //         var issue1 = new IssueModel(){
        //             Message = "Issue for Test Method",
        //         };

        //         // creating issue with different owner
        //         var result1 = await IssuesAccessor.CreateIssue(c.E1, _recurrence.Id, c.E2.Id, issue1);
        //         IssueController iss = new IssueController();
        //         iss.MockUser(c.E1);
        //         var _model = iss.GetUserIssues(c.E1.Id, _recurrence.Id);
        //         Assert.AreEqual(1, _model.Count());
        //         Assert.AreEqual(c.E1.Id, _model.First().Owner.Id);
        //     }

        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestGetRecurrenceIssues() {
            var c = await Ctx.Build();
            //var issue = new IssueModel() {
            //    Message = "Issue for Test Method",
            //};
            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
			var creation = IssueCreation.CreateL10Issue("Issue for Test Method", null, c.E1.Id, _recurrence.Id);
			var result = await IssuesAccessor.CreateIssue(c.E1, creation);// _recurrence.Id, c.E1.Id, issue);
			//var issue1 = new IssueModel() {
			//    Message = "Issue for Test Method",
			//};
			// creating issue with different owner
			var creation2 = IssueCreation.CreateL10Issue("Issue for Test Method", null, c.E2.Id, _recurrence.Id);

			var result1 = await IssuesAccessor.CreateIssue(c.E1, creation2);// _recurrence.Id, c.E2.Id, issue1);
            IssuesController iss = new IssuesController();
            iss.MockUser(c.E1);

            L10Controller l10 = new L10Controller();
            l10.MockUser(c.E1);
            // use GetUserIssues method instead of GetVisibleIssuesForUser method
            IEnumerable<AngularIssue> _angularIssueList = l10.GetRecurrenceIssues(_recurrence.Id);
            
            CompareModelProperties(/*APIResult.IssueApiTests_v0_TestGetRecurrenceIssues,*/ _angularIssueList);

            Assert.AreEqual(2, _angularIssueList.Count());
        }

        [TestMethod]
        [TestCategory("Api_V1")]
        public async Task TestEditIssue() {
            var c = await Ctx.Build();
            //var issue = new IssueModel() {
            //    Message = "Issue for Test Method",
            //};
            var _recurrence = await L10Accessor.CreateBlankRecurrence(c.E1, c.Org.Id);
			var creation = IssueCreation.CreateL10Issue("Issue for Test Method", null, c.E1.Id, _recurrence.Id);
			var result = await IssuesAccessor.CreateIssue(c.E1, creation);// _recurrence.Id, c.E1.Id, issue);
            IssuesController iss = new IssuesController();
            iss.MockUser(c.E1);
            var newMessage = "New Issue message for Test Method.";
            await iss.EditIssue(result.IssueModel.Id, new IssuesController.UpdateIssueModel { title= newMessage });
            var _issue = iss.Get(result.IssueModel.Id);
            Assert.AreNotEqual(result.IssueModel.Message, _issue.Name);
            Assert.AreEqual(newMessage, _issue.Name);
        }
    }
}
