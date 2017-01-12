using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Accessors.TodoIntegrations;
using RadialReview.Exceptions;
using RadialReview.Hooks;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors {
	public class HeadlineAccessor {
		public static async Task<bool> CreateHeadline(ISession s, PermissionsUtility perms, PeopleHeadline headline) {
			if (headline.Id != 0)
				throw new PermissionsException("Id was not zero");

			if (headline.CreatedDuringMeetingId == -1)
				headline.CreatedDuringMeetingId = null;

			if (headline.CreatedDuringMeetingId != null)
				perms.ViewL10Meeting(headline.CreatedDuringMeetingId.Value);

			perms.ViewOrganization(headline.OrganizationId);
			perms.EditL10Recurrence(headline.RecurrenceId);

			headline.CreatedBy = perms.GetCaller().Id;

			if (headline.OwnerId == 0 && headline.Owner == null) {
				headline.OwnerId = perms.GetCaller().Id;
				headline.Owner = perms.GetCaller();
			} else if (headline.OwnerId == 0 && headline.Owner != null) {
				headline.OwnerId = headline.OwnerId;
			} else if (headline.OwnerId != 0 && headline.Owner == null) {
				headline.Owner = s.Load<UserOrganizationModel>(headline.OwnerId);
			}

			perms.ViewUserOrganization(headline.OwnerId, false);

			if (headline.AboutId != null)
				perms.ViewRGM(headline.AboutId.Value);


			L10Recurrence r = null;
			var recurrenceId = headline.RecurrenceId;

			if (recurrenceId > 0) {
				r = s.Get<L10Recurrence>(recurrenceId);
				r.Pristine = false;
				s.Update(r);
			}

			if (String.IsNullOrWhiteSpace(headline.HeadlinePadId))
				headline.HeadlinePadId = Guid.NewGuid().ToString();

			if (!string.IsNullOrWhiteSpace(headline._Details))
				await PadAccessor.CreatePad(headline.HeadlinePadId, headline._Details);

			s.Save(headline);
			headline.Ordering = -headline.Id;
			s.Update(headline);

			if (headline.AboutId.HasValue)
				headline.About = s.Get<ResponsibilityGroupModel>(headline.AboutId.Value);
			
			headline.Owner = s.Get<UserOrganizationModel>(headline.OwnerId);

			HooksRegistry.Each<IHeadlineHook>(x => x.CreateHeadline(s, headline));

			if (recurrenceId > 0) {
				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
				var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

				if (headline.CreatedDuringMeetingId == null) {
					headline.CreatedDuringMeetingId = L10Accessor._GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false).Id;
				}

				var aHeadline = new AngularHeadline(headline);

				meetingHub.appendHeadline(".headlines-list", headline.ToRow());
				meetingHub.showAlert("Created people headline.",1500);
				var updates = new AngularRecurrence(recurrenceId);
				updates.Headlines = AngularList.CreateFrom(AngularListType.Add, aHeadline);
				meetingHub.update(updates);
				Audit.L10Log(s, perms.GetCaller(), recurrenceId, "CreateHeadline", ForModel.Create(headline), headline.NotNull(x => x.Message));
			}

			return true;
		}

		public static async Task<bool> CreateHeadline(UserOrganizationModel caller, PeopleHeadline headline) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var created = await CreateHeadline(s, perms, headline);
					tx.Commit();
					s.Flush();
					return created;
				}
			}
		}

		public static PeopleHeadline GetHeadline(UserOrganizationModel caller, long headlineId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewHeadline(headlineId);
					var h= s.Get<PeopleHeadline>(headlineId);

					if (h.Owner != null) {
						h.Owner.GetName();
						h.Owner.GetImageUrl();
					}
					if (h.About != null) {
						h.About.GetName();
						h.About.GetImageUrl();
					}

					return h;
				}
			}
		}

		public static List<NameId> GetRecurrencesWithHeadlines(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					List<long> attendee_recurrences;
					var uniqueL10NameIds = L10Accessor.GetVisibleL10Meetings_Tiny(s, perms, userId, out attendee_recurrences);
					var uniqueL10Ids = uniqueL10NameIds.Select(x => x.Id).ToList();


					return s.QueryOver<L10Recurrence>()
						.Where(x => x.DeleteTime == null && x.HeadlineType == Model.Enums.PeopleHeadlineType.HeadlinesList)
						.WhereRestrictionOn(x => x.Id).IsIn(uniqueL10Ids)
						.Select(x=>x.Id,x=>x.Name)
						.List<object[]>().Select(x=>new NameId((string)x[1],(long)x[0])).ToList();				

				}
			}
		}
	}
}