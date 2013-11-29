using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Responsibilities
{
    public abstract class ResponsibilityGroupModel
    {
        public virtual long ResponsibilityGroupId { get; set; }
        public virtual OrganizationModel Organization { get; set; }
        public virtual IList<ResponsibilityModel> Responsibilities { get; set; }

        public ResponsibilityGroupModel()
        {
            Responsibilities = new List<ResponsibilityModel>();
        }
    }

    public class OrganizationPositionModel : ResponsibilityGroupModel
    {
        public virtual PositionModel Position { get; set; }
        public virtual String CustomName { get;set;}
    }

    public class OrganizationTeamModel : ResponsibilityGroupModel
    {
        public virtual String Name { get; set; }
        public virtual long CreatedBy { get; set; }
        public virtual Boolean OnlyManagersEdit { get; set; }
        public virtual Boolean Secret { get; set; }
        public virtual IList<TeamMemberModel> Members { get; set; }
        public OrganizationTeamModel():base()
        {
            Members = new List<TeamMemberModel>();
        }
    }
    public class TeamMemberModel : ILongIdentifiable, IDeletable
    {
        public virtual long Id { get; protected set; }
        public virtual UserOrganizationModel UserOrganization { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
    }

    public class TeamMemberModelMap : ClassMap<TeamMemberModel>
    {
        public TeamMemberModelMap()
        {
            Id(x => x.Id);
            References(x => x.UserOrganization).Not.LazyLoad();
            Map(x => x.DeleteTime);
        }
    }

    public class TeamModelMap : SubclassMap<OrganizationTeamModel>
    {
        public TeamModelMap()
        {
            Map(x => x.Name);
            Map(x => x.CreatedBy);
            Map(x => x.Secret);
            Map(x => x.OnlyManagersEdit);
            HasMany(x => x.Members).Not.LazyLoad().Cascade.SaveUpdate();
        }
    }

    public class OrganizationPositionModelMap : SubclassMap<OrganizationPositionModel>
    {
        public OrganizationPositionModelMap()
        {
            Map(x => x.CustomName);
            References(x => x.Position).Not.LazyLoad();
        }
    }

    public class AccountabilityGroupMap : ClassMap<ResponsibilityGroupModel>
    {
        public AccountabilityGroupMap()
        {
            Id(x => x.ResponsibilityGroupId);
            References(x => x.Organization);
            HasMany(x => x.Responsibilities)
                .Cascade.SaveUpdate()
                .Not.LazyLoad();

        }
    }
}