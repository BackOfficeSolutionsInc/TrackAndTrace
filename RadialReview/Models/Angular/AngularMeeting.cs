using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.L10VM;

namespace RadialReview.Models.Angular
{
	public class AngularMeeting : Angular
	{
		public AngularMeeting(long recurrenceId) : base(recurrenceId){
			RecurrenceId = recurrenceId;
		}

		public long? MeetingId { get; set; }
		public long? RecurrenceId { get; set; }
		public String Name { get; set; }
		public DateTime? Start { get; set; }
		public List<AngularUser> Attendees { get; set; }
		public List<AngularAgendaItem> AgendaItems { get; set; }
	
	}

	
}