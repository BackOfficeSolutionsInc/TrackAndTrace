using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace RadialReview.Hangfire {
    public static class HangfireQueues {

        ///<summary>
        ///Scheduled or recurrent jobs SHOULD NOT update with a timestamp. All previous jobs will stop running
        ///</summary>
        public static class Scheduled {
            /*Example (different code for different versions): 
                //need both
                public const string RECURRING_V1 = "recurring_v1"; //Decorate this version of the method with [Queue(HangfireQueues.Scheduled.RECURRING_V1)]
                public const string RECURRING_V2 = "recurring_v2"; //Decorate this version of the method with [Queue(HangfireQueues.Scheduled.RECURRING_V2)]
            */

            /*Example (Both software versions need update): 
                //Do not change the version id.
                //CAUTION: the old server could still execute this code. Publish quickly.
                public const string RECURRING_V1 = "recurring_v1"; //Keep the method decorated with [Queue(HangfireQueues.Scheduled.RECURRING_V1)]
            */
        }


        ///<summary>
        ///Immediate fire jobs should update their queue. If we change the code, we do not want to run on the wrong verions
        ///</summary>
        public static class Immediate {


            public const string BUILD_TIME = "08/23/2018 03:00:00";

            public const string CRITICAL = "critical_w_v1";
            public const string ETHERPAD = "etherpad_w_v1";
            public const string FIREPAD = "firepad_w_v1";
            public const string CONCLUSION_EMAIL = "conclusionemail_w_v1";
            public const string GENERATE_QC = "generateqc_w_v1";
            public const string CHARGE_ACCOUNT_VIA_HANGFIRE = "chargeaccount_w_v1";
            public const string EXECUTE_TASKS = "executetasks_w_v1";
            public const string DAILY_TASKS = "dailytasks_w_v1";
			public const string SCHEDULED_QUARTERLY_EMAIL = "scheduledquarterlyemail_w_v1";
			public const string NOTIFY_MEETING_START = "notifymeetingstart_w_v1";
			public const string EXECUTE_EVENT_ANALYZERS = "executeeventanalyzers_w_v1";
			public const string GENERATE_ALL_DAILY_EVENTS = "generate_all_daily_events_w_v1";

			public const string ASANA_EVENTS = "asana_w_v1";

			public const string ALPHA = "alpha_w_v1";


		}


		public static readonly string[] OrderedQueues = new[]{
			Immediate.CRITICAL,
			Immediate.ETHERPAD,
            Immediate.FIREPAD,
			Immediate.CONCLUSION_EMAIL,
			Immediate.GENERATE_QC,
			Immediate.CHARGE_ACCOUNT_VIA_HANGFIRE,
			Immediate.EXECUTE_TASKS,
			Immediate.DAILY_TASKS,
			Immediate.NOTIFY_MEETING_START,
			Immediate.SCHEDULED_QUARTERLY_EMAIL,
			Immediate.EXECUTE_EVENT_ANALYZERS,
			Immediate.ASANA_EVENTS,
			Immediate.GENERATE_ALL_DAILY_EVENTS,
			DEFAULT,
            Immediate.ALPHA 
        };



        ///<summary>
        ///I think we want it to run the jobs even if they are (incorrectly) unmarked
        ///</summary>
        public const string DEFAULT = "default";
    }
}