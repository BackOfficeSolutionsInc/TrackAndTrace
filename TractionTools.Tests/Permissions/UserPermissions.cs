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

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class UserPermissions : BasePermissionsTest {
		[TestMethod]
		[TestCategory("Permissions")]
		public void EditUserModel() {
			var c = new Ctx();

			c.Org.RegisterUser(c.Employee);

			c.AssertAll(p => p.EditUserModel(YYY), c.Manager);
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
