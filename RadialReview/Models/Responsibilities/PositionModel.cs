using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Responsibilities
{
    public class PositionModel :ILongIdentifiable
    {
        public virtual long Id { get;protected set; }
        public virtual LocalizedStringModel Name { get; set; }
    }

    public class PositionModelMap : ClassMap<PositionModel>
    {
        public PositionModelMap()
        {
            Id(x => x.Id);
            References(x => x.Name)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
        }
    }
  
}