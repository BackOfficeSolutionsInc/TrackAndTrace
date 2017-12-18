using NHibernate;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface IOrganizationHook : IHook {
		Task CreateOrganization(ISession s, UserOrganizationModel creator, OrganizationModel organization);
	}
}
