using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;

namespace RadialReview.Models.Askables
{
    public abstract class Askable : ILongIdentifiable, IHistorical
    {
        public virtual long Id { get; set; }
        public virtual QuestionCategoryModel Category { get; set; }
        public abstract QuestionType GetQuestionType();
		public abstract String GetQuestion();
		public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual WeightType Weight { get; set; }
        public virtual bool Required { get;set; }

		public virtual long? SectionId { get; set; }

		public virtual AboutType OnlyAsk { get; set; }

	    protected Askable()
        {
            Required = true;
            DeleteTime = null;
            Category = new QuestionCategoryModel();
            Weight = WeightType.Normal;
	        OnlyAsk = (AboutType)long.MaxValue;
		    CreateTime = DateTime.UtcNow;
        }
    }



    public class AskableMap : ClassMap<Askable>
    {
        public AskableMap()
        {
            Id(x => x.Id);
			Map(x => x.Required);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
			Map(x => x.SectionId);
			References(x => x.Category).Not.LazyLoad();
            Map(x => x.Weight).CustomType(typeof(WeightType));
			Map(x => x.OnlyAsk).CustomType(typeof(AboutType));
        }
    }
}