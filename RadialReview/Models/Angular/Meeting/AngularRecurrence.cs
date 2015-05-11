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
		}

		public AngularRecurrence(long id):base(id){
			
		}

		public string Name { get; set; }
		public List<AngularUser> Attendees { get; set; }
		public AngularScorecard Scorecard { get; set; }
		public List<AngularMeetingNotes> Notes { get; set; }
		public List<AngularRock> Rocks { get; set; }
		public List<AngularTodo> Todos { get; set; }
		public List<AngularIssue> Issues { get; set; } 
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