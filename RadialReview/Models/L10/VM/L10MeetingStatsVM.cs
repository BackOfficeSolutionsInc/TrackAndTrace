using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.L10.VM
{
	public class L10MeetingStatsVM
	{
		public List<L10Meeting> AllMeetings { get; set; }

		public int IssuesSolved { get; set; }

		public int TodoComplete { get; set; }


	}
}