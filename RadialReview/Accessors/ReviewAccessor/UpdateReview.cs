using Amazon.IdentityManagement.Model;
using Microsoft.AspNet.SignalR;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Prereview;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {
		#region Edit Review
		public static void AddAskablesToReview(AbstractUpdate s, PermissionsUtility perms, UserOrganizationModel caller, UserOrganizationModel forUser, ReviewModel reviewModel,bool anonymous, List<AskableAbout> askables) {
			foreach (var q in askables) {
				switch (q.Askable.GetQuestionType()) {
					case QuestionType.RelativeComparison:
						GenerateRelativeComparisonAnswers(s, caller, forUser, q, reviewModel, anonymous);
						break;
					case QuestionType.Slider:
						GenerateSliderAnswers(s, caller, forUser, q, reviewModel, anonymous);
						break;
					case QuestionType.Thumbs:
						GenerateThumbsAnswers(s, caller, forUser, q, reviewModel, anonymous);
						break;
					case QuestionType.Feedback:
						GenerateFeedbackAnswers(s, caller, forUser, q, reviewModel, anonymous);
						break;
					case QuestionType.GWC:
						GenerateGWCAnswers(s, caller, forUser, q, reviewModel, anonymous);
						break;
					case QuestionType.Rock:
						GenerateRockAnswers(s, caller, forUser, q, reviewModel, anonymous);
						break;
					case QuestionType.CompanyValue:
						GenerateCompanyValuesAnswer(s, caller, forUser, q, reviewModel, anonymous);
						break;
					default:
						throw new ArgumentException("Unrecognized questionType(" + q.Askable.GetQuestionType() + ")");
				}
			}
			reviewModel.QuestionCompletion.NumRequired += askables.Count(x => x.Askable.Required);
			reviewModel.QuestionCompletion.NumOptional += askables.Count(x => !x.Askable.Required);

			/*var counts = s.QueryOver<AnswerModel>()
				.Where(x =>
					x.DeleteTime == null &&
					x.ForReviewContainerId == reviewModel.ForReviewsId &&
					(x.ByUserId == reviewModel.ForUserId || x.AboutUserId == reviewModel.ForUserId)
				).Select(x => x.ByUserId, x => x.AboutUserId).List<long[]>().ToList();

			reviewModel.ReviewedByCompletion.NumRequired = counts.Select(x => x[0]).Distinct().Count();
			reviewModel.ReviewingCompletion.NumRequired = counts.Select(x => x[1]).Distinct().Count();*/

			s.SaveOrUpdate(reviewModel);
		}

		/*private static void UpdateReview_Unsafe(ISession s,long reviewContainerId, IEnumerable<long> userIds)
		{
			var answers = s.QueryOver<AnswerModel>()
				.Where(x=>x.DeleteTime==null && x.ForReviewContainerId==reviewContainerId)
				.WhereRestrictionOn(x=>x.u).IsIn(reviewModels.Select(x=>x.Id).ToArray())
				.Select(x => x.ByUserId, x => x.AboutUserId, x=>x.Complete)
				.List<object[]>().Select(x=>new {ByUserId = (long)x[0],AboutUserId = (long)x[1],Complete=(bool)x[2]}).ToList();

			foreach (var r in reviewModels){

				var filtered = answers.Where(x=>x.Complete)

				r.ReviewedByCompletion = 

			}
		}*/

		private static void DeleteAnswers_Unsafe(ISession s, IEnumerable<AnswerModel> answers,DateTime now)
		{
			var toDelete = answers.Where(x => x.DeleteTime == null).ToList();
			foreach (var answer in toDelete){
				answer.DeleteTime = now;
				s.Update(answer);
			}

			var reviewIds = toDelete.Select(x => x.ForReviewId).Distinct().ToList();
			var reviews = s.QueryOver<ReviewModel>().WhereRestrictionOn(x => x.Id).IsIn(reviewIds).List().ToList();
			foreach (var r in reviews){
				r.QuestionCompletion.NumRequired -= toDelete.Count(x => x.Required && x.ForReviewId == r.Id);
				r.QuestionCompletion.NumOptional -= toDelete.Count(x => !x.Required && x.ForReviewId == r.Id);
				r.QuestionCompletion.NumRequiredComplete -= toDelete.Count(x => x.Required && x.Complete && x.ForReviewId == r.Id);
				r.QuestionCompletion.NumOptionalComplete -= toDelete.Count(x => !x.Required && x.Complete && x.ForReviewId == r.Id);

				/*var counts = s.QueryOver<AnswerModel>().Where(x =>
					x.DeleteTime == null &&
					x.ForReviewContainerId == r.ForReviewsId &&
					(x.ByUserId == r.ForUserId || x.AboutUserId == r.ForUserId)
				).Select(x => x.ByUserId, x => x.AboutUserId, x=>x.Complete).List<long[]>().ToList();
				var countComplete = s.QueryOver<AnswerModel>().Where(x =>
					x.DeleteTime == null &&
					x.ForReviewContainerId == r.ForReviewsId &&
					(x.ByUserId == r.ForUserId || x.AboutUserId == r.ForUserId)
				).Select(x => x.ByUserId, x => x.AboutUserId).List<long[]>().ToList();
				r.ReviewedByCompletion.NumRequired = counts.Select(x => x[0]).Distinct().Count();
				r.ReviewingCompletion.NumRequired = counts.Select(x => x[1]).Distinct().Count();*/

				s.Update(r);
			}
		}

		public void AddToReview(UserOrganizationModel caller, long byUserId, long reviewContainerId, long aboutUserId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					AddToReview(s.ToDataInteraction(true), perms, caller, byUserId, reviewContainerId, aboutUserId, AboutType.NoRelationship);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public void RemoveFromReview(UserOrganizationModel caller, long byUserId, long reviewContainerId, long aboutUserId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					RemoveFromReview(s, perms, caller, byUserId, reviewContainerId, aboutUserId);
					tx.Commit();
					s.Flush();
				}
			}
		}

	

		private static void RemoveFromReview(ISession s, PermissionsUtility perms, UserOrganizationModel caller, long byUserId, long reviewContainerId, long aboutUserId)
		{
			//TODO Fix permissions. Should make sure we can edit the review
			perms.ViewUserOrganization(byUserId, false).ViewReviews(reviewContainerId, false);
			var review = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.ForUserId == byUserId).SingleOrDefault();
			perms.ViewUserOrganization(review.ForUserId, false);
			perms.ManageUserReview(review.Id,false);


			var ans=s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.ByUserId == byUserId && x.AboutUserId == aboutUserId).List();
			var now = DateTime.UtcNow;
			DeleteAnswers_Unsafe(s, ans, now);
		}

		private static void AddToReview(DataInteraction s, PermissionsUtility perms, UserOrganizationModel caller, long byUserId, long reviewContainerId, long aboutUserId, AboutType aboutType) {
			//TODO Fix permissions. Should make sure we can edit the review
			perms.ViewUserOrganization(byUserId, false).ViewReviews(reviewContainerId,false);

			var review = s.Where<ReviewModel>(x=>x.ForReviewsId == reviewContainerId && x.ForUserId==byUserId).Single();
			var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);

			perms.ViewUserOrganization(review.ForUserId, false).ManageUserReview(review.Id, true);

			/*var askable = new AskableUtility();

			var appQuestions = ApplicationAccessor.GetApplicationQuestions(s.GetQueryProvider()).ToList();//, ApplicationAccessor.FEEDBACK);
			var userResponsibilities = AskableAccessor.GetAskablesForUser(caller, s.GetQueryProvider(), perms, aboutUserId, reviewContainer.PeriodId).ToListAlive();
											/*ResponsibilitiesAccessor
												  .GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, aboutUserId)
												  .SelectMany(x => x.Responsibilities)
												  .ToListAlive();*

			askable.AddUnique(userResponsibilities, aboutType, aboutUserId);
			foreach (var aq in appQuestions)
				askable.AddUnique(aq, aboutType, aboutUserId);*/

			var askables = ReviewAccessor.GetAskables(caller, perms, s, new[] { aboutUserId }, byUserId, reviewContainer.PeriodId);

			var forUser = s.Get<UserOrganizationModel>(review.ForUserId);
			//var review=s.QueryOver<ReviewModel>().Where(x=>x.ForReviewsId == reviewContainerId && x.ForUserId==byUserId).SingleOrDefault();

			AddAskablesToReview(s.GetUpdateProvider(), perms, caller, forUser, review,reviewContainer.AnonymousByDefault, askables);
		}

		public void RemoveQuestionFromReviewForUser(UserOrganizationModel caller, long reviewContainerId, long userId, long askableId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var answers = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.Askable.Id == askableId && x.AboutUserId == userId && x.DeleteTime == null).List();
					var deleteTime = DateTime.UtcNow;

					DeleteAnswers_Unsafe(s, answers, deleteTime);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void AddResponsibilityAboutUserToReview(ISession s,UserOrganizationModel caller, PermissionsUtility perms, long reviewContainerId, long userId, long askableId)
		{
			var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
			var orgId = reviewContainer.ForOrganization.Id;
			perms.ViewOrganization(orgId).Or(x => x.EditReviewContainer(reviewContainerId), x => x.EditQuestionForUser(userId));

			var queryProvider = GetReviewQueryProvider(s, orgId, reviewContainerId);
			queryProvider.AddData(reviewContainer.AsList());

			var dataInteration = new DataInteraction(queryProvider, s.ToUpdateProvider());

			var team = dataInteration.Get<OrganizationTeamModel>(reviewContainer.ForTeamId);
			//I think we want ToList, not ToListAlive
			var existingReviewUsers = dataInteration.Where<ReviewModel>(x => x.ForReviewsId == reviewContainerId).Select(x => x.ForUser.Id).ToList();
			var user = dataInteration.Get<UserOrganizationModel>(userId);

			var relationships = GetReviewersForUser(caller, perms, dataInteration, user, reviewContainer.GetParameters(), team, existingReviewUsers);

			//also want to get the NoRelationship people
			var norel = ((long) AboutType.NoRelationship);
			var allNoRelationshipps = s.QueryOver<AnswerModel>()
				.Where(x => x.DeleteTime == null && x.AboutTypeNum == norel && x.AboutUserId == userId && x.ForReviewContainerId==reviewContainerId)
				.Select(x => x.ByUserId)
				.List<long>().Distinct()
				.ToList();
			foreach(var nr in allNoRelationshipps){
				var u = dataInteration.Where<UserOrganizationModel>(x => x.Id == nr).Single();
				relationships.Add(u, AboutType.NoRelationship);
			}

			var askable = s.Get<Askable>(askableId);

			foreach (var r in relationships)
			{
				var existingReview = dataInteration.Where<ReviewModel>(x => x.ForUserId == r.Key.Id).SingleOrDefault();
				if (existingReview != null){
					var askableUtil = new AskableUtility();
					foreach (var about in r.Value){
						if (ReviewAccessor.ShouldAddToReview(askable, about)){
							askableUtil.AddUnique(askable, about.Invert(), userId);
						}
					}
					AddAskablesToReview(dataInteration.GetUpdateProvider(), perms, caller, r.Key, existingReview, reviewContainer.AnonymousByDefault, askableUtil.Askables);
				}
			}

		}

		public void AddResponsibilityAboutUserToReview(UserOrganizationModel caller, long reviewContainerId, long userId, long askableId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					AddResponsibilityAboutUserToReview(s, caller, perms, reviewContainerId, userId, askableId);
					tx.Commit();
					s.Flush();
				}
			}
		}
		#endregion

		public ResultObject RemoveUserFromReview(UserOrganizationModel caller, long reviewContainerId, long userOrganizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller)
						.ManagesUserOrganization(userOrganizationId, false)
						.EditReviewContainer(reviewContainerId);

					var deleteTime = DateTime.UtcNow;
					var user = s.Get<UserOrganizationModel>(userOrganizationId);

					var review = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.ForUserId == userOrganizationId && x.DeleteTime==null).SingleOrDefault();
					review.DeleteTime = deleteTime;
					s.Update(review);
					review.DeleteTime = deleteTime;
					var answers = s.QueryOver<AnswerModel>().Where(x => (x.AboutUserId == userOrganizationId || x.ByUserId == userOrganizationId) && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).List();
					
					DeleteAnswers_Unsafe(s,answers,deleteTime);

					tx.Commit();
					s.Flush();
					return ResultObject.Success("Removed " + user.GetNameAndTitle() + " from the review.");
				}
			}

		}

		#region Update
		public async Task<ResultObject> AddUserToReviewContainer(UserOrganizationModel caller, long reviewContainerId, long userOrganizationId, bool sendEmails) {
			var unsent = new List<MailModel>();
			String userBeingReviewed = null;
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var perms = PermissionsUtility.Create(s, caller)
							.ManagesUserOrganization(userOrganizationId, false)
							.ViewReviews(reviewContainerId, false);
						var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
						var dueDate = reviewContainer.DueDate;
						//var sendEmails = false;
						var reviewSelf = reviewContainer.ReviewSelf;
						var reviewManagers = reviewContainer.ReviewManagers;
						var reviewSubordinates = reviewContainer.ReviewSubordinates;
						var reviewTeammates = reviewContainer.ReviewTeammates;
						var reviewPeers = reviewContainer.ReviewPeers;
						var organization = reviewContainer.ForOrganization;

						OrganizationTeamModel team = TeamAccessor.GetTeam(s, perms, caller, reviewContainer.ForTeamId);


						List<Exception> exceptions = new List<Exception>();
						// int sent = 0;
						//int errors = 0;

						var beingReviewedUser = s.Get<UserOrganizationModel>(userOrganizationId);

						var orgId = organization.Id;

						var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).List();
						var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).List();
						var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).List();
						var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).List();
						var allPositions = s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId).List();
						var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).List();
						var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).List();
						var reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId).List();
						var allAskable = s.QueryOver<RoleModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).List();
						var allRocks = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).List();
						var allValues = s.QueryOver<CompanyValueModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).List();
						var allReviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.ForOrganizationId == orgId && x.DeleteTime == null).List();

						var queryProvider = new IEnumerableQuery();
						queryProvider.AddData(allOrgTeams);
						queryProvider.AddData(allTeamDurations);
						queryProvider.AddData(allMembers);
						queryProvider.AddData(allManagerSubordinates);
						queryProvider.AddData(allPositions);
						queryProvider.AddData(allAskable);
						queryProvider.AddData(allRocks);
						queryProvider.AddData(allValues);
						queryProvider.AddData(allReviewContainers);
						queryProvider.AddData(applicationQuestions);
						queryProvider.AddData(application);
						queryProvider.AddData(reviews);


						var accessibleUsers = TeamAccessor.GetTeamMembers(s.ToQueryProvider(true), perms, reviewContainer.ForTeamId, false)
														.ToListAlive()
														.Select(x => x.User).ToListAlive();

						accessibleUsers.Add(beingReviewedUser);

						//TODO Populate a query provider structure here..
						var toEmail = AddUserToReview(caller, true, dueDate,reviewContainer.GetParameters(),new DataInteraction(queryProvider, s.ToUpdateProvider()), reviewContainer, perms, organization, team, ref exceptions,beingReviewedUser, accessibleUsers);
						unsent.AddRange(toEmail);
						userBeingReviewed = beingReviewedUser.GetName();
						tx.Commit();
						s.Flush();
					}
				}
			}
			catch (Exception e) {
				return new ResultObject(e);
			}
			var result = new EmailResult();

			if (sendEmails) {
				result = await Emailer.SendEmails(unsent);
			}

			return result.ToResults("Successfully added " + userBeingReviewed + " to the review.");
		}

		/*public void UpdateStarted(UserOrganizationModel userOrganizationModel, List<AnswerModel> answers, DateTime now)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					foreach (var p in answers)
					{
						if (p.CompleteTime == null)
						{
							p.StartTime = now;
						}


					}
					tx.Commit();
					s.Flush();
				}
			}

		}*/

		public ReviewContainerStats UpdateAllCompleted(UserOrganizationModel caller, long reviewId, bool started, decimal durationMinutes, int additionalAnswered, int optionalAnswered) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditReview(reviewId);
					var uncompleted = s.QueryOver<AnswerModel>().Where(x => x.ForReviewId == reviewId && x.Required && !x.Complete && x.DeleteTime == null).RowCount();
					var review = s.Get<ReviewModel>(reviewId);

					var output = new ReviewContainerStats(review.ForReviewsId);

					var updated = false;
					if (durationMinutes != 0) {
						if (review.DurationMinutes == null)
							review.DurationMinutes = 0;
						review.DurationMinutes += (decimal)Math.Min(durationMinutes, (decimal)TimingUtility.ExcludeLongerThan.TotalMinutes);
						updated = true;
					}

					if (uncompleted == 0 && !review.Complete) {
						review.Complete = true;
						updated = true;
						output.Stats.ReviewsCompleted += 1;
						output.Completion.Started -= 1;
						output.Completion.Finished += 1;
					}

					if (started && !review.Started) {
						review.Started = true;
						updated = true;
						output.Completion.Started += 1;
						output.Completion.Unstarted -= 1;
					}

					if (uncompleted != 0 && review.Complete) {
						review.Complete = false;
						review.DurationMinutes = null;
						updated = true;
						output.Stats.ReviewsCompleted -= 1;
						output.Completion.Finished -= 1;
						output.Completion.Started += 1;
					}

					output.Stats.QuestionsAnswered += additionalAnswered;
					output.Stats.OptionalsAnswered += optionalAnswered;

					review.QuestionCompletion.NumRequiredComplete += additionalAnswered;

					if (updated || output.Stats.QuestionsAnswered != 0 || output.Stats.OptionalsAnswered != 0) {
						var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
						//hub.Clients.All.updateReviewStats(ResultObject.Create(output));
						hub.Clients.Group("manager_" + caller.Organization.Id).updateReviewStats(ResultObject.Create(output));
					}

					//if (updated) {
					s.Update(review);
					tx.Commit();
					s.Flush();
					//}

					return output;
					//return review.Complete;
				}
			}
		}

		private void UpdateCompletion(AnswerModel answer, DateTime now, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta) {
			if (answer.Complete) {
				if (answer.CompleteTime == null) {
					if (!answer.Required)
						optionalAnsweredDelta += 1;
					questionsAnsweredDelta += 1;
				}
				answer.CompleteTime = now;
			}
			else {
				if (answer.CompleteTime != null) {
					if (!answer.Required)
						optionalAnsweredDelta -= 1;
					questionsAnsweredDelta -= 1;
				}
				answer.CompleteTime = null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="id"></param>
		/// <param name="value"></param>
		/// <returns>Complete</returns>
		
		public void AddAnswerToReview(UserOrganizationModel caller, long reviewId, long answerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);
					var feedback = s.Get<AnswerModel>(answerId);
					var review = s.Get<ReviewModel>(reviewId);
					if (review.ForUserId != feedback.AboutUserId)
						throw new PermissionsException("Answer and Review do not match.");

					review.ClientReview.FeedbackIds.Add(new LongModel() { Value = answerId });
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void RemoveAnswerFromReview(UserOrganizationModel caller, long reviewId, long answerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId,false);
					var answer = s.Get<AnswerModel>(answerId);
					var review = s.Get<ReviewModel>(reviewId);

					if (review.ForUserId != answer.AboutUserId)
						throw new PermissionsException("Answer and Review do not match.");

					foreach (var id in review.ClientReview.FeedbackIds) {
						if (id.Value == answerId)
							id.DeleteTime = DateTime.UtcNow;
					}
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
	

		public void UpdateDueDates(UserOrganizationModel caller, long reviewContainerId, DateTime? prereviewDueDate, DateTime reviewDueDate, DateTime? reportDueDate) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller).EditReviewContainer(reviewContainerId);
					var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
					var update = false;
					if (prereviewDueDate != null) {
						if (reviewContainer.PrereviewDueDate != prereviewDueDate.Value) {
							reviewContainer.PrereviewDueDate = prereviewDueDate.Value;
							update = true;
							var prereviews = s.QueryOver<PrereviewModel>().Where(x => x.ReviewContainerId == reviewContainerId).List().ToList();
							foreach (var p in prereviews) {
								p.PrereviewDue = prereviewDueDate.Value;
								s.Update(p);
							}
						}
					}

					if (reviewContainer.DueDate != reviewDueDate) {
						update = true;
						reviewContainer.DueDate = reviewDueDate;
						var reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId).List().ToList();
						foreach (var r in reviews) {
							r.DueDate = reviewDueDate;
							s.Update(r);
						}
					}

					if (reportDueDate != null && reviewContainer.ReportsDueDate != reportDueDate) {
						update = true;
						reviewContainer.ReportsDueDate = reportDueDate;
					}

					if (update) {
						s.Update(reviewContainer);
						tx.Commit();
						s.Flush();
					}
				}
			}

		}
		public void UpdateDueDate(UserOrganizationModel caller, long reviewId, DateTime dueDate) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var review = s.Get<ReviewModel>(reviewId);
					if (review == null)
						throw new PermissionsException("Review does not exist. (" + reviewId + ")");

					PermissionsUtility.Create(s, caller).EditReviewContainer(review.ForReviewsId);

					review.DueDate = dueDate;
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public void EditReviewName(UserOrganizationModel caller, long reviewContainerId,String reviewName)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditReviewContainer(reviewContainerId);
					var review = s.Get<ReviewsModel>(reviewContainerId);
					if (review == null)
						throw new PermissionsException("Review does not exist. (" + reviewContainerId + ")");

					review.ReviewName = reviewName;
					s.Update(review);

					var reviews =s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId).List().ToList();
					foreach (var r in reviews){
						r.Name = reviewName;
						s.Update(r);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		#endregion
	}
}