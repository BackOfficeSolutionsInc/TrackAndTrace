namespace RadialReview.Models.L10.VM
{
	public class L10VM
	{

		public L10Recurrence Recurrence { get; set; }
		public bool? IsAttendee { get; set; }
		public bool EditMeeting { get; set; }

		public L10VM(L10Recurrence recurrence)
		{
			Recurrence = recurrence;
		}
	}
}