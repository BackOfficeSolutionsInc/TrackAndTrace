using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Responsibilities
{
    public class ResponsibilityModel : Askable, ILongIdentifiable, IDeletable
    {
        public virtual long ForOrganizationId { get;set; }
        public virtual long ForResponsibilityGroup { get; set; }
        public virtual String Responsibility { get; set; }
        public virtual QuestionCategoryModel Category {get;set;}
        public virtual DateTime? DeleteTime { get; set; }

        public ResponsibilityModel()
        {
            Category = new QuestionCategoryModel();
        }

        public override QuestionType GetQuestionType()
        {
            return QuestionType.Slider;
        }

        public override string GetQuestion()
        {
            return Responsibility;
        }
    }

    public class ResponsibilityModelMap : SubclassMap<ResponsibilityModel>
    {
        public ResponsibilityModelMap()
        {
            Map(x => x.DeleteTime);
            Map(x => x.Responsibility);
            Map(x => x.ForOrganizationId);
            Map(x => x.ForResponsibilityGroup);
            References(x => x.Category).Not.LazyLoad();
        }
    }
}