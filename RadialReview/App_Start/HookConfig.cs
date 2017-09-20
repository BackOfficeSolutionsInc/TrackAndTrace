﻿using RadialReview.Accessors.Hooks;
using RadialReview.Hooks;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.App_Start {

   

    public class HookConfig {
        
        internal static void RegisterHooks()
        {
			//HooksRegistry.RegisterHook(new CreateUserOrganization_UpdateHierarchy());

			HooksRegistry.RegisterHook(new UpdateUserModel_TeamNames());
			HooksRegistry.RegisterHook(new UpdateRoles_Notifications());
			//HooksRegistry.RegisterHook(new TodoWebhook());

			//HooksRegistry.RegisterHook(new IssueWebhook());
			HooksRegistry.RegisterHook(new ActiveCampaignEventHooks());
            HooksRegistry.RegisterHook(new EnterpriseHook(Config.EnterpriseAboveUserCount()));

            HooksRegistry.RegisterHook(new Tasks_RealTime());

            //HooksRegistry.RegisterHook(new TodoEdit())
        }
    }
}
