using NHibernate;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
    interface IDeleteUserOrganizationHook : IHook {
        void DeleteUser(ISession s, UserOrganizationModel user);
        void UndeleteUser(ISession s, UserOrganizationModel user);
    }
}
