using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.Utilities;
using RadialReview.Accessors;
using RadialReview.Models.Issues;
using System.Threading.Tasks;
using RadialReview.Utilities;
using static RadialReview.Models.PermItem;
using RadialReview.Exceptions;
using RadialReview.Models.Todo;
using System.Collections.Generic;
using RadialReview.Models;
using RadialReview.Controllers;
using RadialReview.Models.Askables;
using RadialReview;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class RockPermissionTests : BasePermissionsTest {

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task ViewRock() {
			var c = await Ctx.Build();
			
			var l10= await L10Accessor.CreateBlankRecurrence(c.Manager, c.Id, false);

			//var rock = new RockModel() {
			//	ForUserId = c.Middle.Id,
			//	OrganizationId = c.Id,
			//	Rock = "rock"
			//};

			MockHttpContext();
			var rock = await L10Accessor.CreateAndAttachRock(c.Manager, l10.Id, c.Middle.Id, "rock");
			//var rock = await L10Accessor.CreateRock(c.Manager, l10.Id, L10Controller.AddRockVm.CreateRock(l10.Id, c.Middle.Id, "rock"));

			c.AssertAll(p => p.ViewRock(rock.Id), c.AllUsers);
			Assert.Inconclusive("Are the view permissions restrictive enough");
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditRock() {
			var c = await Ctx.Build();
			var l10 = await L10Accessor.CreateBlankRecurrence(c.Middle, c.Id, false);
			//var rock = new RockModel() {
			//	ForUserId = c.E5.Id,
			//	OrganizationId = c.Id,
			//	Rock = "rock"
			//};
			MockHttpContext();
			//Make the rock, assign to L10
			var rock = await L10Accessor.CreateAndAttachRock(c.Manager, l10.Id,c.E5.Id,"rock");

			var perm = new Action<PermissionsUtility>(p => p.EditRock(rock.Id));

			c.AssertAll(perm, c.Manager, c.Middle, c.E1);

			//Add attendee E5
			await L10Accessor.AddAttendee(c.Manager, l10.Id, c.E5.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.E5, c.E1);

			//Add attendee E4
			await L10Accessor.AddAttendee(c.Manager, l10.Id, c.E4.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.E5, c.E1, c.E4);


			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, false, null);
				c.AssertAll(perm, c.Manager, c.E5, c.E1, c.E4);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, false, null);
				c.AssertAll(perm, c.E5, c.Manager, c.E1, c.E4);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, false, null);
				c.AssertAll(perm, c.Manager, c.E1);
			}




		}
		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditRock_OutsideMeeting() {
			var c = await Ctx.Build();			
			var rock = new RockModel() {
				ForUserId = c.E2.Id,
				Rock="Rock"
			};
			MockHttpContext();
			await RockAccessor.EditRocks(c.Middle, c.E2.Id, rock.AsList(), false, false);
			var perm = new Action<PermissionsUtility>(p => p.EditRock(rock.Id));

			OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: false, employeesCanEditSelf: false);
			c.AssertAll(perm, c.Manager, c.Middle);
			OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: true, employeesCanEditSelf: false);
			c.AssertAll(perm, c.Manager, c.Middle, c.E2);
			OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: false, employeesCanEditSelf: true);
			c.AssertAll(perm, c.Manager, c.Middle, c.E2);


			rock = new RockModel() {
				ForUserId = c.E6.Id,
				Rock = "Rock2"
			};
			await RockAccessor.EditRocks(c.Middle, c.E6.Id, rock.AsList(), false, false);

			OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: false, employeesCanEditSelf: false);
			c.AssertAll(perm, c.Manager, c.Middle, c.E2);
			OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: true, employeesCanEditSelf: false);
			c.AssertAll(perm, c.Manager, c.Middle, c.E2);
			OrganizationAccessor.Edit(c.Manager, c.Id, managersCanEditSelf: false, employeesCanEditSelf: true);
			c.AssertAll(perm, c.Manager, c.Middle, c.E2, c.E6);


		}
		/*
		 
		[TestMethod]
		[TestCategory("Permissions")]
		public void XXX() {
			var c = await Ctx.Build();
			c.AssertAll(p => p.XXX(YYY), c.Manager);
		}

		 */
	}
}
