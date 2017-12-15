using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Notifications {
	public class AngularAppNotification : BaseAngular{

		public string Name { get; set; }
		public string Details { get; set; }
		public string ImageUrl { get; set; }
		public DateTime? Date { get; set; }
		public bool? IsRead { get; set; }


	}
}