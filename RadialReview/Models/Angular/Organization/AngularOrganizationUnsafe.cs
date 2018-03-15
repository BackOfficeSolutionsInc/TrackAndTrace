using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Angular.Organization {
	public class AngularOrganizationUnsafe : BaseAngular {
		public AngularOrganizationUnsafe(long id):base(id) {}

		public AngularOrganizationUnsafe(OrganizationModel org):base(org.Id) {
			Name = org.GetName();
			Status = org.AccountType.GetDisplayName();
			CreateTime = org.CreationTime;
		}

		public string Name { get; set; }
		public DateTime CreateTime { get; set; }
		public string Status { get; set; }
	}
}