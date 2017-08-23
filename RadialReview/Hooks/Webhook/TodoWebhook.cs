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
using RadialReview.Models;
using RadialReview.Accessors;

namespace RadialReview.Hooks {
	public class TodoWebhook : ITodoHook {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task CreateTodo(ISession s, TodoModel todo) {
			try {
				var L10Id = todo.ForRecurrenceId;
				var orgId = todo.OrganizationId;
				var userId = todo.AccountableUserId;

				// setup events
				string _event = WebhookEventType.AddTODOtoL10.GetDescription() + L10Id;
				string _event1 = WebhookEventType.AddTODOtoOrganization.GetDescription() + orgId;
				string _event2 = WebhookEventType.AddTODOforUser.GetDescription() + userId;

				IWebHookManager manager = DependencyResolver.Current.GetManager();

				var notifications = new List<NotificationDictionary> { new NotificationDictionary(_event, new AngularTodo(todo)) };
				await manager.NotifyAllAsync(notifications, TodoPermissions(s, todo));

				notifications = new List<NotificationDictionary> { new NotificationDictionary(_event1, new AngularTodo(todo)) };
				await manager.NotifyAllAsync(notifications, TodoPermissions(s, todo));

				notifications = new List<NotificationDictionary> { new NotificationDictionary(_event2, new AngularTodo(todo)) };
				await manager.NotifyAllAsync(notifications, TodoPermissions(s, todo));

			} catch (Exception ex) {
				throw;
			}
		}

		private static Func<WebHook, string, bool> TodoPermissions(ISession s, TodoModel todo) {
			return WebhooksAccessor.PermissionsPredicate(s, x => x.ViewTodo(todo.Id));
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task UpdateMessage(ISession s, TodoModel todo)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			try {
				var L10Id = todo.ForRecurrenceId;
				var orgId = todo.OrganizationId;
				var userId = todo.AccountableUserId;

				// setup events
				string _event = WebhookEventType.ChangingToDotoL10.GetDescription() + L10Id;
				string _event1 = WebhookEventType.ChangingTODOtoOrganization.GetDescription() + orgId;
				string _event2 = WebhookEventType.ChangingToDoforUser.GetDescription() + userId;

				IWebHookManager manager = DependencyResolver.Current.GetManager();

				var notifications = new List<NotificationDictionary> { new NotificationDictionary(_event, new AngularTodo(todo)) };
				await manager.NotifyAllAsync(notifications, TodoPermissions(s, todo));

				notifications = new List<NotificationDictionary> { new NotificationDictionary(_event1, new AngularTodo(todo)) };
				await manager.NotifyAllAsync(notifications, TodoPermissions(s, todo));

				notifications = new List<NotificationDictionary> { new NotificationDictionary(_event2, new AngularTodo(todo)) };
				await manager.NotifyAllAsync(notifications, TodoPermissions(s, todo));

			} catch (Exception ex) {
				throw;
			}
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task UpdateCompletion(ISession s, TodoModel todo)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			try {
				var L10Id = todo.ForRecurrenceId;
				var orgId = todo.OrganizationId;
				var userId = todo.AccountableUserId;

				// setup events
				string _event = WebhookEventType.Checking_Unchecking_Closing_TODOtoL10.GetDescription() + L10Id;
				string _event1 = WebhookEventType.Checking_Unchecking_Closing_TODOtoOrganization.GetDescription() + orgId;
				string _event2 = WebhookEventType.Checking_Unchecking_Closing_TODOforUser.GetDescription() + userId;

				IWebHookManager manager = DependencyResolver.Current.GetManager();

				var notifications = new List<NotificationDictionary> { new NotificationDictionary(_event, new AngularTodo(todo)) };
				await manager.NotifyAllAsync(notifications, TodoPermissions(s, todo));

				notifications = new List<NotificationDictionary> { new NotificationDictionary(_event1, new AngularTodo(todo)) };
				await manager.NotifyAllAsync(notifications, TodoPermissions(s, todo));

				notifications = new List<NotificationDictionary> { new NotificationDictionary(_event2, new AngularTodo(todo)) };
				await manager.NotifyAllAsync(notifications, TodoPermissions(s, todo));

			} catch (Exception ex) {
				throw;
			}
		}		
	}
}