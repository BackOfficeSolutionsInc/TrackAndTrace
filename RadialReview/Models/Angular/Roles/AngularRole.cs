using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Roles {
	public class AngularRole : BaseAngular {
		public AngularRole() { }

		public AngularRole(long id) : base(id) { }


		public AngularRole(RoleGroupRole role) : this(role.Role,role.Ordering) {
		}

		public AngularRole(RoleModel role,long? ordering=null) : base(role.Id) {
			Name = role.Role;
			// Owner = AngularUser.CreateUser(role.Owner);
			CreateTime = role.CreateTime;
			Ordering = ordering;
			//if (attach != null) {
			//	AttachType = attach.Type;
			//	AttachId = attach.Id;
			//	AttachName = attach.Name;
			//}
		}
		public string Name { get; set; }
		public long? Ordering { get; set; }
		//public AngularUser Owner { get; set; }

		public DateTime? CreateTime { get; set; }

	}

	public class AngularRoleGroup : BaseAngular {

		public AngularRoleGroup() { }

		public AttachType? AttachType { get; set; }
		public long? AttachId { get; set; }
		public String AttachName { get; set; }

		public bool? Editable { get; set; }

		public AngularRoleGroup(long id) : base(id) { }

		public static long GetId(Attach attach) {
			return attach.Id * (long) RadialReview.Models.Enums.AttachType.MAX + (long)attach.Type;
		}

		public IEnumerable<AngularRole> Roles { get; set; }

		public AngularRoleGroup(Attach attach,IEnumerable<AngularRole> roles,bool? editable=null) : base(GetId(attach)) {			
			AttachType = attach.Type;
			AttachId = attach.Id;
			AttachName = attach.Name;

			Roles = roles;

			Editable = editable;

		}
	}
	
}