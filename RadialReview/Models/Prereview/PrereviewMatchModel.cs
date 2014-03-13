using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Prereview
{
    public class PrereviewMatchModel : IDeletable
    {
        public virtual long Id { get; set; }
        public virtual long PrereviewId { get; set; }
        public virtual long FirstUserId { get; set; }
        public virtual long SecondUserId { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
    }

    public class PrereviewMatchModelMap : ClassMap<PrereviewMatchModel>
    {
        public PrereviewMatchModelMap()
        {
            Id(x => x.Id);
            Map(x => x.PrereviewId);
            Map(x => x.FirstUserId);
            Map(x => x.SecondUserId);
            Map(x => x.DeleteTime);
        }
    }
}