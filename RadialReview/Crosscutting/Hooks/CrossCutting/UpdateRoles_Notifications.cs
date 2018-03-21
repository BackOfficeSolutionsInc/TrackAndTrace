using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Askables;
using System.Threading.Tasks;
using RadialReview.Notifications;
using RadialReview.Utilities.RealTime;
using RadialReview.Models;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using RadialReview.Models.Components;

namespace RadialReview.Hooks {
	public class UpdateRoles_Notifications : IRolesHook {


		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Database;
		}

		private void AddToNotification(ISession s, RoleModel role, string type) {


			var links = s.QueryOver<RoleLink>().Where(x => x.DeleteTime == null && x.RoleId == role.Id).List().ToList();

			var details = "\"" + role.Role + "\"";

			if (links.Any()) {
				using (var rt = RealTimeUtility.Create()) {
					foreach (var link in links) {
						var name = type;
						if (link != null) {
							var attach = AttachAccessor.PopulateAttachUnsafe(s, link.GetAttach());
							name += " for " + attach.Name;
						}
						var members = AttachAccessor.GetTinyMembersUnsafe(s, link.GetAttach());
						foreach (var pop in members) {
							//var message = "Role created";
							var url = (string)null;
							if (pop != null) {
								url = "/User/Details/" + pop.UserOrgId;
							}
							var myDetails = details;
							if (link.AttachType != AttachType.User) {
								myDetails += " for " + pop.GetName();
							}

							var eventId = "role_"+role.Id +"_"+ pop.GetName() + "_" + DateTime.UtcNow.Ticks / TimeSpan.FromMinutes(10).Ticks;

							PubSub.Publish(s, rt, x => x.Create(role.OrganizationId, name, myDetails, url, NotificationKind.Roles, ForModel.Create<OrganizationModel>(role.OrganizationId),eventId: eventId));
						}
					}
				}
			}
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task CreateRole(ISession s, RoleModel role) {
            AddToNotification(s, role, "Role created");
		}

		public async Task DeleteRole(ISession s, RoleModel role) {
			AddToNotification(s, role, "Role deleted");
		}

		public async Task UpdateRole(ISession s, RoleModel role) {
			AddToNotification(s, role, "Role updated");
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
	}
}