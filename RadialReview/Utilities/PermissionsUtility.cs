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
        protected ISession session;
        protected UserOrganizationModel caller;


        protected PermissionsUtility(ISession session, UserOrganizationModel caller)
        {
            this.session = session;
            this.caller = caller;
        }

        public static PermissionsUtility Create(ISession session,UserOrganizationModel caller)
        {
            var attached=caller;
            if(!session.Contains(caller))
                attached=session.Get<UserOrganizationModel>(caller.Id);
            return new PermissionsUtility(session, attached);
        }


        public Boolean IsRadialAdmin()
        {
            if (caller.IsRadialAdmin)
                return true;
            return false;
        }


        public Boolean EditOrganization(long organizationId)
        {
            if (IsRadialAdmin())
                return true;

            if (caller.Organization.Id == organizationId && caller.IsManagerCanEditOrganization)
                return true;
            throw new PermissionsException();
        }

        public Boolean EditUserOrganization(long userId)
        {
            if (IsRadialAdmin())
                return true;

            if (caller.ManagingUsers.Any(x => x.Id == userId) && caller.IsManager) //IsManager may be too much
                return true;
            //Could do some cascading here if we want.

            throw new PermissionsException();
        }

        public Boolean EditGroup(long groupId)
        {
            if (IsRadialAdmin())
                return true;

            if (caller.ManagingGroups.Any(x => x.Id == groupId) && caller.IsManager) //IsManager may be too much
                return true;
            //Could do some cascading here if we want.

            throw new PermissionsException();
        }

        public Boolean EditApplication(long forId)
        {
            if (IsRadialAdmin())
                return true;
            throw new PermissionsException();
        }

        public Boolean EditIndustry(long forId)
        {
            if (IsRadialAdmin())
                return true;
            throw new PermissionsException();
        }

        public Boolean ViewQuestion(QuestionModel question)
        {
            if (IsRadialAdmin())
                return true;
            switch (question.OriginType)
            {
                case OriginType.User: if (!OwnedBelowOrEqual(x => x.CustomQuestions.Any(y => y.Id == question.Id))) throw new PermissionsException(); break;
                case OriginType.Group: if (!caller.ManagingGroups.Select(x => x.Id).Union(caller.Groups.Select(x => x.Id)).Any(x => question.Id == x)) throw new PermissionsException(); break;
                case OriginType.Organization: if (caller.Organization.Id != question.ForOrganization.Id) throw new PermissionsException(); break;
                case OriginType.Industry: break;
                case OriginType.Application: break;
                case OriginType.Invalid: throw new PermissionsException();
                default: throw new PermissionsException();
            }
            return true;
        }
        public Boolean ViewUserOrganization(long userOrganizationId)
        {
            if (IsRadialAdmin())
                return true;
            if (OwnedBelowOrEqual(x => x.Id == userOrganizationId))
                return true;
            throw new PermissionsException();
        }


        public Boolean ViewOrigin(OriginType originType,long originId)
        {
            switch (originType)
            {
                case OriginType.User: return ViewUserOrganization(originId);
                case OriginType.Group: return ViewGroup(originId);
                case OriginType.Organization: return ViewOrganization(originId);
                case OriginType.Industry: return ViewIndustry(originId);
                case OriginType.Application: return ViewApplication(originId);
                case OriginType.Invalid: throw new PermissionsException();
                default: throw new PermissionsException();
            }
        }
        public Boolean ViewGroup( long groupId)
        {
            if (IsRadialAdmin())
                return true;
            if (caller.Groups.Any(x=>x.Id==groupId))
                return true;
            if (OwnedBelowOrEqual(x => x.ManagingGroups.Any(y=>y.Id==groupId)))
                return true;
            throw new PermissionsException();
        }
        
        public Boolean ViewOrganization(long organizationId)
        {
            if (IsRadialAdmin())
                return true;
            if (caller.Organization.Id == organizationId)
                return true;
            throw new PermissionsException();
        }

        public Boolean ViewApplication(long applicationId)
        {
            log.Info("ViewApplication always returns true.");
            return true;
        }
        public Boolean ViewIndustry( long industryId)
        {
            log.Info("ViewIndustry always returns true.");
            return true;
        }

        public Boolean OwnedBelowOrEqual( Predicate<UserOrganizationModel> visiblility)
        {
            if (visiblility(caller))
                return true;
            foreach (var manager in caller.ManagedBy)
            {
                if (OwnedBelowOrEqual( visiblility))
                    return true;
            }
            return false;
        }


    }
}