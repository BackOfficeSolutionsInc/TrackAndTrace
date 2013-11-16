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

        public PermissionsUtility RadialAdmin()
        {
            if (IsRadialAdmin())
                return this;
            throw new PermissionsException();
        }


        public PermissionsUtility EditOrganization(long organizationId)
        {
            if (IsRadialAdmin())
                return this;

            if (caller.Organization.Id == organizationId && caller.IsManagerCanEditOrganization())
                return this;
            throw new PermissionsException();
        }

        public PermissionsUtility EditUserOrganization(long userId)
        {
            if (IsRadialAdmin())
                return this;

            if (caller.IsManager() && IsOwnedBelowOrEqual(caller,x=>x.Id==userId))
                return this;
            //caller.AllSubordinates.Any(x => x.Id == userId) && caller.IsManager()) //IsManager may be too much
            //return this;
            //Could do some cascading here if we want.

            throw new PermissionsException();
        }

        public PermissionsUtility EditGroup(long groupId)
        {
            if (IsRadialAdmin())
                return this;

            if (caller.AllSubordinates.Any(x => x.Id == groupId) && caller.IsManager()) //IsManager may be too much
                return this;
            //Could do some cascading here if we want.

            throw new PermissionsException();
        }

        public PermissionsUtility EditApplication(long forId)
        {
            if (IsRadialAdmin())
                return this;
            throw new PermissionsException();
        }

        public PermissionsUtility EditIndustry(long forId)
        {
            if (IsRadialAdmin())
                return this;
            throw new PermissionsException();
        }

        public PermissionsUtility ViewQuestion(QuestionModel question)
        {
            if (IsRadialAdmin())
                return this;
            switch (question.OriginType)
            {
                case OriginType.User: if (!IsOwnedBelowOrEqual(caller,x => x.CustomQuestions.Any(y => y.Id == question.Id))) throw new PermissionsException(); break;
                case OriginType.Group: if (!caller.ManagingGroups.Select(x => x.Id).Union(caller.Groups.Select(x => x.Id)).Any(x => question.Id == x)) throw new PermissionsException(); break;
                case OriginType.Organization: if (caller.Organization.Id != question.ForOrganization.Id) throw new PermissionsException(); break;
                case OriginType.Industry: break;
                case OriginType.Application: break;
                case OriginType.Invalid: throw new PermissionsException();
                default: throw new PermissionsException();
            }
            return this;
        }
        public PermissionsUtility ViewUserOrganization(long userOrganizationId)
        {
            if (IsRadialAdmin())
                return this;
            if (IsOwnedBelowOrEqual(caller, x => x.Id == userOrganizationId))
                return this;
            throw new PermissionsException();
        }


        public PermissionsUtility ViewOrigin(OriginType originType, long originId)
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
        public PermissionsUtility ViewGroup(long groupId)
        {
            if (IsRadialAdmin())
                return this;
            if (caller.Groups.Any(x=>x.Id==groupId))
                return this;
            if (IsOwnedBelowOrEqual(caller, x => x.ManagingGroups.Any(y => y.Id == groupId)))
                return this;
            throw new PermissionsException();
        }

        public PermissionsUtility ViewOrganization(long organizationId)
        {
            if (IsRadialAdmin())
                return this;
            if (caller.Organization.Id == organizationId)
                return this;
            throw new PermissionsException();
        }

        public PermissionsUtility ViewApplication(long applicationId)
        {
            log.Info("ViewApplication always returns true.");
            return this;
        }
        public PermissionsUtility ViewIndustry(long industryId)
        {
            log.Info("ViewIndustry always returns true.");
            return this;
        }
        public PermissionsUtility ViewImage(string imageId)
        {
            if (imageId == null)
                throw new PermissionsException();
            Predicate<UserOrganizationModel> p = x => x.User.NotNull(y => y.Image.NotNull(z => z.Id.ToString() == imageId));

            if (IsOwnedBelowOrEqual(caller, p) || IsOwnedAboveOrEqual(caller, p))
            {
                return this;
            }
            throw new PermissionsException();
        }

        public PermissionsUtility OwnedBelowOrEqual(Predicate<UserOrganizationModel> visiblility)
        {
            if(IsOwnedBelowOrEqual(caller,visiblility))
                return this;
            throw new PermissionsException();
        }
        protected Boolean IsRadialAdmin()
        {
            if (caller.IsRadialAdmin)
                return true;
            return false;
        }

        protected bool IsOwnedBelowOrEqual(UserOrganizationModel caller,Predicate<UserOrganizationModel> visibility)
        {
            if (visibility(caller))
                return true;
            foreach (var manager in caller.ManagingUsers)
            {
                if (IsOwnedBelowOrEqual(manager, visibility))
                    return true;
            }
            return false;
        }

        protected bool IsOwnedAboveOrEqual(UserOrganizationModel caller, Predicate<UserOrganizationModel> visibility)
        {
            if (visibility(caller))
                return true;
            foreach (var subordinate in caller.ManagedBy)
            {
                if (IsOwnedAboveOrEqual(subordinate,visibility))
                    return true;
            }
            return false;
        }



    }
}