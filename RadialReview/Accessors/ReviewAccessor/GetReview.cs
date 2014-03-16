﻿using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Models;
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
using NHibernate.Linq;

namespace RadialReview.Accessors
{
    public partial class ReviewAccessor : BaseAccessor
    {
        public static List<ReviewModel> GetReviewsForUserWithVisibleReports(ISession s, PermissionsUtility perms, UserOrganizationModel caller, long forUserId, int page, int pageCount)
        {
            perms.ViewUserOrganization(forUserId, true);
            List<ReviewModel> reviews;
            ClientReviewModel clientReviewAlias = null;
            ReviewModel reviewAlias = null;
            reviews = s.QueryOver<ReviewModel>(() => reviewAlias)
                                .Left.JoinAlias(() => reviewAlias.ClientReview, () => clientReviewAlias)
                                .Where(() => clientReviewAlias.Visible == true && reviewAlias.ForUserId == forUserId && reviewAlias.DeleteTime==null)
                                .OrderBy(() => reviewAlias.CreatedAt)
                                .Desc.Skip(page * pageCount)
                                .Take(pageCount)
                //.Fetch(x => x.Answers).Eager
                //add reviewModel Id to answers, query for that
                                .List().ToList();
            return reviews;
        }

        public static List<ReviewModel> GetReviewsForUser(ISession s, PermissionsUtility perms, UserOrganizationModel caller, long forUserId, int page, int pageCount)
        {
            perms.ViewUserOrganization(forUserId, true);

            List<ReviewModel> reviews;

            reviews = s.QueryOver<ReviewModel>().Where(x => x.ForUserId == forUserId && x.DeleteTime == null)
                                .OrderBy(x => x.CreatedAt)
                                .Desc.Skip(page * pageCount)
                                .Take(pageCount)
                //.Fetch(x => x.Answers).Eager
                //add reviewModel Id to answers, query for that
                                .List().ToList();

            var allAnswers = s.QueryOver<AnswerModel>().Where(x => x.ByUserId == forUserId && x.DeleteTime == null).List().ToListAlive();

            for (int i = 0; i < reviews.Count; i++)
                PopulateAnswers(/*s,*/ reviews[i], allAnswers);
            return reviews;
        }

        #region Get

        public int GetNumberOfReviewsForUser(UserOrganizationModel caller, UserOrganizationModel forUser)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var forUserId = forUser.Id;
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(forUserId, true);

