using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.L10.VM
{
	public class L10ListingVM
	{
		public List<L10VM> Recurrences { get; set; }
		public List<L10Meeting> Meetings { get; set; }

		public L10ListingVM()
		{
			Recurrences = new List<L10VM>();
			Meetings = new List<L10Meeting>();
		}
	}
}