using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TractionTools.Tests.Utilities;

namespace TractionTools.Tests.Permissions {
	public class BasePermissionsTest {
		public class Ctx {
			public FullOrg Org { get; set; }
			public Org OtherOrg { get; set; }

			//Helpers to dive into Org
			public long Id { get { return Org.Id; } }
			public UserOrganizationModel Manager { get { return Org.Manager; } }
			public UserOrganizationModel Employee { get { return Org.Employee; } }
			public List<UserOrganizationModel> AllManagers { get { return Org.AllManagers; } }
			public UserOrganizationModel Middle { get { return Org.Middle; } }


			public PermissionsAccessor Perms { get; set; }

			public Ctx() {
				Org = OrgUtil.CreateFullOrganization();
				OtherOrg = OrgUtil.CreateOrganization();
				Perms = new PermissionsAccessor();
			}

			public void AssertAll(Action<PermissionsUtility> ensurePermitted, IEnumerable<UserOrganizationModel> trueFor) {
				AssertAll(ensurePermitted, trueFor.ToArray());
			}
			public void AssertAll(Action<PermissionsUtility> ensurePermitted, params UserOrganizationModel[] trueFor) {
				var myOrgUsers = Org.AllUsers.Where(x => trueFor.Any(y => y.Id == x.Id));
				var otherOrgUsers = OtherOrg.AllUsers.Where(x => trueFor.Any(y => y.Id == x.Id));
				
				Org.AssertAllUsers(user => Perms.IsPermitted(user, ensurePermitted), myOrgUsers);
				OtherOrg.AssertAllUsers(user => Perms.IsPermitted(user, ensurePermitted), otherOrgUsers);

			}

		}

	}
}