using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Utilities;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class VtoPermissions : BasePermissionsTest {
		[TestMethod]
		[TestCategory("Permissions")]
		public void CreateVTO() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateVTO(c.Id), c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public void ViewVto() {
			var c = new Ctx();

			var l10 = L10Accessor.CreateBlankRecurrence(c.E2, c.Id);
			c.AssertAll(p => p.ViewVTO(l10.VtoId), c.E2, c.Manager);

			L10Accessor.AddAttendee(c.E2, l10.Id, c.E2.Id);
			c.AssertAll(p => p.ViewVTO(l10.VtoId), c.E2, c.Manager);


			L10Accessor.AddAttendee(c.E2, l10.Id, c.E1.Id);
			c.AssertAll(p => p.ViewVTO(l10.VtoId), c.E2, c.Manager, c.E1);
		}


		[TestMethod]
		[TestCategory("Permissions")]
		public void EditVTO() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.E2, c.Id);
			var perm = new Action<PermissionsUtility>(p => p.EditVTO(l10.VtoId));

			c.AssertAll(perm, c.E2, c.Manager);

			L10Accessor.AddAttendee(c.E2, l10.Id, c.E2.Id);
			c.AssertAll(perm, c.E2, c.Manager);


			L10Accessor.AddAttendee(c.E2, l10.Id, c.E1.Id);
			c.AssertAll(perm, c.E2, c.Manager, c.E1);
		}

	}

	/*
		 
		[TestMethod]
		[TestCategory("Permissions")]
		public void XXX() {
			var c = new Ctx();
			c.AssertAll(p => p.XXX(YYY), c.Manager);
			//var perm = new Action<PermissionsUtility>(p=>p.XXX(YYY));
			//c.AssertAll(perm, c.Manager);
		}

		 */
}
