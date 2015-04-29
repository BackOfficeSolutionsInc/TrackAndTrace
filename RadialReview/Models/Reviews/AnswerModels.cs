using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;
using System;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Models
{

    #region AnswerModel
    public abstract class AnswerModel : ILongIdentifiable,ICompletable,IDeletable
    {
        public virtual long Id { get; set; }

	    public virtual string Identifier
	    {
		    get { return Askable.Id + "_" + AboutUserId+"_"+ForReviewContainerId; }
	    }

	    public virtual long ForReviewId { get; set; }
        //public virtual ReviewModel ForReview { get; set; }
        public virtual long ForReviewContainerId { get; set; }
        //public virtual ReviewsModel ForReviewContainer { get; set; }
        public virtual Askable Askable { get; set; }
        public virtual bool Required { get; set; }
        public virtual bool Complete { get; set; }
		public virtual long AboutUserId { get; set; }
		public virtual ResponsibilityGroupModel AboutUser { get; set; }
		//public virtual OrganizationModel AboutOrganization { get; set; }
        public virtual long ByUserId { get; set; }
		public virtual UserOrganizationModel ByUser { get; set; }
		public virtual AboutType AboutType { get; set; }
		public virtual long AboutTypeNum { get; set; }
		public virtual bool Anonymous { get; set; }

        public virtual ICompletionModel GetCompletion(bool split = false)
        {
            if (Required)
                return new CompletionModel(Complete.ToInt(), 1);
            return new CompletionModel(0, 0, Complete.ToInt(), 1);
        }

        public virtual DateTime? DeleteTime { get; set; }
        public virtual DateTime? CompleteTime { get; set; }
		public class AnswerModelMap : ClassMap<AnswerModel> {
			public AnswerModelMap() {
				Id(x => x.Id);
				References(x => x.Askable).Not.LazyLoad();
				//.Cascade.SaveUpdate();
				Map(x => x.Required);
				Map(x => x.Complete);
				Map(x => x.DeleteTime);
				Map(x => x.CompleteTime);
				Map(x => x.Anonymous);

				Map(x => x.AboutType).CustomType(typeof(Int64)).Column("AboutType");
				Map(x => x.AboutTypeNum).Column("AboutType").ReadOnly();

				Map(x => x.AboutUserId).Column("AboutUserId");
				References(x => x.AboutUser).Column("AboutUserId").Not.LazyLoad().ReadOnly();
				//References(x => x.AboutOrganization).Column("AboutUserId").NotFound.Ignore().ReadOnly().Nullable();

				Map(x => x.ByUserId).Column("ByUserId");
				References(x => x.ByUser).Column("ByUserId").Not.LazyLoad().ReadOnly();

				Map(x => x.ForReviewContainerId).Column("ForReviewContainerId");
				//References(x => x.ForReviewContainer).Column("ForReviewContainerId").LazyLoad().ReadOnly();

				Map(x => x.ForReviewId).Column("ForReviewId");
				//References(x => x.ForReview).Column("ForReviewId").LazyLoad().ReadOnly();
			}
		}
    }
    #endregion

    #region Impls
	
    public class FeedbackAnswer : AnswerModel
    {
        public virtual String Feedback { get; set; }
		public class FeedbackAnswerMap : SubclassMap<FeedbackAnswer> {
			public FeedbackAnswerMap() {
				Map(x => x.Feedback).Length(5000); 
			}
		}
    }
    public class RelativeComparisonAnswer : AnswerModel
    {
        public virtual UserOrganizationModel First { get;set;}
        public virtual UserOrganizationModel Second { get; set; }
        public virtual RelativeComparisonType Choice { get;set; }
		public class RelativeComparisonAnswerMap : SubclassMap<RelativeComparisonAnswer> {
			public RelativeComparisonAnswerMap() {
				Map(x => x.Choice);
				References(x => x.First).Not.LazyLoad();
				References(x => x.Second).Not.LazyLoad();
			}
		}
    }
    public class SliderAnswer : AnswerModel
    {
        public virtual decimal? Percentage { get; set; }
		public class SliderAnswerMap : SubclassMap<SliderAnswer> {
			public SliderAnswerMap() {
				Map(x => x.Percentage);
			}
		}
    }
    public class ThumbsAnswer : AnswerModel
    {
        public virtual ThumbsType Thumbs { get; set; }
		public class ThumbAnswerMap : SubclassMap<ThumbsAnswer> {
			public ThumbAnswerMap() {
				Map(x => x.Thumbs);
			}
		}
    }
	public class GetWantCapacityAnswer : AnswerModel {
		public virtual FiveState GetIt { get; set; }
		public virtual FiveState WantIt { get; set; }
		public virtual FiveState HasCapacity { get; set; }
		public virtual String GetItReason { get; set; }
		public virtual String WantItReason { get; set; }
		public virtual String HasCapacityReason { get; set; }

		public virtual Ratio GetItRatio
		{
			get
			{
				return GetIt.Ratio();

				//return new Ratio(GetIt == Tristate.True ? 1 : 0, GetIt != Tristate.Indeterminate ? 1 : 0);
			}
		}
		public virtual Ratio WantItRatio
		{
			get
			{
				return WantIt.Ratio();
				//return new Ratio(WantIt == Tristate.True ? 1 : 0, WantIt != Tristate.Indeterminate ? 1 : 0);
			}
		}
		public virtual Ratio HasCapacityRatio{
			get
			{
				return HasCapacity.Ratio();
				//return new Ratio(HasCapacity == Tristate.True ? 1 : 0, HasCapacity != Tristate.Indeterminate ? 1 : 0);
			}
		}


		public virtual bool IncludeHasCapacityReason { get; set; }
		public virtual bool IncludeGetItReason { get; set; }
		public virtual bool IncludeWantItReason { get; set; }
		public class GetWantCapacityAnswerMap : SubclassMap<GetWantCapacityAnswer> {
			public GetWantCapacityAnswerMap() {
				Map(x => x.GetIt);
				Map(x => x.WantIt);
				Map(x => x.HasCapacity);

				Map(x => x.GetItReason).Length(5000); 
				Map(x => x.WantItReason).Length(5000); 
				Map(x => x.HasCapacityReason).Length(5000);

				Map(x => x.IncludeHasCapacityReason);
				Map(x => x.IncludeGetItReason); 
				Map(x => x.IncludeWantItReason); 
			}
		}
	}
	public class RockAnswer : AnswerModel
	{
		public virtual Tristate Finished { get; set; }
		public virtual RockState Completion { get; set; }
		public virtual RockState ManagerOverride { get; set; }

		public virtual String Reason { get; set; }
		public virtual String OverrideReason { get; set; }
		public class RockAnswerMap : SubclassMap<RockAnswer> {
			public RockAnswerMap() {
				Map(x => x.Finished);
				Map(x => x.Completion);
				Map(x => x.ManagerOverride);
				Map(x => x.Reason).Length(5000);
				Map(x => x.OverrideReason).Length(5000);
			}
		}
	}
	public class CompanyValueAnswer : AnswerModel
	{
		public virtual PositiveNegativeNeutral Exhibits { get; set; }
		public virtual String Reason { get; set; }
		public virtual bool IncludeReason { get; set; }
		public class CompanyValueAnswerMap : SubclassMap<CompanyValueAnswer>
		{
			public CompanyValueAnswerMap()
			{
				Map(x => x.Exhibits);
				Map(x => x.Reason).Length(5000);
				Map(x => x.IncludeReason);
			}
		}

	}

    #endregion


}