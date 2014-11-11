using System;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public partial class ReviewAccessor
	{
		public Boolean UpdateSliderAnswer(UserOrganizationModel caller, long id, decimal? value, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<SliderAnswer>(id);
					PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);
					edited = false;
					if (answer.Percentage != value)
					{
						edited = true;
						answer.Complete = value.HasValue;
						answer.Percentage = value;
						UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
						s.Update(answer);

						tx.Commit();
						s.Flush();
					}
					return answer.Complete || !answer.Required;

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
		public Boolean UpdateThumbsAnswer(UserOrganizationModel caller, long id, ThumbsType value, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<ThumbsAnswer>(id);
					PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

					edited = false;
					if (value != answer.Thumbs)
					{
						edited = true;
						answer.Complete = value != ThumbsType.None;
						answer.Thumbs = value;
						UpdateCompletion(answer, now, ref  questionsAnsweredDelta, ref  optionalAnsweredDelta);

						s.Update(answer);
						tx.Commit();
						s.Flush();
					}
					return answer.Complete || !answer.Required;
				}
			}
		}
		public Boolean UpdateRelativeComparisonAnswer(UserOrganizationModel caller, long questionId, RelativeComparisonType choice, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<RelativeComparisonAnswer>(questionId);
					PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

					edited = false;
					if (choice != answer.Choice)
					{
						edited = true;
						answer.Complete = (choice != RelativeComparisonType.None);
						UpdateCompletion(answer, now, ref  questionsAnsweredDelta, ref  optionalAnsweredDelta);
						answer.Choice = choice;
						s.Update(answer);
						tx.Commit();
						s.Flush();
					}

					return answer.Complete || !answer.Required;
				}
			}
		}
		public Boolean UpdateFeedbackAnswer(UserOrganizationModel caller, long questionId, string feedback, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<FeedbackAnswer>(questionId);
					PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);
					edited = false;
					if (answer.Feedback != feedback)
					{
						edited = true;
						answer.Complete = !String.IsNullOrWhiteSpace(feedback);
						UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
						answer.Feedback = feedback;
						s.Update(answer);
						tx.Commit();
						s.Flush();
					}
					return answer.Complete || !answer.Required;
				}
			}
		}
	}
}