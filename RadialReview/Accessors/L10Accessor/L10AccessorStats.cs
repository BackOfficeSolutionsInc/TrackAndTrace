using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using Amazon.EC2.Model;
using Amazon.ElasticMapReduce.Model;
using FluentNHibernate.Conventions;
using ImageResizer.Configuration.Issues;
using MathNet.Numerics;
using Microsoft.AspNet.SignalR;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Transform;
using RadialReview.Accessors.TodoIntegrations;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Audit;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.AV;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Permissions;
using RadialReview.Models.Scheduler;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Synchronize;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
using RadialReview.Models.Enums;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Angular.Base;
//using System.Web.WebPages.Html;
using RadialReview.Models.VTO;
using RadialReview.Models.Angular.VTO;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Periods;
using RadialReview.Models.Interfaces;
using System.Dynamic;
using Newtonsoft.Json;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.VideoConference;
using System.Linq.Expressions;
using NHibernate.SqlCommand;
using RadialReview.Models.Rocks;
using RadialReview.Models.Angular.Rocks;
using System.Web.Mvc;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using static RadialReview.Utilities.EventUtil;
using Twilio;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
using RadialReview.Accessors;
using RadialReview.Models.UserModels;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		#region Stats
		public class StatsDataGroup {
			public int Margin { get; set; }
			public List<StatsData> Series { get; set; }
			public DateRange Range { get; set; }

			public StatsDataGroup(DateRange range) {
				Margin = 5;
				Series = new List<StatsData>();
				Range = range;
			}
			public String ToJson() {
				///http://n3-charts.github.io/line-chart/#/docs
				dynamic o = new ExpandoObject();


				o.options = new {
					margin = new { top = Margin },
					series = Series.Select(x => new {
						axis = "y",
						dataset = x.Dataset,
						key = "y",
						label = x.Title,
						color = x.Color,
						type = new[] { "dot", "line" },
						id = x.Dataset
					}).ToList(),
					axes = new {
						x = new { key = "x", type = "date" },
						y = new { includeZero = true }
					}
				};

				o.data = Series.ToDictionary(
					s => s.Dataset,
					s => s.Data.Select(d => new { x = d.Key, y = Math.Round(d.Value * 100) / 100.0m })
							   .OrderBy(x => x.x)
							   .Where(x => Range.StartTime <= x.x && x.x <= Range.EndTime)
							   .ToList()
				);
				//var microsoftDateFormatSettings = new JsonSerializerSettings {
				//	DateFormatHandling = DateFormatHandling.
				//};
				return JsonConvert.SerializeObject(o/*, microsoftDateFormatSettings*/);
				/*
                 * $scope.data = {
                      dataset0: [
                        {x: 0, y: 2}, {x: 1, y: 3}
                      ],
                      dataset1: [
                        {x: 0, value: 2}, {x: 1, value: 3}
                      ],
                      ...
                    };*/
			}
		}
		public class StatsData {
			public string Dataset { get; set; }
			public string Title { get; set; }
			public string Color { get; set; }
			public IEnumerable<KeyValuePair<DateTime, decimal>> Data { get; set; }
		}
		public static StatsDataGroup GetStatsData(UserOrganizationModel caller, long recurrenceId, DateRange range = null) {

			var today = DateTime.UtcNow.Date;
			range = range ?? new DateRange(today.AddDays(-7 * 13), today);

			range.EndTime = range.EndTime.AddDays(1);

			var weekStart = caller.Organization.Settings.WeekStart;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					//Do not filter todo completion here.
					var meetings = s.QueryOver<L10Meeting>()
						.Where(x => x.L10RecurrenceId == recurrenceId &&
							x.DeleteTime == null && x.StartTime != null &&
							(x.CompleteTime == null || x.CompleteTime > range.StartTime) &&
							x.StartTime < range.EndTime
						).Future();

					var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x => x.DeleteTime == null && x.CloseTime != null && x.CloseTime > range.StartTime && x.Recurrence.Id == recurrenceId)
						.Future();

					L10Meeting l10Alias = null;

					var ratings = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.JoinAlias(x => x.L10Meeting, () => l10Alias)
						.Where(x => x.DeleteTime == null && l10Alias.L10RecurrenceId == recurrenceId && l10Alias.DeleteTime == null && x.Rating != null)
						.Future();


					var todoData = new StatsData() {
						Dataset = "todo",
						Color = "hsla(88, 48%, 48%, 1)",
						Title = "To-do Completion",
						Data = meetings.Where(x => x.TodoCompletion != null)
									.GroupBy(x => x.StartTime.Value.StartOfWeek(weekStart))
									.SelectMany(x => {

										var den = x.Sum(y => y.TodoCompletion.Denominator);
										if (den == 0)
											return new List<KeyValuePair<DateTime, decimal>>();
										var num = x.Sum(y => y.TodoCompletion.Numerator);

										return (new KeyValuePair<DateTime, decimal>(x.First().StartTime.Value.Date, num / den * 100)).AsList();
									})
					};

					var issueData = new StatsData() {
						Dataset = "issue",
						Color = "hsla(213, 48%, 48%, 1)",
						Title = "Issues Solved",
						Data = issues.GroupBy(x => x.CloseTime.Value.StartOfWeek(weekStart))
									.SelectMany(x => {
										var count = x.Count();
										if (count == 0)
											return new List<KeyValuePair<DateTime, decimal>>();
										return (new KeyValuePair<DateTime, decimal>(x.First().CloseTime.Value.Date, count)).AsList();
									})
					};

					var ratingData = new StatsData() {
						Dataset = "rating",
						Color = "hsla(318, 48%, 48%, 1)",
						Title = "Avg. Meeting Rating",
						Data = ratings.GroupBy(x => x.L10Meeting.StartTime.Value.StartOfWeek(weekStart))
									.SelectMany(x => {
										//if (count == 0)
										//	return new List<KeyValuePair<DateTime, decimal>>();
										return (new KeyValuePair<DateTime, decimal>(x.First().L10Meeting.StartTime.Value.Date, x.Average(y => y.Rating.Value))).AsList();
									})
					};

					var group = new StatsDataGroup(range);
					group.Series.Add(todoData);
					group.Series.Add(issueData);
					group.Series.Add(ratingData);


					return group;
				}
			}


		}
		public static L10MeetingStatsVM GetStats(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var recurrence = s.Get<L10Recurrence>(recurrenceId);
					var o = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).List().ToList();
					var meeting = o.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).FirstOrDefault();
					var prevMeeting = o.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).Take(2).LastOrDefault();

					var version = 0;

					int issuesSolved = 0;
					int todoComplete = 0;
					List<TodoModel> todosCreated;
					List<TodoModel> allTodos;
					List<TodoModel> oldTodos;

					var rating = double.NaN;

					if (meeting == null || meeting.CompleteTime == null) {
						var createTime = meeting.NotNull(x => x.CreateTime);
						issuesSolved = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.Recurrence.Id == recurrenceId && x.CloseTime > createTime).List().Count;
						todosCreated = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CreateTime > createTime).List().ToList();
						allTodos = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CompleteTime == null).List().ToList();
						oldTodos = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CreateTime < createTime && (x.CompleteTime == null || x.CompleteTime > createTime)).List().ToList();
						version = 1;
						if (prevMeeting != null && prevMeeting.CompleteTime != null)
							todoComplete = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CompleteTime > prevMeeting.CompleteTime).List().Count;
					} else {
						version = 2;
						issuesSolved = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.Recurrence.Id == recurrenceId && x.CloseTime > meeting.CreateTime && x.CloseTime < meeting.CompleteTime).List().Count;
						todosCreated = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CreateTime > meeting.CreateTime && x.CreateTime < meeting.CompleteTime).List().ToList();
						allTodos = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CompleteTime == null).List().ToList();
						oldTodos = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CreateTime < meeting.CreateTime && (x.CompleteTime == null || x.CompleteTime > meeting.CreateTime)).List().ToList();
						if (prevMeeting != null && prevMeeting.CompleteTime != null)
							todoComplete = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CompleteTime > prevMeeting.CompleteTime && x.CompleteTime < meeting.CompleteTime).List().Count;
						var ratings = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.L10Meeting.Id == meeting.Id && x.DeleteTime == null).List().Where(x => x.Rating != null).Select(x => x.Rating.Value).ToList();
						if (ratings.Any()) {
							rating = (double)ratings.Average();
						}

					}

					foreach (var todo in todosCreated) {
						todo.AccountableUser.NotNull(x => x.GetName());
						todo.AccountableUser.NotNull(x => x.ImageUrl(true));
					}

					foreach (var todo in allTodos) {
						todo.AccountableUser.NotNull(x => x.GetName());
						todo.AccountableUser.NotNull(x => x.ImageUrl(true));
					}
					var completion = 0m;
					//try {
					//	var todosForRecur = L10Accessor.GetTodosForRecurrence(s, perms, recurrenceId, meeting.NotNull(x => x.Id));
					//	todosForRecur.Where(x=>x.CreateTime<(meeting.NotNull(y=>y.StartTime)??DateTime.MaxValue)).Select(x=>x.CompleteTime==null?0:1)

					//} catch (Exception e) {

					if (oldTodos.Count() > 0) {
						completion = (decimal)oldTodos.Count(x => x.CompleteTime != null) / (decimal)oldTodos.Count() * 100m;
					}
					if (meeting.TodoCompletion != null)
						completion = meeting.TodoCompletion.GetValue(0) * 100m;
					//}

					var stats = new L10MeetingStatsVM() {
						IssuesSolved = issuesSolved,
						TodosCreated = todosCreated,
						AllMeetings = o,
						StartTime = meeting.NotNull(x => x.StartTime),
						EndTime = meeting.NotNull(x => x.CompleteTime),
						TodoCompleted = todoComplete,
						AverageRating = rating,
						AllTodos = allTodos,
						TodoCompletionPercentage = completion,
						Version = version
					};

					//if (stats.StartTime != null)
					//	stats.StartTime = caller.Organization.ConvertFromUTC(stats.StartTime.Value);
					//if (stats.EndTime != null)
					//	stats.EndTime = caller.Organization.ConvertFromUTC(stats.EndTime.Value);

					return stats;
				}
			}
		}
		#endregion
	}
}