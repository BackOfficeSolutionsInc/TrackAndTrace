using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Models;
using TractionTools.Tests.TestUtils;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Models.L10.VM;
using System.Collections.Generic;
using RadialReview.Models.L10;
using System.Web.Mvc;
using RadialReview.Exceptions;

namespace TractionTools.Tests.Accessors
{
    [TestClass]
    public class L10Accessor_EditRecurrenceTests : BaseTest
    {
        [TestMethod]
        public void CreateL10Recurrence()
        {
            UserOrganizationModel employee = null;
            UserOrganizationModel manager = null;
            UserOrganizationModel managerOtherCompany = null;
            DbCommit(s =>{
                //Org
                var o = new OrganizationModel() { };
                o.Settings.TimeZoneId = "GMT Standard Time";
                s.Save(o);
                var o2 = new OrganizationModel() { };
                o2.Settings.TimeZoneId = "GMT Standard Time";
                s.Save(o2);

                //User
                var u = new UserOrganizationModel() { Organization = o };
                s.Save(u);
                employee = u;
                manager = new UserOrganizationModel() { Organization = o, ManagerAtOrganization = true };
                s.Save(manager);

                managerOtherCompany = new UserOrganizationModel() { Organization = o2, ManagerAtOrganization = true };
                s.Save(managerOtherCompany);


            });
            //create
            var controller = new L10Controller();
            controller.SetValue("SkipValidation", true);
            controller.MockUser(employee);
            //controller.GetType().GetField(,System.Reflection.BindingFlags.).SetValue(controller, );
            var create=controller.Create();

            var createVR = create as ViewResult;
            Assert.IsNotNull(create);

            var createVM = createVR.Model as L10EditVM;
            Assert.IsNotNull(createVM);

            // User not have rights.
            controller = new L10Controller();
            controller.SetValue("SkipValidation", true);
            controller.MockUser(employee);
            Throws<PermissionsException>(() => controller.Edit(createVM));
           

            //Not Valid, no name, no users.
            controller = new L10Controller();
            controller.SetValue("SkipValidation", true);
            controller.MockUser(manager);
            var edit = controller.Edit(createVM);
            var editVR = edit as ViewResult;
            Assert.IsNotNull(editVR);
            var editVM = editVR.Model as L10EditVM;

            Assert.IsFalse(editVR.ViewData.ModelState.IsValid);
            Assert.IsNotNull(editVM.PossibleMeasurables);
            Assert.IsNotNull(editVM.PossibleMembers);
            Assert.IsNotNull(editVM.PossibleRocks);
            Assert.IsNotNull(editVM.SelectedRocks);
            Assert.IsNotNull(editVM.SelectedMeasurables);
            Assert.IsNotNull(editVM.SelectedMembers);
            Assert.AreEqual(0, editVM.SelectedMembers.Length);
        
            Assert.IsNull(editVM.Recurrence.Name);

            //Create with Name, no users.
            editVM.Recurrence.Name = "Test Recur";
            controller = new L10Controller();
            controller.SetValue("SkipValidation", true);
            controller.MockUser(manager);
            edit = controller.Edit(createVM);
            editVR = edit as ViewResult;
            Assert.IsNotNull(editVR);
            editVM = editVR.Model as L10EditVM;
            Assert.IsFalse(editVR.ViewData.ModelState.IsValid);

            //No Name, has users.
            editVM.Recurrence.Name = null;
            editVM.SelectedMembers = new[] { employee.Id };
            controller = new L10Controller();
            controller.SetValue("SkipValidation", true);
            controller.MockUser(manager);
            edit = controller.Edit(createVM);
            editVR = edit as ViewResult;
            Assert.IsNotNull(editVR);
            editVM = editVR.Model as L10EditVM;
            Assert.IsFalse(editVR.ViewData.ModelState.IsValid);
            Assert.AreEqual(1, editVM.SelectedMembers.Length);  
            Assert.AreEqual(employee.Id, editVM.SelectedMembers[0]);

            //Create with Name, add user
            editVM.Recurrence.Name = "Test Recur";
            editVM.SelectedMembers = new[] { employee.Id };
            controller = new L10Controller();
            controller.SetValue("SkipValidation", true);
            controller.MockUser(managerOtherCompany);
            Throws<PermissionsException>(()=> controller.Edit(editVM));

            controller.MockUser(manager);
            create = controller.Edit(editVM);
            var redirectVR = create as RedirectToRouteResult;
            Assert.IsNotNull(create);
            Assert.IsNotNull(redirectVR);
            //editVM = redirectVR. as L10EditVM;
            //Assert.IsTrue(createVR.ViewData.ModelState.IsValid);
            //Assert.AreEqual(1, editVM.SelectedMembers.Length);  
            //Assert.AreEqual(employee.Id, editVM.SelectedMembers[0]);
        }
        [TestMethod]
        public void EditL10Recurrence()
        {
            //UserOrganizationModel employee = null;
            OrganizationModel org = null;
            UserOrganizationModel manager = null;
            UserOrganizationModel employee = null;
            DbCommit(s =>
            {
                //Org
                org = new OrganizationModel() { };
                org.Settings.TimeZoneId = "GMT Standard Time";
                s.Save(org);

                //User
                manager = new UserOrganizationModel() { Organization = org, ManagerAtOrganization = true };
                s.Save(manager);

                employee = new UserOrganizationModel() { Organization = org, ManagerAtOrganization = true };
                s.Save(employee);
            });

            //Create The Recurrence
            var recur =new L10Recurrence(){Organization = org,OrganizationId=org.Id,Name="Name"};
            recur._DefaultAttendees = new List<L10Recurrence.L10Recurrence_Attendee>(){new L10Recurrence.L10Recurrence_Attendee(){
                L10Recurrence = recur,
                User = manager
                
            }};
            recur._DefaultMeasurables = new List<L10Recurrence.L10Recurrence_Measurable>();
            recur._DefaultRocks = new List<L10Recurrence.L10Recurrence_Rocks>();
            L10Accessor.EditL10Recurrence(manager, recur);
            
            var foundRecur = L10Accessor.GetL10Recurrence(manager, recur.Id, true);

            Assert.AreEqual(1, foundRecur._DefaultAttendees.Count);
            Assert.AreEqual(manager.Id, foundRecur._DefaultAttendees[0].User.Id);

            //Edit the recurrence
            var controller = new L10Controller();
            controller.SetValue("SkipValidation", true);
            controller.MockUser(employee);
            var editVM = new L10EditVM()
            {
                Recurrence = foundRecur,
                SelectedMembers = new[] { manager.Id, employee.Id }
            };
            Throws<PermissionsException>(() => controller.Edit(editVM));
            
            controller.MockUser(manager);
            var edit = controller.Edit(editVM);
            var editVR = edit as RedirectToRouteResult;
            Assert.IsNotNull(editVR);

            var foundRecur1 = L10Accessor.GetL10Recurrence(manager, recur.Id, true);
            Assert.AreEqual(2, foundRecur1._DefaultAttendees.Count);
            Assert.AreEqual(manager.Id, foundRecur1._DefaultAttendees[0].User.Id);
            Assert.AreEqual(employee.Id, foundRecur1._DefaultAttendees[1].User.Id);


            //Now they have access
            controller.MockUser(employee);
            editVM = new L10EditVM()
            {
                Recurrence = foundRecur,
                SelectedMembers = new[] { manager.Id, employee.Id }
            };
            edit = controller.Edit(editVM);
        }
    }
}
