using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.L10
{
	public class L10VM
	{

		public L10Recurrence Recurrence { get; set; }
		public bool? IsAttendee { get; set; }

		public L10VM(L10Recurrence recurrence)
		{
			Recurrence = recurrence;
		}
	}
}