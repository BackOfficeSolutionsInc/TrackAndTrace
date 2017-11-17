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
using RadialReview.Models;

namespace RadialReview.Hooks.Realtime {
	public class RealTime_VTO_UpdateRocks : IRockHook, IMeetingRockHook {

		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		private void _DoUpdate(ISession s, long rockId, long? recurrenceId, bool allowDeleted,string connectionId, Func<long, L10Recurrence.L10Recurrence_Rocks, AngularUpdate> action) {
			var recurRocksQ = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.ForRock.Id == rockId);
			if (recurrenceId != null) {
				recurRocksQ = recurRocksQ.Where(x => x.L10Recurrence.Id == recurrenceId);
			}
			if (!allowDeleted) {
				recurRocksQ = recurRocksQ.Where(x => x.DeleteTime == null);
			}
			//if (vtoRock!=null) {
			//	recurRocksQ = recurRocksQ.Where(x => x.VtoRock == vtoRock.Value);
			//}

			var recurRocks = recurRocksQ.List().ToList();

			var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();



			foreach (var recurRock in recurRocks) {
				//var vto = s.Get<VtoModel>(recurRock.L10Recurrence.VtoId);
				var vtoId = recurRock.L10Recurrence.VtoId;

				var qrId = s.QueryOver<QuarterlyRocksModel>().Where(x => x.Vto == vtoId).SingleOrDefault().Id;

				//if (vtoId != null) {
					var updates = action(qrId, recurRock);
					var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);
					group.update(updates);
				//}
			}
		}

		private void AddRockToVto(ISession s, long rockId, long? recurrenceId) {
			_DoUpdate(s, rockId, recurrenceId,false, null, (qrId, recurRock) =>
				new AngularUpdate() {
					new AngularQuarterlyRocks(qrId) {
						Rocks = AngularList.CreateFrom(AngularListType.Add, AngularVtoRock.Create(recurRock))
					}
				}
			);			
		}

		private void RemoveRockFromVto(ISession s, long rockId, long? recurrenceId) {
			_DoUpdate(s, rockId, recurrenceId,true,null, (qrId, recurRock) =>
				new AngularUpdate() {
					new AngularQuarterlyRocks(qrId) {
						Rocks = AngularList.CreateFrom(AngularListType.Remove, new AngularVtoRock(recurRock.Id))
					}
				}
			);
		}

		private void UpdateRock(ISession s, long rockId, long? recurrenceId) {
			_DoUpdate(s, rockId, recurrenceId,false, RealTimeHelpers.GetConnectionString(), (qrId, recurRock) =>
				new AngularUpdate() {
					new AngularQuarterlyRocks(qrId) {
						Rocks = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, AngularVtoRock.Create(recurRock))
					}
				}
			);
		}


		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			//var recurDatas = RealTimeHelpers.GetRecurrenceRockData(s, rock.Id);

			//foreach (var recurId in recurDatas.RecurIds.Select(x => x.RecurrenceId)) {
				RemoveRockFromVto(s, rock.Id, null);
			//}
		}

		public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
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

		public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {
			UpdateRock(s, rock.Id, null);
		}

		public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
			if (recurRock.VtoRock && recurRock.DeleteTime == null) {
				AddRockToVto(s, recurRock.ForRock.Id, recurRock.L10Recurrence.Id);
			} else {
				RemoveRockFromVto(s, recurRock.ForRock.Id, recurRock.L10Recurrence.Id);
			}
		}
		
	}
}