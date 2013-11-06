using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System.ComponentModel;
using RadialReview.Properties;
using System.ComponentModel.DataAnnotations;


namespace RadialReview.Models
{
    public class QuestionCategoryModel : IDeletable
    {
        public virtual long Id { get; set; }
        public virtual OrganizationModel Organization { get; set; }

        [Display(Name = "category", ResourceType = typeof(DisplayNameStrings))]
        public virtual String Category { get; set; }

        [Display(Name = "active", ResourceType = typeof(DisplayNameStrings))]
        public virtual Boolean Active { get; set; }
        public virtual DateTime? DeleteTime { get; set; }



    }

    public class QuestionCategoryModelMap : ClassMap<QuestionCategoryModel>
    {
        public QuestionCategoryModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Category);
            Map(x => x.DeleteTime);
            Map(x => x.Active);
            References(x => x.Organization)
                .Column("OrganizationId")
                .Cascade.SaveUpdate();
        }
    }
}