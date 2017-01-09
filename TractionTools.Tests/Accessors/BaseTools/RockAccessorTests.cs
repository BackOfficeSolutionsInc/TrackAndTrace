using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Periods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;

namespace TractionTools.Tests.Accessors
{
    [TestClass]
    public class RockAccessorTests : BaseTest
    {
        [TestMethod]
        public async Task EditRocks()
        {
           // OrganizationModel org = null;
            L10Recurrence recur = null;
            PeriodModel period = null;
            var testId = Guid.NewGuid();
            MockApplication();
            MockHttpContext();
			var org = OrgUtil.CreateOrganization(time: new DateTime(2016, 1, 1));
            UserOrganizationModel employee = org.Employee;
            UserOrganizationModel manager = org.Manager;

			DbCommit(s=>{
                    //org = new OrganizationModel() { };
                    //org.Settings.TimeZoneId = "GMT Standard Time";
                    //s.Save(org);
                    //employee = new UserOrganizationModel() { Organization = org };
                    //s.Save(employee);
                    //manager = new UserOrganizationModel() { Organization = org, ManagerAtOrganization = true };
                    //s.Save(manager);

                    period = new PeriodModel(){
                        Name="Rock A",
                        Organization = org.Organization,
                        OrganizationId = org.Id,
                        StartTime = new DateTime(2016,1,1),
                        EndTime = new DateTime(2016,4,1)
                    };
                    s.Save(period);
                    //DeepSubordianteAccessor.Add(s, manager, employee, o.Id);
                });

			//new UserAccessor().AddManager(await GetAdminUser(testId), employee.Id, manager.Id, new DateTime(2016, 1, 1));


			var accessor = new RockAccessor();
            var controller = new RocksController();
            controller.MockUser(manager);
            var rocks = RockAccessor.GetAllRocks(manager, employee.Id);

            Assert.AreEqual(0, rocks.Count);

            var newRocks = new List<RockModel>();


            var row = controller.BlankEditorRow(true,false) as PartialViewResult;
            var rowVm = row.Model as RockModel;

            //Test Rock
            Assert.AreEqual(0,rowVm.Id);
            Assert.AreEqual(AboutType.Self,rowVm.OnlyAsk);
            Assert.AreEqual(false,rowVm.CompanyRock);

            //Test periods
            //var allPeriods= (List<SelectListItem>)(row.ViewBag.Periods);
           // Assert.AreEqual(5,allPeriods.Count);
            //Assert.AreEqual(""+period.Id,allPeriods[0].Value);

            rowVm.Rock = "Rock A";
            rowVm.ForUserId = employee.Id;
            rowVm.AccountableUser = employee;
            rowVm.Period = period;
            rowVm.PeriodId = period.Id;
            newRocks.Add(rowVm);

            row = controller.BlankEditorRow(true, false) as PartialViewResult;
            rowVm = row.Model as RockModel;
            rowVm.Rock = "Rock B";
            rowVm.ForUserId = employee.Id;
            rowVm.AccountableUser = employee;
            rowVm.Period = period;
            rowVm.PeriodId = period.Id;
            newRocks.Add(rowVm);

			RockAccessor.EditRocks(manager,employee.Id,newRocks,false,true);
            rocks = RockAccessor.GetAllRocks(manager, employee.Id);
            Assert.AreEqual(2, rocks.Count);

            var l10Accessor = new L10Accessor();
            recur = new L10Recurrence(){Name = "test recur",Organization=org.Organization,OrganizationId=org.Id};
            recur._DefaultAttendees=new List<L10Recurrence.L10Recurrence_Attendee>() { 
                new L10Recurrence.L10Recurrence_Attendee(){ User= employee , L10Recurrence=recur}
            };
            recur._DefaultMeasurables = new List<L10Recurrence.L10Recurrence_Measurable>();
            recur._DefaultRocks = new List<L10Recurrence.L10Recurrence_Rocks>();

            L10Accessor.EditL10Recurrence(manager, recur);
            Assert.AreNotEqual(0, recur.Id);

            //Try to add rocks without adding to L10
            row = controller.BlankEditorRow(true, false) as PartialViewResult;
            rowVm = row.Model as RockModel;
            rowVm.Rock = "Rock C";
            rowVm.ForUserId = employee.Id;
            rowVm.AccountableUser = employee;
            rowVm.Period = period;
            rowVm.PeriodId = period.Id;
            newRocks.Add(rowVm);
			RockAccessor.EditRocks(manager, employee.Id, newRocks, false, false);

                //Test rock count
                rocks = RockAccessor.GetAllRocks(manager, employee.Id);
                Assert.AreEqual(3, rocks.Count);
                //Test L10 rocks
                var recurLoaded = L10Accessor.GetL10Recurrence(manager, recur.Id, true);
                Assert.AreEqual(0, recurLoaded._DefaultRocks.Count);

            row = controller.BlankEditorRow(true, false) as PartialViewResult;
            rowVm = row.Model as RockModel;
            rowVm.Rock = "Rock D";
            rowVm.ForUserId = employee.Id;
            rowVm.AccountableUser = employee;
            rowVm.Period = period;
            rowVm.PeriodId = period.Id;
            newRocks.Add(rowVm);
			RockAccessor.EditRocks(manager, employee.Id, newRocks, false, true);

            recurLoaded = L10Accessor.GetL10Recurrence(manager, recur.Id, true);
            Assert.AreEqual(1, recurLoaded._DefaultRocks.Count);
            Assert.AreEqual("Rock D", recurLoaded._DefaultRocks[0].ForRock.Rock);
            Assert.AreEqual(recur.Id, recurLoaded._DefaultRocks[0].L10Recurrence.Id);
        }

        
    }
}
