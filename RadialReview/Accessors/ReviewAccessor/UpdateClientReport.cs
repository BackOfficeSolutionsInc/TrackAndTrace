using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Json;
using RadialReview.Models.Reviews;
using RadialReview.Utilities;
using RadialReview.Models.Enums;

namespace RadialReview.Accessors
{
	public partial class ReviewAccessor
	{
		private void SetAggregateBy(UserOrganizationModel caller, long reviewId, string aggregateBy)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId,false);

					var review = s.Get<ReviewModel>(reviewId);
					review.ClientReview.ScatterChart.Groups = aggregateBy;
					s.Update(review.ClientReview);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void UpdateScatterChart(
			UserOrganizationModel caller,
			long reviewId,
			string aggregateBy,
			string filterBy,
			string title,
			long? xAxis,
			long? yAxis,
			DateTime? startTime,
			DateTime? endTime,
			bool? included)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);

					var review = s.Get<ReviewModel>(reviewId);
					if (aggregateBy != null)
						review.ClientReview.ScatterChart.Groups = aggregateBy;
					if (filterBy != null)
						review.ClientReview.ScatterChart.Filters = filterBy;
					if (title != null)
						review.ClientReview.ScatterChart.Title = title;

					if (xAxis != null)
						review.ClientReview.ScatterChart.Item1 = xAxis.Value;
					if (yAxis != null)
						review.ClientReview.ScatterChart.Item2 = yAxis.Value;
					if (startTime != null)
						review.ClientReview.ScatterChart.StartDate = startTime.Value;
					if (endTime != null)
						review.ClientReview.ScatterChart.EndDate = endTime.Value;
					if (included != null)
						review.ClientReview.IncludeScatterChart = included.Value;

					s.Update(review.ClientReview);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public long AddChartToReview(UserOrganizationModel caller, long reviewId, long xCategoryId, long yCategoryId, String groups, String filters, DateTime startTime, DateTime endTime)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false).ViewCategory(xCategoryId).ViewCategory(yCategoryId);
					var xAxis = s.Get<QuestionCategoryModel>(xCategoryId);
					var yAxis = s.Get<QuestionCategoryModel>(yCategoryId);

					var review = s.Get<ReviewModel>(reviewId);

					var tuple = new LongTuple()
					{
						Item1 = xCategoryId,
						Item2 = yCategoryId,
						Groups = groups,
						Filters = filters,
						StartDate = startTime,
						EndDate = endTime
					};
					review.ClientReview.Charts.Add(tuple);
					s.Update(review);
					tx.Commit();
					s.Flush();
					return tuple.Id;
				}
			}
		}
		public void RemoveChartFromReview(UserOrganizationModel caller, long reviewId, long chartId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);

					var review = s.Get<ReviewModel>(reviewId);
					foreach (var id in review.ClientReview.Charts)
					{
						if (id.Id == chartId)
							id.DeleteTime = DateTime.UtcNow;
					}
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void SetIncludeScatter(UserOrganizationModel caller, long reviewId, bool on)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);

					var review = s.Get<ReviewModel>(reviewId);
					review.ClientReview.IncludeScatterChart = on;
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void SetIncludeTimeline(UserOrganizationModel caller, long reviewId, bool on)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);

					var review = s.Get<ReviewModel>(reviewId);
					review.ClientReview.IncludeTimelineChart = on;
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void SetIncludeQuestionTable(UserOrganizationModel caller, long reviewId, bool on)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);

					var review = s.Get<ReviewModel>(reviewId);
					review.ClientReview.IncludeQuestionTable = on;
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void SetIncludeManagerAnswers(UserOrganizationModel caller, long reviewId, bool on)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId,false);

					var review = s.Get<ReviewModel>(reviewId);
					review.ClientReview.IncludeManagerFeedback = on;
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void SetIncludeNotes(UserOrganizationModel caller, long reviewId, bool on)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);

					var review = s.Get<ReviewModel>(reviewId);
					review.ClientReview.IncludeNotes = on;
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void SetIncludeEvaluation(UserOrganizationModel caller, long reviewId, bool on)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);

					var review = s.Get<ReviewModel>(reviewId);
					review.ClientReview.IncludeEvaluation = on;
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void SetIncludeSelfAnswers(UserOrganizationModel caller, long reviewId, bool on)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);

					var review = s.Get<ReviewModel>(reviewId);
					review.ClientReview.IncludeSelfFeedback = on;
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void UpdateNotes(UserOrganizationModel caller, long id, string notes)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(id, false);
					var review = s.Get<ReviewModel>(id);

					review.ClientReview.ManagerNotes = notes;
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void UpdateRockCompletionOverride(UserOrganizationModel caller, long rockAnswerId, Tristate val)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var rockAnswer = s.Get<RockAnswer>(rockAnswerId);
					PermissionsUtility.Create(s, caller).ManageUserReview(rockAnswer.ForReviewId, false);
					switch (val)
					{
						case Tristate.False: rockAnswer.ManagerOverride = RockState.AtRisk; break;
						case Tristate.True: rockAnswer.ManagerOverride = RockState.Complete; break;
						case Tristate.Indeterminate: rockAnswer.ManagerOverride = RockState.Indeterminate; break;
					}
					s.Update(rockAnswer);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public void UpdateRockCompletionNotes(UserOrganizationModel caller, long rockAnswerId, string notes)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var rockAnswer = s.Get<RockAnswer>(rockAnswerId);
					PermissionsUtility.Create(s, caller).ManageUserReview(rockAnswer.ForReviewId, false);
					rockAnswer.OverrideReason = notes;
					s.Update(rockAnswer);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public void SetIncludeGWCFeedback(UserOrganizationModel caller, bool on,long answerId, string gwc)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<GetWantCapacityAnswer>(answerId);
					PermissionsUtility.Create(s, caller).ManageUserReview(answer.ForReviewId, false);



					switch (gwc.ToLower())
					{
						case "get": answer.IncludeGetItReason = on;break;
						case "want": answer.IncludeWantItReason = on;break;
						case "capacity": answer.IncludeHasCapacityReason = on;break;
						default: throw new Exception(gwc.ToLower());
					}

					s.Update(answer);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public void SetIncludeValueFeedback(UserOrganizationModel caller, bool on, long answerId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<CompanyValueAnswer>(answerId);
					PermissionsUtility.Create(s, caller).ManageUserReview(answer.ForReviewId, false);
					
					answer.IncludeReason = on;
					s.Update(answer);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public ResultObject Authorize(UserOrganizationModel caller, long reviewId, bool authorized)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);
					var review = s.Get<ReviewModel>(reviewId);
					review.ClientReview.Visible = authorized;
					s.Update(review);
					tx.Commit();
					s.Flush();
					if (authorized)
						return ResultObject.Success(review.ForUser.GetFirstName() + " is authorized to view this report.");
					else
						return ResultObject.Success(review.ForUser.GetFirstName() + " is NOT authorized to view this report.");
				}
			}
		}

	}
}