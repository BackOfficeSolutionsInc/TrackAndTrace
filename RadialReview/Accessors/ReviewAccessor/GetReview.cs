using NHibernate;
using NHibernate.Criterion;
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

		public static List<ReviewModel> GetReviewsForUser(ISession s, PermissionsUtility perms, UserOrganizationModel caller, long forUserId, int page, int pageCount, DateTime dueAfter) {
			perms.ViewUserOrganization(forUserId, true);

			List<ReviewModel> reviews;

			var user = s.Get<UserOrganizationModel>(forUserId);

			var usersIds = user.UserIds;

			reviews = s.QueryOver<ReviewModel>().Where(x => /*x.ForUserId == forUserId &&*/ x.DeleteTime == null && x.DueDate > dueAfter)
								.WhereRestrictionOn(x => x.ForUserId).IsIn(usersIds)
								.OrderBy(x => x.CreatedAt)
								.Desc.Skip(page * pageCount)
								.Take(pageCount)
				//.Fetch(x => x.Answers).Eager
				//add reviewModel Id to answers, query for that
								.List().ToList();

			var allAnswers = s.QueryOver<AnswerModel>()
								.Where(x => /*x.ByUserId == forUserId &&*/ x.DeleteTime == null)
								.WhereRestrictionOn(x => x.ByUserId).IsIn(usersIds)
								.List().ToListAlive();

			for (int i = 0; i < reviews.Count; i++)
				PopulateAnswers(/*s,*/ reviews[i], allAnswers);
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

		public List<ReviewModel> GetReviewForUser(UserOrganizationModel caller, long userId, long reviewContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					perms.ViewUserOrganization(userId, true);

					var user = s.Get<UserOrganizationModel>(userId);
					var usersIds = user.UserIds;

					var reviews = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForReviewsId == reviewContainerId)
										.WhereRestrictionOn(x => x.ForUserId).IsIn(usersIds)
										.List().ToList();

					var allAnswers = s.QueryOver<AnswerModel>()
										.Where(x => x.ForReviewContainerId == reviewContainerId && /*x.ByUserId == forUserId &&*/ x.DeleteTime == null)
										.WhereRestrictionOn(x => x.ByUserId).IsIn(usersIds)
										.List().ToListAlive();

					for (int i = 0; i < reviews.Count; i++)
						PopulateAnswers(/*s,*/ reviews[i], allAnswers);
					return reviews;

				}
			}
		}



		public List<ReviewModel> GetReviewsForUser(UserOrganizationModel caller, long forUserId, int page, int pageCount, DateTime dueAfter) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetReviewsForUser(s, perms, caller, forUserId, page, pageCount, dueAfter);
				}
			}
		}

		public List<ReviewModel> GetReviewsForReviewContainer(UserOrganizationModel caller, long reviewContainerId,bool includeUser) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, caller.Organization.Id).ViewReviews(reviewContainerId, false);
					var query = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null);
					if (includeUser){
						query = query.Fetch(x => x.ForUser.User).Eager;
					}
					return query.List().ToList();
				}
			}
		}
		public ReviewModel GetReview(UserOrganizationModel caller, long reviewId,bool includeReviewContainer=false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var reviewPopulated = s.Get<ReviewModel>(reviewId);

					PermissionsUtility.Create(s, caller).ViewUserOrganization(reviewPopulated.ForUserId, true).ViewReview(reviewId);

					var allAnswers = s.QueryOver<AnswerModel>()
										.Where(x => x.ForReviewId == reviewId && x.DeleteTime == null)
										.List().ToListAlive();
					var allAlive = UserAccessor.WasAliveAt(s, allAnswers.Select(x => x.AboutUserId).Distinct().ToList(), reviewPopulated.DueDate);
					allAnswers = allAnswers.Where(x => allAlive.Contains(x.AboutUserId)).ToList();
					PopulateAnswers(/*s,*/ reviewPopulated, allAnswers);
					if (includeReviewContainer){
						reviewPopulated.ForReviewContainer = s.Get<ReviewsModel>(reviewPopulated.ForReviewsId);
					}

					return reviewPopulated;
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

		public List<AnswerModel> GetAnswersForUserReview(UserOrganizationModel caller, long userOrgId, long reviewContainerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewUserOrganization(userOrgId, true);

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

						//Query        
						var result = multiCriteria.Execute();

						int totalReviews = result.Get<int>();
						int startedReviews = result.Get<int>();
						int finishedReviews = result.Get<int>();
						List<ClientReviewModel> clientReports = result.GetList<ClientReviewModel>();
						var avgList = result.GetList<decimal>();
						decimal? avgDuration=null;
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
					}
					catch (Exception e) {
						throw e;
					}

					//

					return output;
				}
			}

		}

		public ReviewsModel GetReviewContainer(UserOrganizationModel caller, long reviewContainerId, bool populateAnswers, bool populateReport, bool sensitive = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
					reviewContainer.Period = s.Get<PeriodModel>(reviewContainer.PeriodId);
					reviewContainer.NextPeriod = s.Get<PeriodModel>(reviewContainer.NextPeriodId);


					if (sensitive)
						perms.ViewReviews(reviewContainerId, false);
					else
						perms.ViewOrganization(reviewContainer.ForOrganizationId);

					if (populateAnswers || populateReport) {
						PopulateReviewContainer(s.ToQueryProvider(true), reviewContainer, populateAnswers, populateReport);
					}

					return reviewContainer;
				}
			}
		}

		#endregion


		public ReviewsModel GetMostRecentReviewContainer(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
					var user = s.Get<UserOrganizationModel>(userId);
					var orgId = user.Organization.Id;
					var review = s.QueryOver<ReviewModel>().Where(x => x.ForUserId == userId && x.DeleteTime == null).OrderBy(x => x.DueDate).Desc.Take(1).SingleOrDefault();
					if (review == null)
						return null;
					return s.Get<ReviewsModel>(review.ForReviewsId);
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


	}
}