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
using RadialReview.Models.Issues;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models;
using static RadialReview.Accessors.IssuesAccessor;
using RadialReview.Accessors;

namespace RadialReview.Hooks {
	public class IssueWebhook : IIssueHook {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task CreateIssue(ISession s, IssueModel.IssueModel_Recurrence issue) {
			var L10Id = issue.Recurrence.Id;
			var orgId = issue.Recurrence.OrganizationId;
			var userId = issue.Owner.Id;

			string _event = WebhookEventType.AddIssuetoL10.GetDescription() + L10Id;
			string _event1 = WebhookEventType.AddIssuetoOrganization.GetDescription() + orgId;
			string _event2 = WebhookEventType.AddIssueforUser.GetDescription() + userId;


			IWebHookManager manager = DependencyResolver.Current.GetManager();

			var notifications = new List<NotificationDictionary> { new NotificationDictionary(_event, new AngularIssue(issue)) };
			await manager.NotifyAllAsync(notifications, IssuePermissions(s, issue));

			notifications = new List<NotificationDictionary> { new NotificationDictionary(_event1, new AngularIssue(issue)) };
			await manager.NotifyAllAsync(notifications, IssuePermissions(s, issue));

			notifications = new List<NotificationDictionary> { new NotificationDictionary(_event2, new AngularIssue(issue)) };
			await manager.NotifyAllAsync(notifications, IssuePermissions(s, issue));

			//throw new NotImplementedException();
		}

		private static Func<WebHook, string, bool> IssuePermissions(ISession s, IssueModel.IssueModel_Recurrence issue) {
			return WebhooksAccessor.PermissionsPredicate(s, x => x.ViewIssue(issue.Id));
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task UpdateMessage(ISession s, IssueModel.IssueModel_Recurrence issue)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			var L10Id = issue.Recurrence.Id;
			var orgId = issue.Recurrence.OrganizationId;
			var userId = issue.Owner.Id;

			string _event = WebhookEventType.ChangingIssuetoL10.GetDescription() + L10Id;
			string _event1 = WebhookEventType.ChangingIssuetoOrganization.GetDescription() + orgId;
			string _event2 = WebhookEventType.ChangingIssueforUser.GetDescription() + userId;


			IWebHookManager manager = DependencyResolver.Current.GetManager();

			var notifications = new List<NotificationDictionary> { new NotificationDictionary(_event, new AngularIssue(issue)) };
			await manager.NotifyAllAsync(notifications, IssuePermissions(s, issue));

			notifications = new List<NotificationDictionary> { new NotificationDictionary(_event1, new AngularIssue(issue)) };
			await manager.NotifyAllAsync(notifications, IssuePermissions(s, issue));

			notifications = new List<NotificationDictionary> { new NotificationDictionary(_event2, new AngularIssue(issue)) };
			await manager.NotifyAllAsync(notifications, IssuePermissions(s, issue));

		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task UpdateCompletion(ISession s, IssueModel.IssueModel_Recurrence issue)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			var L10Id = issue.Recurrence.Id;
			var orgId = issue.Recurrence.OrganizationId;
			var userId = issue.Owner.Id;

			string _event = WebhookEventType.Checking_Unchecking_Closing_IssuetoL10.GetDescription() + L10Id;
			string _event1 = WebhookEventType.Checking_Unchecking_Closing_IssuetoOrganization.GetDescription() + orgId;
			string _event2 = WebhookEventType.Checking_Unchecking_Closing_IssueforUser.GetDescription() + userId;


			IWebHookManager manager = DependencyResolver.Current.GetManager();

			var notifications = new List<NotificationDictionary> { new NotificationDictionary(_event, new AngularIssue(issue)) };
			await manager.NotifyAllAsync(notifications, IssuePermissions(s, issue));

			notifications = new List<NotificationDictionary> { new NotificationDictionary(_event1, new AngularIssue(issue)) };
			await manager.NotifyAllAsync(notifications, IssuePermissions(s, issue));

			notifications = new List<NotificationDictionary> { new NotificationDictionary(_event2, new AngularIssue(issue)) };
			await manager.NotifyAllAsync(notifications, IssuePermissions(s, issue));

		}
	}
}