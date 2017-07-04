using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Events;
using RadialReview.Utilities;
using System.Net.Http;
using System.Threading.Tasks;
using RadialReview.Utilities.Integrations;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Accessors;
using RadialReview.Models.Payments;
using RadialReview.Models.Application;
using static RadialReview.Utilities.Config;

namespace RadialReview.Hooks {
	public class ActiveCampaignEventHooks : IAccountEvent, ICreateUserOrganizationHook {

		public ActiveCampaignConfig Configs { get; protected set; }
		public ActiveCampaignConnector Connector { get; protected set; }

		public ActiveCampaignEventHooks() {
			Configs = Config.GetActiveCampaignConfig();
			Connector = new ActiveCampaignConnector(Configs);
		}

		public async Task CreateEvent(ISession s, AccountEvent evt) {
			var type = evt.Type;

			if (type == EventType.CreatePrimaryContact) {
				await CreatePrimaryContact(s, evt, Configs, Connector);
			} else {
				string email = null;
				//Custom actions here to select email
				var eventToCallerWhen = new[] {
					EventType.CreateLeadershipMeeting,
					EventType.CreateDepartmentMeeting,
					EventType.CreateMeeting,
				};
				if (eventToCallerWhen.Any(x => x == type) && evt.TriggeredBy != null) {
					email = s.Get<UserOrganizationModel>(evt.TriggeredBy.Value).NotNull(x => x.GetEmail());
				}
				var eventToBillingWhen = new[] {
					EventType.PaymentFailed,
					EventType.PaymentFree,
					EventType.PaymentReceived,
				};
				if (eventToBillingWhen.Any(x => x == type) && evt.TriggeredBy != null) {
					var tokens = s.QueryOver<PaymentSpringsToken>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == evt.OrgId && x.Active == true)
						.List().SingleOrDefault();
					email = tokens.NotNull(x => x.ReceiptEmail);
				}

				//Fallback to primary contact
				if (email == null) {
					var pid = s.Get<OrganizationModel>(evt.OrgId).PrimaryContactUserId;
					email = pid.NotNull(x => s.Get<UserOrganizationModel>(x.Value).GetEmail());
				}

				if (email != null) {
					await Connector.EventAsync(evt.Type.Kind(), email, new Dictionary<string, string>() {
						{ "eventdata", ""+evt.Id+"~"+evt.Message }
					});
				}
			}
		}

		public async Task CreateUserOrganization(ISession s, UserOrganizationModel user) {
			//var configs = Config.GetActiveCampaignConfig();
			//var connector = new ActiveCampaignConnector(configs);
			await Connector.SyncContact(Configs, user, new List<long>());
		}

		public async Task OnUserRegister(ISession s, UserModel user) {
			//var configs = Config.GetActiveCampaignConfig();
			//var connector = new ActiveCampaignConnector(configs);
			await Connector.EventAsync("RegistrationComplete", user.UserName);
		}

		public async Task OnUserOrganizationAttach(ISession s, UserOrganizationModel userOrganization) {
			//var configs = Config.GetActiveCampaignConfig();
			//var connector = new ActiveCampaignConnector(configs);
			await Connector.EventAsync("AttachComplete", userOrganization.GetEmail());
		}



		private static async Task CreatePrimaryContact(ISession s, AccountEvent evt, Config.ActiveCampaignConfig configs, ActiveCampaignConnector connector) {
			var contact = s.Get<UserOrganizationModel>(evt.ForModel.ModelId);
			var tags = new List<string> { "primary_contact" };
			var listIds = new List<long>() { configs.Lists.PrimaryContact };
			if (contact.Organization.AccountType == AccountType.Implementer) {
				listIds.Add(configs.Lists.Implementer);
				tags.Add("is_eosi");
			}

			var fields = new Dictionary<long, string>();
			fields[configs.Fields.AccountType] = contact.Organization.AccountType + "";

			var orgCreationData = s.QueryOver<OrgCreationData>().Where(x => x.OrgId == contact.Organization.Id).List().FirstOrDefault();
			if (orgCreationData != null) {
				var assignedTo = orgCreationData.AssignedTo.NotNull(x => s.Get<SupportMember>(x.Value).User.GetName());
				if (assignedTo != null) {
					fields[configs.Fields.AssignedTo] = assignedTo;
				}
				var coach = orgCreationData.CoachId.NotNull(x => s.Get<Coach>(x.Value));

				fields[configs.Fields.CoachName] = coach.NotNull(x => x.Name);
				fields[configs.Fields.CoachType] = "" + coach.NotNull(x => x.CoachType);
				fields[configs.Fields.HasEosImplementer] = "" + orgCreationData.NotNull(x => x.HasCoach);

				fields[configs.Fields.ReferralSource] = orgCreationData.ReferralSource;
				fields[configs.Fields.ReferralYear] = ""+DateTime.UtcNow.Year;
				fields[configs.Fields.ReferralQuarter] = "Q" + ApplicationAccessor.GetTTQuarter(DateTime.UtcNow);

				if (coach != null) {
					await connector.EventAsync("AClientRegistered", coach.Email);
				}
			}

			await connector.SyncContact(configs, contact, listIds, tags, fields);
		}		
	}
}
