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
using log4net;

namespace RadialReview.Hooks {
	public class ActiveCampaignEventHooks : IAccountEvent, ICreateUserOrganizationHook {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

				var emails = new List<string>();
				//Send event to primary contact when these events happen
				var eventToPrimaryWhen = new[] {
					EventType.StartDepartmentMeeting,
					EventType.StartLeadershipMeeting,
					EventType.CreateMeeting,
					EventType.PaymentFailed,
					EventType.PaymentEntered,
				};
				if (eventToPrimaryWhen.Any(x => x == type)) {
					try {
						var pid = s.Get<OrganizationModel>(evt.OrgId).PrimaryContactUserId;
						if (pid != null) {
							var e = pid.NotNull(x => s.Get<UserOrganizationModel>(x.Value).GetEmail());
							if (e != null)
								emails.Add(e);
						}
					} catch (Exception e) {
						log.Error(e);
					}
				}

				//Send event to trigger user when these events happen
				var eventToCallerWhen = new[] {
					EventType.CreateLeadershipMeeting,
					EventType.CreateDepartmentMeeting,
					EventType.CreateMeeting,
					EventType.ConcludeMeeting,
				};
				if (eventToCallerWhen.Any(x => x == type) && evt.TriggeredBy != null) {
					try {
						var e = s.Get<UserOrganizationModel>(evt.TriggeredBy.Value).NotNull(x => x.GetEmail());
						if (e != null)
							emails.Add(e);
					} catch (Exception e) {
						log.Error(e);
					}
				}

				//Send event to Billing user when these events happen
				var eventToBillingWhen = new[] {
					EventType.PaymentFailed,
					EventType.PaymentFree,
					EventType.PaymentReceived,
					EventType.PaymentEntered,
				};
				if (eventToBillingWhen.Any(x => x == type) && evt.TriggeredBy != null) {
					try {
						var tokens = s.QueryOver<PaymentSpringsToken>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == evt.OrgId && x.Active == true)
						.List().SingleOrDefault();
						var e = tokens.NotNull(x => x.ReceiptEmail);
						if (e != null)
							emails.Add(e);
					} catch (Exception e) {
						log.Error(e);
					}
				}

				//None set? Send to Primary contact just to be sure.
				if (!emails.Any()) {
					try {
						var pid = s.Get<OrganizationModel>(evt.OrgId).PrimaryContactUserId;
						if (pid != null) {
							var e = pid.NotNull(x => s.Get<UserOrganizationModel>(x.Value).GetEmail());
							if (e != null)
								emails.Add(e);
						}
					} catch (Exception e) {
						log.Error(e);
					}
				}

				if (emails.Any()) {
					foreach (var e in emails.Distinct()) {
						await Connector.EventAsync(evt.Type.Kind(), e, new Dictionary<string, string>() {
							{ "eventdata", "eid:"+evt.Id+" msg:"+evt.Message }
						});
					}
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
				fields[configs.Fields.ReferralYear] = "" + DateTime.UtcNow.Year;
				fields[configs.Fields.ReferralQuarter] = "Q" + ApplicationAccessor.GetTTQuarter(DateTime.UtcNow);

				if (orgCreationData.AccountType == AccountType.Demo) {

					fields[configs.Fields.TrialStart] = DateTime.UtcNow.Date.ToShortDateString();
					if (orgCreationData.TrialEnd.HasValue) {
						fields[configs.Fields.TrialEnd] = orgCreationData.TrialEnd.Value.ToShortDateString();
					}
				}

				if (coach != null) {
					await connector.EventAsync("AClientRegistered", coach.Email);
					var updateCoach = new Dictionary<long, string>() {
						{configs.Fields.CoachLastReferral, DateTime.UtcNow.ToShortDateString() },
						{configs.Fields.CoachHasReferral, "Yes" }
					};
					var lists = configs.Lists.CoachThatReferred.AsList();

					await connector.SyncContact(configs, coach.Email, listIds: lists, fieldVals: updateCoach);
				}
			}

			await connector.SyncContact(configs, contact, listIds, tags, fields);
		}
	}
}
