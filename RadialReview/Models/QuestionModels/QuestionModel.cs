
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

        public virtual IOrigin Origin
        {
            get
            {
                if (ForApplication != null) return ForApplication;
                else if (ForIndustry != null) return ForIndustry;
                else if (ForOrganization != null) return ForOrganization;
                else if (ForGroup != null) return ForGroup;
                else if (ForUser != null) return ForUser;
                return null;
            }
        }

        protected virtual OriginType? _OriginType { get; set; }

        public virtual OriginType OriginType
        {
            get
            {
                if (_OriginType.HasValue)
                    return _OriginType.Value;
                if (ForApplication != null) return OriginType.Application;
                else if (ForIndustry != null) return OriginType.Industry;
                else if (ForOrganization != null) return OriginType.Organization;
                else if (ForGroup != null) return OriginType.Group;
                else if (ForUser != null) return OriginType.User;
                else return OriginType.Invalid;
            }
            set
            {
                _OriginType = value;
            }

        }


        public QuestionModel()
        {
            DateCreated = DateTime.UtcNow;
            KeyValues = new List<QuestionKeyValues>();
            DisabledFor = new List<LongModel>();
        }

        public virtual long CategoryId
        {
            get { return Category.NotNull(x => x.Id); }
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