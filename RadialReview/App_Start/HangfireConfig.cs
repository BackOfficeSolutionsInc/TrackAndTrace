using Hangfire;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Owin;
using RadialReview.Accessors;
using RadialReview.Hangfire;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RadialReview.App_Start {
	public class HangfireConfig {

		public static void Configure(IAppBuilder app) {
			GlobalConfiguration.Configuration.UseRedisStorage(Config.GetHangfireConnectionString());
			//GlobalConfiguration.Configuration.UseStorage(new MySqlStorage(Config.GetHangfireConnectionString(), new MySqlStorageOptions()));

			app.UseHangfireDashboard("/hangfire", new DashboardOptions {
				Authorization = new[] { new HangfireAuth() }
			});

            
            var awsEnv = "awsenv_"+(new Regex("[^a-zA-Z0-9]").Replace(Config.GetAwsEnv(), ""));
            string[] myQueues = new string[] { awsEnv , HangfireQueues.DEFAULT};
            if (Config.IsHangfireWorker()) {
			    myQueues = new[] { awsEnv }.Union(HangfireQueues.OrderedQueues).ToArray();

                if (!Config.IsDefinitelyAlpha()) {
                    myQueues = myQueues.Where(x => x != HangfireQueues.Immediate.ALPHA).ToArray();
                }
            }


            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });
			app.UseHangfireServer(new BackgroundJobServerOptions() {
				//WorkerCount = 1,
				Queues = myQueues
			});

			GlobalJobFilters.Filters.Add(new ProlongExpirationTimeAttribute());

		}

		public class ProlongExpirationTimeAttribute : JobFilterAttribute, IApplyStateFilter {
			public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction) {
				context.JobExpirationTimeout = TimeSpan.FromDays(7);
			}

			public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) {}
		}

		public class HangfireAuth : IDashboardAuthorizationFilter {
			public bool Authorize(DashboardContext context) {
				// In case you need an OWIN context, use the next line, `OwinContext` class
				// is the part of the `Microsoft.Owin` package.
				var owinContext = new OwinContext(context.GetOwinEnvironment());

				// Allow all authenticated users to see the Dashboard (potentially dangerous).
				try {
					var user = new UserAccessor().GetUserById(owinContext.Authentication.User.Identity.GetUserId());
					if (user != null) {
						return user.IsRadialAdmin;
					}
				} catch (Exception e) {
					int a = 0;
				}
				return false;
			}
		}
	}
}