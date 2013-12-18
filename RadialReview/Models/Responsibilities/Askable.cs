using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Responsibilities
{
    public abstract class Askable : ILongIdentifiable, IDeletable
    {
        public virtual long Id { get; set; }
        public virtual QuestionCategoryModel Category { get; set; }
        public abstract QuestionType GetQuestionType();
        public abstract String GetQuestion();
        public virtual DateTime? DeleteTime { get; set; }
        public virtual WeightType Weight { get; set; }
        public virtual bool Required { get;set; }

        public Askable()
        {
            Required = true;
            DeleteTime = null;
            Category = new QuestionCategoryModel();
            Weight = WeightType.Normal;
        }
    }



    public class AskableMap : ClassMap<Askable>
    {
        public AskableMap()
        {
            Id(x => x.Id);
            Map(x => x.Weight).CustomType(typeof(WeightType));
            Map(x => x.DeleteTime);
            Map(x => x.Required);
            References(x => x.Category).Not.LazyLoad();
        }
    }
}