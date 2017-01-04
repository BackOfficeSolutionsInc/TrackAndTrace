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

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class RockPermissions : BasePermissionsTest {

		[TestMethod]
		[TestCategory("Permissions")]
		public void ViewRock() {
			var c = new Ctx();

			var l10=L10Accessor.CreateBlankRecurrence(c.Manager, c.Id);

			var rock = new RockModel() {
				ForUserId = c.Middle.Id,
				OrganizationId = c.Id,
				Rock = "rock"
			};
			L10Accessor.CreateRock(c.Manager, l10.Id, L10Controller.AddRockVm.CreateRock(l10.Id, rock));

			c.AssertAll(p => p.ViewRock(rock.Id), c.AllUsers);
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public void EditRock() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var rock = new RockModel() {
				ForUserId = c.E5.Id,
				OrganizationId = c.Id,
				Rock = "rock"
			};
			//Make the rock, assign to L10
			L10Accessor.CreateRock(c.Manager, l10.Id, L10Controller.AddRockVm.CreateRock(l10.Id, rock));

			var perm = new Action<PermissionsUtility>(p => p.EditRock(rock.Id));

			c.AssertAll(perm, c.Manager, c.Middle, c.E1);

			//Add attendee E5
			L10Accessor.AddAttendee(c.Manager, l10.Id, c.E5.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.E5, c.E1);

			//Add attendee E4
			L10Accessor.AddAttendee(c.Manager, l10.Id, c.E4.Id);
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

		/*
		 
		[TestMethod]
		[TestCategory("Permissions")]
		public void XXX() {
			var c = new Ctx();
			c.AssertAll(p => p.XXX(YYY), c.Manager);
		}

		 */
	}
}
