using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{

    #region AnswerModel
    public abstract class AnswerModel : ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual long ForReviewId { get; set; }
        public virtual QuestionModel Question { get; set; }
        public virtual bool Required { get; set; }
        public virtual bool Complete { get; set; }

    }
    #endregion

    #region Impls
    public class FeedbackAnswer : AnswerModel
    {
        public virtual String Feedback { get; set; }
    }

    public class RelativeComparisonAnswer : AnswerModel
    {
        public virtual UserOrganizationModel First { get;set;}
        public virtual UserOrganizationModel Second { get; set; }
        public virtual RelativeComparisonType Choice { get;set; }
    }

    public class SliderAnswer : AnswerModel
    {
        public virtual decimal? Percentage { get; set; }
    }
    public class ThumbsAnswer : AnswerModel
    {
        public virtual ThumbsType Thumbs { get; set; }
    }
    #endregion

    #region ClassMaps
    public class AnswerModelMap : ClassMap<AnswerModel>
    {
        public AnswerModelMap()
        {
            Id(x => x.Id);
            References(x => x.Question)
                .Not.LazyLoad();
            //.Cascade.SaveUpdate();
            Map(x => x.Required);
            Map(x => x.ForReviewId);
            Map(x => x.Complete);
        }
    }
    public class FeedbackAnswerMap : SubclassMap<FeedbackAnswer>
    {
        public FeedbackAnswerMap()
        {
            Map(x => x.Feedback);
        }
    }
    public class ThumbAnswerMap : SubclassMap<ThumbsAnswer>
    {
        public ThumbAnswerMap()
        {
            Map(x => x.Thumbs);
        }
    }
    public class SliderAnswerMap : SubclassMap<SliderAnswer>
    {
        public SliderAnswerMap()
        {
            Map(x => x.Percentage);
        }
    }
    public class RelativeComparisonAnswerMap : SubclassMap<RelativeComparisonAnswer>
    {
        public RelativeComparisonAnswerMap()
        {
            Map(x => x.Choice);
            References(x => x.First).Not.LazyLoad();
            References(x => x.Second).Not.LazyLoad();
        }
    }
    #endregion

}