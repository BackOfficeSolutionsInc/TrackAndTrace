using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using RadialReview.Models.Askables;
using RadialReview.Models.Permissions;
using RadialReview.Models.Scorecard;

namespace RadialReview.Models.L10.VM
{
	public class L10EditVM
	{
		public L10Recurrence Recurrence { get; set; }
		public List<RockModel> PossibleRocks { get; set; }
		public List<UserOrganizationModel> PossibleMembers { get; set; }
		public List<MeasurableModel> PossibleMeasurables { get; set; }
		[MinLength(1)]
		public long[] SelectedMembers { get; set; }
		public long[] SelectedMeasurables { get; set; }
		public long[] SelectedRocks { get; set; }

		

		public L10EditVM()
		{
			SelectedMembers = new long[0] { };
			SelectedMeasurables = new long[0] { };
			SelectedRocks=new long[0]{};

	

		}

		public string Return { get; set; }

		public PermissionDropdownVM PermissionsDropdown { get; set; }
	}
}