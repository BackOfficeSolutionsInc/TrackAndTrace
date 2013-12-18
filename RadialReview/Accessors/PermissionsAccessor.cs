using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class PermissionsAccessor
    {

        public void Permitted(UserOrganizationModel caller, Action<PermissionsUtility> ensurePermitted)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    ensurePermitted(PermissionsUtility.Create(s, caller));
                }
            }
        }
    }
}