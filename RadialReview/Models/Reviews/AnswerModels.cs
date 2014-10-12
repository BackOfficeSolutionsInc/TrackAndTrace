using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{

    #region AnswerModel
    public abstract class AnswerModel : ILongIdentifiable,ICompletable,IDeletable
    {
        public virtual long Id { get; set; }
        public virtual long ForReviewId { get; set; }
        public virtual ReviewModel ForReview { get; set; }
        public virtual long ForReviewContainerId { get; set; }
        public virtual ReviewsModel ForReviewContainer { get; set; }
        public virtual Askable Askable { get; set; }
        public virtual bool Required { get; set; }
        public virtual bool Complete { get; set; }
        public virtual long AboutUserId { get; set; }
        public virtual UserOrganizationModel AboutUser { get; set; }
        public virtual long ByUserId { get; set; }
        public virtual UserOrganizationModel ByUser { get; set; }
        public virtual AboutType AboutType { get; set; }

        public virtual ICompletionModel GetCompletion(bool split = false)
        {
            if (Required)
                return new CompletionModel(Complete.ToInt(), 1);
            return new CompletionModel(0, 0, Complete.ToInt(), 1);
        }

        public virtual DateTime? DeleteTime { get; set; }
        public virtual DateTime? CompleteTime { get; set; }
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

    public class GetWantCapacityAnswer : AnswerModel
    {
        public virtual Tristate GetIt { get; set; }
        public virtual Tristate WantIt { get; set; }
        public virtual Tristate HasCapacity { get; set; }
    }

    #endregion

    #region ClassMaps
    public class AnswerModelMap : ClassMap<AnswerModel>
    {
        public AnswerModelMap()
        {
            Id(x => x.Id);
            References(x => x.Askable).Not.LazyLoad();
            //.Cascade.SaveUpdate();
            Map(x => x.Required);
            Map(x => x.Complete);
            Map(x => x.DeleteTime);
            Map(x => x.CompleteTime);

            Map(x=> x.AboutType).CustomType(typeof(Int64));

            Map(x => x.AboutUserId).Column("AboutUserId");
            References(x => x.AboutUser).Column("AboutUserId").Not.LazyLoad().ReadOnly();

            Map(x => x.ByUserId).Column("ByUserId");
            References(x => x.ByUser).Column("ByUserId").Not.LazyLoad().ReadOnly();

            Map(x => x.ForReviewContainerId).Column("ForReviewContainerId");
            References(x => x.ForReviewContainer).Column("ForReviewContainerId").LazyLoad().ReadOnly();

            Map(x => x.ForReviewId).Column("ForReviewId");
            References(x => x.ForReview).Column("ForReviewId").LazyLoad().ReadOnly();
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
    public class GetWantCapacityAnswerMap : SubclassMap<GetWantCapacityAnswer>
    {
        public GetWantCapacityAnswerMap()
        {
            Map(x => x.GetIt);
            Map(x => x.WantIt);
            Map(x => x.HasCapacity);
        }
    }
    #endregion

}