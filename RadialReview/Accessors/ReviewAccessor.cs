using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class ReviewAccessor : BaseAccessor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns>Complete</returns>
        public Boolean UpdateSliderAnswer(UserOrganizationModel caller, long id, decimal? value)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<SliderAnswer>(id);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

                    answer.Complete = value.HasValue;
                    answer.Percentage = value;
                    s.Update(answer);

                    tx.Commit();
                    s.Flush();
                    return answer.Complete;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns>Complete</returns>
        public Boolean UpdateThumbsAnswer(UserOrganizationModel caller, long id, ThumbsType value)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<ThumbsAnswer>(id);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

                    answer.Complete = value != ThumbsType.None;
                    answer.Thumbs = value;
                    s.Update(answer);
                    tx.Commit();
                    s.Flush();
                    return answer.Complete;
                }
            }
        }
        public Boolean UpdateRelativeComparisonAnswer(UserOrganizationModel caller, long questionId, RelativeComparisonType choice)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<RelativeComparisonAnswer>(questionId);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

                    answer.Complete = (choice != RelativeComparisonType.None);
                    answer.Choice = choice;
                    s.Update(answer);
                    tx.Commit();
                    s.Flush();
                    return answer.Complete;
                }
            }
        }

        public Boolean UpdateFeedbackAnswer(UserOrganizationModel caller, long questionId, string feedback)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<FeedbackAnswer>(questionId);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);
                    answer.Complete = !String.IsNullOrWhiteSpace(feedback);
                    answer.Feedback = feedback;
                    s.Update(answer);
                    tx.Commit();
                    s.Flush();
                    return answer.Complete;
                }
            }
        }


        public void CreateReviewContainer(UserOrganizationModel caller, ReviewsModel reviewContainer)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditReviews(caller.Organization.Id);
                    reviewContainer.CreatedById = caller.Id;
                    reviewContainer.ForOrganization = caller.Organization;
                    s.SaveOrUpdate(reviewContainer);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        /// <summary>
        /// Not secure
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="review"></param>
        /*public void UpdateIndividualReview(UserOrganizationModel caller,ReviewModel review)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    //PermissionsUtility.Create(s, caller).EditReview();
                 
                    s.SaveOrUpdate(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }*/

        private decimal Completion(decimal numerator, decimal denomiator)
        {
            if (denomiator == 0)
                return 1;
            return numerator / denomiator;
        }

        private void PopulateAnswers(ISession session, ReviewModel review)
        {
            var answers = session.QueryOver<AnswerModel>().Where(x => x.ForReviewId == review.Id).List().ToList();
            review.Answers = answers;
        }

        public List<ReviewModel> GetReviewsForUser(UserOrganizationModel caller, UserOrganizationModel forUser)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var forUserId = forUser.Id;
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(forUserId);

                    var reviews = s.QueryOver<ReviewModel>()
                        .Where(x => x.ForUserId == forUserId)
                        //.Fetch(x => x.Answers).Eager
                        //add reviewModel Id to answers, query for that
                        .List().ToList();

                    for (int i = 0; i < reviews.Count; i++)
                        PopulateAnswers(s, reviews[i]);
                    return reviews;
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
                    var reviewPopulated = s.QueryOver<ReviewModel>()
                       .Where(x => x.Id == reviewId)
                       .Fetch(x => x.Answers).Eager
                       .SingleOrDefault();

                    PermissionsUtility.Create(s, caller).ViewUserOrganization(reviewPopulated.ForUserId).ViewReview(reviewId);


                    PopulateAnswers(s, reviewPopulated);
                    return reviewPopulated;
                }
            }
        }

        private void PopulateReviewContainer(ISession s,ReviewsModel reviewContainer)
        {
            var reviewContainerId = reviewContainer.Id;
            var reviewsQuery = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId);
            reviewsQuery.Fetch(x=>x.ForUser).Eager.Future();
            var reviews = reviewsQuery.List().ToList();

            foreach (var r in reviews)
            {
                PopulateAnswers(s, r);
            }

            reviewContainer.Reviews = reviews;
        }


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
        }

        public List<ReviewsModel> GetReviewsForOrganization(UserOrganizationModel caller, long organizationId, bool populate)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id,organizationId);
                    var reviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.ForOrganization.Id == organizationId).List().ToList();
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

        public ReviewsModel GetReviewContainer(UserOrganizationModel caller, long reviewContainerId,bool populate)
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
    }
}