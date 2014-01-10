using NHibernate;
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

namespace RadialReview.Accessors
{
    public partial class ReviewAccessor : BaseAccessor
    {

        #region Get
        public List<ReviewModel> GetReviewsForUser(UserOrganizationModel caller, UserOrganizationModel forUser)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var forUserId = forUser.Id;
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(forUserId, true);

                    var reviews = s.QueryOver<ReviewModel>()
                        .Where(x => x.ForUserId == forUserId)
                        //.Fetch(x => x.Answers).Eager
                        //add reviewModel Id to answers, query for that
                        .List().ToList();

                    var allAnswers = s.QueryOver<AnswerModel>().Where(x => x.ByUserId == forUser.Id).List().ToListAlive();


                    for (int i = 0; i < reviews.Count; i++)
                        PopulateAnswers(s, reviews[i], allAnswers);
                    return reviews;
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
                    return s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId).List().ToList();
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
                                        .Where(x => x.ForReviewId == reviewId)
                                        .List().ToListAlive();

                    PopulateAnswers(s, reviewPopulated, allAnswers);
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
                        .Where(x => x.DeleteTime==null && x.AboutUserId == userOrgId && x.ForReviewContainerId == reviewContainerId)
                        .List().ToListAlive().Distinct(x=>x.Askable.Id).ToList();
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
                                        .Where(x => x.AboutUserId == userOrgId && x.ForReviewContainerId == reviewContainerId)
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
                                        .Where(x => x.ForReviewContainerId == reviewContainerId)
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

        public List<ReviewsModel> GetReviewsForOrganization(UserOrganizationModel caller, long organizationId, bool populate)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
                    var reviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.ForOrganization.Id == organizationId).List().ToList();

                    //Completion should be much faster than getting all the reviews...

                    foreach (var rc in reviewContainers)
                    {
                        PopulateReviewContainerCompletion(s, rc);
                    }


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

                    var reviewUsers = s.QueryOver<ReviewModel>().Where(x =>x.DeleteTime==null && x.ForReviewsId == reviewContainerId).Fetch(x => x.ForUser).Default.List().ToList().Select(x => x.ForUser).ToList();
                    return reviewUsers;
                }
            }
        }

        public ReviewsModel GetReviewContainer(UserOrganizationModel caller, long reviewContainerId, bool populate)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewReviews(reviewContainerId);
                    var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
                    if (populate)
                        PopulateReviewContainer(s, reviewContainer);

                    return reviewContainer;
                }
            }
        }

        #endregion
    }
}