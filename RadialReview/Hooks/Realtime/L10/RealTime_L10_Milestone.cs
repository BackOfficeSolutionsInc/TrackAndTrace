using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Dashboard;
using RadialReview.Models;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Rocks;
using RadialReview.Models.Askables;

namespace RadialReview.Hooks.Realtime.L10 {
	public class RealTime_L10_Milestone : IMilestoneHook {
		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task CreateMilestone(ISession s, Milestone milestone) {

			var rock = s.Get<RockModel>(milestone.RockId);
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			var userMeetingHub = hub.Clients.Group(RealTimeHub.Keys.UserId(rock.ForUserId));
			var updates = new AngularRecurrence(-2);
			updates.Milestones = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(milestone, rock.AccountableUser,rock.Name));
			userMeetingHub.update(updates);

		}

		public async Task UpdateMilestone(ISession s, UserOrganizationModel caller, Milestone milestone, IMilestoneHookUpdates updates) {

			var rock = s.Get<RockModel>(milestone.RockId);
			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			var group = hub.Clients.Group(RealTimeHub.Keys.UserId(rock.ForUserId), RealTimeHelpers.GetConnectionString());
			if (updates.IsDeleted) {
				var update1 = new AngularRecurrence(-2);
				update1.Milestones = AngularList.CreateFrom(AngularListType.Remove, new AngularTodo(milestone, rock.AccountableUser));
				group.update(update1);
			} else
				group.update(new AngularUpdate() { new AngularTodo(milestone, rock.AccountableUser, rock.Name) });

		}
	}
}