using NHibernate;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
    public interface ICreateUserOrganizationHook: IHook {
        void CreateUser(ISession s, UserOrganizationModel user);
    }
}
