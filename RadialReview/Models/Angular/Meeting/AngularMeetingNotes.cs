using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.L10;

namespace RadialReview.Models.Angular.Meeting
{

	public class AngularMeetingNotes : BaseAngular 
	{
		public AngularMeetingNotes(L10Note note) : base(note.Id)
		{
			Contents = note.Contents;
			Title = note.Name;
		}

		public AngularMeetingNotes(){
		}

		public String Contents { get; set; }
		public String Title { get; set; }
	}
}