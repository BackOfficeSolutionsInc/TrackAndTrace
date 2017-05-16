using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels {
	public class WebHookViewModel {
		public string Id { get; set; }
		public Uri WebHookUri { get; set; }
		public string Description { get; set; }

	}
}