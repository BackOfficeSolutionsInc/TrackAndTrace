using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels {
	public class CreateRockViewModel {
		public string Title { get; set; }
		public long AccountableUser { get; set; }
		public bool AddToVTO { get; set; }

		public List<SelectListItem> PotentialUsers { get; set; }

	}
}