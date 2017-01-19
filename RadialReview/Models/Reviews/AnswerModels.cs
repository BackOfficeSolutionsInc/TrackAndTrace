using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;
using System;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.UserModels;

namespace RadialReview.Models {
	public class AnswerModel_TinyScatterPlot {
		//public string Category { get; set; }
		public WeightType Weight { get; set; }
		//public bool Required { get; set; }
		public AboutType AboutType { get; set; }
		public long ByUserId { get; set; }
		public string ByUserName { get; set; }
		public string ByUserImage { get; set; }
		public string ByUserPosition { get; set; }
		public QuestionType QuestionType { get; set; }
		public Ratio Score { get; set; }
		public string Axis { get; set; }

		public AnswerModel_TinyScatterPlot() {
			ByUserName = "";
			ByUserPosition = "";
			ByUserImage = UserLookup.TransformImageSuffix(null);
		}
	}


	#region AnswerModel
	public abstract class AnswerModel : ILongIdentifiable, ICompletable, IDeletable {
		public virtual long Id { get; set; }

		public virtual string Identifier {
			get { return Askable.Id + "_" + RevieweeUserId + "_" + ForReviewContainerId; }
		}

		public virtual long ForReviewId { get; set; }
		public virtual long ForReviewContainerId { get; set; }

		public virtual Askable Askable { get; set; }
		public virtual bool Required { get; set; }
		public virtual bool Complete { get; set; }
		public virtual long RevieweeUserId { get; set; }
		public virtual long? RevieweeUser_AcNodeId { get; set; }
		public virtual ResponsibilityGroupModel RevieweeUser { get; set; }

		public virtual long ReviewerUserId { get; set; }
		public virtual UserOrganizationModel ReviewerUser { get; set; }
		public virtual AboutType AboutType { get; set; }
		public virtual long AboutTypeNum { get; set; }
		public virtual bool Anonymous { get; set; }

		public virtual ICompletionModel GetCompletion(bool split = false) {
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

				Map(x => x.RevieweeUser_AcNodeId).Column("AboutACId");

				Map(x => x.AboutType).CustomType(typeof(Int64)).Column("AboutType");
				Map(x => x.AboutTypeNum).Column("AboutType").ReadOnly();

				Map(x => x.RevieweeUserId).Column("AboutUserId");
				References(x => x.RevieweeUser).Column("AboutUserId").Not.LazyLoad().ReadOnly();

				Map(x => x.ReviewerUserId).Column("ByUserId");
				References(x => x.ReviewerUser).Column("ByUserId").Not.LazyLoad().ReadOnly();

				Map(x => x.ForReviewContainerId).Column("ForReviewContainerId");
				Map(x => x.ForReviewId).Column("ForReviewId").Index("AnswerModel_Review_IDX");
			}
		}
	}
	#endregion

	#region Impls

	public class RadioAnswer : AnswerModel {
		public virtual String Selected { get; set; }
		public virtual String Reason { get; set; }
		public virtual decimal Weight { get; set; }
		public class Map : SubclassMap<RadioAnswer> {
			public Map() {
				Map(x => x.Selected).Length(1024);
				Map(x => x.Reason).Length(5000);
				Map(x => x.Weight);
			}
		}
	}

	public class FeedbackAnswer : AnswerModel {
		public virtual String Feedback { get; set; }
		public class FeedbackAnswerMap : SubclassMap<FeedbackAnswer> {
			public FeedbackAnswerMap() {
				Map(x => x.Feedback).Length(5000);
			}
		}
	}
	public class RelativeComparisonAnswer : AnswerModel {
		public virtual UserOrganizationModel First { get; set; }
		public virtual UserOrganizationModel Second { get; set; }
		public virtual RelativeComparisonType Choice { get; set; }
		public class RelativeComparisonAnswerMap : SubclassMap<RelativeComparisonAnswer> {
			public RelativeComparisonAnswerMap() {
				Map(x => x.Choice);
				References(x => x.First).Not.LazyLoad();
				References(x => x.Second).Not.LazyLoad();
			}
		}
	}
	public class SliderAnswer : AnswerModel {
		public virtual decimal? Percentage { get; set; }
		public class SliderAnswerMap : SubclassMap<SliderAnswer> {
			public SliderAnswerMap() {
				Map(x => x.Percentage);
			}
		}
	}
	public class ThumbsAnswer : AnswerModel {
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

		public virtual Ratio GetItRatio {
			get {
				return GetIt.Ratio();

				//return new Ratio(GetIt == Tristate.True ? 1 : 0, GetIt != Tristate.Indeterminate ? 1 : 0);
			}
		}
		public virtual Ratio WantItRatio {
			get {
				return WantIt.Ratio();
				//return new Ratio(WantIt == Tristate.True ? 1 : 0, WantIt != Tristate.Indeterminate ? 1 : 0);
			}
		}
		public virtual Ratio HasCapacityRatio {
			get {
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
	public class RockAnswer : AnswerModel {
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
	public class CompanyValueAnswer : AnswerModel {
		public virtual PositiveNegativeNeutral Exhibits { get; set; }
		public virtual String Reason { get; set; }
		public virtual bool IncludeReason { get; set; }
		public class CompanyValueAnswerMap : SubclassMap<CompanyValueAnswer> {
			public CompanyValueAnswerMap() {
				Map(x => x.Exhibits);
				Map(x => x.Reason).Length(5000);
				Map(x => x.IncludeReason);
			}
		}

	}

	#endregion


}