using Microsoft.AspNet.WebHooks.Services;
using NHibernate;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using RadialReview.Models.Todo;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.WebHooks;
using Microsoft.AspNet.WebHooks.Diagnostics;
using RadialReview.Models.Angular.Todos;
using System.Web.Mvc;

namespace RadialReview.Hooks {
	public class TodoWebhook : ITodoHook {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task CreateTodo(ISession s, TodoModel todo) {
			var L10Id = todo.ForRecurrenceId;
			var orgId = todo.OrganizationId;

			string _event = "Add TODO to L10_" + L10Id;
			string _event1 = "Add TODO to Organization_" + orgId;

			//var _store = CustomServices.GetStore();

			//WebHookManager _manager = new WebHookManager(_store, null, null);

			IWebHookManager manager = DependencyResolver.Current.GetManager();

			var notifications = new List<NotificationDictionary> { new NotificationDictionary(_event, new AngularTodo(todo)) };
			notifications.Add(new NotificationDictionary(_event1, new AngularTodo(todo)));

			var _y = await manager.NotifyAllAsync(notifications, (x, y) => true);

			//throw new NotImplementedException();
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task UpdateMessage(ISession s, TodoModel todo)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			throw new NotImplementedException();
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task UpdateCompletion(ISession s, TodoModel todo)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			throw new NotImplementedException();
		}

		public Task UpdateDueDate(ISession s, TodoModel todo) {
			throw new NotImplementedException();
		}
	}
}