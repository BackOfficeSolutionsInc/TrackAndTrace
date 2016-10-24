using NHibernate;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface IUpdateUserModelHook : IHook{

		void UpdateUserModel(ISession s, UserModel user);

	}
}
