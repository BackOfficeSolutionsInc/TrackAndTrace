using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class UrlHitModel : ILongIdentifiable
    {
        public virtual long Id { get; protected set; }
        public virtual String IP {get;set;}
        public virtual DateTime Time {get;set;}

    }

    public class UrlModel : ILongIdentifiable
    {
        public virtual long Id { get; protected set; }
        public virtual String Url { get; set; }
        public virtual String Email { get; set; }
        public virtual String Map { get; set; }
        public virtual IList<UrlHitModel> Hits { get; set; }

        public UrlModel()
        {
            Hits = new List<UrlHitModel>();
        }
    }

    public class UrlModelMap : ClassMap<UrlModel>
    {
        public UrlModelMap()
        {
            Id(x => x.Id).GeneratedBy.Increment();
            Map(x => x.Url);
            Map(x => x.Email);
            Map(x => x.Map);
            HasMany(x => x.Hits).Cascade.SaveUpdate();
        }
    }
    public class UrlHitModelMap : ClassMap<UrlHitModel>
    {
        public UrlHitModelMap()
        {
            Id(x => x.Id).GeneratedBy.Increment();
            Map(x => x.IP);
            Map(x => x.Time);
        }
    }

}