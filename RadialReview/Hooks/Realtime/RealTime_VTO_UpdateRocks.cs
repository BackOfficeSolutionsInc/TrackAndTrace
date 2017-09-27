using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using System.Threading.Tasks;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.VTO;
using RadialReview.Hubs;
using Microsoft.AspNet.SignalR;
using RadialReview.Models.VTO;

namespace RadialReview.Hooks.Realtime {
	public class RealTime_VTO_UpdateRocks : IRockHook, IMeetingRockHooks {

		public bool CanRunRemotely() {
			return false;
		}
		
		private void AddRockToVto(ISession s, long rockId, long recurrenceId) {
			var recurRocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
				.Where(x => x.ForRock.Id == rockId && x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null)
				.List().ToList();

			var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			foreach (var recurRock in recurRocks) {
				var vto = s.Get<VtoModel>(recurRock.L10Recurrence.VtoId);
				if (vto != null) {
					var angularItems = AngularList.CreateFrom(AngularListType.Add, AngularVtoRock.Create(recurRock));
					var updates = new AngularUpdate() {
						new AngularQuarterlyRocks(vto.QuarterlyRocks.Id) {
							Rocks = angularItems
						}
					};
					var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vto.Id), null);
					group.update(updates);
				}
			}
		}

		private void RemoveRockFromVto(ISession s, long rockId, long recurrenceId) {
			var recurRocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
				.Where(x => x.ForRock.Id == rockId && x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null)
				.List().ToList();

			var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			foreach (var recurRock in recurRocks) {
				var vto = s.Get<VtoModel>(recurRock.L10Recurrence.VtoId);
				if (vto != null) {
					var angularItems = AngularList.CreateFrom(AngularListType.Remove, new AngularVtoRock(recurRock.Id));
					var updates = new AngularUpdate() {
						new AngularQuarterlyRocks(vto.QuarterlyRocks.Id) {
							Rocks = angularItems
						}
					};
					var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vto.Id), null);
					group.update(updates);
				}
			}
		}


		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			var recurDatas = RealTimeHelpers.GetRecurrenceRockData(s, rock.Id);

			foreach (var recurId in recurDatas.RecurIds.Select(x => x.RecurrenceId)) {
				RemoveRockFromVto(s, rock.Id, recurId);
			}
		}

		public async Task AttachRock(ISession s, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
			if (recurRock.VtoRock) {
				AddRockToVto(s, rock.Id, recurRock.L10Recurrence.Id);
			}
		}
		public async Task DetatchRock(ISession s, RockModel rock, long recurrenceId) {
			RemoveRockFromVto(s, rock.Id, recurrenceId);
		}

		public async Task CreateRock(ISession s, RockModel rock) {
			//Nothing to do
		}

		public async Task UpdateRock(ISession s, RockModel rock) {
			//Nothing to do
		}

		public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
			if (recurRock.VtoRock) {
				AddRockToVto(s, recurRock.ForRock.Id, recurRock.L10Recurrence.Id);
			} else {
				RemoveRockFromVto(s, recurRock.ForRock.Id, recurRock.L10Recurrence.Id);
			}
		}
	}
}