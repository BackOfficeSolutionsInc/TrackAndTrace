using NHibernate;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public interface ICreateUserOrganizationHook : IHook {
		[Obsolete("User might not be attached yet")]
		Task CreateUserOrganization(ISession s, UserOrganizationModel user);
		Task OnUserOrganizationAttach(ISession s, UserOrganizationModel userOrganization);
		Task OnUserRegister(ISession s, UserModel user);
	}

	public interface IUpdateUserModelHook : IHook {
		Task UpdateUserModel(ISession s, UserModel user);
	}

	interface IDeleteUserOrganizationHook : IHook {
        Task DeleteUser(ISession s, UserOrganizationModel user);
        Task UndeleteUser(ISession s, UserOrganizationModel user);
    }
}
