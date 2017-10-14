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

		public bool CanRunRemotely() {
			return false;
		}
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task CreateTodo(ISession s, TodoModel todo) {

			var recurrenceId = todo.ForRecurrenceId;
			var orgId = todo.OrganizationId;
			var userId = todo.AccountableUserId;

			// setup events
			var events = new List<string>();
			events.Add(WebhookEventType.AddTODOtoL10.GetDescription() + recurrenceId);
			events.Add(WebhookEventType.AddTODOtoOrganization.GetDescription() + orgId);
			events.Add(WebhookEventType.AddTODOforUser.GetDescription() + userId);

			await RunEvents(s, todo, events);
		}

		public async Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates) {

			var recurrenceId = todo.ForRecurrenceId;
			var orgId = todo.OrganizationId;
			var userId = todo.AccountableUserId;

			var events = new List<string>();

			//Message
			if (updates.MessageChanged) {
				events.Add(WebhookEventType.ChangingToDotoL10.GetDescription() + recurrenceId);
				events.Add(WebhookEventType.ChangingTODOtoOrganization.GetDescription() + orgId);
				events.Add(WebhookEventType.ChangingToDoforUser.GetDescription() + userId);
			}
			//Completion
			if (updates.CompletionChanged) {
				events.Add(WebhookEventType.Checking_Unchecking_Closing_TODOtoL10.GetDescription() + recurrenceId);
				events.Add(WebhookEventType.Checking_Unchecking_Closing_TODOtoOrganization.GetDescription() + orgId);
				events.Add(WebhookEventType.Checking_Unchecking_Closing_TODOforUser.GetDescription() + userId);
			}
			//TODO add update due date.
			await RunEvents(s, todo, events);
		}

		private static Func<WebHook, string, bool> TodoPermissions(ISession s, TodoModel todo) {
			return WebhooksAccessor.PermissionsPredicate(s, x => x.ViewTodo(todo.Id));
		}

		private static async Task RunEvents(ISession s, TodoModel todo, List<string> events) {
			if (events.Any()) {
				IWebHookManager manager = DependencyResolver.Current.GetManager();
				var tasks = new List<Task<int>>();
				foreach (var _event in events) {
					var notifications = new List<NotificationDictionary> { new NotificationDictionary(_event, new AngularTodo(todo)) };
					tasks.Add(manager.NotifyAllAsync(notifications, TodoPermissions(s, todo)));
				}
				await Task.WhenAll(tasks);
			}
		}
	}
}