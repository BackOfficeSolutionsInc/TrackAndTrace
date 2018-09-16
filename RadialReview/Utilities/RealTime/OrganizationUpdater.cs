﻿using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Notifications;
using RadialReview.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.RealTime {


    public partial class RealTimeUtility {
        public class RTOrganizationUpdater {

            protected long _OrganizationId { get; set; }
            protected RealTimeUtility rt;
            public RTOrganizationUpdater(long orgId, RealTimeUtility rt)
            {
                _OrganizationId = orgId;
                this.rt = rt;
			}
			public RTOrganizationUpdater NotificationStatus(long notificationId,bool seen, string username) {
				rt.AddAction(() => {
					var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
					var group = hub.Clients.User(username);
					var updates = new AngularNotification(notificationId) {
						 Seen = seen
					};
					group.update(updates);
				});
				return this;
			}

			public RTOrganizationUpdater Notification(Notification notification,IEnumerable<string> usernames) {
				rt.AddAction(() => {
					var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
					var group = hub.Clients.Users(usernames.ToList());
					var updates = new {
#pragma warning disable CS0618 // Type or member is obsolete
						Notifications = AngularList.CreateFrom(AngularListType.Add, new AngularNotification(notification))
#pragma warning restore CS0618 // Type or member is obsolete
					};
					group.update(updates);
				});
				return this;
			}

            protected void UpdateAll(Func<long, IAngularId> itemGenerater, bool forceNoSkip = false)
            {
                var updater = rt.GetUpdater<RealTimeHub>(RealTimeHub.Keys.OrganizationId(_OrganizationId),!forceNoSkip);
                updater.Add(itemGenerater(_OrganizationId));
			}
			public RTOrganizationUpdater Update(IAngularId item, bool forceNoSkip = false) {
				return Update(rid => item, forceNoSkip);
			}
			public RTOrganizationUpdater ForceUpdate(IAngularId item) {
				return Update(rid => item, true);
			}
			public RTOrganizationUpdater Update(Func<long, IAngularId> item,bool forceNoSkip = false)
            {
                rt.AddAction(() => {
                    UpdateAll(item, forceNoSkip);
                });
                return this;
            }

			public RTOrganizationUpdater AddLowLevelAction(Action<dynamic> action, bool forceNoSkip = false) {
				rt.AddAction(() => {
					var updater = rt.GetUpdater<RealTimeHub>(RealTimeHub.Keys.OrganizationId(_OrganizationId), forceNoSkip);
					action(updater);
				});
				return this;
			}
		}

	}
}