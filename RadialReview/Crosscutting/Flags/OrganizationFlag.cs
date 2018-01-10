using FluentNHibernate.Mapping;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Crosscutting.Flags {
    public enum OrganizationFlagType {
        Standard = 0,
        ExtendedTrial = 99,
        Close =100,
    }

    public class OrganizationFlag: IHistorical,ILongIdentifiable {
        public virtual long Id { get; set; }
        public virtual long OrganizationId { get; set; }
        public virtual OrganizationModel Organization { get; set; }
        public virtual OrganizationFlagType FlagType { get; set; }

        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        public OrganizationFlag() {
            CreateTime = DateTime.UtcNow;
        }

        public class Map : ClassMap<OrganizationFlag> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.OrganizationId).Column("OrganizationId");
                References(x => x.Organization).Column("OrganizationId").ReadOnly().LazyLoad();
                Map(x => x.FlagType).CustomType<OrganizationFlagType>();
                
            }

        }
    }
}