                    return s.QueryOver<ReviewModel>()
                        .Where(x => x.ForUserId == forUserId && x.DeleteTime == null)
                        .RowCount();
                }
            }
        }

        public int GetNumberOfReviewsWithVisibleReportsForUser(UserOrganizationModel caller, long forUserId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
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

        public List<ReviewModel> GetReviewsWithVisibleReports(UserOrganizationModel caller, long forUserId, int page, int pageCount)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetReviewsForUserWithVisibleReports(s, perms, caller, forUserId, page, pageCount);
                }
            }
        }

        public List<ReviewModel> GetReviewsForUser(UserOrganizationModel caller, long forUserId, int page, int pageCount)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetReviewsForUser(s, perms, caller, forUserId, page, pageCount);
                }
            }
        }

        public List<ReviewModel> GetReviewsForReviewContainer(UserOrganizationModel caller, long reviewContainerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, caller.Organization.Id).ViewReviews(reviewContainerId);
                    return s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.DeleteTime == null).List().ToList();
                }
            }
        }
        public ReviewModel GetReview(UserOrganizationModel caller, long reviewId)
        {
            var output = new ReviewModel();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var reviewPopulated = s.Get<ReviewModel>(reviewId);

                    PermissionsUtility.Create(s, caller).ViewUserOrganization(reviewPopulated.ForUserId, true).ViewReview(reviewId);

                    var allAnswers = s.QueryOver<AnswerModel>()
                                        .Where(x => x.ForReviewId == reviewId && x.DeleteTime == null)
                                        .List().ToListAlive();

                    PopulateAnswers(/*s,*/ reviewPopulated, allAnswers);
                    return reviewPopulated;
                }
            }
        }

        public List<AnswerModel> GetDistinctQuestionsAboutUserFromReview(UserOrganizationModel caller, long userOrgId, long reviewContainerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(userOrgId, false);

                    return s.QueryOver<AnswerModel>()
                        .Where(x => x.DeleteTime == null && x.AboutUserId == userOrgId && x.ForReviewContainerId == reviewContainerId && x.DeleteTime==null)
                        .List().ToListAlive().Distinct(x => x.Askable.Id).ToList();
                }
            }
        }

        public List<AnswerModel> GetAnswersForUserReview(UserOrganizationModel caller, long userOrgId, long reviewContainerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(userOrgId, true);

                    var answers = s.QueryOver<AnswerModel>()
                                        .Where(x => x.AboutUserId == userOrgId && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null)
                                        .List()
                                        .ToListAlive();
                    return answers;
                }
            }
        }

        public List<AnswerModel> GetAnswersAboutUser(UserOrganizationModel caller, long userOrgId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(userOrgId, true);

                    var answers = s.QueryOver<AnswerModel>()
                                        .Where(x => x.AboutUserId == userOrgId && x.DeleteTime == null)
                                        .List()
                                        .ToListAlive();
                    return answers;
                }
            }
        }

        public List<AnswerModel> GetReviewContainerAnswers(UserOrganizationModel caller, long reviewContainerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
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

        public ClientReviewModel GetClientReview(UserOrganizationModel caller, long reviewId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
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

        public long GetNumberOfReviewsForOrganization(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
                    var count = s.QueryOver<ReviewsModel>()
                                                .Where(x => x.ForOrganization.Id == organizationId && x.DeleteTime == null)
                                                .RowCountInt64();
                    return count;
                }
            }
        }

        public List<ReviewModel> GetUsefulReview(UserOrganizationModel caller, long userId, DateTime afterTime)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
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
            long organizationId, bool populateAnswers, bool populateReport, bool populateReviews, int pageSize, int page, DateTime afterTime)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
                    var reviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.DueDate > afterTime && x.ForOrganization.Id == organizationId && x.DeleteTime == null)
                                                .OrderBy(x => x.DateCreated).Desc
                                                .Skip(page * pageSize)
                                                .Take(pageSize)
                                                .List().ToList();

                    //Completion should be much faster than getting all the reviews...

                    foreach (var rc in reviewContainers)
                    {
                        PopulateReviewContainerCompletion(s, rc);
                    }

                    if (populateAnswers || populateReport || populateReviews)
                    {
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

                        foreach (var rc in reviewContainers)
                        {
                            PopulateReviewContainer(s.ToQueryProvider(true), rc, populateAnswers, populateReport);
                        }
                    }
                    return reviewContainers;
                }
            }
        }

        public List<UserOrganizationModel> GetUsersInReview(UserOrganizationModel caller, long reviewContainerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
                    PermissionsUtility.Create(s, caller)
                        .ManagerAtOrganization(caller.Id, reviewContainer.ForOrganization.Id)
                        .ViewReviews(reviewContainerId);

                    var reviewUsers = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForReviewsId == reviewContainerId).Fetch(x => x.ForUser).Default.List().ToList().Select(x => x.ForUser).ToList();
                    return reviewUsers;
                }
            }
        }


        public ReviewsModel GetReviewContainer(UserOrganizationModel caller, long reviewContainerId, bool populateAnswers, bool populateReport,bool sensitive=true)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);

                    var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);

                    if (sensitive)
                        perms.ViewReviews(reviewContainerId);
                    else
                        perms.ViewOrganization(reviewContainer.ForOrganizationId);

                    if (populateAnswers || populateReport)
                    {
                        PopulateReviewContainer(s.ToQueryProvider(true), reviewContainer, populateAnswers, populateReport);
                    }

                    return reviewContainer;
                }
            }
        }

        #endregion
    }
}