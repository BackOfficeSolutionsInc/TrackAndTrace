using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {
		public static TeamAccessor _TeamAccessor = new TeamAccessor();
		
		#region Generate Review
		#region Generate Answers
		private static void GenerateSliderAnswers(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review) {

			var slider = new SliderAnswer() {
				Complete = false,
				Percentage = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ByUserId = forUser.Id,
				AboutUserId = askable.AboutUserId,
				ForReviewContainerId = review.ForReviewsId,
				AboutType = askable.AboutType

			};
			session.Save(slider);

		}

		private static void GenerateGWCAnswers(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review) {
			var gwc = new GetWantCapacityAnswer() {
				Complete = false,
				GetIt		= Tristate.Indeterminate,
				WantIt		= Tristate.Indeterminate,
				HasCapacity = Tristate.Indeterminate,
				GetItReason  = null,
				WantItReason = null,
				HasCapacityReason = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ByUserId = forUser.Id,
				AboutUserId = askable.AboutUserId,
				ForReviewContainerId = review.ForReviewsId,
				AboutType = askable.AboutType
			};
			session.Save(gwc);
		}

		private static void GenerateRockAnswers(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review) {
			var rock = new RockAnswer() {
				Complete = false,
				Finished = Tristate.Indeterminate,
				Reason = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ByUserId = forUser.Id,
				AboutUserId = askable.AboutUserId,
				ForReviewContainerId = review.ForReviewsId,
				AboutType = askable.AboutType
			};
			session.Save(rock);
		}

		private static void GenerateCompanyValuesAnswer(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review)
		{
			var gwc = new CompanyValueAnswer()
			{
				Complete = false,
				Exhibits = PositiveNegativeNeutral.Indeterminate,
				Reason = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ByUserId = forUser.Id,
				AboutUserId = askable.AboutUserId,
				ForReviewContainerId = review.ForReviewsId,
				AboutType = askable.AboutType
			};
			session.Save(gwc);
		}

		private static void GenerateFeedbackAnswers(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review) {
			var feedback = new FeedbackAnswer() {
				Complete = false,
				Feedback = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ByUserId = forUser.Id,
				AboutUserId = askable.AboutUserId,
				ForReviewContainerId = review.ForReviewsId,
				AboutType = askable.AboutType
			};
			session.Save(feedback);

		}

		private static void GenerateThumbsAnswers(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review) {
			var thumbs = new ThumbsAnswer() {
				Complete = false,
				Thumbs = ThumbsType.None,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ByUserId = forUser.Id,
				AboutUserId = askable.AboutUserId,
				ForReviewContainerId = review.ForReviewsId,
				AboutType = askable.AboutType
			};
			session.Save(thumbs);

		}

		private static void GenerateRelativeComparisonAnswers(AbstractUpdate session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review) {
			var peers = forUser.ManagedBy.ToListAlive().Select(x => x.Manager).SelectMany(x => x.ManagingUsers.ToListAlive().Select(y => y.Subordinate));
			var managers = forUser.ManagedBy.ToListAlive().Select(x => x.Manager);
			var managing = forUser.ManagingUsers.ToListAlive().Select(x => x.Subordinate);

			var groupMembers = forUser.Groups.SelectMany(x => x.GroupUsers);

			var union = peers.UnionBy(x => x.Id, managers, managing, groupMembers).ToList();

			var len = union.Count();
			List<Tuple<UserOrganizationModel, UserOrganizationModel>> items = new List<Tuple<UserOrganizationModel, UserOrganizationModel>>();
			for (int i = 0; i < len - 1; i++) {
				for (int j = i + 1; j < len; j++) {
					var relComp = new RelativeComparisonAnswer() {
						Required = askable.Askable.Required,
						Askable = askable.Askable,
						Complete = false,
						First = union[i],
						Second = union[j],
						Choice = RelativeComparisonType.Skip,
						ForReviewId = review.Id,
						ByUserId = forUser.Id,
						AboutUserId = askable.AboutUserId,
						ForReviewContainerId = review.ForReviewsId,
						AboutType = askable.AboutType
					};
					items.Add(Tuple.Create(union[i], union[j]));
					session.Save(relComp);
				}
			}

		}
		#endregion
		#endregion
	}
}