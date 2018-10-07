using RadialReview.Exceptions;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities {
    public class LockoutUtility {

        public static void ProcessLockout(UserOrganizationModel userOrg) {
            if (userOrg.DeleteTime != null)
                throw new PermissionsException("You're no longer attached to this organization.");
            if (userOrg.Organization.DeleteTime != null) {
                if (userOrg.Organization.Lockout == LockoutType.Payment) {
                    if (userOrg._PermissionsOverrides!=null && userOrg._PermissionsOverrides.IgnorePaymentLockout)
                        return; //SKIP ALL REMAINING CHECKS...
                    throw new RedirectToActionException("Payment", "Lockout");
                }
                throw new PermissionsException("This organization no longer exists.");
            }
        }
    }
}