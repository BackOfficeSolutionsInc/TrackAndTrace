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
	public class OrganizationPermissions : BasePermissionsTest {
		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditOrganization() {
			var c = await Ctx.Build();
			var perm = new Action<PermissionsUtility>(p => p.EditOrganization(c.Id));

			//Only managers by default
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task ViewOrganization() {
			var c = await Ctx.Build();
			c.AssertAll(p => p.ViewOrganization(c.Id), c.AllUsers);

		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditCompanyValues() {
			var c = await Ctx.Build();
			var perm = new Action<PermissionsUtility>(p => p.EditCompanyValues(c.Id));
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
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
