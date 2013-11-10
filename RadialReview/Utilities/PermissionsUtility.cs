using log4net;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    [Obsolete("Not really obsolete. I just want this to stick out.", false)]
    public class PermissionsUtility
    {
        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public static Boolean IsRadialAdmin(ISession session, UserOrganizationModel caller)
        {
            if (caller.IsRadialAdmin)
                return true;
            return false;
        }


        public static Boolean EditOrganization(ISession session, UserOrganizationModel caller, long organizationId)
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

            if (caller.ManagingUsers.Any(x => x.Id == userId) && caller.IsManager) //IsManager may be too much
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

        public static Boolean ViewQuestion(ISession session, UserOrganizationModel caller, QuestionModel question)
        {
            if (IsRadialAdmin(session, caller))
                return true;
            switch (question.OriginType)
            {
                case OriginType.User: if (!OwnedBelowOrEqual(session, caller, x => x.CustomQuestions.Any(y => y.Id == question.Id))) throw new PermissionsException(); break;
                case OriginType.Group: if (!caller.ManagingGroups.Select(x => x.Id).Union(caller.Groups.Select(x => x.Id)).Any(x => question.Id == x)) throw new PermissionsException(); break;
                case OriginType.Organization: if (caller.Organization.Id != question.ForOrganization.Id) throw new PermissionsException(); break;
                case OriginType.Industry: break;
                case OriginType.Application: break;
                case OriginType.Invalid: throw new PermissionsException();
                default: throw new PermissionsException();
            }
            return true;
        }
        public static Boolean ViewUserOrganization(ISession session, UserOrganizationModel caller, long userOrganizationId)
        {
            if (IsRadialAdmin(session, caller))
                return true;
            if (OwnedBelowOrEqual(session, caller, x => x.Id == userOrganizationId))
                return true;
            throw new PermissionsException();
        }


        public static Boolean ViewOrigin(ISession session,UserOrganizationModel caller, OriginType originType,long originId)
        {
            switch (originType)
            {
                case OriginType.User: return ViewUserOrganization(session, caller, originId);
                case OriginType.Group: return ViewGroup(session, caller, originId);
                case OriginType.Organization: return ViewOrganization(session, caller, originId);
                case OriginType.Industry: return ViewIndustry(session, caller, originId);
                case OriginType.Application: return ViewApplication(session, caller, originId);
                case OriginType.Invalid: throw new PermissionsException();
                default: throw new PermissionsException();
            }
        }
        public static Boolean ViewGroup(ISession session, UserOrganizationModel caller, long groupId)
        {
            if (IsRadialAdmin(session, caller))
                return true;
            if (caller.Groups.Any(x=>x.Id==groupId))
                return true;
            if (OwnedBelowOrEqual(session, caller, x => x.ManagingGroups.Any(y=>y.Id==groupId)))
                return true;
            throw new PermissionsException();
        }

        public static Boolean ViewOrganization(ISession session, UserOrganizationModel caller, long organizationId)
        {
            if (IsRadialAdmin(session, caller))
                return true;
            if (caller.Organization.Id == organizationId)
                return true;
            throw new PermissionsException();
        }

        public static Boolean ViewApplication(ISession session, UserOrganizationModel caller, long applicationId)
        {
            log.Info("ViewApplication always returns true.");
            return true;
        }
        public static Boolean ViewIndustry(ISession session, UserOrganizationModel caller, long industryId)
        {
            log.Info("ViewIndustry always returns true.");
            return true;
        }

        public static Boolean OwnedBelowOrEqual(ISession session, UserOrganizationModel caller, Predicate<UserOrganizationModel> visiblility)
        {
            if (visiblility(caller))
                return true;
            foreach (var manager in caller.ManagedBy)
            {
                if (OwnedBelowOrEqual(session, manager, visiblility))
                    return true;
            }
            return false;
        }


    }
}