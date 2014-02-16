using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Reviews
{
    public class ReviewWhoSettingsModel : ILongIdentifiable, IDeletable
    {
        public virtual long Id { get; set; }
        public virtual long ForUserId { get; set; }
        public virtual long ByUserId { get; set; }
        public virtual long SetByUserId { get; set; }
        public virtual Boolean ForceState { get; set; }
        public virtual DateTime Created { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
    }

    public class ReviewWhoSettingsModelMap : ClassMap<ReviewWhoSettingsModel>
    {
        public ReviewWhoSettingsModelMap()
        {
            Id(x => x.Id);
            Map(x => x.ForUserId);
            Map(x => x.ByUserId);
            Map(x => x.SetByUserId);
            Map(x => x.ForceState);
            Map(x => x.Created);
            Map(x => x.DeleteTime);
        }
    }
}