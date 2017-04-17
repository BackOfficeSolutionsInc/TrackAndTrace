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
using Newtonsoft.Json;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.Angular.DataType;

namespace RadialReview.Models.Angular.Meeting
{
	public class AngularRecurrence : BaseAngular
	{
		public AngularRecurrence(L10Recurrence recurrence) : this(recurrence.Id){
            Basics = new AngularBasics(recurrence.Id) {
			    Name = recurrence.Name,
                TeamType = recurrence.TeamType,                
            };
            IssuesList.Prioritization = recurrence.Prioritization;
            VtoId = recurrence.VtoId;
			HeadlineType = recurrence.HeadlineType;
			_Recurrence = AngularIgnore.Create(recurrence);


			ShowSegue		 =true;
			ShowScorecard	 =true;
			ShowRockReview	 =true;
			ShowHeadlines	 =true;
			ShowTodos		 =true;
			ShowIDS			 =true;
			ShowConclude	 =true;

		}

		public AngularRecurrence(long id):base(id){
            IssuesList = new AngularIssuesList(id);
		}

        public AngularRecurrence(){
        }

        public AngularBasics Basics { get; set; }
		
		public IEnumerable<AngularUser> Attendees { get; set; }
		public AngularScorecard Scorecard { get; set; }
		public IEnumerable<AngularMeetingNotes> Notes { get; set; }
		public IEnumerable<AngularRock> Rocks { get; set; }
		public IEnumerable<AngularTodo> Todos { get; set; }
		public IEnumerable<AngularHeadline> Headlines { get; set; }
		public AngularIssuesList IssuesList { get; set; }
		public AngularDateRange date { get; set; }
		public AngularDateRange dateDataRange { get; set; }
        public string HeadlinesUrl { get; set; }
		public PeopleHeadlineType? HeadlineType { get; set; }
        public long? VtoId { get; set; }

		public bool? ShowSegue			{ get; set; }
		public bool? ShowScorecard		{ get; set; }
		public bool? ShowRockReview		{ get; set; }
		public bool? ShowHeadlines		{ get; set; }
		public bool? ShowTodos			{ get; set; }
		public bool? ShowIDS			{ get; set; }
		public bool? ShowConclude		{ get; set; }

		public MeetingType? MeetingType { get; set; }

		public AngularIgnore<L10Recurrence> _Recurrence { get; set; }
        public string Focus { get; set; }
		public List<AngularString> LoadUrls { get; set; }
	}

    public class AngularBasics : BaseAngular {
        public AngularBasics(long recurrenceId): base(recurrenceId){
		}
		
        public AngularBasics(){
        }

        public string Name { get; set; }
        public L10TeamType? TeamType { get; set; }
    }

    public class AngularIssuesList : BaseAngular {

        public AngularIssuesList(long recurrenceId): base(recurrenceId){
		}
		
        public AngularIssuesList(){
        }

        public IEnumerable<AngularIssue> Issues { get; set; }
        public PrioritizationType? Prioritization { get; set; }
    }

	public class AngularDateRange
	{
		public DateTime startDate { get; set; }
		public DateTime endDate { get; set; }

		public AngularDateRange() {

		}
		public AngularDateRange(DateRange range) {
			startDate = range.StartTime;
			endDate = range.EndTime;
		}
	}	
}