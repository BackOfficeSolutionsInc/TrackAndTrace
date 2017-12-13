using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.L10;
using RadialReview.Utilities;

namespace RadialReview.Models.Angular.Meeting
{

	public class AngularMeetingNotes : BaseAngular 
	{
		public AngularMeetingNotes(L10Note note) : base(note.Id)
		{
			//Contents = note.Contents;
			Title = note.Name;
			DetailsUrl = Config.NotesUrl() + "p/" + note.PadId + "?showControls=true&showChat=false";
		}

		public AngularMeetingNotes(){
		}

		public string DetailsUrl { get; set; }
		//public String Contents { get; set; }
		public String Title { get; set; }
	}
}