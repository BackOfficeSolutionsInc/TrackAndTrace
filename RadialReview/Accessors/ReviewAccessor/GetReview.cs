using NHibernate;
using NHibernate.Criterion;
using NHibernate.Util;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Periods;
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
using NHibernate.Linq;
using RadialReview.Models.Permissions;
using RadialReview.Models.Askables;
using RadialReview.Engines;
using NHibernate.Transform;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {
		public static List<ReviewModel> GetReviewsForUserWithVisibleReports(ISession s, PermissionsUtility perms, UserOrganizationModel caller, long forUserId, int page, int pageCount) {
			perms.ViewUserOrganization(forUserId, true);
			List<ReviewModel> reviews;
			ClientReviewModel clientReviewAlias = null;
			ReviewModel reviewAlias = null;
			reviews = s.QueryOver<ReviewModel>(() => reviewAlias)
								.Left.JoinAlias(() => reviewAlias.ClientReview, () => clientReviewAlias)
								.Where(() => clientReviewAlias.Visible == true && reviewAlias.ReviewerUserId == forUserId && reviewAlias.DeleteTime == null)
								.OrderBy(() => reviewAlias.CreatedAt)
								.Desc.Skip(page * pageCount)
								.Take(pageCount)
								//.Fetch(x => x.Answers).Eager
								//add reviewModel Id to answers, query for that
								.List().ToList();
			return reviews;
		}

		public static List<ReviewModel> GetReviewsForUser(ISession s, PermissionsUtility perms, UserOrganizationModel caller, long forUserId, int page, int pageCount, DateTime dueAfter, bool includeAnswers = true, bool includeAllUserOrgs = true) {
			perms.ViewUserOrganization(forUserId, includeAnswers);

			List<ReviewModel> reviews;

			var user = s.Get<UserOrganizationModel>(forUserId);
			long[] usersIds;
			if (includeAllUserOrgs)
				usersIds = user.UserIds;
			else
				usersIds = new long[] { forUserId };

			reviews = s.QueryOver<ReviewModel>().Where(x => /*x.ForUserId == forUserId &&*/ x.DeleteTime == null && x.DueDate > dueAfter)
								.WhereRestrictionOn(x => x.ReviewerUserId).IsIn(usersIds)
								.OrderBy(x => x.CreatedAt)
								.Desc.Skip(page * pageCount)
								.Take(pageCount)
								//.Fetch(x => x.Answers).Eager
								//add reviewModel Id to answers, query for that
								.List().ToList();
			if (includeAnswers) {
				var allAnswers = s.QueryOver<AnswerModel>()
					.Where(x => /*x.ByUserId == forUserId &&*/ x.DeleteTime == null)
					.WhereRestrictionOn(x => x.ReviewerUserId).IsIn(usersIds)
					.List().ToListAlive();

				for (int i = 0; i < reviews.Count; i++)
					PopulateAnswers( /*s,*/ reviews[i], allAnswers);
			}
			return reviews;
		}

		#region Get

		public int GetNumberOfReviewsForUser(UserOrganizationModel caller, UserOrganizationModel forUser) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var forUserId = forUser.Id;
					PermissionsUtility.Create(s, caller).ViewUserOrganization(forUserId, true);

					return s.QueryOver<ReviewModel>()
						.Where(x => x.ReviewerUserId == forUserId && x.DeleteTime == null)
						.RowCount();
				}
			}
		}

		public int GetNumberOfReviewsWithVisibleReportsForUser(UserOrganizationModel caller, long forUserId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewUserOrganization(forUserId, true);
					ReviewModel reviewAlias = null;
					ClientReviewModel reportAlias = null;

					return s.QueryOver<ReviewModel>(() => reviewAlias)
						.JoinAlias(() => reviewAlias.ClientReview, () => reportAlias)
						.Where(() => reviewAlias.ReviewerUserId == forUserId && reportAlias.Visible && reviewAlias.DeleteTime == null)
						.RowCount();
				}
			}
		}

		public List<ReviewModel> GetReviewsWithVisibleReports(UserOrganizationModel caller, long forUserId, int page, int pageCount) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetReviewsForUserWithVisibleReports(s, perms, caller, forUserId, page, pageCount);
				}
			}
		}

		public static List<ReviewModel> GetReviewForUser_SpecificAnswers(ISession s, PermissionsUtility perms, long userId, long reviewContainerId, List<long> askableIds) {
			perms.ViewUserOrganization(userId, true);

			var user = s.Get<UserOrganizationModel>(userId);
			var usersIds = user.UserIds;
			var reviews = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId)
								.WhereRestrictionOn(x => x.ReviewerUserId).IsIn(usersIds)
								.Future();

			var allAnswers = s.QueryOver<AnswerModel>()
				.Where(x => x.ForReviewContainerId == reviewContainerId && /*x.ByUserId == forUserId &&*/ x.DeleteTime == null)
				.WhereRestrictionOn(x => x.ReviewerUserId).IsIn(usersIds)
				.WhereRestrictionOn(x => x.Askable.Id).IsIn(askableIds)
				.Future();

			var reviewsResolved = reviews.ToList();
			var allAnswersResolved = allAnswers.ToList();
			for (int i = 0; i < reviewsResolved.Count; i++)
				PopulateAnswers(/*s,*/ reviewsResolved[i], allAnswersResolved);
			return reviewsResolved;
		}

		public static List<ReviewModel> GetReviewForUser(ISession s, PermissionsUtility perms, long userId, long reviewContainerId) {
			perms.ViewUserOrganization(userId, true);

			var user = s.Get<UserOrganizationModel>(userId);
			var usersIds = user.UserIds;

			var reviews = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId)
								.WhereRestrictionOn(x => x.ReviewerUserId).IsIn(usersIds)
								.Future();

			var allAnswers = s.QueryOver<AnswerModel>()
				.Where(x => x.ForReviewContainerId == reviewContainerId && /*x.ByUserId == forUserId &&*/ x.DeleteTime == null)
				.WhereRestrictionOn(x => x.ReviewerUserId).IsIn(usersIds)
				.Future();
			var reviewsResolved = reviews.ToList();
			var allAnswersResolved = allAnswers.ToList();
			for (int i = 0; i < reviewsResolved.Count; i++)
				PopulateAnswers(/*s,*/ reviewsResolved[i], allAnswersResolved);
			return reviewsResolved;

		}

		public List<ReviewModel> GetReviewForUser(UserOrganizationModel caller, long userId, long reviewContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetReviewForUser(s, perms, userId, reviewContainerId);
				}
			}
		}



		public List<ReviewModel> GetReviewsForUser(UserOrganizationModel caller, long forUserId, int page, int pageCount, DateTime dueAfter, bool includeAllUserOrgs = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetReviewsForUser(s, perms, caller, forUserId, page, pageCount, dueAfter, false, includeAllUserOrgs);
				}
			}
		}

		public List<ReviewModel> GetReviewsForReviewContainer(UserOrganizationModel caller, long reviewContainerId, bool includeUser) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, caller.Organization.Id).ViewReviews(reviewContainerId, false);
					var query = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null);
					if (includeUser) {
						query = query.Fetch(x => x.ReviewerUser.User).Eager;
					}
					return query.List().ToList();
				}
			}
		}

		public static ReviewModel GetReview(ISession s, PermissionsUtility perms, long reviewId, bool includeReviewContainer = false, bool populate = true) {
			var reviewPopulated = s.Get<ReviewModel>(reviewId);

			perms.ViewUserOrganization(reviewPopulated.ReviewerUserId, true, PermissionType.ViewReviews)
				.ViewReview(reviewId);
			if (populate) {
				var allAnswers = s.QueryOver<AnswerModel>()
									.Where(x => x.ForReviewId == reviewId && x.DeleteTime == null)
									.List().ToListAlive();

				var allAlive = UserAccessor.WasAliveAt(s, allAnswers.Select(x => x.RevieweeUserId).Distinct().ToList(), reviewPopulated.DueDate);
				allAnswers = allAnswers.Where(x => allAlive.Contains(x.RevieweeUserId)).ToList();
				PopulateAnswers(/*s,*/ reviewPopulated, allAnswers);
			}
			if (includeReviewContainer) {
				reviewPopulated.ForReviewContainer = s.Get<ReviewsModel>(reviewPopulated.ForReviewContainerId);
			}

			return reviewPopulated;
		}

		public ReviewModel GetReview(UserOrganizationModel caller, long reviewId, bool includeReviewContainer = false, bool populate = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var perms = PermissionsUtility.Create(s, caller);
					return GetReview(s, perms, reviewId, includeReviewContainer, populate);
				}
			}
		}

		public List<AnswerModel> GetDistinctQuestionsAboutUserFromReview(UserOrganizationModel caller, long userOrgId, long reviewContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userOrgId, false);

					return s.QueryOver<AnswerModel>()
						.Where(x => x.DeleteTime == null && x.RevieweeUserId == userOrgId && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
						.List().ToListAlive().Distinct(x => x.Askable.Id).ToList();
				}
			}
		}
		public List<AnswerModel_TinyScatterPlot> GetAnswersForUserReview_Scatter(UserOrganizationModel caller, long userOrgId, long reviewContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userOrgId, true, PermissionType.ViewReviews);
					Askable ask = null;
					QuestionCategoryModel c = null;
					var gwcAnswers = s.QueryOver<GetWantCapacityAnswer>()
										.Where(x => x.RevieweeUserId == userOrgId && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
										.JoinAlias(x => x.Askable, () => ask)
										.Select(x => x.ReviewerUserId, x => x.AboutType, x => ask.Weight, x => x.GetIt, x => x.WantIt, x => x.HasCapacity)
										.Future<object[]>();
					var valueAnswers = s.QueryOver<CompanyValueAnswer>()
										.Where(x => x.RevieweeUserId == userOrgId && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
										.JoinAlias(x => x.Askable, () => ask)
										.Select(x => x.ReviewerUserId, x => x.AboutType, x => ask.Weight, x => x.Exhibits)
										.Future<object[]>();

					var o = new List<AnswerModel_TinyScatterPlot>();

					foreach (var a in gwcAnswers.ToList()) {
						//var type = (Type)a[3];
						var get = (FiveState)a[3];
						var want = (FiveState)a[4];
						var cap = (FiveState)a[5];

						o.Add(new AnswerModel_TinyScatterPlot() {
							ByUserId = (long)a[0],
							AboutType = (AboutType)a[1],
							Weight = (WeightType)a[2],
							Score = ChartsEngine.ScatterScorer.ScoreRole(get.Ratio(), want.Ratio(), cap.Ratio()),
							Axis = ChartsEngine.ScatterScorer.ROLE_CATEGORY,

						});
					}
					foreach (var a in valueAnswers.ToList()) {
						//var type = (Type)a[3];
						var pnn = (PositiveNegativeNeutral)a[3];

						o.Add(new AnswerModel_TinyScatterPlot() {
							ByUserId = (long)a[0],
							AboutType = (AboutType)a[1],
							Weight = (WeightType)a[2],
							Score = ChartsEngine.ScatterScorer.ScoreValue(pnn),
							Axis = ChartsEngine.ScatterScorer.VALUE_CATEGORY,

						});
					}

					var userIds = o.Select(x => x.ByUserId).Distinct().ToArray();

					var ulu = s.QueryOver<UserLookup>()
						.WhereRestrictionOn(x => x.UserId).IsIn(userIds)
						.Select(x => x.UserId, x => x.Name, x => x._ImageUrlSuffix, x => x.Positions)
						.List<object[]>()
						.ToDictionary(x => (long)x[0], x => new UserLookup {
							UserId = (long)x[0],
							Name = (string)x[1],
							_ImageUrlSuffix = (string)x[2],
							Positions = (string)x[3]
						});

					foreach (var a in o) {
						UserLookup ot = null;
						if (ulu.TryGetValue(a.ByUserId, out ot)) {
							a.ByUserName = ot.Name;
							a.ByUserImage = ot.ImageUrl();
							a.ByUserPosition = ot.Positions;
						}
					}

					return o;
				}
			}
		}

		public List<AnswerModel> GetAnswersForUserReview(UserOrganizationModel caller, long userOrgId, long reviewContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userOrgId, true, PermissionType.ViewReviews);

					var answers = s.QueryOver<AnswerModel>()
										.Where(x => x.RevieweeUserId == userOrgId && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
										.List()
										.ToListAlive();
                    answers = FilterOutDuplicates(answers).ToList();

					return answers;
				}
			}
		}

		public List<AnswerModel> GetAnswersAboutUser(UserOrganizationModel caller, long userOrgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userOrgId, true);

					var answers = s.QueryOver<AnswerModel>()
										.Where(x => x.RevieweeUserId == userOrgId && x.DeleteTime == null)
										.List()
										.ToListAlive();
					return answers;
				}
			}
		}

		public List<AnswerModel> GetReviewContainerAnswers(UserOrganizationModel caller, long reviewContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);

					PermissionsUtility.Create(s, caller).ViewOrganization(reviewContainer.Organization.Id);

					var answers = s.QueryOver<AnswerModel>()
										.Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
										.List()
										.ToListAlive();
                    answers = FilterOutDuplicates(answers).ToList();

					return answers;
				}
			}
		}

		public ClientReviewModel GetClientReview(UserOrganizationModel caller, long reviewId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewReview(reviewId);
					return s.QueryOver<ClientReviewModel>().Where(x => x.ReviewId == reviewId).SingleOrDefault();
				}
			}
		}
		
		public long GetNumberOfReviewsForOrganization(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
					var count = s.QueryOver<ReviewsModel>()
												.Where(x => x.Organization.Id == organizationId && x.DeleteTime == null)
												.RowCountInt64();
					return count;
				}
			}
		}


		public System.Tuple<List<ReviewsModel>, List<ReviewModel>> GetUsefulReviewFaster(UserOrganizationModel caller, long userId, DateTime afterTime) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditUserOrganization(userId);

					var user = s.Get<UserOrganizationModel>(userId);
					var orgId = user.Organization.Id;
					
					var reviews = s.QueryOver<ReviewModel>().Where(reviewAlias => reviewAlias.DueDate > afterTime && reviewAlias.DeleteTime == null && reviewAlias.ReviewerUserId == userId).List().ToList();
					var reviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.DueDate > afterTime && x.DeleteTime == null && x.OrganizationId == orgId).List().ToList();
					
					var outputContainers = new List<ReviewsModel>();

					foreach (var r in reviews) {
						r.ForReviewContainer = reviewContainers.FirstOrDefault(x => x.Id == r.ForReviewContainerId);						
					}
					return Tuple.Create(reviewContainers, reviews);
				}
			}

		}

		public List<ReviewModel> GetUsefulReview(UserOrganizationModel caller, long userId, DateTime afterTime) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditUserOrganization(userId);

					var user = s.Get<UserOrganizationModel>(userId);
					var orgId = user.Organization.Id;

					ReviewModel reviewAlias = null;
					ReviewsModel reviewsAlias = null;

					var reviews = s.QueryOver<ReviewModel>(() => reviewAlias)
						.JoinAlias(() => reviewAlias.ForReviewContainer, () => reviewsAlias)
						.Where(() => reviewsAlias.DueDate > afterTime && reviewsAlias.OrganizationId == orgId && reviewsAlias.DeleteTime == null)
						.List().ToList();
					
					return reviews;									
				}
			}

		}

				

		public List<ReviewsModel> GetReviewsForOrganization(UserOrganizationModel caller,
			long organizationId, bool populateAnswers, bool populateReport, bool populateReviews, int pageSize, int page, DateTime afterTime) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
					var reviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.DueDate > afterTime && x.Organization.Id == organizationId && x.DeleteTime == null)
												.OrderBy(x => x.DateCreated).Desc
												.Skip(page * pageSize)
												.Take(pageSize)
												.List().ToList();

					//Completion should be much faster than getting all the reviews...

					foreach (var rc in reviewContainers) {
						PopulateReviewContainerCompletion(s, rc);
					}

					if (populateAnswers || populateReport || populateReviews) {
						foreach (var rc in reviewContainers) {
							PopulateReviewContainer(s.ToQueryProvider(true), rc, populateAnswers, populateReport);
						}
					}
					return reviewContainers;
				}
			}
		}

		public List<UserOrganizationModel> GetUsersInReview(UserOrganizationModel caller, long reviewContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
					PermissionsUtility.Create(s, caller)
						.ManagerAtOrganization(caller.Id, reviewContainer.Organization.Id)
						.ViewReviews(reviewContainerId, false);

					var reviewUsers = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId)
						.Fetch(x => x.ReviewerUser).Default.List().ToList().Select(x => x.ReviewerUser).ToList();
					return reviewUsers;
				}
			}
		}

		public ReviewContainerStats GetReviewStats(UserOrganizationModel caller, long reviewContainerId) {
			var output = new ReviewContainerStats(reviewContainerId);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewReviews(reviewContainerId, false);

					try {
						var reviewData = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).Select(
								Projections.RowCount(),
								Projections.Sum(
									Projections.Conditional(
										Restrictions.Where<ReviewModel>(x => x.Started && !x.Complete),
										Projections.Constant(1), Projections.Constant(0))),
								Projections.Sum(
									Projections.Conditional(
										Restrictions.Where<ReviewModel>(x => x.Complete),
										Projections.Constant(1), Projections.Constant(0))),
								Projections.Sum(
									Projections.Conditional(
										Restrictions.Where<ReviewModel>(x => x.Started && !x.Complete),
										Projections.Constant(1), Projections.Constant(0)))
								).Future<object>();
						
						var clientData = s.QueryOver<ClientReviewModel>().Where(x => x.ReviewContainerId == reviewContainerId).Select(
								Projections.Sum(
									Projections.Conditional(
										Restrictions.Where<ClientReviewModel>(x =>
											!x.Visible && x.SignedTime == null &&
											(x.IncludeManagerFeedback || x.IncludeManagerFeedback || x.IncludeQuestionTable ||
											x.IncludeSelfFeedback || x.IncludeEvaluation || x.IncludeScatterChart || x.IncludeTimelineChart || x.ManagerNotes != null)),
										Projections.Constant(1), Projections.Constant(0))),
								Projections.Sum(
									Projections.Conditional(
										Restrictions.Where<ClientReviewModel>(x => x.Visible && x.SignedTime == null),
										Projections.Constant(1), Projections.Constant(0))),
								Projections.Sum(
									Projections.Conditional(
										Restrictions.Where<ClientReviewModel>(x => x.SignedTime != null),
										Projections.Constant(1), Projections.Constant(0)))
								).Future<object>();


						var durationAvg = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).Where(x => x.DurationMinutes != null)
							.Select(Projections.Avg(Projections.Property<ReviewModel>(x => x.DurationMinutes))).FutureValue<double>();

						var answersData = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).Select(
								Projections.RowCount(),
								Projections.Sum(
									Projections.Conditional(
										Restrictions.Where<AnswerModel>(x => x.Complete),
										Projections.Constant(1), Projections.Constant(0))),
								Projections.Sum(
									Projections.Conditional(
										Restrictions.Where<AnswerModel>(x => x.Complete && !x.Required),
										Projections.Constant(1), Projections.Constant(0)))
								).Future<object>();

						var reviewDataList = (object[])reviewData.ToList()[0];

						var totalReviews = (int)reviewDataList[0];
						var startedReviews = (int)reviewDataList[1];
						var finishedReviews = (int)reviewDataList[2];


						var unstartedReviews = totalReviews - startedReviews - finishedReviews;
						output.Completion = new ReviewContainerStats.CompletionStats {
							Total = totalReviews,
							Started = startedReviews,
							Finished = finishedReviews,
							Unstarted = unstartedReviews
						};

						//Reports 
						var reportsDataList = (object[])clientData.ToList()[0];
						var startedReports = (int)(reportsDataList[0] ?? 0);//clientReports.Where(x => x.Started() && !x.Visible && x.SignedTime == null).Count();
						var visibleReports = (int)(reportsDataList[1] ?? 0); //clientReports.Where(x => x.Visible && x.SignedTime == null).Count();
						var signedReports = (int)(reportsDataList[2] ?? 0);//clientReports.Where(x => x.SignedTime != null).Count();
						var unstarted = totalReviews - visibleReports - signedReports - startedReports;

						output.Reports = new ReviewContainerStats.ReportStats {
							Total = totalReviews,
							Unstarted = unstarted,
							Started = startedReports,
							Visible = visibleReports,
							Signed = signedReports
						};

						var answersDataList = (object[])answersData.ToList()[0];
						var totalQs = (int)answersDataList[0];
						var completedQs = (int)answersDataList[1];
						var optionalCompletedQs = (int)answersDataList[2];
						output.Stats = new ReviewContainerStats.OverallStats {
							MinsPerReview = (decimal)durationAvg.Value,
							OptionalsAnswered = optionalCompletedQs,
							QuestionsAnswered = completedQs,
							ReviewsCompleted = finishedReviews,
							TotalQuestions = totalQs,
							//NumberOfPeopleTakingReview = numberOfPeopleTakingReviews,
							//NumberOfUniqueMatches = numberOfUniqueMatches,

						};


					} catch (Exception) {
						//var i = 0;
					}
				}
			}
			return output;
		}

		public ReviewContainerStats GetReviewStats1(UserOrganizationModel caller, long reviewContainerId) {
			var output = new ReviewContainerStats(reviewContainerId);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewReviews(reviewContainerId, false);

					try {
						//var multiCriteria = MultiCriteria.Create(s);

						MultiCriteria multiCriteria = MultiCriteria.Create(s);

						//Reviews
						//var matchingReview = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null);
						multiCriteria.AddInt(s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).ToRowCountQuery());//Total
						multiCriteria.AddInt(s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).Where(x => x.Started && !x.Complete).ToRowCountQuery());//Started
						multiCriteria.AddInt(s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).Where(x => x.Complete).ToRowCountQuery());//Finished
																																																  //Reports
						ClientReviewModel crmAlias = null;
						var matchingClientReview = s.QueryOver(() => crmAlias).WithSubquery.WhereExists(
										QueryOver.Of<ReviewModel>()
											.Where(e => e.DeleteTime == null && e.ForReviewContainerId == reviewContainerId) // filter
											.Where(d => d.ClientReview.Id == crmAlias.Id) // match
											.Select(d => d.Id) // return something
										);
						multiCriteria.Add(matchingClientReview);
						
						multiCriteria.Add<ReviewModel, decimal>(s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).Where(x => x.DurationMinutes != null)
							.Select(Projections.Property<ReviewModel>(x => x.DurationMinutes)
							//Projections.Count<ReviewModel>(x=>x.Id)
							));

						//Answers
						multiCriteria.AddInt(s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).ToRowCountQuery());//Total
						multiCriteria.AddInt(s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).Where(x => x.Complete).ToRowCountQuery());//Complete
						multiCriteria.AddInt(s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).Where(x => x.Complete && !x.Required).ToRowCountQuery());//Optional
											
						//Query        
						var result = multiCriteria.Execute();

						int totalReviews = result.Get<int>();
						int startedReviews = result.Get<int>();
						int finishedReviews = result.Get<int>();
						List<ClientReviewModel> clientReports = result.GetList<ClientReviewModel>();
						var avgList = result.GetList<decimal>();
						decimal? avgDuration = null;
						if (avgList.Any())
							avgDuration = avgList.Average();
						
						// double avgDuration = ((TimeSpan)avgDurations[0]).TotalMinutes / ((double)avgDurations[1]);
						int totalQs = result.Get<int>();
						int completedQs = result.Get<int>();
						int optionalCompletedQs = result.Get<int>();
						

						//Completion 
						int unstartedReviews = totalReviews - startedReviews - finishedReviews;
						output.Completion = new ReviewContainerStats.CompletionStats {
							Total = totalReviews,
							Started = startedReviews,
							Finished = finishedReviews,
							Unstarted = unstartedReviews
						};

						//Reports 
						var startedReports = clientReports.Where(x => x.Started() && !x.Visible && x.SignedTime == null).Count();
						var visibleReports = clientReports.Where(x => x.Visible && x.SignedTime == null).Count();
						var signedReports = clientReports.Where(x => x.SignedTime != null).Count();
						var unstarted = totalReviews - visibleReports - signedReports - startedReports;

						output.Reports = new ReviewContainerStats.ReportStats {
							Total = totalReviews,
							Unstarted = unstarted,
							Started = startedReports,
							Visible = visibleReports,
							Signed = signedReports
						};

						output.Stats = new ReviewContainerStats.OverallStats {
							MinsPerReview = avgDuration,
							OptionalsAnswered = optionalCompletedQs,
							QuestionsAnswered = completedQs,
							ReviewsCompleted = finishedReviews,
							TotalQuestions = totalQs,
							//NumberOfPeopleTakingReview = numberOfPeopleTakingReviews,
							//NumberOfUniqueMatches = numberOfUniqueMatches,

						};
					} catch (Exception e) {
						throw e;
					}

					//

					return output;
				}
			}

		}

		public ReviewsModel GetReviewContainer(UserOrganizationModel caller, long reviewContainerId, bool populateAnswers, bool populateReport, bool sensitive = true, bool populateReview = false, bool deduplicate = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);

					/*if (reviewContainer.PeriodId != null)
						reviewContainer.Period = s.Get<PeriodModel>(reviewContainer.PeriodId);
					if (reviewContainer.NextPeriodId != null)
						reviewContainer.NextPeriod = s.Get<PeriodModel>(reviewContainer.NextPeriodId);
                    */
					var a = reviewContainer.Organization.Settings.Branding;

					if (sensitive)
						perms.ViewReviews(reviewContainerId, false);
					else
						perms.ViewOrganization(reviewContainer.OrganizationId);

					if (populateAnswers || populateReport) {
						PopulateReviewContainer(s.ToQueryProvider(true), reviewContainer, populateAnswers, populateReport);
					}

					if (reviewContainer.Reviews == null && populateReview) {
						reviewContainer.Reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).List().ToList();
						if (deduplicate)
							reviewContainer.Reviews = reviewContainer.Reviews.GroupBy(x => x.ReviewerUserId).Select(x => x.First()).ToList();
					}

					return reviewContainer;
				}
			}
		}

		#endregion


		public List<ReviewsModel> GetMostRecentReviewContainer(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
					var user = s.Get<UserOrganizationModel>(userId);
					var orgId = user.Organization.Id;
					var review = s.QueryOver<ReviewModel>().Where(x => x.ReviewerUserId == userId && x.DeleteTime == null).OrderBy(x => x.DueDate).Desc.Select(x => x.ForReviewContainerId).List<long>().Distinct().ToList();
					//review.Distinct();
					return s.QueryOver<ReviewsModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(review).List().ToList();

					/*if (review.Any())
						return null;
					return s.Get<ReviewsModel>(review.ForReviewsId);*/
				}
			}
		}

		public LongTuple GetChartTuple(UserOrganizationModel caller, long reviewId, long chartTupleId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewReview(reviewId);

					var review = s.Get<ReviewModel>(reviewId);

					//Tuple
					var tuple = review.ClientReview.Charts.FirstOrDefault(x => x.Id == chartTupleId);

					if (tuple == null && review.ClientReview.ScatterChart.NotNull(x => x.Id == chartTupleId))
						tuple = review.ClientReview.ScatterChart;
					if (tuple == null)
						throw new PermissionsException("The chart you requested does not exist");
					return tuple;
				}
			}
		}

		public ReviewsModel GetReviewContainerByReviewId(UserOrganizationModel caller, long clientReviewId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var clientReview = s.Get<ReviewModel>(clientReviewId);
					var reviewsId = clientReview.ForReviewContainerId;
					PermissionsUtility.Create(s, caller).ViewReviews(reviewsId, false);
					var review = s.Get<ReviewsModel>(reviewsId);

					return review;
				}
			}
		}

		public static ReviewsModel GetReviewContainer(AbstractQuery abstractQuery, PermissionsUtility perms, long reviewContainerId) {
			perms.ViewReviews(reviewContainerId, false);
			return abstractQuery.Get<ReviewsModel>(reviewContainerId);
		}


		public static List<ReviewsModel> OutstandingReviewsForOrganization_Unsafe(ISession s, long orgId) {
			return s.QueryOver<ReviewsModel>().Where(x => x.DeleteTime == null && x.OrganizationId == orgId && x.DueDate > DateTime.UtcNow).List().ToList();
			//return s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null ).WhereRestrictionOn(x=>x.ForReviewsId).IsIn(found).List().ToList();
		}

		private static List<Reviewee> PostprocessesReviewees_Unsafe(IEnumerable<Reviewee> reviewees, OrganizationModel org) {
			var output = reviewees.ToList();
			output.Add(new Reviewee(org.Id, null, org.GetName()) {
				Type = OriginType.Organization
			});

			return output.OrderByDescending(x => x.Type).ThenBy(x => x._Name).ToList();
		}

		public static List<Reviewee> GetPossibleOrganizationReviewees(UserOrganizationModel caller, long organizationId, DateRange range, bool includeTeamsAndPositions = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var perms = PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);

					var org = s.Get<OrganizationModel>(organizationId);
					var tree = AccountabilityAccessor.GetTree(s, perms, org.AccountabilityChartId, range: range);
					var allUsers = TinyUserAccessor.GetOrganizationMembers(s, perms, organizationId, true);

					var treeNodes = AngularTreeUtil.GetAllNodes(tree.Root).Where(x => x.User != null).Select(x => new Reviewee(x)).ToList();
					var output = new List<Reviewee>();
					output.AddRange(treeNodes);
					foreach (var user in allUsers.Where(user => !treeNodes.Any(tn => tn.RGMId == user.UserOrgId))) {
						output.Add(new Reviewee(user.UserOrgId, null, user.GetName()));
					}

					if (includeTeamsAndPositions) {
						var allAvail = s.QueryOver<ResponsibilityGroupModel>().Where(x => 
							x.DeleteTime == null && x.Organization.Id == organizationId &&
							x.GetType() != typeof(UserOrganizationModel) && x.GetType() != typeof(OrganizationModel)
						).List().ToList();

						output.AddRange(allAvail.Select(x => new Reviewee(x.Id, null, x.GetName()) {Type = x.GetOrigin()}));
					}

					return PostprocessesReviewees_Unsafe(output, org);
				}
			}
		}

		public static List<Reviewee> GetAllPossibleReviewees(UserOrganizationModel caller, long organizationId, DateRange range) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					//var team = s.Get<OrganizationTeamModel>(teamId);
					var perms = PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);

					var org = s.Get<OrganizationModel>(organizationId);
					var tree = AccountabilityAccessor.GetTree(s, perms, org.AccountabilityChartId, range: range);
					//var teamIds = TeamAccessor.GetTeamMemberIds(s, perms, teamId);
					//.Where(x => teamIds.Contains(x.User.Id))

					var reviewees = AngularTreeUtil.GetAllNodes(tree.Root).Where(x => x.User != null)
						.Select(x => new Reviewee(x))
						.ToList();

					var allUsers = TinyUserAccessor.GetOrganizationMembers(s, perms, organizationId, true);

					var additional = allUsers.Where(x => !reviewees.Any(reviewee => reviewee.RGMId == x.UserOrgId))
											 .Select(x=>new Reviewee(x.UserOrgId,null,x.GetName()))
											 .ToList();

					reviewees.AddRange(additional);

					return PostprocessesReviewees_Unsafe(reviewees, org);
				}
			}
		}

		public static List<Reviewee> GetReviewees(UserOrganizationModel caller, long reviewContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewReviews(reviewContainerId, false);

					var criteria = s.CreateCriteria(typeof(AnswerModel));
					criteria.Add(Restrictions.Conjunction()
						.Add(Restrictions.Eq(Projections.Property<AnswerModel>(x => x.ForReviewContainerId), reviewContainerId))
						.Add(Restrictions.IsNull(Projections.Property<AnswerModel>(x => x.DeleteTime)))
					);
					criteria.SetProjection(
						Projections.Distinct(Projections.ProjectionList()
							.Add(Projections.Property<AnswerModel>(x => x.RevieweeUserId))
							.Add(Projections.Property<AnswerModel>(x => x.RevieweeUser_AcNodeId))
							)
						);

					//criteria.SetResultTransformer(new AliasToBeanResultTransformer(typeof(AnswerModel)));

					var distinct = criteria.List();
					var reviewees = new List<Reviewee>();
					foreach (var d in distinct.Cast<object[]>()) {//.RevieweeUserId  .RevieweeUser_AcNodeId
						reviewees.Add(new Reviewee((long)d[0], (long?)d[1]));
					}

					return reviewees;
				}
			}
		}
	}
}