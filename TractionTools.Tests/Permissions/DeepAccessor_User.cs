using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Utilities;
using RadialReview.Exceptions;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class RecursionsPermissions : BasePermissionsTest {
		[TestMethod]
		[TestCategory("Permissions")]
		public void OwnedBelowOrEqual() {
			var c = new Ctx();

			foreach(var u in c.Org.AllUsers)
				PermissionsAccessor.EnsurePermitted(c.Manager, p => p.OwnedBelowOrEqual(u.Id));

			foreach (var u in c.OtherOrg.AllUsers)
				Throws<PermissionsException>(() => PermissionsAccessor.EnsurePermitted(c.Manager, p => p.OwnedBelowOrEqual(u.Id)));



			c.AssertAll(p => p.OwnedBelowOrEqual(c.Employee.Id), c.Manager, c.Employee);
			c.AssertAll(p => p.OwnedBelowOrEqual(c.Middle.Id), c.Manager, c.Middle);
			c.AssertAll(p => p.OwnedBelowOrEqual(c.E1.Id), c.Manager, c.Middle, c.E1);
			c.AssertAll(p => p.OwnedBelowOrEqual(c.E2.Id), c.Manager, c.Middle, c.E2);
			c.AssertAll(p => p.OwnedBelowOrEqual(c.E3.Id), c.Manager, c.Middle, c.E3);
			c.AssertAll(p => p.OwnedBelowOrEqual(c.E4.Id), c.Manager, c.E1, c.E4);
			c.AssertAll(p => p.OwnedBelowOrEqual(c.E5.Id), c.Manager, c.E1, c.E5);
			c.AssertAll(p => p.OwnedBelowOrEqual(c.E6.Id), c.Manager, c.Middle, c.E2, c.E6);
			
			c.AssertAll(p => p.OwnedBelowOrEqual(c.E7.Id), c.Manager,c.E7);
			c.AssertAll(p => p.OwnedBelowOrEqual(c.Client.Id), c.Manager,c.Client);
			
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
