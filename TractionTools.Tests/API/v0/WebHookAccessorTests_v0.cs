﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public class WebHookAccessorTests_v0 : BaseTest {

        [TestMethod]
        [TestCategory("Api_V0")]
        public void TestEnsureApplicationExists() {
            ApplicationAccessor.EnsureApplicationExists();
        }

        [TestMethod]
        [TestCategory("Api_V0")]
        public async Task TestCreateWebhook() {
            var c = await Ctx.Build();
            //Assert.IsTrue(getResult > 0);

            WebHook webhook = new WebHook() {
                WebHookUri = new System.Uri("http://localhost:3751/api/webhooks/incoming/custom"),
                Secret = "12345678901234567890123456789012",
                Description = "Test",
            };
            webhook.Filters.Add("*");

            List<string> selectedEvents = new List<string>();

            var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E1, c.E1.Organization.Id);
            for (int i = 0; i < getAllL10RecurrenceAtOrganization.Count; i++) {

                //L10 Add TODO Events
                selectedEvents.Add(WebhookEventType.AddTODOtoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);


                //L10 Add Issue Events
                selectedEvents.Add(WebhookEventType.AddIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);

                //Organization todo Event
                selectedEvents.Add(WebhookEventType.AddTODOtoOrganization.GetDescription() + c.E1.Organization.Id);

                //Organization Changing TODO Event
                selectedEvents.Add(WebhookEventType.ChangingTODOtoOrganization.GetDescription() + c.E1.Organization.Id);

                //Organization Issue Event
                selectedEvents.Add(WebhookEventType.AddIssuetoOrganization.GetDescription() + c.E1.Organization.Id);
            }



            //User Events
            var getUserOrg = TinyUserAccessor.GetOrganizationMembers(c.E1, c.E1.Organization.Id);
            for (int i = 0; i < getUserOrg.Count; i++) {

                //User Add TODO Event
                selectedEvents.Add(WebhookEventType.AddTODOforUser.GetDescription() + c.E1.Organization.Id);

                //User Changing TODO Event
                selectedEvents.Add(WebhookEventType.ChangingToDoforUser.GetDescription() + getUserOrg[i].UserOrgId);

                //User Changing Issue Event
                selectedEvents.Add(WebhookEventType.ChangingIssueforUser.GetDescription() + getUserOrg[i].UserOrgId);

            }
            WebhooksAccessor webhookAccessor = new WebhooksAccessor();
            StoreResult result = webhookAccessor.InsertWebHook(c.E1.GetEmail(), webhook, selectedEvents);
            Assert.AreEqual(result, StoreResult.Success);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
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

            var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E1, c.E1.Organization.Id);
            for (int i = 0; i < getAllL10RecurrenceAtOrganization.Count; i++) {

                //L10 Add TODO Events
                selectedEvents.Add(WebhookEventType.AddTODOtoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);


                //L10 Add Issue Events
                selectedEvents.Add(WebhookEventType.AddIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);

            }

            WebhooksAccessor webhookAccessor = new WebhooksAccessor();
            StoreResult result = webhookAccessor.InsertWebHook(c.E1.GetEmail(), webhook, selectedEvents);


            var getWebHook = webhookAccessor.LookupWebHook(c.E1.GetEmail(), webhook.Id);
            var getUserOrg = TinyUserAccessor.GetOrganizationMembers(c.E1, c.E1.Organization.Id);
            for (int i = 0; i < getUserOrg.Count; i++) {

                //User Add TODO Event
                selectedEvents.Add(WebhookEventType.AddTODOforUser.GetDescription() + c.E1.Organization.Id);

                //User Changing TODO Event
                selectedEvents.Add(WebhookEventType.ChangingToDoforUser.GetDescription() + getUserOrg[i].UserOrgId);

                //User Changing Issue Event
                selectedEvents.Add(WebhookEventType.ChangingIssueforUser.GetDescription() + getUserOrg[i].UserOrgId);

            }

            var updatedVal = "Test123";
            webhook.Description = updatedVal;
            var updateWebHook = webhookAccessor.UpdateWebHook(c.E1.GetEmail(), webhook, selectedEvents);
            var getUpdatedWebHook = webhookAccessor.LookupWebHook(c.E1.GetEmail(), webhook.Id);

            Assert.AreEqual(updatedVal, getUpdatedWebHook.Description);
        }


        [TestMethod]
        [TestCategory("Api_V0")]
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

            var getAllL10RecurrenceAtOrganization = L10Accessor.GetAllL10RecurrenceAtOrganization(c.E1, c.E1.Organization.Id);
            for (int i = 0; i < getAllL10RecurrenceAtOrganization.Count; i++) {

                //L10 Add TODO Events
                selectedEvents.Add(WebhookEventType.AddTODOtoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);


                //L10 Add Issue Events
                selectedEvents.Add(WebhookEventType.AddIssuetoL10.GetDescription() + getAllL10RecurrenceAtOrganization[i].Id);

            }

            WebhooksAccessor webhookAccessor = new WebhooksAccessor();
            StoreResult result = webhookAccessor.InsertWebHook(c.E1.GetEmail(), webhook, selectedEvents);


            var getWebHook = webhookAccessor.LookupWebHook(c.E1.GetEmail(), webhook.Id);
            var s = webhookAccessor.DeleteWebHook(c.E1.GetEmail(), getWebHook.Id);

            Assert.AreEqual(s, StoreResult.Success);
        }

    }
}
