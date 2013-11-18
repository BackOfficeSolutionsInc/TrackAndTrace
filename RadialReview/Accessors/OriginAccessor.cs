using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class OriginAccessor
    {
        /*[Obsolete("Want this to stand out.",false)]*/
        public IOrigin GetOrigin(UserOrganizationModel caller,OriginType originType,long originId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrigin(originType, originId);
                    switch (originType)
                    {
                        case OriginType.User:           return s.Get<UserOrganizationModel>(originId);
                        case OriginType.Group:          return s.Get<GroupModel>(originId);
                        case OriginType.Organization:   return s.Get<OrganizationModel>(originId);
                        case OriginType.Industry:       return s.Get<IndustryModel>(originId);
                        case OriginType.Application:    return s.Get<ApplicationWideModel>(originId);
                        case OriginType.Invalid:        throw new PermissionsException();
                        default:                        throw new PermissionsException();
                    }
                }
            }
        }
        /*
        protected static UserAccessor _UserAccessor = new UserAccessor();
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static GroupAccessor _GroupAccessor = new GroupAccessor();


        public IOrigin GetOriginQuestion(UserOrganizationModel caller,OriginType origin, long originId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    switch (origin)
                    {
                        case OriginType.User:           return _UserAccessor.GetUserOrganization(caller,originId);
                        case OriginType.Group:          return _GroupAccessor.Get(caller,originId);
                        case OriginType.Organization:   return _OrganizationAccessor
                        case OriginType.Industry:       return s.Get<IndustryModel>(originId);
                        case OriginType.Application:    return s.Get<ApplicationWideModel>(originId);
                        case OriginType.Invalid:        throw new PermissionsException();
                        default:                        throw new PermissionsException();
                    }


                }
            }
        }*/
    }
}