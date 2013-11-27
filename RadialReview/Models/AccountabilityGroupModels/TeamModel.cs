﻿using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.AccountabilityGroupModels
{
    public class TeamModel : AccountabilityGroupModel
    {
        public virtual String Name { get;set;}

        public virtual long CreatedBy { get; set; }

        public virtual Boolean OnlyManagersEdit { get; set; }

        public virtual Boolean Secret { get; set; }

        public virtual IList<TeamMemberModel> Members { get; set; }

        public TeamModel()
        {
            Members = new List<TeamMemberModel>();
        }
    }

    public class TeamMemberModel : ILongIdentifiable, IDeletable
    {
        public virtual long Id { get; protected set; }
        public virtual UserOrganizationModel UserOrganization { get; set; }
        public DateTime? DeleteTime { get; set; }
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

    public class TeamModelMap : SubclassMap<TeamModel>
    {
        public TeamModelMap()
        {
            Map(x => x.Name);
            Map(x => x.CreatedBy);
            Map(x => x.Secret);
            Map(x => x.OnlyManagersEdit);
            HasMany(x => x.Members).Not.LazyLoad();
        }
    }
}