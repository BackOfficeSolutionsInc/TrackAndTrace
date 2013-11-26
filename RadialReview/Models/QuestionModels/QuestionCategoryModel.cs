using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System.ComponentModel;
using RadialReview.Properties;
using System.ComponentModel.DataAnnotations;
using RadialReview.Models.Enums;


namespace RadialReview.Models
{
    public class QuestionCategoryModel : IOrigin,IDeletable
    {
        public virtual long Id { get; set; }
        public virtual OriginType OriginType { get; set; }
        public virtual long OriginId { get; set; }

        [Display(Name = "category", ResourceType = typeof(DisplayNameStrings))]
        public virtual LocalizedStringModel Category { get; set; }

        [Display(Name = "active", ResourceType = typeof(DisplayNameStrings))]
        public virtual Boolean Active { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        
        public virtual OriginType GetOriginType()
        {
            return OriginType;
        }

        public virtual string GetSpecificNameForOrigin()
        {
            return Category.Translate();
        }

        public virtual List<IOrigin> OwnsOrigins()
        {
            return new List<IOrigin>();
        }

        public virtual List<IOrigin> OwnedByOrigins()
        {
            return new List<IOrigin>();
        }
    }

    public class QuestionCategoryModelMap : ClassMap<QuestionCategoryModel>
    {
        public QuestionCategoryModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Active);
            Map(x => x.OriginId);
            Map(x => x.OriginType);
            Map(x => x.DeleteTime);

            References(x => x.Category)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();

            /*
            References(x => x.Organization)
                .Column("OrganizationId")
                .Cascade.SaveUpdate();*/
        }
    }
}