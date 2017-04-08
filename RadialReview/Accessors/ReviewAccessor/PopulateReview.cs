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

namespace RadialReview.Accessors {
	public class SimpleAnswerLookup {
		public List<AnswerModel> SurveyAnswers { get; set; }
		public List<ByAboutStarted> ByAboutStartedList { get; set; }

		/// <summary>
		/// Gets the number of people this person started reviewing
		/// </summary>
		/// <param name="byUserId"></param>
		/// <returns></returns>
		public Ratio GetNumberReviewed_ByUser(long byUserId) {
			var total = ByAboutStartedList.Where(x => x.ByUserId == byUserId).Count();
			var started = ByAboutStartedList.Where(x => x.ByUserId == byUserId && x.StartedReviewing).Count();

			return new Ratio(started, total);
		}

		/// <summary>
		/// Gets the number of people reviewed by this person
		/// </summary>
		/// <param name="byUserId"></param>
		/// <returns></returns>
		public Ratio GetNumberReviewees_AboutUser(long aboutUserId) {
			var total = ByAboutStartedList.Where(x => x.AboutUserId == aboutUserId).Count();
			var started = ByAboutStartedList.Where(x => x.AboutUserId == aboutUserId && x.StartedReviewing).Count();
			return new Ratio(started, total);
		}

		public class ByAboutStarted {
			public long ByUserId { get; set; }
			public long AboutUserId { get; set; }
			public bool StartedReviewing { get; set; }
		}
	}

	public partial class ReviewAccessor : BaseAccessor {


		public static SimpleAnswerLookup GetSimpleAnswerLookup_Unsafe(long reviewContainerId,bool includeSurvey,bool includeCompletion) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
					IEnumerable<AnswerModel> surveyAnswers = new List<AnswerModel>();
					IEnumerable<object[]> userReviewCounts = new List<object[]>();

					if (includeSurvey) {
						surveyAnswers=s.QueryOver<AnswerModel>().Where(x =>
							x.DeleteTime == null &&
							x.ForReviewContainerId == reviewContainerId &&
							x.RevieweeUserId == reviewContainer.OrganizationId
						).Future();
					}

					if (includeCompletion) {
						userReviewCounts = s.QueryOver<AnswerModel>().Where(x =>
							  x.DeleteTime == null &&
							  x.ForReviewContainerId == reviewContainerId
						).Select(
							Projections.Group<AnswerModel>(x => x.RevieweeUserId),
							Projections.Group<AnswerModel>(x => x.ReviewerUserId),
							Projections.Max<AnswerModel>(x => x.Complete)
						).Future<object[]>();
					}


					return new SimpleAnswerLookup() {
						SurveyAnswers = surveyAnswers.ToList(),
						ByAboutStartedList = userReviewCounts.Select(x => {
							return new SimpleAnswerLookup.ByAboutStarted() {
								AboutUserId = (long)x[0],
								ByUserId = (long)x[1],
								StartedReviewing = ((bool)x[2])
							};
						}).ToList()
					};
				}
			}
		}


		#region Populate
		private void PopulateReviewContainerCompletion(ISession s, ReviewsModel reviewContainer) {
			var reviewContainerId = reviewContainer.Id;
			//var reviewsQuery = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId).List();

			var optional = s.QueryOver<AnswerModel>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId && !x.Required).Select(Projections.RowCount()).SingleOrDefault<int>();
			var required = s.QueryOver<AnswerModel>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId && x.Required).Select(Projections.RowCount()).SingleOrDefault<int>();
			var optComp = s.QueryOver<AnswerModel>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId && !x.Required && x.Complete).Select(Projections.RowCount()).SingleOrDefault<int>();
			var reqComp = s.QueryOver<AnswerModel>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId && x.Required && x.Complete).Select(Projections.RowCount()).SingleOrDefault<int>();

			var completion = new CompletionModel(reqComp, required, optComp, optional);
			/*foreach (var answer in reviewsQuery)
            {
                completion += answer.GetCompletion();
            }*/
			reviewContainer.Completion = completion;
		}

		private void PopulateReviewContainer(AbstractQuery s, ReviewsModel reviewContainer, bool populateAnswers, bool populateClientReport) {
			var reviewContainerId = reviewContainer.Id;
			var reviewsQuery = s.Where<ReviewModel>(x => x.ForReviewContainerId == reviewContainerId);
			//reviewsQuery.Fetch(x => x.ForUser).Default.Future();
			/*if (populateClientReport){
                reviewsQuery.Fetch(x => x.ClientReview).Default.Future();
            }*/
			var reviews = reviewsQuery.ToList();
			if (populateAnswers) {
				var allAnswers = s.Where<AnswerModel>(x => x.ForReviewContainerId == reviewContainerId).ToList();

				foreach (var r in reviews) {
					PopulateAnswers(/*s,*/ r, allAnswers);
				}
			}


			reviewContainer.Reviews = reviews;
		}

		private static void PopulateAnswers(/*ISession session,*/ ReviewModel review, List<AnswerModel> allAnswers) {
			// var answers = session.QueryOver<AnswerModel>().Where(x => x.ForReviewId == review.Id).List().ToList();
			var answers = allAnswers.Where(x => x.ForReviewId == review.Id).ToList();
			review.Answers = answers;
		}
		#endregion

	}
}