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
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Positions;

namespace RadialReview.Hooks.Realtime.L10 {
    public class RealTime_Positions : IPositionHooks {
        public bool CanRunRemotely() {
            return false;
        }

        public HookPriority GetHookPriority() {
            return HookPriority.UI;
        }

        public async Task CreatePosition(ISession s, OrganizationPositionModel creator) {
            // Noop
        }

        public async Task UpdatePosition(ISession s, OrganizationPositionModel position, IPositionHookUpdates updates) {
            using (var rt = RealTimeUtility.Create()) {
                var updater = rt.UpdateOrganization(position.Organization.Id);

                if (updates.NameChanged) {
                    updater.ForceUpdate(new AngularPosition(position.Id) {
                        Name = position.GetName() ?? Removed.String(),
                    });
                }

                if (updates.WasDeleted) {
                    updater.ForceUpdate(new AngularPosition(position.Id) {
                        Name = Removed.String(),
                    });
                }
            }
        }
    }
}
