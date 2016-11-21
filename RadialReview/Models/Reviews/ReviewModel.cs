using FluentNHibernate.Mapping;
using NHibernate.Mapping;
using RadialReview.Models.Components;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Periods;
using RadialReview.Models.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Models
{
	public class ReviewModel : ICompletable, ILongIdentifiable, IDeletable
	{
		public virtual long Id { get; protected set; }
		public virtual ReviewsModel ForReviewContainer { get; set; }
		public virtual long ForReviewContainerId { get; set; }
		public virtual UserOrganizationModel ReviewerUser { get; set; }
		public virtual long ReviewerUserId { get; set; }
		public virtual String Name { get; set; }
		public virtual DateTime CreatedAt { get; set; }
		//public virtual DateTime De { get; set; }
		public virtual DateTime DueDate { get; set; }
		public virtual ClientReviewModel ClientReview { get; set; }

		public virtual List<AnswerModel> Answers { get; set; }

		public virtual long? PeriodId { get; set; }
		public virtual PeriodModel Period { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual bool Complete { get; set; }
		public virtual decimal? DurationMinutes { get; set; }
		public virtual bool Started { get; set; }
		public virtual bool SeenHints { get; set; }

		//public virtual Ratio Roles { get; set; }
		//public virtual Ratio Values{ get; set; } 

		public virtual Completion QuestionCompletion { get; set; }

		public virtual Completion ReviewedByCompletion { get; set; }

		public virtual Completion ReviewingCompletion { get; set; }

		/*	public virtual int NumRequiredComplete { get; set; }
			public virtual int NumOptionalComplete { get; set; }
			public virtual int NumRequired { get; set; }
			public virtual int NumOptional { get; set; }*/

		/*public virtual decimal Completion { get; set; }
		public virtual Boolean Complete { get; set; }
		public virtual Boolean FullyComplete { get; set; }*/

		/*private  decimal Divide(decimal numerator, decimal denomiator)
		{
			if (denomiator == 0)
				return 1;
			return numerator / denomiator;
		}*/


		public virtual ICompletionModel GetCompletion(bool split = false)
		{

			if (Answers == null)
				return null;

			if (!split)
			{
				var requiredComplete = Answers.Count(x => x.Required && x.Complete);
				var optionalComplete = Answers.Count(x => x.Complete && !x.Required);
				var required = Answers.Count(x => x.Required);
				var optional = Answers.Count(x => !x.Required);

				return new CompletionModel(requiredComplete, required, optionalComplete, optional, "")
				{
					ForceInactive = DateTime.UtcNow > DueDate
				};
			}
			else
			{
				var completions = new List<CompletionModel>();
				var types = new AboutType[] {
					 AboutType.Self,
					 AboutType.Manager,
					 AboutType.Subordinate,
					 AboutType.Peer,
					 AboutType.Teammate,
				 };


				foreach (var flag in types)
				{
					if (flag != AboutType.NoRelationship)
					{
						var limit = Answers.Where(x => (x.AboutType & flag) == flag);
						completions.Add(new CompletionModel(limit.Count(x => x.Complete && x.Required), limit.Count(x => x.Required), limit.Count(x => !x.Required && x.Complete), limit.Count(x => !x.Required), flag.ToString()));
					}
				}

				return new MultibarCompletion(completions);

				/*
				var supervisorCount =   Answers.Where(x => (x.AboutType & AboutType.Manager)        == AboutType.Manager);
				var subordinateCount = Answers.Where(x => (x.AboutType & AboutType.Subordinate) == AboutType.Subordinate);
				var peerCount = Answers.Where(x => (x.AboutType & AboutType.Peer) == AboutType.Peer);
				var teammateCount = Answers.Where(x => (x.AboutType & AboutType.Teammate) == AboutType.Teammate);
				var selfCount = Answers.Where(x => (x.AboutType & AboutType.Self) == AboutType.Self);

                
             

				int requirerComplete = Answers.Count(x => x.Required && x.Complete);
				var optionalComplete = Answers.Count(x => x.Complete && !x.Required);
				int required = Answers.Count(x => x.Required);
				int optional = Answers.Count(x => !x.Required);*/




			}
			/*
			if (requiredComplete < required)
			{
				return Divide(requiredComplete, required);
			}
			else
			{
				return Divide(complete, required);
			}*/


			////var requiredComplete1 = QuestionCompletion.NumRequiredComplete;//Answers.Count(x => x.Required && x.Complete);
			////var optionalComplete1 = QuestionCompletion.NumOptionalComplete;//Answers.Count(x => x.Complete && !x.Required);
			////var required1 = QuestionCompletion.NumRequired;//Answers.Count(x => x.Required);
			////var optional1 = QuestionCompletion.NumOptional;//Answers.Count(x => !x.Required);

			////return new CompletionModel(requiredComplete1, required1, optionalComplete1, optional1, "")
			////{
			////	ForceInactive = DateTime.UtcNow > DueDate
			////};
		}


		public ReviewModel()
		{
			Answers = null;//new List<AnswerModel>();
			CreatedAt = DateTime.UtcNow;
			ClientReview = new ClientReviewModel();

			QuestionCompletion = new Completion();
			ReviewedByCompletion = new Completion();
			ReviewingCompletion = new Completion();

		}

	}

	public class ReviewModelMap : ClassMap<ReviewModel>
	{
		public ReviewModelMap(){
			Id(x => x.Id);
			//Map(x => x.Complete);
			//Map(x => x.Completion);
			//Map(x => x.FullyComplete);
			Map(x => x.ReviewerUserId)
				.Column("ForUserId").Index("Review_ForUserId_IDX");
			References(x => x.ReviewerUser)
				.Column("ForUserId")
				.Not.LazyLoad()
				.ReadOnly();
			Map(x => x.ForReviewContainerId)
				.Column("ForReviewsId");
			References(x => x.ForReviewContainer)
				.Column("ForReviewsId").LazyLoad().ReadOnly();
			Map(x => x.Name);
			Map(x => x.Complete);
			Map(x => x.CreatedAt);
			Map(x => x.DurationMinutes);
			Map(x => x.DueDate);
			Map(x => x.Started);
			Map(x => x.DeleteTime);
			Map(x => x.SeenHints);


			Component(x => x.QuestionCompletion).ColumnPrefix("QuestionCompletion_");
			Component(x => x.ReviewedByCompletion).ColumnPrefix("ReviewedByCompletion_");
			Component(x => x.ReviewingCompletion).ColumnPrefix("ReviewingCompletion_");

			//Component(x => x.Roles).ColumnPrefix("Roles_");
			//Component(x => x.Values).ColumnPrefix("Values_");


			Map(x => x.PeriodId).Column("PeriodId");
			References(x => x.Period).Column("PeriodId").LazyLoad().ReadOnly();

			References(x => x.ClientReview)
				.Not.LazyLoad()
				.Cascade.SaveUpdate();

			/*HasMany(x => x.Answers)
				.Not.LazyLoad()
				.Cascade.SaveUpdate();*/
		}
	}
}