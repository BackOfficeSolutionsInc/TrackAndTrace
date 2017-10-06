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
using RadialReview.Models.Dashboard;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Base;
using Common.Logging;

namespace RadialReview.Hooks.Realtime {
	public class RealTime_Dashboard_Todo : ITodoHook {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public bool CanRunRemotely() {
			return false;
		}

		public async Task CreateTodo(ISession s, TodoModel todo) {
			if (todo.ForRecurrenceId > 0) {
				var recurrenceId = todo.ForRecurrenceId.Value;
				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
				var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
				var todoData = TodoData.FromTodo(todo);
				
				
				try {
					Dashboard dashboardAlias = null;
					var dashs = s.QueryOver<TileModel>()
						.JoinAlias(x => x.Dashboard, () => dashboardAlias)
						.Where(x => x.DeleteTime == null && x.Type == TileType.Url && x.DataUrl == "/TileData/L10Todos/" + recurrenceId)
						.Select(x => x.Dashboard.Id, x => x.Id, x => dashboardAlias.ForUser.Id)
						.List<object[]>()
						.Select(x => new {
							DashboardId = (long)x[0],
							TileId = (long)x[1],
							UserId = (string)x[2]
						}).ToList();

					if (dashs.Any()) {
						//Only do if there are tiles
						var dashUserIds = dashs.Select(x => x.UserId).Distinct().ToArray();
						var dashUsers = s.QueryOver<UserOrganizationModel>()
											.Where(x => x.DeleteTime == null)
											.WhereRestrictionOn(x => x.User.Id)
											.IsIn(dashUserIds)
											.List().ToList();

						var canView = new DefaultDictionary<string, bool>(x => false);
						foreach (var u in dashUsers) {
							if (canView[u.User.Id] == false) {
								try {
									PermissionsUtility.Create(s, u).ViewL10Recurrence(recurrenceId);
									canView[u.User.Id] = true;
								} catch (PermissionsException) {
								}
							}
						}

						foreach (var d in dashs) {
							if (canView[d.UserId]) {
								var tile = new AngularTileId<IEnumerable<AngularTodo>>(d.TileId, recurrenceId, null, AngularTileKeys.L10TodoList(recurrenceId)) {
									Contents = AngularList.Create(AngularListType.Add, new[] { new AngularTodo(todo) })
								};
								meetingHub.update(new AngularUpdate() { tile });
							}
						}
					}
				} catch (Exception e) {
					//Special stuff,
					log.Error(e);
				}
			}




		}

		public Task UpdateTodo(ISession s, TodoModel todo, ITodoHookUpdates updates) {
			throw new NotImplementedException();
		}
	}
}