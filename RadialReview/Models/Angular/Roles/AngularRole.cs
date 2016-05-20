using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Roles {
    public class AngularRole : BaseAngular
    {
        public AngularRole() { }
        public AngularRole(long id):base(id) {}

        public AngularRole(RoleModel role): base(role.Id)
        {
			Name = role.Role;
            Owner = AngularUser.CreateUser(role.Owner);
            CreateTime = role.CreateTime;
		}
		public string Name { get; set; }
		public AngularUser Owner { get; set; }

        public DateTime? CreateTime { get;set;}


    }
}