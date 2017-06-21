using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.CoreProcess.Accessors {
	public class ProcessDefAccessor : IProcessDefAccessor {
		public string Deploy(UserOrganizationModel caller, string key) {
			// call Comm Layer
			return string.Empty;
		}
		public IProcessDef GetProcessDefByKey(UserOrganizationModel caller, string key) {
			throw new NotImplementedException();
		}
	}
}