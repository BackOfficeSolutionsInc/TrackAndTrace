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
using RadialReview.Models.L10;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Accessors;
using RadialReview.Utilities;

namespace RadialReview.Hooks.Realtime.L10 {
	public class RealTime_L10_Headline : IHeadlineHook {
		public bool CanRunRemotely() {
			return false;
		}
		
		public async Task CreateHeadline(ISession s, PeopleHeadline headline) {
			var recurrenceId = headline.RecurrenceId;
			if (recurrenceId > 0) {
				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
				var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

				if (headline.CreatedDuringMeetingId == null) {
					headline.CreatedDuringMeetingId = L10Accessor._GetCurrentL10Meeting(s, PermissionsUtility.CreateAdmin(s), recurrenceId, true, false, false).NotNull(x => (long?)x.Id);
				}
				var aHeadline = new AngularHeadline(headline);
				meetingHub.appendHeadline(".headlines-list", headline.ToRow());
				meetingHub.showAlert("Created people headline.", 1500);
				var updates = new AngularRecurrence(recurrenceId);
				updates.Headlines = AngularList.CreateFrom(AngularListType.Add, aHeadline);
				meetingHub.update(updates);
			}
		}

		public async Task UpdateHeadline(ISession s, PeopleHeadline headline, IHeadlineHookUpdates updates) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(headline.RecurrenceId), RealTimeHelpers.GetConnectionString());

			if (updates.MessageChanged) {
				group.updateHeadlineMessage(headline.Id, headline.Message);

				group.update(new AngularUpdate() {
					new AngularHeadline(headline.Id) {
						Name = headline.Message
					}
				});
			}

		}
	}
}