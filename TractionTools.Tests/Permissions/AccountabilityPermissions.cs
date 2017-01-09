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

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class AccountabilityPermissions : BasePermissionsTest {

		[TestMethod]
		[TestCategory("Permissions")]
		public void ViewHierarchy() {
			var c = new Ctx();
			//Everyone can see by default
			c.AssertAll(p => p.ViewHierarchy(c.Org.Organization.AccountabilityChartId), c.AllUsers);
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public void ManagesAccountabilityNode() {
			var c = new Ctx();

			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.ManagerNode.Id), c.Manager);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.MiddleNode.Id), c.Manager,c.Middle);
			//We can manage the user if we manage them elsewhere
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E1MiddleNode.Id), c.Manager, c.Middle,c.E1);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E1BottomNode.Id), c.Manager, c.Middle, c.E1);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E2Node.Id), c.Manager, c.Middle, c.E2);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E3Node.Id), c.Manager, c.Middle, c.E3);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E4Node.Id), c.Manager, c.E1, c.E4);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E5Node.Id), c.Manager, c.E1, c.E5);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E6Node.Id), c.Manager, c.Middle, c.E2, c.E6);
			

		}

		[TestMethod]
		[TestCategory("Permissions")]
		public void EditHierarchy() {
			var c = new Ctx();
			c.AssertAll(p => p.EditHierarchy(c.Org.Organization.AccountabilityChartId), c.AllAdmins);
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
