using System;
using System.Collections.Generic;
using NHibernate.Linq.GroupBy;
using RadialReview.Models.Angular.Users;

namespace RadialReview.Models.Angular.Meeting
{
	public class AngularMeeting : Base.BaseAngular
	{
		public AngularMeeting(long recurrenceId) : base(recurrenceId){
			RecurrenceId = recurrenceId;
			Attendees = new List<AngularUser>();
			AgendaItems = new List<AngularAgendaItem>();
			Notes = new List<AngularMeetingNotes>();
		}
	
		public AngularUser Leader { get; set; }
		public string CurrentPage { get; set; }

		public long? MeetingId { get; set; }
		public long? RecurrenceId { get; set; }
		public String Name { get; set; }
		public DateTime? Start { get; set; }
		public List<AngularUser> Attendees { get; set; }
		public List<AngularAgendaItem> AgendaItems { get; set; }
		public List<AngularMeetingNotes> Notes { get; set; } 
		
	}

	
}