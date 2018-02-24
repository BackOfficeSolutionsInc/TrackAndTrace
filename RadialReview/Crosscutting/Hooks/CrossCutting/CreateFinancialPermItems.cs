using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Accessors;

namespace RadialReview.Hooks.CrossCutting {
	public class CreateFinancialPermItems : IOrganizationHook {
		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Database;
		}

		public async Task CreateOrganization(ISession s, UserOrganizationModel creator, OrganizationModel organization) {
			PermissionsAccessor.CreatePermItems(s, creator, PermItem.ResourceType.UpdatePaymentForOrganization, organization.Id, PermTiny.Admins(true, true, true) );
		}

	}
}