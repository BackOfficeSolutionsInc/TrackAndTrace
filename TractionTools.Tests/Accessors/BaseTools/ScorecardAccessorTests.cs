using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Models;
using RadialReview.Models.L10;
using TractionTools.Tests.TestUtils;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Models.Askables;
using System.Web.Mvc;
using System.Collections.Generic;
using RadialReview.Models.Enums;
using RadialReview.Models.Scorecard;
using System.Threading.Tasks;
using TractionTools.Tests.Utilities;
using RadialReview.Utilities;
using RadialReview;

namespace TractionTools.Tests.Accessors
{
    [TestClass]
    public class ScorecardAccessorTests : BaseTest
    {
        [TestMethod]
        public async Task EditMeasurables()
        {
           // OrganizationModel org = null;
            L10Recurrence recur = null;

            var testId = Guid.NewGuid();
            MockApplication();
            MockHttpContext();
			var org = await OrgUtil.CreateOrganization(time: new DateTime(2016, 1, 1));
            UserOrganizationModel employee = org.Employee;
            UserOrganizationModel manager = org.Manager;
            //DbCommit(s =>
            //{
            //    org = new OrganizationModel() { };
            //    org.Settings.TimeZoneId = "GMT Standard Time";
            //    s.Save(org);
            //    employee = new UserOrganizationModel() { Organization = org };
            //    s.Save(employee);
            //    manager = new UserOrganizationModel() { Organization = org, ManagerAtOrganization = true };
            //    s.Save(manager);                
            //});

			//AccountabilityAccessor.AppendNode(await GetAdminUser(testId),

			//new UserAccessor().AddManager(await GetAdminUser(testId), employee.Id, manager.Id, new DateTime(2016, 1, 1));

			var accessor = new ScorecardAccessor();
            var controller = new MeasurableController();
            controller.MockUser(manager);


            var measurables = ScorecardAccessor.GetOrganizationMeasurables(manager, org.Id,false);
            Assert.AreEqual(0, measurables.Count);

            var newMeasurables = new List<MeasurableModel>();
            
            var row = controller.BlankEditorRow(true) as PartialViewResult;
            var rowVm = row.Model as MeasurableModel;

            //Test Measurable
            Assert.AreEqual(0, rowVm.Id);
            Assert.AreEqual(org.Id, rowVm.OrganizationId);
          
            rowVm.Title = "Measurable A";
            rowVm.AccountableUserId = employee.Id;
            rowVm.AccountableUser = employee;
            rowVm.AdminUserId = employee.Id;
            rowVm.AdminUser = employee;
            rowVm.Goal = 101;
            rowVm.GoalDirection = LessGreater.GreaterThan;
            rowVm.UnitType = UnitType.Dollar;
            newMeasurables.Add(rowVm);

            row = controller.BlankEditorRow(true) as PartialViewResult;
            rowVm = row.Model as MeasurableModel;
            rowVm.Title = "Measurable B";
            rowVm.AccountableUserId = employee.Id;
            rowVm.AccountableUser = employee;
            rowVm.AdminUserId = employee.Id;
            rowVm.AdminUser = employee;
            rowVm.Goal = 102;
            rowVm.GoalDirection = LessGreater.LessThan;
            rowVm.UnitType = UnitType.Percent;
            newMeasurables.Add(rowVm);


            await ScorecardAccessor.EditMeasurables(manager, employee.Id, newMeasurables, false);

            var addedMeasurables = ScorecardAccessor.GetOrganizationMeasurables(manager, org.Id, false);
            Assert.AreEqual(2, newMeasurables.Count);

            //Add recur
            var l10Accessor = new L10Accessor();
            recur = new L10Recurrence() { Name = "test recur", Organization = org.Organization, OrganizationId = org.Id, IncludeAggregateTodoCompletion=false,IncludeIndividualTodos=false };
            recur._DefaultAttendees = new List<L10Recurrence.L10Recurrence_Attendee>() { new L10Recurrence.L10Recurrence_Attendee(){ User= employee , L10Recurrence=recur}};
            recur._DefaultMeasurables = new List<L10Recurrence.L10Recurrence_Measurable>();
            recur._DefaultRocks = new List<L10Recurrence.L10Recurrence_Rocks>();
			await L10Accessor.EditL10Recurrence(manager, recur);
            Assert.AreNotEqual(0, recur.Id);

            //Add measurables skip l10s
            row = controller.BlankEditorRow(true) as PartialViewResult;
            rowVm = row.Model as MeasurableModel;
            rowVm.Title = "Measurable C";
            rowVm.AccountableUserId = employee.Id;
            rowVm.AccountableUser = employee;
            rowVm.AdminUserId = employee.Id;
            rowVm.AdminUser = employee;
            rowVm.Goal = 102;
            rowVm.GoalDirection = LessGreater.LessThan;
            rowVm.UnitType = UnitType.Percent;
            newMeasurables.Add(rowVm);

            await ScorecardAccessor.EditMeasurables(manager, employee.Id, newMeasurables, false);

                //Test rock count
                measurables = ScorecardAccessor.GetOrganizationMeasurables(manager, org.Id, false);
                Assert.AreEqual(3, measurables.Count);
                //Test L10 rocks
                var recurLoaded = L10Accessor.GetL10Recurrence(manager, recur.Id, true);
                Assert.AreEqual(0, recurLoaded._DefaultMeasurables.Count);

            //Add measurables skip l10s
            row = controller.BlankEditorRow(true) as PartialViewResult;
            rowVm = row.Model as MeasurableModel;
            rowVm.Title = "Measurable D";
            rowVm.AccountableUserId = employee.Id;
            rowVm.AccountableUser = employee;
            rowVm.AdminUserId = employee.Id;
            rowVm.AdminUser = employee;
            rowVm.Goal = 102;
            rowVm.GoalDirection = LessGreater.LessThan;
            rowVm.UnitType = UnitType.Percent;
            newMeasurables.Add(rowVm);

            await ScorecardAccessor.EditMeasurables(manager, employee.Id, newMeasurables, true);
                //Test rock count
                measurables = ScorecardAccessor.GetOrganizationMeasurables(manager, org.Id, false);
                Assert.AreEqual(4, measurables.Count);
                //Test L10 rocks
                recurLoaded = L10Accessor.GetL10Recurrence(manager, recur.Id, true);
                Assert.AreEqual(1, recurLoaded._DefaultMeasurables.Count);
                Assert.AreEqual("Measurable D", recurLoaded._DefaultMeasurables[0].Measurable.Title);
                Assert.AreEqual(recur.Id, recurLoaded._DefaultMeasurables[0].L10Recurrence.Id);
        }

        
        [TestMethod]
        public async Task UpdateScoreInMeeting_CreateScores()
        {
            var r = await L10Utility.CreateRecurrence();
            MeasurableModel m=null;
            DbCommit(async s => {
				var perms = PermissionsUtility.Create(s, r.Creator);
				m = new MeasurableModel() {
					OrganizationId = r.Org.Id,
					Title = "UpdateScoreInMeeting_CreateScores",
					AccountableUserId = r.Creator.Id,
					AdminUserId = r.Employee.Id
				};
				var mvm = L10Controller.AddMeasurableVm.CreateNewMeasurable(r.Id, m);
				MockHttpContext();
				await L10Accessor.AddMeasurable(s, perms, null, r.Id, mvm, skipRealTime: true);
			});

            await L10Accessor.StartMeeting(r.Creator, r.Creator, r.Id, r.Creator.Id.AsList());

            var week = DateTime.UtcNow.AddDays(-7*16);
            using (var frame = TestUtilities.CreateFrame()) {
                {
                    var score = ScorecardAccessor.UpdateScoreInMeeting(r.Creator, r.Id, -1, week, m.Id, 101.5m, null, null);

                    frame.EnsureContainsAndClear(
                        "Score not found. Score below boundry. Creating scores down to value.",
                        "Scores created: 1");

                    Assert.AreEqual(week.StartOfWeek(DayOfWeek.Sunday), score.ForWeek);
                    Assert.AreEqual(r.Org.Id, score.OrganizationId);
                    Assert.AreEqual(m.Id, score.Measurable.Id);
                    Assert.AreEqual(m.Id, score.MeasurableId);
                    Assert.AreEqual(101.5m, score.Measured);
                    Assert.AreEqual(r.Creator.Id, score.AccountableUserId);
                    Assert.AreEqual(r.Creator.Id, m.AccountableUserId);
                    Assert.AreEqual(r.Employee.Id, m.AdminUserId);
                }

                {
                    week = DateTime.UtcNow.AddDays(7 * 3);
                    var score2 = ScorecardAccessor.UpdateScoreInMeeting(r.Creator, r.Id, -1, week, m.Id, 102, null, null);

                    frame.EnsureContainsAndClear(
                        "Score not found. Score above boundry. Creating scores up to value.",
                        "Scores created: 3");

                    Assert.AreEqual(week.StartOfWeek(DayOfWeek.Sunday), score2.ForWeek);
                    Assert.AreEqual(r.Org.Id, score2.OrganizationId);
                    Assert.AreEqual(m.Id, score2.Measurable.Id);
                    Assert.AreEqual(m.Id, score2.MeasurableId);
                    Assert.AreEqual(102m, score2.Measured);
                    Assert.AreEqual(r.Creator.Id, score2.AccountableUserId);
                    Assert.AreEqual(r.Creator.Id, m.AccountableUserId);
                    Assert.AreEqual(r.Employee.Id, m.AdminUserId);
                }
                var scoreId = -1L;
                {
                    week = DateTime.UtcNow.AddDays(-7 * 15);

                    var score3 = ScorecardAccessor.UpdateScoreInMeeting(r.Creator, r.Id, -1, week, m.Id, 1042, null, null);

                    frame.EnsureContainsAndClear(
                        "Score not found. Score inside boundry. Creating score.",
                        "Scores created: 1");

                    Assert.AreEqual(week.StartOfWeek(DayOfWeek.Sunday), score3.ForWeek);
                    Assert.AreEqual(r.Org.Id, score3.OrganizationId);
                    Assert.AreEqual(m.Id, score3.Measurable.Id);
                    Assert.AreEqual(m.Id, score3.MeasurableId);
                    Assert.AreEqual(1042m, score3.Measured);
                    Assert.AreEqual(r.Creator.Id, score3.AccountableUserId);
                    Assert.AreEqual(r.Creator.Id, m.AccountableUserId);
                    Assert.AreEqual(r.Employee.Id, m.AdminUserId);
                    scoreId = score3.Id;
                }

                {
                    week = DateTime.UtcNow.AddDays(-7 * 15);

                    r.Creator._ClientTimestamp = DateTime.UtcNow.ToJavascriptMilliseconds();

                    var score4 = ScorecardAccessor.UpdateScoreInMeeting(r.Creator, r.Id, scoreId, week, m.Id, 333, null, null);

                    frame.EnsureContainsAndClear("Found one or more score. Updating All.");
                    Assert.AreEqual(scoreId, score4.Id);
                    Assert.AreEqual(week.StartOfWeek(DayOfWeek.Sunday), score4.ForWeek);
                    Assert.AreEqual(r.Org.Id, score4.OrganizationId);
                    Assert.AreEqual(m.Id, score4.Measurable.Id);
                    Assert.AreEqual(m.Id, score4.MeasurableId);
                    Assert.AreEqual(333m, score4.Measured);
                    Assert.AreEqual(r.Creator.Id, score4.AccountableUserId);
                    Assert.AreEqual(r.Creator.Id, m.AccountableUserId);
                    Assert.AreEqual(r.Employee.Id, m.AdminUserId);
                }
            }




        }
    }
}
