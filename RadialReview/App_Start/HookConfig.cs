using RadialReview.Accessors.Hooks;
using RadialReview.Crosscutting.Hooks.CrossCutting;
using RadialReview.Crosscutting.Hooks.CrossCutting.Formula;
using RadialReview.Crosscutting.Hooks.Payment;
using RadialReview.Crosscutting.Hooks.QuarterlyConversation;
using RadialReview.Hooks;
using RadialReview.Hooks.CrossCutting;
using RadialReview.Hooks.CrossCutting.ActiveCampaign;
using RadialReview.Hooks.CrossCutting.Payment;
using RadialReview.Hooks.Meeting;
using RadialReview.Hooks.Realtime;
using RadialReview.Hooks.Realtime.Dashboard;
using RadialReview.Hooks.Realtime.L10;
using RadialReview.Hooks.UserRegistration;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.App_Start {



	public class HookConfig {

		public static void RegisterHooks() {
			//HooksRegistry.RegisterHook(new CreateUserOrganization_UpdateHierarchy());

			HooksRegistry.RegisterHook(new UpdateUserModel_TeamNames());
			HooksRegistry.RegisterHook(new UpdateRoles_Notifications());
			HooksRegistry.RegisterHook(new UpdateUserCache());

			//HooksRegistry.RegisterHook(new TodoWebhook());
			//HooksRegistry.RegisterHook(new IssueWebhook());

			HooksRegistry.RegisterHook(new ActiveCampaignEventHooks());
			HooksRegistry.RegisterHook(new EnterpriseHook(Config.EnterpriseAboveUserCount()));
			HooksRegistry.RegisterHook(new ActiveCampaignFirstThreeMeetings());


			HooksRegistry.RegisterHook(new DepristineHooks());
			HooksRegistry.RegisterHook(new MeetingRockCompletion());
			HooksRegistry.RegisterHook(new AuditLogHooks());

			HooksRegistry.RegisterHook(new RealTime_Tasks());
			HooksRegistry.RegisterHook(new RealTime_L10_Todo());
			HooksRegistry.RegisterHook(new RealTime_Dashboard_Todo());
			HooksRegistry.RegisterHook(new RealTime_L10_Issues());

			HooksRegistry.RegisterHook(new Realtime_L10Scorecard());
			HooksRegistry.RegisterHook(new RealTime_L10_UpdateRocks());
			HooksRegistry.RegisterHook(new RealTime_VTO_UpdateRocks());
			HooksRegistry.RegisterHook(new RealTime_Dashboard_UpdateL10Rocks());
			HooksRegistry.RegisterHook(new RealTime_Dashboard_Scorecard());
			HooksRegistry.RegisterHook(new RealTime_L10_Headline());

			HooksRegistry.RegisterHook(new CalculateCumulative());
			HooksRegistry.RegisterHook(new AttendeeHooks());
			HooksRegistry.RegisterHook(new SwapScorecardOnRegister());

			HooksRegistry.RegisterHook(new CreateFinancialPermItems());

			HooksRegistry.RegisterHook(new UpdatePlaceholder());
			HooksRegistry.RegisterHook(new RealTime_L10_Milestone());
			//HooksRegistry.RegisterHook(new TodoEdit())
			HooksRegistry.RegisterHook(new CascadeScorecardFormulaUpdates());
			HooksRegistry.RegisterHook(new RealTime_Positions());

			HooksRegistry.RegisterHook(new CascadeScorecardFormulaUpdates());

			HooksRegistry.RegisterHook(new ExecutePaymentCardUpdate());
			HooksRegistry.RegisterHook(new FirstPaymentEmail());
			HooksRegistry.RegisterHook(new SetDelinquentFlag());

			HooksRegistry.RegisterHook(new QuarterlyConversationCreationNotifications());
			HooksRegistry.RegisterHook(new SetPeopleToolsTrial());
			

			//HooksRegistry.RegisterHook(new TodoEdit())
		}
	}
}
