using NHibernate;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Responsibilities;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace RadialReview
{
    public static partial class ResponsibilityGroupExtensions
    {
        public static ResponsibilityGroupHydration<T> HydrateResponsibilityGroup<T>(this T responsiblityGroup) where T : ResponsibilityGroupModel
        {
            return ResponsibilityGroupHydration<T>.Hydrate(responsiblityGroup);
        }
    }
}

namespace RadialReview
{
    public class ResponsibilityGroupHydration<T> where T : ResponsibilityGroupModel
    {
        private static ResponsibilitiesAccessor _ResponsibilityAccessor { get; set; }
        private T Responsibility { get; set; }
        private ResponsibilityGroupModel _UnderlyingResponsibility { get; set; }
        private ISession Session { get; set; }


        private ResponsibilityGroupHydration()
        {
        }

        public static ResponsibilityGroupHydration<U> Hydrate<U>(U responsibility) where U: ResponsibilityGroupModel
        {
            return new ResponsibilityGroupHydration<U>()
            {
                Responsibility = responsibility,
                Session = HibernateSession.GetCurrentSession()
            };
        }

        private ResponsibilityGroupModel GetUnderlying()
        {
            if (_UnderlyingResponsibility == null)
            {
                using (var tx = Session.BeginTransaction())
                {
                    _UnderlyingResponsibility = Session.Get<ResponsibilityGroupModel>(Responsibility.Id);
                }
            }
            return _UnderlyingResponsibility;
        }

        public T Execute()
        {
            Session.Dispose();
            return Responsibility;
        }
        public ResponsibilityGroupHydration<T> PersonallyManaging(UserOrganizationModel self)
        {
            using (var tx = Session.BeginTransaction())
            {
                var rg = GetUnderlying();
                var rgId = rg.Id;
                bool owned = false;
                //Blah blah blah this is bad.. 
                try
                {
                    var perms=PermissionsUtility.Create(Session, self);
                    if (rg is UserOrganizationModel)
                        perms.ManagesUserOrganization(rgId,false);
                    else if (rg is OrganizationTeamModel)
                        perms.ManagingTeam(rgId);
                    else if (rg is OrganizationPositionModel)
                        perms.ManagingPosition(rgId);
                    else
                        throw new NotImplementedException("Unknown Responsibility Group");
                    owned = true;
                }
                catch (PermissionsException)
                {
                    owned = false;
                }
                Responsibility.SetEditable(owned);
            }
            return this;
        }
    }
}