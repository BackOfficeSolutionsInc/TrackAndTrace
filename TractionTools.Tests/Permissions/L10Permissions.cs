using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.Utilities;
using RadialReview.Accessors;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class L10Permissions {
		[TestMethod]
		public void TestCreateRecurrence() {

			var org = OrgUtil.CreateFullOrganization();
			var otherOrg = OrgUtil.CreateOrganization();

			var perms = new PermissionsAccessor();

			perms.IsPermitted(



		}
	}
}
