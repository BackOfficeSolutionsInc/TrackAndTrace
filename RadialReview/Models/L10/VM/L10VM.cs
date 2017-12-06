using RadialReview.Utilities.DataTypes;
using System.Collections.Generic;

namespace RadialReview.Models.L10.VM
{
	public class L10VM
	{

		public TinyRecurrence Recurrence { get; set; }
		public bool? IsAttendee { get; set; }
		//public bool AdminMeeting { get; set; }

		public L10VM(TinyRecurrence recurrence)
		{
			Recurrence = recurrence;
		}
	}

	public class TinyRecurrence {
		public long Id { get; set; }
		public string Name { get; set; }
		public long? MeetingInProgress { get; set; }
		public bool IsAttendee { get; set; }
		public List<TinyUser> _DefaultAttendees { get; set; }
	}
}