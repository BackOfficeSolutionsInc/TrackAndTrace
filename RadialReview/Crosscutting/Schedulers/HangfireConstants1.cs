/*

	WARNING: YOU SHOULD COMPLETELY UNDERSTAND THESE INSTRUCTIONS
	Not doing so could result in mission critital code not executing.

	INSTRUCTIONS
	When you add a Queue, choose if it is Immediate or Scheduled.

		
	For Immediate jobs:
		This Text Transform will automatically increment the queue version ID for you. 
		This way when we have two versions of the software, servers only execute jobs on the servers they started on

	For Scheduled jobs:
		We want hangfire to still work for old jobs. 
		Decide if you want old jobs to continue on the old version of the code or if you want all recurring jobs to move to the new code
			If you want old jobs to use old code, you need to have 2 versions of the queue and therefore 2 versions of the method
			If you want old jobs to use new code (Ex. You're fixing a bug), DO NOT increment the version. You need 1 version of the queue and 1 version of the method.
				CAUTION: Scheduled jobs can still run on the old version of the software, so you'll want to quickly publish.
	
*/
#region AUTO-GENERATED. Edit within HangfireConstants.tt
namespace RadialReview.Hangfire
{
    public static class HangfireQueues
    {
	
		///<summary>
		///Scheduled or recurrent jobs SHOULD NOT update with a timestamp. All previous jobs will stop running
		///</summary>
		public static class Scheduled{			
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
		public static class Immediate{

			
			public const string BUILD_TIME = "08/22/2018 03:27:22"; 
			public const string CONCLUSION_EMAIL = "conclusionemail_636705052428";  
			public const string GENERATE_QC = "generateqc_636705052428";  
			public const string ALPHA = "alpha_636705052428";   
			public const string CRITICAL = "critical_636705052428"; 
			public const string CHARGE_ACCOUNT_VIA_HANGFIRE = "chargeaccount_636705052428";   
			public const string EXECUTE_TASKS = "executetasks_636705052428";   
			public const string ETHERPAD = "etherpad_636705052428"; 
			public const string DAILY_TASKS = "dailytasks_636705052428";  

		}


		public static readonly string[] OrderedQueues=new []{
					HangfireQueues.Immediate.CRITICAL,
					HangfireQueues.Immediate.ETHERPAD,
					HangfireQueues.Immediate.CONCLUSION_EMAIL,
					HangfireQueues.Immediate.GENERATE_QC,
					HangfireQueues.Immediate.CHARGE_ACCOUNT_VIA_HANGFIRE,
					HangfireQueues.Immediate.EXECUTE_TASKS,
					HangfireQueues.DEFAULT,
					HangfireQueues.Immediate.ALPHA			 
		};



		///<summary>
		///I think we want it to run the jobs even if they are (incorrectly) unmarked
		///</summary>
		public const string DEFAULT = "default";
    }
}
#endregion 