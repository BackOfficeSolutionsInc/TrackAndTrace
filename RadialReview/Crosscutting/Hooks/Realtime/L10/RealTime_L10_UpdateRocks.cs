using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Accessors;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Base;
using RadialReview.Hooks.Realtime;
using RadialReview.Models;

namespace RadialReview.Hooks.Realtime.L10 {
    public class RealTime_L10_UpdateRocks : IRockHook, IMeetingRockHook {

        public bool CanRunRemotely() {
            return false;
        }

        public HookPriority GetHookPriority() {
            return HookPriority.UI;
        }

        public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
            using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
                rt.UpdateRecurrences(recurRock.L10Recurrence.Id).Update(rid => new AngularRock(recurRock.ForRock.Id) { VtoRock = recurRock.VtoRock });

                var adminPerms = PermissionsUtility.CreateAdmin(s);
                ///
                var recurrenceId = recurRock.L10Recurrence.Id;
                var recur = s.Get<L10Recurrence>(recurrenceId);

                var current = L10Accessor._GetCurrentL10Meeting_Unsafe(s, recurrenceId, true, false, false);
                var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));


                if (current != null) {
                    _UpdateRocksInMeeting(s, rock, recurRock, adminPerms, recurrenceId, recur, current, group);
                } else {
                    var recurRocks = L10Accessor.GetRocksForRecurrence(s, adminPerms, recurrenceId);
                    //var arecur = new AngularRecurrence(recurrenceId) {
                    //	Rocks = AngularList.Create(AngularListType.ReplaceAll, recurRocks.Select(x => new AngularRock(x)).ToList()),
                    //};
                    string focus = null;
                    if (recurRocks.Any() && recurRocks.Last().ForRock != null) {
                        focus = "[data-rock='" + rock.Id /*recurRocks.Last().ForRock.Id */+ "'] input:visible:first";
                    }

                    var arecur = new AngularUpdate { new AngularRecurrence(recurrenceId) {
                            Rocks = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularRock(recurRock)),
                           // Focus = focus,
                        }
                    };
                    group.update(arecur);

                    if (RealTimeHelpers.GetConnectionString() != null) {
                        var me = hub.Clients.Client(RealTimeHelpers.GetConnectionString());
                        me.update(new AngularUpdate() {
                            new AngularRecurrence(recurrenceId) {
                                Focus = focus
                            }
                        });
                    }
                }
            }
        }

        private static void _UpdateRocksInMeeting(ISession s, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock, PermissionsUtility adminPerms, long recurrenceId, L10Recurrence recur, L10Meeting current, dynamic group) {
            _UpdateL10MeetingRocks(s, adminPerms, recurrenceId, recur, current, group);

            //Update Angular
            var arecur = new AngularRecurrence(recurrenceId) {
                Rocks = AngularList.Create(AngularListType.ReplaceIfNewer, new[]{new AngularRock(recurRock){
                            ForceOrder =int.MaxValue,
                        }}),
                // Focus = "[data-rock='" + rock.Id + "'] input:visible:first"
            };
            group.update(new AngularUpdate { arecur });


            if (RealTimeHelpers.GetConnectionString() != null) {
                var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                var me = hub.Clients.Client(RealTimeHelpers.GetConnectionString());
                me.update(new AngularUpdate {new AngularRecurrence(recurrenceId) {
                    Focus = "[data-rock='" + rock.Id + "'] input:visible:first"
                }});
            }
        }

        private static void _UpdateL10MeetingRocks(ISession s, PermissionsUtility adminPerms, long recurrenceId, L10Recurrence recur, L10Meeting current, dynamic group) {
            var rocksAndMilestones = L10Accessor.GetRocksForMeeting(s, adminPerms, recurrenceId, current.Id);
            var builder = "";
            if (!recur.CombineRocks && rocksAndMilestones.Where(x => x.Rock.VtoRock/*.ForRock.CompanyRock*/).Any()) {
                var crow = ViewUtility.RenderPartial("~/Views/L10/partial/CompanyRockGroup.cshtml", rocksAndMilestones.Select(x => x.Rock).Where(x => x.VtoRock/* ForRock.CompanyRock*/).ToList());
                builder += " <div class='company-rock-container'> " + crow.Execute() + " <hr/> </div> ";
            }

            //Update L10 meeting
            var row = ViewUtility.RenderPartial("~/Views/L10/partial/RockGroup.cshtml", rocksAndMilestones.Select(x => x.Rock).ToList());
            builder = builder + row.Execute();
            group.updateRocks(builder);
        }

        public async Task DetachRock(ISession s, RockModel rock, long recurrenceId) {
            using (var rt = RealTimeUtility.Create(/*RealTimeHelpers.GetConnectionString()*/)) {
                var recur = s.Get<L10Recurrence>(recurrenceId);

                if (recur.MeetingInProgress != null) {
                    var meeting = s.Get<L10Meeting>(recur.MeetingInProgress.Value);
                    rt.UpdateRecurrences(recurrenceId).AddLowLevelAction(group => {
                        _UpdateL10MeetingRocks(s, PermissionsUtility.CreateAdmin(s), recurrenceId, recur, meeting, group);
                    });
                }

                rt.UpdateRecurrences(recurrenceId).Update(
                    new AngularRecurrence(recurrenceId) {
                        Rocks = AngularList.CreateFrom(AngularListType.ReplaceIfExists, new AngularRock(rock.Id) {
							Archived= true
						})
                    }
                );
            }
        }

        public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {
            using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
                var rock_ids = RealTimeHelpers.GetRecurrenceRockData(s, rock.Id);
                var allRecurIds = rock_ids.RecurData.Select(x => x.RecurrenceId);
                //var allMeetingRockIds = rock_ids.MeetingData.Select(x => x.MeetingRockId);

                var updater = rt.UpdateRecurrences(allRecurIds).Update(rid => new AngularRock(rock, null));
                //Update Name
                if (updates.MessageChanged)
                    updater.AddLowLevelAction(x => x.updateRockName(rock.Id, rock.Name));

				//Update Due Date
				if (updates.DueDateChanged)
					updater.AddLowLevelAction(x => x.updateRockDueDate(rock.Id, rock.DueDate.Value.ToJsMs()));

				//Update Completion
				if (updates.StatusChanged) {
                    //foreach (var meetingRockId in allMeetingRockIds) {
                    updater.AddLowLevelAction(x => x.updateRockCompletion(rock.Id, rock.Completion.ToString()));
                    //}
                    updater.AddLowLevelAction(x => x.updateRockCompletion(0, rock.Completion.ToString(), rock.Id));
                }
            }
        }

        public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
            using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
                rt.UpdateRecurrences(recurRock.L10Recurrence.Id)
                    .Update(rid => new AngularRock(recurRock.ForRock.Id) {
                        VtoRock = recurRock.VtoRock
                    });
            }


            var adminPerms = PermissionsUtility.CreateAdmin(s);
            var rock = recurRock.ForRock;
            var recurrenceId = recurRock.L10Recurrence.Id;
            var recur = s.Get<L10Recurrence>(recurrenceId);

            var current = L10Accessor._GetCurrentL10Meeting_Unsafe(s, recurrenceId, true, false, false);
            var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
            var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), RealTimeHelpers.GetConnectionString());


            if (current != null) {
                _UpdateRocksInMeeting(s, rock, recurRock, adminPerms, recurrenceId, recur, current, group);
            }

        }

        public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
            //Nothing to do..
        }

        public async Task UnArchiveRock(ISession s, RockModel rock, bool v)
        {
            //Nothing to do...
        }

        public async Task CreateRock(ISession s, RockModel rock) {
            //Nothing to do..
        }
    }
}