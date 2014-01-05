using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Responsibilities
{
    public class ResponsibilityModel : Askable, ILongIdentifiable
    {
        public virtual long ForOrganizationId { get;set; }
        public virtual long ForResponsibilityGroup { get; set; }
        public virtual String Responsibility { get; set; }
        
        public ResponsibilityModel() :base()
        {
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
            Map(x => x.Responsibility).Length(65000);
            Map(x => x.ForOrganizationId);
            Map(x => x.ForResponsibilityGroup);
            References(x => x.Category).Not.LazyLoad();
        }
    }
}