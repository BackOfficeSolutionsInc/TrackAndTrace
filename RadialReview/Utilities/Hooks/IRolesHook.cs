using NHibernate;
using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface IRolesHook : IHook {
		Task CreateRole(ISession s, RoleModel role);
		Task UpdateRole(ISession s, RoleModel role);
		Task DeleteRole(ISession s, RoleModel role);
	}
}
