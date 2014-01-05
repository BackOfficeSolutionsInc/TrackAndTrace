using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Rockstars
{
    public class RockstarModel : ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual long ForOrganizationId { get; set; }
        public virtual long AboutUserId { get; set; }
        public virtual long ByUserId { get; set; }
        public virtual String Feedback { get; set; }
        public virtual bool Anonymous { get; set; }
        public virtual DateTime DateCreated { get; set; }
        public virtual UserOrganizationModel AboutUser { get; set; }
        public virtual UserOrganizationModel ByUser {get;set;}

    }

    public class RockstarModelMap : ClassMap<RockstarModel>
    {
        public RockstarModelMap()
        {
            Id(x => x.Id);
            Map(x => x.ForOrganizationId);
            Map(x => x.AboutUserId).Column("AboutUser_Id");
            Map(x => x.ByUserId).Column("ByUser_Id");
            Map(x => x.Feedback);
            Map(x => x.Anonymous);
            Map(x => x.DateCreated);

            References(x => x.AboutUser)
                .Column("AboutUser_Id")
                .Not.LazyLoad()
                .ReadOnly();
            References(x => x.ByUser)
                .Column("ByUser_Id")
                .Not.LazyLoad()
                .ReadOnly();

        }
    }
}