using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class LongModel : IDeletable
    {
        public virtual long Id { get; set; }
        public virtual long Long { get;set; }
        public virtual DateTime? DeleteTime { get; set; }
    }
    public class LongModelMap : ClassMap<LongModel>
    {
        public LongModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Long);
            Map(x => x.DeleteTime);
        }
    }
}