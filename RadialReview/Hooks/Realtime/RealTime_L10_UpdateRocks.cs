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

namespace RadialReview.Hooks.Realtime {
	public class RealTime_L10_UpdateRocks : IRockHook, IMeetingRockHooks {

		public bool CanRunRemotely() {
			return false;
		}
				

		[Untested("Did it add?","Both conditions","in meeting", "in wizard","in archive")]
		public async Task AttachRock(ISession s, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
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

					//Update Angular
					var arecur = new AngularRecurrence(recurrenceId) {
							Rocks = AngularList.Create(AngularListType.ReplaceIfNewer, new[]{new AngularRock(recurRock){
							ForceOrder =int.MaxValue,
						}}),
						Focus = "[data-rock='" + rock.Id + "'] input:visible:first"
					};
					group.update(new AngularUpdate { arecur });
				} else {
					var recurRocks = L10Accessor.GetRocksForRecurrence(s, adminPerms, recurrenceId);
					//var arecur = new AngularRecurrence(recurrenceId) {
					//	Rocks = AngularList.Create(AngularListType.ReplaceAll, recurRocks.Select(x => new AngularRock(x)).ToList()),
					//};
					string focus = null;
					if (recurRocks.Any() && recurRocks.Last().ForRock != null) {
						focus = "[data-rock='" + recurRocks.Last().ForRock.Id + "'] input:visible:first";
					}

					var arecur = new AngularUpdate { new AngularRecurrence(recurrenceId) {
							Rocks = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularRock(recurRock)),
							Focus = focus,
						}
					};
					group.update(arecur);
				}
				///
			}

		}

		

		[Untested("Did it remove?")]
		public async Task DetatchRock(ISession s, RockModel rock, long recurrenceId) {
			using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				rt.UpdateRecurrences(recurrenceId).Update(
					new AngularRecurrence(recurrenceId) {
						Rocks = AngularList.CreateFrom(AngularListType.Remove, new AngularRock(rock.Id))
					}
				);
			}
		}
		
		[Untested("Did it update?")]
		public async Task UpdateRock(ISession s, RockModel rock) {
			using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				var rock_ids = RealTimeHelpers.GetRecurrenceRockData(s, rock.Id);
				var allRecurIds = rock_ids.RecurData.Select(x => x.RecurrenceId);
				var allMeetingRockIds = rock_ids.MeetingData.Select(x => x.MeetingRockId);

				var updater = rt.UpdateRecurrences(allRecurIds).Update(rid => new AngularRock(rock, null));
				//Update Name
				updater.AddLowLevelAction(x => x.updateRockName(rock.Id, rock.Name));

				//Update Completion
				foreach (var meetingRockId in allMeetingRockIds) {
					updater.AddLowLevelAction(x => x.updateRockCompletion(meetingRockId, rock.Completion.ToString()));
				}
				updater.AddLowLevelAction(x => x.updateRockCompletion(0, rock.Completion.ToString(), rock.Id));
			}
		}

		public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
			using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
				rt.UpdateRecurrences(recurRock.L10Recurrence.Id)
					.Update(rid => new AngularRock(recurRock.ForRock.Id) {
						VtoRock = recurRock.VtoRock
					});
			}
		}

		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			//Nothing to do..
		}

		[Untested("Should it add? Maybe this is handled by attach")]
		public async Task CreateRock(ISession s, RockModel rock) {
			//Nothing to do..
		}
	}
}