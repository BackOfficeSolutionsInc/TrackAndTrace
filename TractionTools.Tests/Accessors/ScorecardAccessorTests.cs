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

namespace TractionTools.Tests.Accessors
{
    [TestClass]
    public class ScorecardAccessorTests : BaseTest
    {
        [TestMethod]
        public async Task EditMeasurables()
        {
            OrganizationModel org = null;
            UserOrganizationModel employee = null;
            UserOrganizationModel manager = null;
            L10Recurrence recur = null;

            var testId = Guid.NewGuid();
            
            DbCommit(s =>
            {
                org = new OrganizationModel() { };
                org.Settings.TimeZoneId = "GMT Standard Time";
                s.Save(org);
                employee = new UserOrganizationModel() { Organization = org };
                s.Save(employee);
                manager = new UserOrganizationModel() { Organization = org, ManagerAtOrganization = true };
                s.Save(manager);

                
            });
            MockApplication();
            MockHttpContext();
            new UserAccessor().AddManager(await GetAdminUser(testId), employee.Id, manager.Id, new DateTime(2016, 1, 1));

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


            ScorecardAccessor.EditMeasurables(manager, employee.Id, newMeasurables, false);

            var addedMeasurables = ScorecardAccessor.GetOrganizationMeasurables(manager, org.Id, false);
            Assert.AreEqual(2, newMeasurables.Count);

            //Add recur
            var l10Accessor = new L10Accessor();
            recur = new L10Recurrence() { Name = "test recur", Organization = org, OrganizationId = org.Id, IncludeAggregateTodoCompletion=false,IncludeIndividualTodos=false };
            recur._DefaultAttendees = new List<L10Recurrence.L10Recurrence_Attendee>() { new L10Recurrence.L10Recurrence_Attendee(){ User= employee , L10Recurrence=recur}};
            recur._DefaultMeasurables = new List<L10Recurrence.L10Recurrence_Measurable>();
            recur._DefaultRocks = new List<L10Recurrence.L10Recurrence_Rocks>();
            L10Accessor.EditL10Recurrence(manager, recur);
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

            ScorecardAccessor.EditMeasurables(manager, employee.Id, newMeasurables, false);

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

            ScorecardAccessor.EditMeasurables(manager, employee.Id, newMeasurables, true);
                //Test rock count
                measurables = ScorecardAccessor.GetOrganizationMeasurables(manager, org.Id, false);
                Assert.AreEqual(4, measurables.Count);
                //Test L10 rocks
                recurLoaded = L10Accessor.GetL10Recurrence(manager, recur.Id, true);
                Assert.AreEqual(1, recurLoaded._DefaultMeasurables.Count);
                Assert.AreEqual("Measurable D", recurLoaded._DefaultMeasurables[0].Measurable.Title);
                Assert.AreEqual(recur.Id, recurLoaded._DefaultMeasurables[0].L10Recurrence.Id);
        }
    }
}
