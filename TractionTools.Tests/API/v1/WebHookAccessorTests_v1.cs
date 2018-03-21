using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using TractionTools.Tests.TestUtils;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using System.Threading.Tasks;
using RadialReview.Areas.CoreProcess.Accessors;
using Microsoft.AspNet.WebHooks;
using System.Collections.Generic;
using RadialReview.Models;
using RadialReview;

namespace TractionTools.Tests.Api {
	[TestClass]
	public class WebHookAccessorTests_v1 : BaseTest {

		[TestMethod]
		[TestCategory("Api_V1")]
		public async Task DISABLED_TestCreateWebhook() {

			Assert.Inconclusive("Webhooks not setup");
			var c = await Ctx.Build();
			//Assert.IsTrue(getResult > 0);

			WebHook webhook = new WebHook() {
				WebHookUri = new System.Uri("http://localhost:3751/api/webhooks/incoming/custom"),
				Secret = "12345678901234567890123456789012",
				Description = "Test",
			};
			webhook.Filters.Add("*");

			List<string> selectedEvents = new List<string>();

			var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E3, c.E3.Organization.Id);
			for (int i = 0; i < getAllL10RecurrenceAtOrganization.Count; i++) {

				//L10 Add TODO Events
				selectedEvents.Add(WebhookEventType.AddTODOtoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);


				//L10 Add Issue Events
				selectedEvents.Add(WebhookEventType.AddIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);

				//Organization todo Event
				selectedEvents.Add(WebhookEventType.AddTODOtoOrganization.GetDescription() + c.E3.Organization.Id);

				//Organization Changing TODO Event
				selectedEvents.Add(WebhookEventType.ChangingTODOtoOrganization.GetDescription() + c.E3.Organization.Id);

				//Organization Issue Event
				selectedEvents.Add(WebhookEventType.AddIssuetoOrganization.GetDescription() + c.E3.Organization.Id);
			}



			//User Events
			var getUserOrg = TinyUserAccessor.GetOrganizationMembers(c.E3, c.E3.Organization.Id);
			for (int i = 0; i < getUserOrg.Count; i++) {

				//User Add TODO Event
				selectedEvents.Add(WebhookEventType.AddTODOforUser.GetDescription() + c.E3.Organization.Id);

				//User Changing TODO Event
				selectedEvents.Add(WebhookEventType.ChangingToDoforUser.GetDescription() + getUserOrg[i].UserOrgId);

				//User Changing Issue Event
				selectedEvents.Add(WebhookEventType.ChangingIssueforUser.GetDescription() + getUserOrg[i].UserOrgId);

			}
			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			var result = webhookAccessor.InsertWebHook(c.E3, webhook, selectedEvents);
			Assert.IsNotNull(result);
		}


		[TestMethod]
		[TestCategory("Api_V1")]
		public async Task TestUpdateWebhook() {
			var c = await Ctx.Build();
			//Assert.IsTrue(getResult > 0);

			WebHook webhook = new WebHook() {
				WebHookUri = new System.Uri("http://localhost:3751/api/webhooks/incoming/custom"),
				Secret = "12345678901234567890123456789012",
				Description = "Test",
			};
			webhook.Filters.Add("*");

			List<string> selectedEvents = new List<string>();

			var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E3, c.E3.Organization.Id);
			for (int i = 0; i < getAllL10RecurrenceAtOrganization.Count; i++) {

				//L10 Add TODO Events
				selectedEvents.Add(WebhookEventType.AddTODOtoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);


				//L10 Add Issue Events
				selectedEvents.Add(WebhookEventType.AddIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);

			}

			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			webhookAccessor.InsertWebHook(c.E3, webhook, selectedEvents);


			var getWebHook = webhookAccessor.LookupWebHook(c.E3, webhook.Id);
			var getUserOrg = TinyUserAccessor.GetOrganizationMembers(c.E3, c.E3.Organization.Id);
			for (int i = 0; i < getUserOrg.Count; i++) {

				//User Add TODO Event
				selectedEvents.Add(WebhookEventType.AddTODOforUser.GetDescription() + c.E3.Organization.Id);

				//User Changing TODO Event
				selectedEvents.Add(WebhookEventType.ChangingToDoforUser.GetDescription() + getUserOrg[i].UserOrgId);

				//User Changing Issue Event
				selectedEvents.Add(WebhookEventType.ChangingIssueforUser.GetDescription() + getUserOrg[i].UserOrgId);

			}

			var updatedVal = "Test123";
			webhook.Description = updatedVal;
			var updateWebHook = webhookAccessor.UpdateWebHook(c.E3, webhook, selectedEvents);
			var getUpdatedWebHook = webhookAccessor.LookupWebHook(c.E3, webhook.Id);

			Assert.AreEqual(updatedVal, getUpdatedWebHook.Description);
		}


		[TestMethod]
		[TestCategory("Api_V1")]
		public async Task TestDeleteWebhook() {
			var c = await Ctx.Build();
			//Assert.IsTrue(getResult > 0);

			WebHook webhook = new WebHook() {
				WebHookUri = new System.Uri("http://localhost:3751/api/webhooks/incoming/custom"),
				Secret = "12345678901234567890123456789012",
				Description = "Test",
			};
			webhook.Filters.Add("*");

			List<string> selectedEvents = new List<string>();

			var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E3, c.E3.Organization.Id);
			for (int i = 0; i < getAllL10RecurrenceAtOrganization.Count; i++) {

				//L10 Add TODO Events
				selectedEvents.Add(WebhookEventType.AddTODOtoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);


				//L10 Add Issue Events
				selectedEvents.Add(WebhookEventType.AddIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);

			}

			WebhooksAccessor webhookAccessor = new WebhooksAccessor();
			webhookAccessor.InsertWebHook(c.E3, webhook, selectedEvents);


			var getWebHook = webhookAccessor.LookupWebHook(c.E3, webhook.Id);
			var s = webhookAccessor.DeleteWebHook(c.E3, getWebHook.Id);
			var getWebHook1 = webhookAccessor.LookupWebHook(c.E3, webhook.Id);

			Assert.AreEqual(s, StoreResult.Success);
			Assert.IsNull(getWebHook1);
		}

	}
}
