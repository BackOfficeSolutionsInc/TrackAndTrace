using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.UserModels
{
    public class ManagerDuration : IHistorical,ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual long ManagerId { get; set; }
        public virtual UserOrganizationModel Manager {get;set;}
        public virtual long SubordinateId { get; set; }
        public virtual UserOrganizationModel Subordinate {get;set;}
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual long? DeletedBy { get; set; }
        public virtual long PromotedBy { get; set; }
				
		public ManagerDuration()
        {

        }

        public ManagerDuration(long managerId,long subordinateId, long promotedById)
        {
            ManagerId = managerId;
            SubordinateId = subordinateId;
            CreateTime = DateTime.UtcNow;
            PromotedBy = promotedById;
        }
    }

    public class ManagerDurationMap : ClassMap<ManagerDuration>
    {
        public ManagerDurationMap()
        {
            Id(x => x.Id);
            Map(x => x.CreateTime).Column("Start");
            Map(x => x.DeleteTime);
            Map(x => x.DeletedBy);
            Map(x => x.PromotedBy);

            Map(x => x.ManagerId).Column("ManagerId");
            References(x => x.Manager).Column("ManagerId")
                .Not.LazyLoad()
                .ReadOnly();

            Map(x => x.SubordinateId).Column("SubordinateId");
            References(x => x.Subordinate).Column("SubordinateId")
                .Not.LazyLoad()
                .ReadOnly();
        }
    }
}