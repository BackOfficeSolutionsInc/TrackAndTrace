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

namespace RadialReview.Hooks.Realtime {
	public class RealTime_L10_Todo : ITodoHook {
		public bool CanRunRemotely() {
			return false;
		}

		[Untested("var updates = new AngularRecurrence(todo.ForRecurrenceId) ??? recurrentId ???")]
		public async Task CreateTodo(ISession s, TodoModel todo) {

			if (todo.TodoType == TodoType.Personal) {
				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
				var userMeetingHub = hub.Clients.Group(MeetingHub.GenerateUserId(todo.AccountableUserId));
				var todoData = TodoData.FromTodo(todo);
				userMeetingHub.appendTodo(".todo-list", todoData);
				var updates = new AngularRecurrence(todo.ForRecurrenceId.Value);
				updates.Todos = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(todo));
				userMeetingHub.update(updates);
			}

			if (todo.ForRecurrenceId > 0) {
				var recurrenceId = todo.ForRecurrenceId.Value;
				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
				var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
				var todoData = TodoData.FromTodo(todo);

				if (todo.CreatedDuringMeetingId != null)
					todoData.isNew = true;
				meetingHub.appendTodo(".todo-list", todoData);

				var message = "Created to-do.";
				try {
					message = todo.CreatedBy.GetFirstName() + " created a to-do.";
				} catch (Exception) {
				}

				meetingHub.showAlert(message, 1500);

				var updates = new AngularRecurrence(recurrenceId);
				updates.Todos = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(todo));
				updates.Focus = "[data-todo='" + todo.Id + "'] input:visible:first";
				meetingHub.update(updates);
			}
		}

		[Untested("fill out")]
		public async Task UpdateTodo(ISession s, TodoModel todo, ITodoHookUpdates updates) {
			throw new NotImplementedException();

		}
	}
}