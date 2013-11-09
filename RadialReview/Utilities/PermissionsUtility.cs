using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    [Obsolete("Not really obsolete. I just want this to stick out.",false)]
    public class PermissionsUtility
    {
        public static Boolean IsRadialAdmin(ISession session, UserOrganizationModel caller)
        {
            if (caller.IsRadialAdmin)
                return true;
            return false;
        }


        public static Boolean EditOrganization(ISession session,UserOrganizationModel caller,long organizationId)
        {
            if (IsRadialAdmin(session, caller))
                return true;

            if (caller.Organization.Id == organizationId && caller.IsManagerCanEditOrganization)
                return true;
            throw new PermissionsException();
        }

        public static Boolean EditUserOrganization(ISession session, UserOrganizationModel caller, long userId)
        {
            if (IsRadialAdmin(session, caller))
                return true;

            if (caller.ManagingUsers.Any(x=>x.Id==userId) && caller.IsManager) //IsManager may be too much
                return true;
            //Could do some cascading here if we want.

            throw new PermissionsException();
        }

        public static Boolean EditGroup(ISession session, UserOrganizationModel caller, long groupId)
        {
            if (IsRadialAdmin(session, caller))
                return true;

            if (caller.ManagingGroups.Any(x => x.Id == groupId) && caller.IsManager) //IsManager may be too much
                return true;
            //Could do some cascading here if we want.

            throw new PermissionsException();
        }

        public static Boolean EditApplication(ISession session, UserOrganizationModel caller, long forId)
        {
            if (IsRadialAdmin(session, caller))
                return true;
            throw new PermissionsException();
        }

        public static Boolean EditIndustry(ISession session, UserOrganizationModel caller, long forId)
        {
            if (IsRadialAdmin(session, caller))
                return true;
            throw new PermissionsException();
        }
    }
}