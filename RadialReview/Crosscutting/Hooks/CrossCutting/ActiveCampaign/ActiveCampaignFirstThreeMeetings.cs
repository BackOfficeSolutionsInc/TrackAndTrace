using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using RadialReview.Utilities.Integrations;
using RadialReview.Utilities;
using static RadialReview.Utilities.Config;

namespace RadialReview.Hooks.CrossCutting.ActiveCampaign {
	public class ActiveCampaignFirstThreeMeetings : IMeetingEvents {
		public bool CanRunRemotely() {
			return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.Lowest;
		}

		public ActiveCampaignConfig Configs { get; protected set; }
		public ActiveCampaignConnector Connector { get; protected set; }

		public ActiveCampaignFirstThreeMeetings() {
			Configs = Config.GetActiveCampaignConfig();
			Connector = new ActiveCampaignConnector(Configs);
		}


		public async Task ConcludeMeeting(ISession s, L10Recurrence recur, L10Meeting meeting) {
			var limit = TimeSpan.FromMinutes(30);
			if (meeting.CompleteTime - meeting.StartTime > limit) {
				var rows = s.QueryOver<L10Meeting>()
					.Where(x => x.DeleteTime == null && x.OrganizationId == meeting.OrganizationId && x.CompleteTime != null && x.StartTime != null)
					.Select(x=>x.CompleteTime, x=>x.StartTime).List<object[]>()
					.Select(x=>new { Duration = ((DateTime)x[0]) - ((DateTime)x[1]) })
					.Where(x=> x.Duration > limit).ToList();//.RowCount();

				//var rowCount = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.OrganizationId == meeting.OrganizationId && (x.CompleteTime - x.StartTime > limit)).RowCount();
				if (rows.Count <= 3) {

					var pid = s.Get<OrganizationModel>(meeting.OrganizationId).PrimaryContactUserId;
					if (pid != null) {
						var email = s.Get<UserOrganizationModel>(pid.Value).GetEmail();
						await Connector.EventAsync("InitialMeetingConclude-" + rows.Count, email);
					}
				}
			}
		}

		#region Noop
		public async Task AddAttendee(ISession s, long recurrenceId, UserOrganizationModel user, L10Recurrence.L10Recurrence_Attendee attendee) {
			//Noop
		}


		public async Task CreateRecurrence(ISession s, L10Recurrence recur) {
			//Noop
		}

		public async Task DeleteMeeting(ISession s, L10Meeting meeting) {
			//Noop
		}

		public async Task DeleteRecurrence(ISession s, L10Recurrence recur) {
			//Noop
		}

		public async Task RemoveAttendee(ISession s, long recurrenceId, long userId) {
			//Noop
		}

		public async Task StartMeeting(ISession s, L10Recurrence recur, L10Meeting meeting) {
			//Noop
		}

		public async Task UndeleteRecurrence(ISession s, L10Recurrence recur) {
			//Noop
		}
		#endregion
	}
}