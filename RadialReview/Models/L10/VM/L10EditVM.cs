using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using RadialReview.Models.Scorecard;

namespace RadialReview.Models.L10.VM
{
	public class L10EditVM
	{
		public L10Recurrence Recurrence { get; set; }
		public List<UserOrganizationModel> PossibleMembers { get; set; }
		public List<MeasurableModel> PossibleMeasurables { get; set; }
		[MinLength(1)]
		public long[] SelectedMembers { get; set; }
		public long[] SelectedMeasurables { get; set; }

		public L10EditVM()
		{
			SelectedMembers = new long[0] { };
			SelectedMeasurables = new long[0] { };
		}
	}
}