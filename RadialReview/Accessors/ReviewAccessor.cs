using NHibernate;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class ReviewAccessor : BaseAccessor
    {

        public void CreateReviewContainer(UserOrganizationModel caller,ReviewsModel reviewContainer)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditReview();
                    reviewContainer.CreatedById = caller.Id;
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

        private decimal Completion(decimal numerator,decimal denomiator)
        {
            if (denomiator == 0)
                return 1;
            return numerator / denomiator;
        }

        private void PopulateCompletion(ISession session,ReviewModel review)
        {
            var answers = session.QueryOver<AnswerModel>().Where(x => x.ForReviewId == review.Id).List().ToList();
            review.Answers = answers;
            
            decimal requiredComplete = review.Answers.Count(x => x.Required && x.Complete);
            decimal required = review.Answers.Count(x => x.Required);
            decimal total = review.Answers.Count();
            if (requiredComplete<required)
            {
                review.Completion = Completion(requiredComplete, required);
                review.Complete=true;
                review.FullyComplete=(requiredComplete==total);
            }else{
                var complete=review.Answers.Count(x=>x.Complete);
                review.Completion=Completion(complete,required);
                review.Complete=true;
                review.FullyComplete=(total==complete);
            }
        }

        public List<ReviewModel> GetReviewsForUser(UserOrganizationModel caller, UserOrganizationModel forUser)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var forUserId = forUser.Id;
                    PermissionsUtility.Create(s, caller).ManagesUserOrganization(forUserId);

                    var reviews = s.QueryOver<ReviewModel>()
                        .Where(x => x.ForUserId == forUserId)
                        //.Fetch(x => x.Answers).Eager
                        //add reviewModel Id to answers, query for that
                        .List().ToList();

                    for(int i=0;i<reviews.Count;i++)
                        PopulateCompletion(s,reviews[i]);
                    return reviews;
                }
            }
        }
        public ReviewModel GetReviewForUser(UserOrganizationModel caller, long reviewId)
        {
            var output = new ReviewModel();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var review = s.Get<ReviewModel>(reviewId);
                    var reviews = s.QueryOver<ReviewModel>()
                       .Where(x => x.Id == reviewId)
                       .Fetch(x => x.Answers).Eager
                       .SingleOrDefault();
                    PermissionsUtility.Create(s, caller).ManagesUserOrganization(review.ForUserId);
                    PopulateCompletion(s,review);
                    return review;
                }
            }
        }

    }
}