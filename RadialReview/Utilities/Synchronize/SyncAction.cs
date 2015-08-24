using System;

namespace RadialReview.Utilities.Synchronize
{
	public class SyncAction
	{
		#region Internal 
		protected String ActionString;

		protected SyncAction(string actionString){
			ActionString = actionString;
		}
		
		public override string ToString()
		{
			return ActionString;
		}
		#endregion

		public static SyncAction MeasurableReorder(long recurrenceId){
			return new SyncAction("MeasurableReorder_" + recurrenceId);
		}
		public static SyncAction UpdateScore(long scoreId){
			return new SyncAction("ScoreValue_" + scoreId);
		}
		public static SyncAction UpdateIssueMessage(long issueId){
			return new SyncAction("IssueMessage_" + issueId);
		}
		public static SyncAction UpdateIssueDetails(long issueId){
			return new SyncAction("IssueDetails_" + issueId);
		}
		public static SyncAction UpdateTodoMessage(long todoId){
			return new SyncAction("TodoMessage_" + todoId);
		}
		public static SyncAction UpdateTodoDetails(long todoId){
			return new SyncAction("TodoDetails_" + todoId);
		}
		public static SyncAction UpdateTodoCompletion(long todoId){
			return new SyncAction("TodoCompletion_" + todoId);
		}
		public static SyncAction UpdateRockCompletion(long rockId){
			return new SyncAction("RockCompletion_" + rockId);
		}

	}
}