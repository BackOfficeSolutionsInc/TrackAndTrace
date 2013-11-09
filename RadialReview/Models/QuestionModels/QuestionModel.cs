
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class QuestionModel : IDeletable
    {
        public virtual long Id { get; set; }
        public virtual DateTime DateCreated { get; set; }
        public virtual String Question { get; set; }
        public virtual UserOrganizationModel CreatedBy { get; set; }
        public virtual QuestionCategoryModel Category { get; set; }
        public virtual IList<QuestionKeyValues> KeyValues { get; set; }
        public virtual IList<LongModel> DisabledFor { get; set; }
        public virtual ApplicationWideModel ForApplication { get; set; }
        public virtual OrganizationModel ForOrganization { get; set; }
        public virtual UserOrganizationModel ForUser { get; set; }
        public virtual IndustryModel ForIndustry { get; set; }
        public virtual GroupModel ForGroup { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        public virtual OriginType Origin { get {
            if      (ForApplication != null)    return OriginType.Application;
            else if (ForOrganization != null)   return OriginType.Organization;
            else if (ForUser != null)           return OriginType.User;
            else if (ForGroup != null)          return OriginType.Group;
            else if (ForIndustry != null)       return OriginType.Industry;
            else                                return OriginType.Invalid;
        } }


        public QuestionModel()
        {
            DateCreated = DateTime.UtcNow;
            KeyValues = new List<QuestionKeyValues>();
            DisabledFor = new List<LongModel>();
        }
    }

    public class QuestionModelMap : ClassMap<QuestionModel>
    {
        public QuestionModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Question);
            Map(x => x.DateCreated);
            Map(x => x.DeleteTime);

            References(x => x.ForOrganization)
                .Column("OrganizationQuestion_Id")
                .Cascade.SaveUpdate();
            References(x => x.ForApplication)
                .Column("ApplicationQuestion_Id")
                .Cascade.SaveUpdate();
            References(x => x.ForGroup)
                .Column("GroupQuestion_Id")
                .Cascade.SaveUpdate();
            References(x => x.ForUser)
                .Column("UserQuestion_Id")
                .Cascade.SaveUpdate();
            References(x => x.ForIndustry)
                .Column("IndustryQuestion_Id")
                .Cascade.SaveUpdate();
            
            References(x => x.CreatedBy)
                .Cascade.SaveUpdate()
                .Column("CreatedQuestionsId");
            References(x => x.Category)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();


            HasMany(x => x.DisabledFor)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
            HasMany(x => x.KeyValues)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();

        }
    }
}