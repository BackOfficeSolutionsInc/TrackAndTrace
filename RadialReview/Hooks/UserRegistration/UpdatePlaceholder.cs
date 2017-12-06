using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.UserModels;
using System.Threading.Tasks;
using RadialReview.Models;

namespace RadialReview.Hooks.UserRegistration {
	public class UpdatePlaceholder : IUserRoleHook {
		public bool CanRunRemotely() {
			return true;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Database;
		}

		public async Task AddRole(ISession s, long userId, UserRoleType type) {
			if (type == UserRoleType.PlaceholderOnly)
				s.Get<UserOrganizationModel>(userId).IsPlaceholder = true;
		}
		
		public async Task RemoveRole(ISession s, long userId, UserRoleType type) {
			if (type == UserRoleType.PlaceholderOnly)
				s.Get<UserOrganizationModel>(userId).IsPlaceholder = false;
		}
	}
}