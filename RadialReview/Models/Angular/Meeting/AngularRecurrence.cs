﻿using System;
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
using RadialReview.Models.Angular.Headlines;
using RadialReview.Model.Enums;

namespace RadialReview.Models.Angular.Meeting
{
	public class AngularRecurrence : BaseAngular
	{
		public AngularRecurrence(L10Recurrence recurrence) : this(recurrence.Id){
            Basics = new AngularBasics(recurrence.Id) {
			    Name = recurrence.Name,
                TeamType = recurrence.TeamType,                
            };
            IssuesList.Prioritization = recurrence.Prioritization!=PrioritizationType.Invalid?recurrence.Prioritization:PrioritizationType.Priority;
            VtoId = recurrence.VtoId;

			HeadlineType = recurrence.HeadlineType;
		}

		public AngularRecurrence(long id):base(id){
            IssuesList = new AngularIssuesList(id);
		}
        //[Obsolete("Do not use.",false)]
        public AngularRecurrence(){
        }

        public AngularBasics Basics { get; set; }

		//public string Name { get; set; }
		public IEnumerable<AngularUser> Attendees { get; set; }
		public AngularScorecard Scorecard { get; set; }
		public IEnumerable<AngularMeetingNotes> Notes { get; set; }
		public IEnumerable<AngularRock> Rocks { get; set; }
		public IEnumerable<AngularTodo> Todos { get; set; }
		public IEnumerable<AngularHeadline> Headlines { get; set; }
		public AngularIssuesList IssuesList { get; set; }
		public AngularDateRange date { get; set; }
        public string HeadlinesUrl { get; set; }
		public PeopleHeadlineType HeadlineType { get; set; }
        //public PrioritizationType Prioritization { get; set; }
        public long? VtoId { get; set; }
	}

    public class AngularBasics : BaseAngular {
        public AngularBasics(long recurrenceId): base(recurrenceId){
		}

        //[Obsolete("Do not use")]
        public AngularBasics(){
        }

        public string Name { get; set; }
        public L10TeamType? TeamType { get; set; }
    }
    public class AngularIssuesList : BaseAngular {

        public AngularIssuesList(long recurrenceId): base(recurrenceId){
		}

        //[Obsolete("Do not use")]
        public AngularIssuesList(){
        }
        public IEnumerable<AngularIssue> Issues { get; set; }
        public PrioritizationType? Prioritization { get; set; }
    }

	public class AngularDateRange
	{
		public DateTime startDate { get; set; }
		public DateTime endDate { get; set; }

        public AngularDateRange()
        {

        }
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