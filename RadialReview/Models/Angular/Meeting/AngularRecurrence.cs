using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Models.Angular.Meeting
{
	public class AngularRecurrence : BaseAngular
	{
		public AngularRecurrence(L10Recurrence recurrence) : this(recurrence.Id){
			Name = recurrence.Name;
            Prioritization = recurrence.Prioritization!=PrioritizationType.Invalid?recurrence.Prioritization:PrioritizationType.Priority;

		}

		public AngularRecurrence(long id):base(id){
			
		}

		public string Name { get; set; }
		public IEnumerable<AngularUser> Attendees { get; set; }
		public AngularScorecard Scorecard { get; set; }
		public IEnumerable<AngularMeetingNotes> Notes { get; set; }
		public IEnumerable<AngularRock> Rocks { get; set; }
		public IEnumerable<AngularTodo> Todos { get; set; }
		public IEnumerable<AngularIssue> Issues { get; set; } 
		public AngularDateRange date { get; set; }
		public string HeadlinesUrl { get; set; }
        public PrioritizationType Prioritization { get; set; }
	}


	public class AngularDateRange
	{
		public DateTime startDate { get; set; }
		public DateTime endDate { get; set; }
	}

	//	public class AngularScorecardRow : BaseAngular
	//	{
	//		public AngularScorecardRow(MeasurableModel measurable,List<AngularWeek> weeks,List<ScoreModel> allScores) : base(measurable.Id){
					
	//		}
	//		public List<AngularScorecardItem> Items { get; set; }
	//	}

	//	public class AngularScorecardItem : BaseAngular
	//	{
	//		public AngularScorecardItem(ScoreModel score) :base(score.Id){
				
	//		}
	//	}
	//}
}