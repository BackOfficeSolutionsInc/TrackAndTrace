using NHibernate;
using RadialReview.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface IUserRoleHook : IHook {
		Task AddRole(ISession s, long userId, UserRoleType type);
		Task RemoveRole(ISession s, long userId, UserRoleType type);
	}
}
