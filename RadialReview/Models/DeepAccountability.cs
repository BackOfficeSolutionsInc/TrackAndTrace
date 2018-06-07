using FluentNHibernate.Mapping;
using RadialReview.Models.Accountability;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class DeepAccountability : IHistorical
    {
        public virtual long Id { get; set; }
        //public virtual UserOrganizationModel Manager { get; set; }
        //public virtual UserOrganizationModel Subordinate { get; set; }
        public virtual long ParentId { get; set; }
		public virtual long ChildId { get; set; }

		public virtual AccountabilityNode Child { get; set; }
		public virtual AccountabilityNode Parent { get; set; }


		//public virtual bool SubordinateIsNode { get; set; }
		//public virtual bool ManagerIsNode { get; set; }
		public virtual int Links { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime {get;set;}

        public virtual long OrganizationId { get; set; }

        public DeepAccountability()
        {
            CreateTime = DateTime.UtcNow;
        }
    }

    public class DeepAccountabilityMap : ClassMap<DeepAccountability>
    {
        public DeepAccountabilityMap()
        {
            Id(x => x.Id);
            Map(x => x.CreateTime);
            Map(x => x.DeleteTime);
            Map(x => x.OrganizationId);
			Map(x => x.Links);

			//Map(x => x.ManagerIsNode);
			//Map(x => x.SubordinateIsNode);

			Map(x => x.ParentId).Column("ParentId").Index("idx__DeepAccountability_ChildId");
			References(x => x.Parent).Column("ParentId").LazyLoad().ReadOnly();

            Map(x => x.ChildId).Column("ChildId").Index("idx__DeepAccountability_ChildId");
			References(x => x.Child).Column("ChildId").LazyLoad().ReadOnly();

		}
    }
}