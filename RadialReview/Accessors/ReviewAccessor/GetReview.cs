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

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {
		public static List<ReviewModel> GetReviewsForUserWithVisibleReports(ISession s, PermissionsUtility perms, UserOrganizationModel caller, long forUserId, int page, int pageCount) {
			perms.ViewUserOrganization(forUserId, true);
			List<ReviewModel> reviews;
			ClientReviewModel clientReviewAlias = null;
			ReviewModel reviewAlias = null;
			reviews = s.QueryOver<ReviewModel>(() => reviewAlias)
								.Left.JoinAlias(() => reviewAlias.ClientReview, () => clientReviewAlias)
								.Where(() => clientReviewAlias.Visible == true && reviewAlias.ForUserId == forUserId && reviewAlias.DeleteTime == null)
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
								.WhereRestrictionOn(x => x.ForUserId).IsIn(usersIds)
								.OrderBy(x => x.CreatedAt)
								.Desc.Skip(page * pageCount)
								.Take(pageCount)
								//.Fetch(x => x.Answers).Eager
								//add reviewModel Id to answers, query for that
								.List().ToList();
			if (includeAnswers) {
				var allAnswers = s.QueryOver<AnswerModel>()
					.Where(x => /*x.ByUserId == forUserId &&*/ x.DeleteTime == null)
					.WhereRestrictionOn(x => x.ByUserId).IsIn(usersIds)
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
						.Where(x => x.ForUserId == forUserId && x.DeleteTime == null)
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
						.Where(() => reviewAlias.ForUserId == forUserId && reportAlias.Visible && reviewAlias.DeleteTime == null)
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
			var reviews = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForReviewsId == reviewContainerId)
								.WhereRestrictionOn(x => x.ForUserId).IsIn(usersIds)
								.Future();

			var allAnswers = s.QueryOver<AnswerModel>()
				.Where(x => x.ForReviewContainerId == reviewContainerId && /*x.ByUserId == forUserId &&*/ x.DeleteTime == null)
				.WhereRestrictionOn(x => x.ByUserId).IsIn(usersIds)
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

			var reviews = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForReviewsId == reviewContainerId)
								.WhereRestrictionOn(x => x.ForUserId).IsIn(usersIds)
								.Future();

			var allAnswers = s.QueryOver<AnswerModel>()
				.Where(x => x.ForReviewContainerId == reviewContainerId && /*x.ByUserId == forUserId &&*/ x.DeleteTime == null)
				.WhereRestrictionOn(x => x.ByUserId).IsIn(usersIds)
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
					var query = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null);
					if (includeUser) {
						query = query.Fetch(x => x.ForUser.User).Eager;
					}
					return query.List().ToList();
				}
			}
		}

		public static ReviewModel GetReview(ISession s, PermissionsUtility perms, long reviewId, bool includeReviewContainer = false, bool populate = true) {
			var reviewPopulated = s.Get<ReviewModel>(reviewId);

			perms.ViewUserOrganization(reviewPopulated.ForUserId, true, PermissionType.ViewReviews)
				.ViewReview(reviewId);
			if (populate) {
				var allAnswers = s.QueryOver<AnswerModel>()
									.Where(x => x.ForReviewId == reviewId && x.DeleteTime == null)
									.List().ToListAlive();
				var allAlive = UserAccessor.WasAliveAt(s, allAnswers.Select(x => x.AboutUserId).Distinct().ToList(), reviewPopulated.DueDate);
				allAnswers = allAnswers.Where(x => allAlive.Contains(x.AboutUserId)).ToList();
				PopulateAnswers(/*s,*/ reviewPopulated, allAnswers);
			}
			if (includeReviewContainer) {
				reviewPopulated.ForReviewContainer = s.Get<ReviewsModel>(reviewPopulated.ForReviewsId);
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
						.Where(x => x.DeleteTime == null && x.AboutUserId == userOrgId && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
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
										.Where(x => x.AboutUserId == userOrgId && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
										.JoinAlias(x => x.Askable, () => ask)
										.Select(x => x.ByUserId, x => x.AboutType, x => ask.Weight, x => x.GetIt, x => x.WantIt, x => x.HasCapacity)
										.Future<object[]>();
					var valueAnswers = s.QueryOver<CompanyValueAnswer>()
										.Where(x => x.AboutUserId == userOrgId && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
										.JoinAlias(x => x.Askable, () => ask)
										.Select(x => x.ByUserId, x => x.AboutType, x => ask.Weight, x => x.Exhibits)
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
										.Where(x => x.AboutUserId == userOrgId && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
										.List()
										.ToListAlive();
					return answers;
				}
			}
		}

		public List<AnswerModel> GetAnswersAboutUser(UserOrganizationModel caller, long userOrgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userOrgId, true);

					var answers = s.QueryOver<AnswerModel>()
										.Where(x => x.AboutUserId == userOrgId && x.DeleteTime == null)
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

					PermissionsUtility.Create(s, caller).ViewOrganization(reviewContainer.ForOrganization.Id);

					var answers = s.QueryOver<AnswerModel>()
										.Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
										.List()
										.ToListAlive();
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
		/*
		public List<ReviewsModel> GetReviewsCreatedByUser(UserOrganizationModel caller, long userOrganizationId, bool populate)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditReviews(userOrganizationId);
					var reviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.CreatedById == userOrganizationId).List().ToList();
					if (populate)
					{
						foreach (var rc in reviewContainers)
						{
							PopulateReviewContainer(s, rc);
						}
					}
					return reviewContainers;
				}
			}
		}*/

		public long GetNumberOfReviewsForOrganization(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
					var count = s.QueryOver<ReviewsModel>()
												.Where(x => x.ForOrganization.Id == organizationId && x.DeleteTime == null)
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

					//ReviewModel reviewAlias = null;
					//ReviewsModel reviewsAlias = null;

					var reviews = s.QueryOver<ReviewModel>().Where(reviewAlias => reviewAlias.DueDate > afterTime && reviewAlias.DeleteTime == null && reviewAlias.ForUserId == userId).List().ToList();
					var reviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.DueDate > afterTime && x.DeleteTime == null && x.ForOrganizationId == orgId).List().ToList();


					var outputContainers = new List<ReviewsModel>();

					foreach (var r in reviews) {
						r.ForReviewContainer = reviewContainers.FirstOrDefault(x => x.Id == r.ForReviewsId);
						//if (r.ForReviewContainer!=null)
						//	outputContainers.Add(r.ForReviewContainer);
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
						.Where(() => reviewsAlias.DueDate > afterTime && reviewsAlias.ForOrganizationId == orgId && reviewsAlias.DeleteTime == null)
						.List().ToList();
					/*.Fetch(x=>x.ForReviewContainer).Eager
					.Where(x => x.ForReviewContainer.DueDate > afterTime && x.ForReviewContainer.ForOrganizationId == orgId)
					//.JoinQueryOver(x => x.ForReviewContainer)
					//.Where(x => x.DueDate > afterTime && x.ForOrganizationId == user.Organization.Id)
					.List().ToList();*/
					return reviews;

					/*
					var part = s.Query<ReviewsModel>().Where(x => x.DueDate > afterTime).ToList();
					foreach (var p in part)
					{
						var one = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == p.Id).List().ToList();
					}

					var dsm= s.Query<DeepSubordinateModel>().Where(x=>x.ManagerId==userId && x.DeleteTime==null).ToList();
					var ann= s.Query<DeepSubordinateModel>().Where(x=>x.ManagerId==userId && x.DeleteTime==null)
						.Join(s.Query<ReviewModel>(),x=>x.SubordinateId,x=>x.ForUserId,(d,r)=>r).ToList();

					var aaa = s.Query<ReviewModel>().Join(s.Query<DeepSubordinateModel>().Where(x => x.ManagerId == userId), x => x.ForUserId, x => x.SubordinateId, (r, ss) => r).ToList();


					var allReviews=s.Query<DeepSubordinateModel>()
						.Where(x=>x.ManagerId==userId && x.DeleteTime==null)
						.Join(s.Query<ReviewModel>(),x=>x.SubordinateId,x=>x.ForUserId,(d,r)=>r)
						.Join(s.Query<ReviewsModel>(),x=>x.ForReviewsId,x=>x.Id,(rr,rs)=>new {rr,rs})
						.ToList();
					return allReviews.Select(x => { x.rr.ForReviewContainer = x.rs; return x.rr; }).ToList();*/
				}
			}

		}
		/*
		public List<ReviewsModel> GetEditableReviews(UserOrganizationModel caller, long userId, DateTime afterTime, int page, int pageSize)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var user = s.Get<UserOrganizationModel>(userId);
					PermissionsUtility.Create(s, caller).EditUserOrganization(userId);

					if (user.ManagingOrganization)
					{
						return s.QueryOver<ReviewsModel>().Where(x => x.DueDate > afterTime && x.ForOrganizationId == user.Organization.Id).Skip(pageSize * page).Take(pageSize).List().ToList();
					}
					else
					{
						var output = s.Query<DeepSubordinateModel>()
							.Where(x => x.DeleteTime == null && x.ManagerId == userId)
							.Join(s.Query<ReviewsModel>(), x => x.SubordinateId, x => x.CreatedById, (x, z) => z)
							.Where(x => x.DueDate > afterTime)
							.Distinct(x => x.Id)
							.Skip(pageSize * page)
							.Take(pageSize)
							.ToList();
						return output;
					}

				}
			}
		}

		public List<ReviewsModel> GetVisibleReviews(UserOrganizationModel caller, long userId, DateTime afterTime, int page, int pageSize)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var user = s.Get<UserOrganizationModel>(userId);
					PermissionsUtility.Create(s,caller).ViewOrganization(user.Organization.Id);
					var output=s.Query<DeepSubordinateModel>()
						.Where(x=>x.DeleteTime==null && x.ManagerId==userId)
						.Join(s.Query<ReviewModel>(),x=>x.SubordinateId,x=>x.ForUserId,(x,y)=>y)
						.Join(s.Query<ReviewsModel>(), x => x.ForReviewsId, x => x.Id, (x, z) => z)
						.Where(x=>x.DueDate>afterTime)
						.Distinct(x=>x.Id)
						.Skip(pageSize*page)
						.Take(pageSize)
						.ToList();
					return output;
					/*
					var reviews = (
									from d in s.Query<DeepSubordinateModel>()
									where d.ManagerId == userId && d.DeleteTime==null
									join 

									/*
									from rs in s.Query<ReviewsModel>()
									where rs.DueDate > afterTime && rs.ForOrganizationId == user.Organization.Id
									join r in s.Query<ReviewModel>() on rs.Id equals r.ForReviewsId
									join d in s.Query<DeepSubordinateModel>() on r.ForUserId equals d.SubordinateId
									where d.ManagerId == userId
									select rs*
								  ).ToList();
					/*var reviews = s.Query<ReviewsModel>()
						.Where(x => x.DueDate > afterTime)
						.Join(s.Query<ReviewModel>(), x => x.Id, x => x.ForReviewsId, (rs, r) => new { rs, r })
						.Join(s.Query<DeepSubordinateModel>().Where(x => x.ManagerId == userId), item => item.r.ForUserId, x => x.SubordinateId, (rsr, d) => new { rsr, d })
						.Where(x => x.d.DeleteTime == null && x.d.ManagerId == userId)
						.Select(x => x.rsr.rs)
						.OrderByDescending(x => x.DateCreated)
						.Skip(page * pageSize)
						.Take(pageSize)
						.ToList();*/
		/*
		var result = ( from rs in s.Query<ReviewsModel>().j
					   join r in s.Query<ReviewModel>() on rs.Id equals r.ForReviewsId into n
					   join j in s.Query<DeepSubordinateModel>() on n.f 
					   where 
					   );*

		//s.QueryOver<DeepSubordinateModel>(()=>deepAlias).JoinAlias(()=>deepAlias.Subordinate,()=>subordinateAlias).JoinAlias(


		//tx.Commit();
		//s.Flush();
	}
}
}*/

		public List<ReviewsModel> GetReviewsForOrganization(UserOrganizationModel caller,
			long organizationId, bool populateAnswers, bool populateReport, bool populateReviews, int pageSize, int page, DateTime afterTime) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
					var reviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.DueDate > afterTime && x.ForOrganization.Id == organizationId && x.DeleteTime == null)
												.OrderBy(x => x.DateCreated).Desc
												.Skip(page * pageSize)
												.Take(pageSize)
												.List().ToList();

					//Completion should be much faster than getting all the reviews...

					foreach (var rc in reviewContainers) {
						PopulateReviewContainerCompletion(s, rc);
					}

					if (populateAnswers || populateReport || populateReviews) {
						/*
						var queryProvider = new IEnumerableQuery(true);

						var allOrgReviewQuery = s.QueryOver<ReviewModel>()
							.JoinQueryOver(x => x.ForUser)
							.Where(x => x.Organization.Id == organizationId);
						if(populateReport)
						{
							allOrgReviewQuery.Fetch(x=>x.ClientReview).Default.Future();
						}
						 var allOrgReview = allOrgReviewQuery.List().ToList();
						*/
						/*if (populateAnswers)
						{
							/*ReviewModel reviewsAlias = null;
							AnswerModel answerAlias = null;
							var answerQuery = s.QueryOver<AnswerModel>().JoinQueryOver(x=>x.ForReviewContainer).Where(x=>x.ForOrganizationId==organizationId).List().ToList();
												/*.Fetch(x => x.ForReviewContainer).Eager
												.Where(x => x.ForReviewContainer.ForOrganizationId == organizationId)
												.List().ToList();*
						   queryProvider.AddData(answerQuery);
						}

					   queryProvider.AddData(allOrgReview);*/

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
						.ManagerAtOrganization(caller.Id, reviewContainer.ForOrganization.Id)
						.ViewReviews(reviewContainerId, false);

					var reviewUsers = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForReviewsId == reviewContainerId).Fetch(x => x.ForUser).Default.List().ToList().Select(x => x.ForUser).ToList();
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
						var reviewData = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null).Select(
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
						/*ClientReviewModel crmAlias = null;
						var matchingClientReview = s.QueryOver(() => crmAlias).WithSubquery.WhereExists(
										QueryOver.Of<ReviewModel>()
											.Where(e => e.DeleteTime == null && e.ForReviewsId == reviewContainerId) // filter
											.Where(d => d.ClientReview.Id == crmAlias.Id) // match
											.Select(d => d.Id) // return something
										).Future<long>();*/
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


						var durationAvg = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null).Where(x => x.DurationMinutes != null)
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
						multiCriteria.AddInt(s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null).ToRowCountQuery());//Total
						multiCriteria.AddInt(s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null).Where(x => x.Started && !x.Complete).ToRowCountQuery());//Started
						multiCriteria.AddInt(s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null).Where(x => x.Complete).ToRowCountQuery());//Finished
																																														  //Reports
						ClientReviewModel crmAlias = null;
						var matchingClientReview = s.QueryOver(() => crmAlias).WithSubquery.WhereExists(
										QueryOver.Of<ReviewModel>()
											.Where(e => e.DeleteTime == null && e.ForReviewsId == reviewContainerId) // filter
											.Where(d => d.ClientReview.Id == crmAlias.Id) // match
											.Select(d => d.Id) // return something
										);
						multiCriteria.Add(matchingClientReview);

						//var avg = matchingReview.Where(x => x.Duration != null).Select(Projections.Avg<ReviewModel>(x => x.Duration)).SingleOrDefault<double>();
						//var match = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null).Where(x => x.Duration != null).List().ToList();

						multiCriteria.Add<ReviewModel, decimal>(s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null).Where(x => x.DurationMinutes != null)
							.Select(Projections.Property<ReviewModel>(x => x.DurationMinutes)
							//Projections.Count<ReviewModel>(x=>x.Id)
							));

						//Answers
						multiCriteria.AddInt(s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).ToRowCountQuery());//Total
						multiCriteria.AddInt(s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).Where(x => x.Complete).ToRowCountQuery());//Complete
						multiCriteria.AddInt(s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).Where(x => x.Complete && !x.Required).ToRowCountQuery());//Optional

						#region comment
						//Number reviewed
						/*var query = (from a in s.Query<AnswerModel>()
									where a.ForReviewContainerId == reviewContainerId && a.DeleteTime ==null
									group a by new { AboutUserId = a.AboutUserId, ByUserId = a.ByUserId} into grp);*/

						//s.Query<AnswerModel>().Where(a=>a.ForReviewContainerId == reviewContainerId && a.DeleteTime ==null).GroupBy(a=>new { AboutUserId = a.AboutUserId, ByUserId = a.ByUserId});

						/*
						multiCriteria.AddInt(s.QueryOver<AnswerModel>()
												.Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
												.UnderlyingCriteria.SetProjection(Projections.Group<AnswerModel>(x => x.AboutUserId),Projections.Group<AnswerModel>(x=>x.ByUserId))
												.
												.ToRowCountQuery()
											);*/
						/*multiCriteria.AddInt(s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
								.SelectList(x => x.SelectGroup(y => y.ByUserId)).ToRowCountQuery());*/
						/* var groupByAboutAndBy = Projections.ProjectionList();
						 groupByAboutAndBy.Add(Projections.Group<AnswerModel>(x => x.AboutUserId));
						 groupByAboutAndBy.Add(Projections.Group<AnswerModel>(x => x.ByUserId));
						 groupByAboutAndBy.Add(Projections.Property<AnswerModel>(x => x.Id));*/

						/*var uniqueMatches = s.CreateCriteria<AnswerModel>()
							.Add(Restrictions.Eq(Projections.Property<AnswerModel>(x=>x.ForReviewContainerId),reviewContainerId))                            
							.SetProjection(Projections.ProjectionList()
								.Add(Projections.RowCount())
								.Add(Projections.GroupProperty(Projections.Property<AnswerModel>(x => x.ByUserId)))
								.Add(Projections.GroupProperty(Projections.Property<AnswerModel>(x => x.AboutUserId)))
								);

						var peopleTakingReview = s.CreateCriteria<AnswerModel>()
							.Add(Restrictions.Eq(Projections.Property<AnswerModel>(x => x.ForReviewContainerId), reviewContainerId))
							.SetProjection(
							Projections.ProjectionList()
								.Add(Projections.RowCount())
								.Add(Projections.GroupProperty(Projections.Property<AnswerModel>(x => x.ByUserId)))
							);*/
						/*var peopleTakingReview = s.CreateCriteria<AnswerModel>()
							.Add(Restrictions.Eq(Projections.Property<AnswerModel>(x => x.ForReviewContainerId), reviewContainerId))
							.SetProjection(
							Projections.ProjectionList()
								.Add(Projections.RowCount())
								.Add(Projections.GroupProperty(Projections.Property<AnswerModel>(x => x.ByUserId)))
							);*/

						//s.CreateCriteria<AnswerModel>().SetProjection(Projections.RowCount()).sub
						//http://stackoverflow.com/questions/7318176/how-to-get-the-value-from-a-subquery-in-nhibernate
						/*var uniqueMatchesSubquery = DetachedCriteria.For<AnswerModel>()
								.Add(Restrictions.Eq(Projections.Property<AnswerModel>(x => x.ForReviewContainerId), reviewContainerId))
								.SetProjection(Projections.ProjectionList()
									.Add(Projections.GroupProperty(Projections.Property<AnswerModel>(x => x.ByUserId)))
									.Add(Projections.GroupProperty(Projections.Property<AnswerModel>(x => x.AboutUserId)))
								);
						var uniqueMatches = s.QueryOver<AnswerModel>().Select(
							Projections.SubQuery(uniqueMatchesSubquery)).ToRowCountQuery();


						var peopleTakingReviewSubquery = DetachedCriteria.For<AnswerModel>()
								.Add(Restrictions.Eq(Projections.Property<AnswerModel>(x => x.ForReviewContainerId), reviewContainerId))
								.SetProjection(Projections.ProjectionList()
									.Add(Projections.GroupProperty(Projections.Property<AnswerModel>(x => x.ByUserId)))
								);
						var peopleTakingReview = s.QueryOver<AnswerModel>().Select(
							Projections.SubQuery(uniqueMatchesSubquery)).ToRowCountQuery();*/

						/*var uniqueMatches = s.CreateQuery("SELECT a.Id FROM AnswerModel a WHERE a.ForReviewContainerId = :rcid group by a.AboutUserId, a.ByUserId")
							.SetParameter("rcid", reviewContainerId);
						var peopleTakingReview = s.CreateQuery("SELECT count(distinct a.ByUserId) FROM AnswerModel a WHERE a.ForReviewContainerId = :rcid")
							.SetParameter("rcid", reviewContainerId);
                        
						multiCriteria.Add(uniqueMatches);
						multiCriteria.Add(peopleTakingReview);*/



						//multiCriteria.AddInt(s.QueryOver<AnswerModel>().Select(groupByAboutAndBy).ToRowCountQuery());
						// multiCriteria.AddInt(s.QueryOver<AnswerModel>().Select(Projections.Group<AnswerModel>(x => x.ByUserId), Projections.Property<AnswerModel>(x => x.Id)).ToRowCountQuery());
						#endregion
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
						/*try{
							var dur = result.GetList<double>();
							if (dur[0] == null || dur[1] == 0)
								throw new Exception("Illegal");
							avgDuration = dur[0] / dur[1];
						}catch{

						}*/
						// double avgDuration = ((TimeSpan)avgDurations[0]).TotalMinutes / ((double)avgDurations[1]);
						int totalQs = result.Get<int>();
						int completedQs = result.Get<int>();
						int optionalCompletedQs = result.Get<int>();
						//int numberOfUniqueMatches = result.GetList<int>().Count();
						//int numberOfPeopleTakingReviews = result.Get<int>();
						/* int numberOfUniqueMatches = s.Query<AnswerModel>()
														 .Where(a=>a.ForReviewContainerId == reviewContainerId && a.DeleteTime ==null)
														 .GroupBy(a=>new { AboutUserId = a.AboutUserId, ByUserId = a.ByUserId})
														 .Count();//result.Get<int>();// Number A<=>B review matches
						 *
						 int numberOfPeopleTakingReviews = s.Query<AnswerModel>()
														 .Where(a=>a.ForReviewContainerId == reviewContainerId && a.DeleteTime ==null)
														 .GroupBy(a=>new { ByUserId = a.ByUserId})
														 .Count();//result.Get<int>();         
						 */

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
					var a = reviewContainer.ForOrganization.Settings.Branding;

					if (sensitive)
						perms.ViewReviews(reviewContainerId, false);
					else
						perms.ViewOrganization(reviewContainer.ForOrganizationId);

					if (populateAnswers || populateReport) {
						PopulateReviewContainer(s.ToQueryProvider(true), reviewContainer, populateAnswers, populateReport);
					}

					if (reviewContainer.Reviews == null && populateReview) {
						reviewContainer.Reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null).List().ToList();
						if (deduplicate)
							reviewContainer.Reviews = reviewContainer.Reviews.GroupBy(x => x.ForUserId).Select(x => x.First()).ToList();
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
					var review = s.QueryOver<ReviewModel>().Where(x => x.ForUserId == userId && x.DeleteTime == null).OrderBy(x => x.DueDate).Desc.Select(x => x.ForReviewsId).List<long>().Distinct().ToList();
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
					var reviewsId = clientReview.ForReviewsId;
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
			return s.QueryOver<ReviewsModel>().Where(x => x.DeleteTime == null && x.ForOrganizationId == orgId && x.DueDate > DateTime.UtcNow).List().ToList();
			//return s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null ).WhereRestrictionOn(x=>x.ForReviewsId).IsIn(found).List().ToList();
		}
	}
}