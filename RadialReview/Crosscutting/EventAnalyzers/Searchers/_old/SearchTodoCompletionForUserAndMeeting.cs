using RadialReview.Accessors;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

//namespace RadialReview.Crosscutting.EventAnalyzers.Searchers {
//	public class SearchTodoCompletionForUserAndMeeting : BaseSearch<List<ScoreModel>> {
//		public SearchTodoCompletionForUserAndMeeting(long userId, long recurrenceId) {
//			UserId = userId;
//			RecurrenceId = recurrenceId;
//		}

//		public long UserId { get; set; }
//		public long RecurrenceId { get; set; }

//		public override List<ScoreModel> PerformSearch(IEventSettings settings) {
//			L10Accessor.CalculateIndividualTodoCompletionScores(
//		}

//		protected override IEnumerable<string> UniqueKeys(IEventSettings settings) {
//			return new[] {
//				"u_"+UserId,
//				"r_"+RecurrenceId,
//			};
//		}
//	}
//}