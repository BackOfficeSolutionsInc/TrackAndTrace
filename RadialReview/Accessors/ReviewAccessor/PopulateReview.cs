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
        #region Populate
        private void PopulateReviewContainerCompletion(ISession s, ReviewsModel reviewContainer)
        {
            var reviewContainerId = reviewContainer.Id;
            //var reviewsQuery = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId).List();


            var optional = s.QueryOver<AnswerModel>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId && !x.Required).Select(Projections.RowCount()).SingleOrDefault<int>();
            var required = s.QueryOver<AnswerModel>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId && x.Required).Select(Projections.RowCount()).SingleOrDefault<int>();
            var optComp = s.QueryOver<AnswerModel>().Where(x =>  x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId && !x.Required && x.Complete).Select(Projections.RowCount()).SingleOrDefault<int>();
            var reqComp = s.QueryOver<AnswerModel>().Where(x =>  x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId && x.Required && x.Complete).Select(Projections.RowCount()).SingleOrDefault<int>();

            var completion = new CompletionModel(reqComp, required, optComp, optional);
            /*foreach (var answer in reviewsQuery)
            {
                completion += answer.GetCompletion();
            }*/
            reviewContainer.Completion = completion;
        }

        private void PopulateReviewContainer(ISession s, ReviewsModel reviewContainer)
        {
            var reviewContainerId = reviewContainer.Id;
            var reviewsQuery = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId);
            reviewsQuery.Fetch(x => x.ForUser).Default.Future();
            var reviews = reviewsQuery.List().ToList();
            var allAnswers = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId).List().ToList();

            foreach (var r in reviews)
            {
                PopulateAnswers(s, r, allAnswers);
            }

            reviewContainer.Reviews = reviews;
        }

        private void PopulateAnswers(ISession session, ReviewModel review, List<AnswerModel> allAnswers)
        {
            // var answers = session.QueryOver<AnswerModel>().Where(x => x.ForReviewId == review.Id).List().ToList();
            var answers = allAnswers.Where(x => x.ForReviewId == review.Id).ToList();
            review.Answers = answers;
        }
        #endregion

    }
}