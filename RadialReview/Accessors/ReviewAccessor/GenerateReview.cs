using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {
		public static TeamAccessor _TeamAccessor = new TeamAccessor();
		
		#region Generate Review
		#region Generate Answers
		private static void GenerateSliderAnswers(DataInteraction session, Reviewer reviewer, AskableAbout askable, ReviewModel review, bool anonymous)
		{

			var slider = new SliderAnswer()
			{
				Anonymous = anonymous,
				Complete = false,
				Percentage = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ReviewerUserId = reviewer.RGMId,
				RevieweeUserId = askable.Reviewee.RGMId,
				RevieweeUser = session.Load<ResponsibilityGroupModel>(askable.Reviewee.RGMId),
				RevieweeUser_AcNodeId = askable.Reviewee.ACNodeId,
				ForReviewContainerId = review.ForReviewContainerId,
				AboutType = askable.ReviewerIsThe
			};
			session.Save(slider);

		}

		private static void GenerateGWCAnswers(DataInteraction session, Reviewer reviewer, AskableAbout askable, ReviewModel review, bool anonymous)
		{
			var gwc = new GetWantCapacityAnswer()
			{
				Anonymous = anonymous,
				Complete = false,
				GetIt		= FiveState.Indeterminate,
				WantIt		= FiveState.Indeterminate,
				HasCapacity = FiveState.Indeterminate,
				GetItReason  = null,
				WantItReason = null,
				HasCapacityReason = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ReviewerUserId = reviewer.RGMId,
				RevieweeUserId = askable.Reviewee.RGMId,
				RevieweeUser = session.Load<ResponsibilityGroupModel>(askable.Reviewee.RGMId),
				RevieweeUser_AcNodeId = askable.Reviewee.ACNodeId,
				ForReviewContainerId = review.ForReviewContainerId,
				AboutType = askable.ReviewerIsThe
			};
			session.Save(gwc);
		}

		private static void GenerateRockAnswers(DataInteraction session, Reviewer reviewer, AskableAbout askable, ReviewModel review, bool anonymous)
		{
			var rock = new RockAnswer()
			{
				Anonymous = anonymous,
				Complete = false,
				Finished = Tristate.Indeterminate,
				ManagerOverride = RockState.Indeterminate,
				Completion = RockState.Indeterminate,
				Reason = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ReviewerUserId = reviewer.RGMId,
				RevieweeUserId = askable.Reviewee.RGMId,
				RevieweeUser = session.Load<ResponsibilityGroupModel>(askable.Reviewee.RGMId),
				RevieweeUser_AcNodeId = askable.Reviewee.ACNodeId,
				ForReviewContainerId = review.ForReviewContainerId,
				AboutType = askable.ReviewerIsThe
			};
			session.Save(rock);
		}

		private static void GenerateCompanyValuesAnswer(DataInteraction session, Reviewer reviewer, AskableAbout askable, ReviewModel review, bool anonymous) {
			var gwc = new CompanyValueAnswer() {
				Anonymous = anonymous,
				Complete = false,
				Exhibits = PositiveNegativeNeutral.Indeterminate,
				Reason = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ReviewerUserId = reviewer.RGMId,
				RevieweeUserId = askable.Reviewee.RGMId,
				RevieweeUser = session.Load<ResponsibilityGroupModel>(askable.Reviewee.RGMId),
				RevieweeUser_AcNodeId = askable.Reviewee.ACNodeId,
				ForReviewContainerId = review.ForReviewContainerId,
				AboutType = askable.ReviewerIsThe
			};
			session.Save(gwc);
		}

		private static void GenerateRadioAnswer(DataInteraction session, Reviewer reviewer, AskableAbout askable, ReviewModel review, bool anonymous) {
			var gwc = new RadioAnswer() {
				Anonymous = anonymous,
				Complete = false,
				Selected = null,
				Reason = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ReviewerUserId = reviewer.RGMId,
				RevieweeUserId = askable.Reviewee.RGMId,
				RevieweeUser = session.Load<ResponsibilityGroupModel>(askable.Reviewee.RGMId),
				RevieweeUser_AcNodeId = askable.Reviewee.ACNodeId,
				ForReviewContainerId = review.ForReviewContainerId,
				AboutType = askable.ReviewerIsThe
			};
			session.Save(gwc);
		}

		private static void GenerateFeedbackAnswers(DataInteraction session, Reviewer reviewer, AskableAbout askable, ReviewModel review, bool anonymous)
		{
			var feedback = new FeedbackAnswer() {
				Anonymous = anonymous,
				Complete = false,
				Feedback = null,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ReviewerUserId = reviewer.RGMId,
				RevieweeUserId = askable.Reviewee.RGMId,
				RevieweeUser = session.Load<ResponsibilityGroupModel>(askable.Reviewee.RGMId),
				RevieweeUser_AcNodeId = askable.Reviewee.ACNodeId,
				ForReviewContainerId = review.ForReviewContainerId,
				AboutType = askable.ReviewerIsThe
			};
			session.Save(feedback);

		}

		private static void GenerateThumbsAnswers(DataInteraction session, Reviewer reviewer, AskableAbout askable, ReviewModel review, bool anonymous)
		{
			var thumbs = new ThumbsAnswer() {
				Anonymous = anonymous,
				Complete = false,
				Thumbs = ThumbsType.None,
				Askable = askable.Askable,
				Required = askable.Askable.Required,
				ForReviewId = review.Id,
				ReviewerUserId = reviewer.RGMId,
				RevieweeUserId = askable.Reviewee.RGMId,
				RevieweeUser = session.Load<ResponsibilityGroupModel>(askable.Reviewee.RGMId),
				RevieweeUser_AcNodeId = askable.Reviewee.ACNodeId,
				ForReviewContainerId = review.ForReviewContainerId,
				AboutType = askable.ReviewerIsThe
			};
			session.Save(thumbs);

		}

		private static void GenerateRelativeComparisonAnswers(DataInteraction session, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review, bool anonymous)
		{
			var peers = forUser.ManagedBy.ToListAlive().Select(x => x.Manager).SelectMany(x => x.ManagingUsers.ToListAlive().Select(y => y.Subordinate));
			var managers = forUser.ManagedBy.ToListAlive().Select(x => x.Manager);
			var managing = forUser.ManagingUsers.ToListAlive().Select(x => x.Subordinate);

			var groupMembers = forUser.Groups.SelectMany(x => x.GroupUsers);

			var union = peers.UnionBy(x => x.Id, managers, managing, groupMembers).ToList();

			var len = union.Count();
			var items = new List<Tuple<UserOrganizationModel, UserOrganizationModel>>();
			for (int i = 0; i < len - 1; i++) {
				for (int j = i + 1; j < len; j++) {
					var relComp = new RelativeComparisonAnswer()
					{
						Anonymous = anonymous,
						Required = askable.Askable.Required,
						Askable = askable.Askable,
						Complete = false,
						First = union[i],
						Second = union[j],
						Choice = RelativeComparisonType.Skip,
						ForReviewId = review.Id,
						ReviewerUserId = forUser.Id,
						RevieweeUserId = askable.Reviewee.RGMId,
						RevieweeUser = session.Load<ResponsibilityGroupModel>(askable.Reviewee.RGMId),
						RevieweeUser_AcNodeId = askable.Reviewee.ACNodeId,
						ForReviewContainerId = review.ForReviewContainerId,
						AboutType = askable.ReviewerIsThe
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