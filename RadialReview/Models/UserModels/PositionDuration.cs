using FluentNHibernate.Mapping;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.UserModels
{
    public class PositionDurationModel:IDeletable
    {
        public virtual long Id { get; set; }
        public virtual long UserId { get; set; }
        public virtual OrganizationPositionModel Position { get; set; }
        public virtual DateTime Start { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual long? DeletedBy { get; set; }
        public virtual long PromotedBy { get; set; }

        public PositionDurationModel()
        {

        }

        public PositionDurationModel(OrganizationPositionModel position, long promotedBy, long forUserId)
        {
            Position = position;
            PromotedBy = promotedBy;
            Start = DateTime.UtcNow;
            UserId = forUserId;
        }
    }

    public class PositionDurationMap : ClassMap<PositionDurationModel>
    {
        public PositionDurationMap()
        {
            Id(x => x.Id);
            Map(x => x.Start);
            Map(x => x.DeletedBy);
            Map(x => x.DeleteTime);
            Map(x => x.PromotedBy);
            Map(x => x.UserId);
            References(x => x.Position).Not.LazyLoad();
        }
    }
}