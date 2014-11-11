using System;
using System.Collections.Generic;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Properties;

namespace RadialReview.Models.Askables
{
    public abstract class ResponsibilityGroupModel : ILongIdentifiable, IDeletable
    {
        public virtual long Id { get; set; }
        public abstract String GetName();
        public abstract String GetGroupType();
        public virtual OrganizationModel Organization { get; set; }
        public virtual IList<ResponsibilityModel> Responsibilities { get; set; }
        protected virtual Boolean? _Editable { get; set; }

        public virtual Boolean GetEditable()
        {
            return _Editable.Value;
        }
        public virtual void SetEditable(bool editable)
        {
            _Editable = editable;
        }

        public ResponsibilityGroupModel()
        {
            Responsibilities = new List<ResponsibilityModel>();
        }

		#region IDeletable Members

		public virtual DateTime? DeleteTime { get;set; }

		#endregion
	}

    public class OrganizationPositionModel : ResponsibilityGroupModel
    {
        public virtual PositionModel Position { get; set; }
        public virtual String CustomName { get; set; }
        public virtual long CreatedBy { get; set; }

        public override string GetName()
        {
            return CustomName;
        }
        public override string GetGroupType()
        {
            return DisplayNameStrings.position;
        }
    }

    public class OrganizationTeamModel : ResponsibilityGroupModel
    {
        public virtual TeamType Type { get; set; }
        public virtual String Name { get; set; }
        public virtual long CreatedBy { get; set; }
        public virtual long ManagedBy { get; set; }
        public virtual Boolean OnlyManagersEdit { get; set; }
        public virtual Boolean InterReview { get; set; }
        public virtual Boolean Secret { get; set; }
       // public virtual IList<TeamMemberModel> Members { get; set; }
        public OrganizationTeamModel():base()
        {
           // Members = new List<TeamMemberModel>();
            OnlyManagersEdit = true;
            InterReview = true;
        }
        public override string GetName()
        {
            return Name;
        }
        public override string GetGroupType()
        {
            return DisplayNameStrings.team;
        }

        public static OrganizationTeamModel SubordinateTeam(UserOrganizationModel creator,UserOrganizationModel manager)
        {
            return new OrganizationTeamModel() {
                CreatedBy = creator.Id,
                InterReview = false,
                ManagedBy = manager.Id,
                Name = manager.GetNameAndTitle() + " Subordinates",
                OnlyManagersEdit=true,
                Organization = manager.Organization,
                Responsibilities = new List<ResponsibilityModel>(),
                Secret = true,
                Type = TeamType.Subordinates,
                _Editable= false,
            };
        }
    }
    /*
    public class TeamMemberModel : ILongIdentifiable, IDeletable
    {
        public virtual long Id { get; protected set; }
        public virtual UserOrganizationModel UserOrganization { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
    }*/
    /*
    public class TeamMemberModelMap : ClassMap<TeamMemberModel>
    {
        public TeamMemberModelMap()
        {
            Id(x => x.Id);
            References(x => x.UserOrganization).Not.LazyLoad();
            Map(x => x.DeleteTime);
        }
    }
    */
    public class TeamModelMap : SubclassMap<OrganizationTeamModel>
    {
        public TeamModelMap()
        {
            Map(x => x.Name);
            Map(x => x.CreatedBy);
            Map(x => x.Secret);
            Map(x => x.Type);
            Map(x => x.ManagedBy);
            Map(x => x.InterReview);
            Map(x => x.OnlyManagersEdit);
            //HasMany(x => x.Members).Not.LazyLoad().Cascade.SaveUpdate();
        }
    }

    public class OrganizationPositionModelMap : SubclassMap<OrganizationPositionModel>
    {
        public OrganizationPositionModelMap()
        {
            Map(x => x.CustomName);
            Map(x => x.CreatedBy);
            References(x => x.Position).Not.LazyLoad();
        }
    }

    public class ResponsibilityGroupModelMap : ClassMap<ResponsibilityGroupModel>
    {
        public ResponsibilityGroupModelMap()
        {
			Id(x => x.Id);
			Map(x => x.DeleteTime);
			References(x => x.Organization).Not.LazyLoad();
            HasMany(x => x.Responsibilities)
                .Cascade.SaveUpdate()
                .Not.LazyLoad();

        }
    }
}