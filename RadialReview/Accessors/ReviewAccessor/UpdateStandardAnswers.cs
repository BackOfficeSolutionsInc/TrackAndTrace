using System;
using System.Collections.Generic;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using System.Web.Mvc;
using RadialReview.Utilities.DataTypes;
using System.Linq;
using RadialReview.Exceptions;

namespace RadialReview.Accessors
{
	public partial class ReviewAccessor
	{
		public bool UpdateAnswers(UserOrganizationModel caller, long reviewId, FormCollection collection, DateTime now, out DateTime dueDate)
		{
			var started = false;
			var editAny = false;

			var questionsAnswered = new DefaultDictionary<long, int>(x => 0);
			var optionalAnswered = new DefaultDictionary<long, int>(x => 0);
			var durationMinutes = 0.0m;
			var allComplete = true;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var user = caller;
					var perms = PermissionsUtility.Create(s, caller);
					var askableIds =collection.AllKeys.SelectMany(k =>{
						var args = k.Split('_');
						if (args[0] == "question"){
							return long.Parse(args[2]).AsList();
						}
						return new List<long>();
					}).ToList();


					var reviews = GetReviewForUser_SpecificAnswers(s, perms, caller.Id, reviewId, askableIds);

					if (!reviews.All(x => user.UserIds.Any(y => y == x.ReviewerUserId)))
						throw new PermissionsException("You cannot take this review.");

					var answers = reviews.SelectMany(x => x.Answers).ToList();

					dueDate = reviews.Max(x => x.DueDate);
					if (dueDate < DateTime.UtcNow){
						return false;
					}
					//var values = collection.AllKeys.Select(k => collection[k]).ToList();



					foreach (var k in collection.AllKeys)
					{
						var args = k.Split('_');
						if (args[0] == "question")
						{
							var askableId = long.Parse(args[2]);
							var aboutUserId = long.Parse(args[3]);
							var forReviewContainerId = long.Parse(args[4]);
							var edited = false;
							var currentComplete = false;

							var matchingQuestions = answers.Where(x => x.Askable.Id == askableId && x.RevieweeUserId == aboutUserId && x.ForReviewContainerId == forReviewContainerId);



							foreach (var question in matchingQuestions)
							{
								var rid = question.ForReviewId;
								var questionId = question.Id;
								var qA = 0;
								var oA = 0;
								switch (args[1].Parse<QuestionType>())
								{
									case QuestionType.Slider:
										{
											decimal value = 0;
											decimal? output = null;
											if (decimal.TryParse(collection[k], out value))
												output = value / 100.0m;
											if (value == 0)
												output = null;
											currentComplete = UpdateSliderAnswer(s, perms, questionId, output, now, out edited, ref qA, ref oA);
										} break;
									case QuestionType.Thumbs:
										currentComplete = UpdateThumbsAnswer(s, perms, questionId, collection[k].Parse<ThumbsType>(), now, out edited, ref qA, ref oA);
										break;
									case QuestionType.Feedback:
										currentComplete = UpdateFeedbackAnswer(s, perms, questionId, collection[k], now, out edited, ref qA, ref oA);
										break;
									case QuestionType.RelativeComparison:
										currentComplete = UpdateRelativeComparisonAnswer(s, perms, questionId, collection[k].Parse<RelativeComparisonType>(), now, out edited, ref qA, ref oA);
										break;
									case QuestionType.GWC:
										if (args[5].EndsWith("Reason"))
											currentComplete = UpdateGWCReasonAnswer(s, perms, questionId, args[5], collection[k], now, out edited, ref qA, ref oA);
										else
											currentComplete = UpdateGWCAnswer(s, perms, questionId, args[5], collection[k].Parse<FiveState>(), now, out edited, ref qA, ref oA);
										break;
									case QuestionType.CompanyValue:

										if (args.Length == 6)
										{
											if (args[5] != "Reason")
												throw new Exception("Unexpected CompanyValue argument.");
											currentComplete = UpdateCompanyValueReasonAnswer(s, perms, questionId, collection[k], now, out edited, ref qA, ref oA);
										}
										else
										{
											currentComplete = UpdateCompanyValueAnswer(s, perms, questionId, collection[k].Parse<PositiveNegativeNeutral>(), now, out edited, ref qA, ref oA);
										}
										break;
									case QuestionType.Rock:

										if (args.Length == 6)
										{
											//reason
											if (args[5] != "Reason")
												throw new Exception("Unexpected Rock argument.");
											currentComplete = UpdateRockReasonAnswer(s, perms, questionId, collection[k], now, out edited, ref qA, ref oA);
										}
										else
										{
											currentComplete = UpdateRockAnswer(s, perms, questionId, collection[k].Parse<RockState>(), now, out edited, ref qA, ref oA);
										}
										break;
									default:
										throw new Exception();
								}
								allComplete = allComplete && currentComplete;
								started = started || currentComplete;
								editAny = editAny || edited;
								questionsAnswered[rid] += qA;
								optionalAnswered[rid] += oA;

							}
						}
					}
					var startTime = new DateTime(collection["StartTime.Ticks"].ToLong());
					if (editAny){
						durationMinutes = (decimal)(now - startTime).TotalMinutes;
					}

					var allReviewIncomplete =FastReviewQueries.AnyUnansweredReviewQuestions(s, questionsAnswered.Keys);

					foreach (var reviewIncomplete in allReviewIncomplete)
					{
						UpdateAllCompleted(s, perms, caller.Organization.Id, reviewIncomplete, started, durationMinutes, questionsAnswered[reviewIncomplete.reviewId], optionalAnswered[reviewIncomplete.reviewId]);
					}
					tx.Commit();
					s.Flush();
					return allComplete;
				}
			}
		}


		public static Boolean UpdateSliderAnswer(ISession s, PermissionsUtility perms, long id, decimal? value, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{

			var answer = s.Get<SliderAnswer>(id);
			perms.EditReview(answer.ForReviewId);
			edited = false;
			if (answer.Percentage != value)
			{
				edited = true;
				answer.Complete = value.HasValue;
				answer.Percentage = value;
				UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
				s.Update(answer);

			}
			return answer.Complete || !answer.Required;

		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="id"></param>
		/// <param name="value"></param>
		/// <returns>Complete</returns>
		public static Boolean UpdateThumbsAnswer(ISession s, PermissionsUtility perms, long id, ThumbsType value, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{

			var answer = s.Get<ThumbsAnswer>(id);
			perms.EditReview(answer.ForReviewId);

			edited = false;
			if (value != answer.Thumbs)
			{
				edited = true;
				answer.Complete = value != ThumbsType.None;
				answer.Thumbs = value;
				UpdateCompletion(answer, now, ref  questionsAnsweredDelta, ref  optionalAnsweredDelta);

				s.Update(answer);
				//tx.Commit();
				//s.Flush();
			}
			return answer.Complete || !answer.Required;

		}
		public static Boolean UpdateRelativeComparisonAnswer(ISession s, PermissionsUtility perms, long questionId, RelativeComparisonType choice, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{

			var answer = s.Get<RelativeComparisonAnswer>(questionId);
			perms.EditReview(answer.ForReviewId);

			edited = false;
			if (choice != answer.Choice)
			{
				edited = true;
				answer.Complete = (choice != RelativeComparisonType.None);
				UpdateCompletion(answer, now, ref  questionsAnsweredDelta, ref  optionalAnsweredDelta);
				answer.Choice = choice;
				s.Update(answer);
				//tx.Commit();
				//s.Flush();
			}

			return answer.Complete || !answer.Required;

		}
		public Boolean UpdateFeedbackAnswer(ISession s, PermissionsUtility perms, long questionId, string feedback, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{
			var answer = s.Get<FeedbackAnswer>(questionId);
			perms.EditReview(answer.ForReviewId);
			edited = false;
			if (answer.Feedback != feedback)
			{
				edited = true;
				answer.Complete = !String.IsNullOrWhiteSpace(feedback);
				UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
				answer.Feedback = feedback;
				s.Update(answer);
				//tx.Commit();
				//s.Flush();
			}
			return answer.Complete || !answer.Required;
		}
	}
}