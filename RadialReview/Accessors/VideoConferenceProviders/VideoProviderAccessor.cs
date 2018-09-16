using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Models.VideoConference;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors.VideoConferenceProviders {
	public class VideoProviderAccessor {

		public static ZoomUserLink GenerateLink(UserOrganizationModel caller, long userId, string zoomMeetingId, long? recurId = null, string name = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
						L10Recurrence recur = null;
						var user = s.Get<UserOrganizationModel>(userId);
						if (recurId != null) {
							perms.EditL10Recurrence(recurId.Value);
							recur = s.Get<L10Recurrence>(recurId);
						}


						if (name == null) {
							name = user.GetName() + " Zoom";
						}


						var zul = new ZoomUserLink() {
							FriendlyName = name,
							OwnerId = userId,
							ZoomMeetingId = zoomMeetingId,
						};

						s.Save(zul);

						if (recurId != null) {
							AttachVideoProviderToMeeting(s, perms, rt, zul, recurId.Value);

						}

						tx.Commit();
						s.Flush();

						return zul;
					}
				}
			}
		}

		public static void StartMeeting(UserOrganizationModel caller, long vcProviderId,string connectionId) {
			AbstractVCProvider vcp = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					vcp = s.Get<AbstractVCProvider>(vcProviderId);
					var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(vcp.OwnerId, false);
					vcp = (AbstractVCProvider)s.GetSessionImplementation().PersistenceContext.Unproxy(vcp);
					if (vcp is ZoomUserLink) {
						var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
						var group = hub.Clients.Client(connectionId).joinVideoConference((ZoomUserLink)vcp);
					} else {
						throw new PermissionsException("Unhandled video type");
					}
				}
			}


		}

		public static void AttachVideoProviderToMeeting(ISession s, PermissionsUtility perms, RealTimeUtility rt, AbstractVCProvider vcProvider, long recurId) {
			perms.EditL10Recurrence(recurId);
			if (vcProvider.Id <= 0)
				throw new PermissionsException("Link ID less than zero");

			var link = new L10Recurrence.L10Recurrence_VideoConferenceProvider {
				L10Recurrence = s.Load<L10Recurrence>(recurId),
				Provider = vcProvider,
			};

			rt.UpdateRecurrences(recurId).AddLowLevelAction(x => {
				x.addVideoProvider(vcProvider);
			});

			s.Save(link);
		}
	}
}