using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using RadialReview.Models.Dashboard;
using RadialReview.Models;
using RadialReview.Utilities.DataTypes;
using RadialReview.Exceptions;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Todos;
using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using RadialReview.Models.Angular.Rocks;
using System.Diagnostics;

namespace RadialReview.Hooks.Realtime {
    public class RealTime_Dashboard_UpdateL10Rocks : IRockHook, IMeetingRockHook {
        public bool CanRunRemotely() {
            return false;
		}
		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
            var data = RealTimeHelpers.GetRecurrenceRockData(s, rock.Id);
            foreach (var recur in data.GetRecurrenceIds()) {
                RemoveRock(s, recur, rock.Id);
            }

            RealTimeHelpers.GetUserHubForRecurrence(rock.ForUserId).update(new AngularUpdate() {
                new ListDataVM(rock.ForUserId) {
                    Rocks = AngularList.CreateFrom(AngularListType.Remove, new AngularRock(rock.Id))
                }
            });
        }

        public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
            AddRock(s, recurRock.L10Recurrence.Id, recurRock);
        }

        public async Task DetatchRock(ISession s, RockModel rock, long recurrenceId) {
            RemoveRock(s, recurrenceId, rock.Id);
        }

        public async Task CreateRock(ISession s, RockModel rock) {
            RealTimeHelpers.GetUserHubForRecurrence(rock.ForUserId).update(new AngularUpdate() {
                new ListDataVM(rock.ForUserId) {
                    Rocks = AngularList.CreateFrom(AngularListType.Add, new AngularRock(rock,null))
                }
            });
        }


        public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {

            if (updates.AccountableUserChanged) {
                RealTimeHelpers.GetUserHubForRecurrence(updates.OriginalAccountableUserId,false).update(new AngularUpdate() {
                    new ListDataVM(updates.OriginalAccountableUserId) {
                        Rocks = AngularList.CreateFrom(AngularListType.Remove, new AngularRock(rock.Id))
                    }
                });
            }

            RealTimeHelpers.GetUserHubForRecurrence(rock.ForUserId,false).update(new AngularUpdate() {
                new ListDataVM(rock.ForUserId) {
                    Rocks = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularRock(rock,null))
                }
            });
        }

        #region Helpers


        /*private dynamic _DoUpdate(ISession s, long recurrenceId, Func<AngularUpdate> action) {

			//Dashboard dashboardAlias = null;


			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
			//var meetingHub = _GetHub(recurrenceId);

			//var times = new List<TimeSpan>();
			//var sw = new Stopwatch();
			//sw.Start();
			var a = action();
			meetingHub.update(a);


			return meetingHub;

			//var dashs = s.QueryOver<TileModel>()
			//	//.JoinAlias(x => x.Dashboard, () => dashboardAlias)
			//	.Where(x => x.DeleteTime == null && x.Type == TileType.Url && x.DataUrl == "/TileData/L10Rocks/" + recurrenceId)
			//	.Select(x => x.Id, x => x.ForUser.Id/*, x => x.Dashboard.Id*)
			//	.List<object[]>()
			//	.Select(x => new {
			//		TileId = (long)x[0],
			//		UserId = (string)x[1]
			//		//DashboardId = (long)x[2],
			//	}).ToList();

			//var t1 = sw.Elapsed;
			//times.Add(t1);

			//if (dashs.Any()) {
			//	//Only do if there are tiles
			//	var dashUserIds = dashs.Select(x => x.UserId).Distinct().ToArray();




			//	//var userModels = s.QueryOver<UserModel>()
			//	//					.WhereRestrictionOn(x => x.Id).IsIn(dashUserIds)
			//	//					.Select(x => x.Id, x => x.CurrentRole)
			//	//					.List<object[]>()
			//	//					.Select(x => new {
			//	//						Id = (string)x[0],
			//	//						CurrentRole = (long)x[1]
			//	//					}).ToList();


			//	var dashUsers = s.QueryOver<UserOrganizationModel>()											
			//						.Where(x => x.DeleteTime == null)
			//						.WhereRestrictionOn(x => x.User.Id)
			//						.IsIn(dashUserIds)
			//						.List().ToList();

			//	var t2 = sw.Elapsed;
			//	times.Add(t2 - t1);

			//	var canView = new DefaultDictionary<string, bool>(x => false);
			//	foreach (var u in dashUsers) {
			//		if (canView[u.User.Id] == false) {
			//			try {
			//				//var user = s.Load<UserOrganizationModel>(u.CurrentRole);
			//				var user = u;
			//				PermissionsUtility.Create(s, user).ViewL10Recurrence(recurrenceId);
			//				canView[u.User.Id] = true;
			//			} catch (PermissionsException) {
			//			}
			//		}
			//	}
			//	var t3 = sw.Elapsed;
			//	times.Add(t3 - t2);
			//	foreach (var d in dashs) {
			//		if (canView[d.UserId]) {
			//			var a = action(d.TileId);
			//			meetingHub.update(a);
			//		}
			//	}
			//	var t4 = sw.Elapsed;
			//	times.Add(t4 - t3);
			//	var abc = 0;
			//}

			//var t5 = sw.Elapsed;
			//times.Add(t5-t4);
		}
	*/

        private void AddRock(ISession s, long recurrenceId, L10Recurrence.L10Recurrence_Rocks rock) {
            RealTimeHelpers.DoRecurrenceUpdate(s, recurrenceId, () =>
                  new AngularUpdate() {
                    new AngularTileId<IEnumerable<AngularRock>>(0, recurrenceId, null,AngularTileKeys.L10RocksList(recurrenceId)) {
                        Contents = AngularList.CreateFrom(AngularListType.Add, new AngularRock(rock))
                    }
                  }
            );
        }

        private void RemoveRock(ISession s, long recurrenceId, long rockId) {
            RealTimeHelpers.DoRecurrenceUpdate(s, recurrenceId, () =>
                  new AngularUpdate() {
                    new AngularTileId<IEnumerable<AngularRock>>(0, recurrenceId, null,AngularTileKeys.L10RocksList(recurrenceId)) {
                        Contents = AngularList.CreateFrom(AngularListType.Remove,  new AngularRock(rockId))
                    }
                  }
            );
        }
        #endregion

        #region NoOps

        public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
            //Nothing to do...
        }
        #endregion

    }
}