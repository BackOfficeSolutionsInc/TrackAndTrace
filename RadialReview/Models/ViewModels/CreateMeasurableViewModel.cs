using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels {
	public class CreateMeasurableViewModel {
		public string Title { get; set; }
		public long AccountableUser { get; set; }
		public long? AdminUser { get; set; }
		public UnitType Units { get; set; }
		public decimal Goal { get; set; }
		public decimal? AltGoal { get; set; }
		public bool ShowCumulative { get; set; }
		public DateTime? CumulativeRange { get; set; }
		public LessGreater GoalDirection { get; set; }

		public List<SelectListItem> PotentialUsers { get; set; }

	}
}