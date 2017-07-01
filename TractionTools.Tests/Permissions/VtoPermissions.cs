using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Utilities;
using System.Threading.Tasks;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class VtoPermissions : BasePermissionsTest {
		[TestMethod]
		[TestCategory("Permissions")]
		public async Task CreateVTO() {
			var c = await Ctx.Build();
			c.AssertAll(p => p.CreateVTO(c.Id), c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task ViewVto() {
			var c = await Ctx.Build();

			var l10 = await L10Accessor.CreateBlankRecurrence(c.E2, c.Id);
			c.AssertAll(p => p.ViewVTO(l10.VtoId), c.E2, c.Manager);

			L10Accessor.AddAttendee(c.E2, l10.Id, c.E2.Id);
			c.AssertAll(p => p.ViewVTO(l10.VtoId), c.E2, c.Manager);


			L10Accessor.AddAttendee(c.E2, l10.Id, c.E1.Id);
			c.AssertAll(p => p.ViewVTO(l10.VtoId), c.E2, c.Manager, c.E1);
		}


		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditVTO() {
			var c = await Ctx.Build();
			var l10 = await L10Accessor.CreateBlankRecurrence(c.E2, c.Id);
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
			var c = await Ctx.Build();
			c.AssertAll(p => p.XXX(YYY), c.Manager);
			//var perm = new Action<PermissionsUtility>(p=>p.XXX(YYY));
			//c.AssertAll(perm, c.Manager);
		}

		 */
}
