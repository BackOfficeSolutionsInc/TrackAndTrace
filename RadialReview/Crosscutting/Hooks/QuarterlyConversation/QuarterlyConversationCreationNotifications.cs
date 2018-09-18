using RadialReview.Crosscutting.Hooks.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;
using RadialReview.Hubs;
using Microsoft.AspNet.SignalR;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Json;

namespace RadialReview.Crosscutting.Hooks.QuarterlyConversation {
	public class QuarterlyConversationCreationNotifications : IQuarterlyConversationHook {
		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.UI;
		}

		public async Task QuarterlyConversationCreated(ISession s, long qcId) {			
			var sc = s.Get<SurveyContainer>(qcId);
			if (sc.CreatedBy.ModelType == ForModel.GetModelType<UserOrganizationModel>()) {
				var user = s.Get<UserOrganizationModel>(sc.CreatedBy.ModelId);
				var hub = GlobalHost.ConnectionManager.GetHubContext<MessageHub>();
				hub.Clients.Group(MessageHub.GenerateUserId(user.Id)).addQC("Quarterly Conversation issued!", new AngularSurveyContainer(sc, false, AngularUser.CreateUser(user)) {
					Ordering = -1,
				});
			}
		}

		public async Task QuarterlyConversationError(ISession s, IForModel creator, QuarterlyConversationErrorType failureType, List<string> errors) {
			if (creator.Is<UserOrganizationModel>()) {
				var user = s.Get<UserOrganizationModel>(creator.ModelId);
				var hub = GlobalHost.ConnectionManager.GetHubContext<MessageHub>();
				var message = "An error occurred issuing the quarterly conversation.";
				switch (failureType) {
					case QuarterlyConversationErrorType.EmailsFailed:
						message = "Quarterly Conversation was generated, but email notifications failed";
						break;
					default:
						break;
				}
				hub.Clients.Group(MessageHub.GenerateUserId(user.Id)).showAlert(ResultObject.CreateError(message));
			}
		}

		public async Task QuarterlyConversationEmailsSent(ISession s, long qcId) {
			//Noop
		}

	}
}