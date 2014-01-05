using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Application
{
    public class KeyValueModel : ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual String K { get; set; }
        public virtual String V {get;set; }
        public virtual DateTime Created {get;set;}

        public KeyValueModel()
        {
            Created = DateTime.UtcNow;
        }
    }

    public class KeyValueModelMap: ClassMap<KeyValueModel>
    {
        public KeyValueModelMap()
        {
            Id(x => x.Id);
            Map(x => x.K);
            Map(x => x.V);
            Map(x => x.Created);
        }
    }
}