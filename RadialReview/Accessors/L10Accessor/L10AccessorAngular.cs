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

		#region Angular

		public static async Task<AngularRecurrence> GetOrGenerateAngularRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeScores = true, bool includeHistorical = true, bool fullScorecard = true, DateRange range = null, bool forceIncludeTodoCompletion = false, DateRange scorecardRange = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var angular = await GetOrGenerateAngularRecurrence(s, perms, recurrenceId, includeScores, includeHistorical, fullScorecard, range, forceIncludeTodoCompletion, scorecardRange);

					tx.Commit();
					s.Flush();

					return angular;
				}
			}
		}

		[Obsolete("Must call commit")]
		public static async Task<AngularRecurrence> GetOrGenerateAngularRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool includeScores = true, bool includeHistorical = true, bool fullScorecard = true, DateRange range = null, bool forceIncludeTodoCompletion = false, DateRange scorecardRange = null) {
			perms.ViewL10Recurrence(recurrenceId);
			var recurrence = s.Get<L10Recurrence>(recurrenceId);
			_LoadRecurrences(s, true, true, true, true, recurrence);

			var recur = new AngularRecurrence(recurrence);

			recur.Attendees = recurrence._DefaultAttendees.Select(x => {
				var au = AngularUser.CreateUser(x.User);
				au.CreateTime = x.CreateTime;
				return au;
			}).ToList();

			scorecardRange = scorecardRange ?? range;
			DateRange lookupRange = null;
			if (range != null) {
				lookupRange = new DateRange(range.StartTime, range.EndTime);
			}

			if (fullScorecard) {
				var period = perms.GetCaller().GetTimeSettings().Period;

				switch (period) {
					case ScorecardPeriod.Monthly:
						scorecardRange = new DateRange(DateTime.UtcNow.AddMonths(-12).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddMonths(1).StartOfWeek(DayOfWeek.Sunday));
						lookupRange = new DateRange(DateTime.UtcNow.AddMonths(-12).AddDays(-37).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddMonths(1).StartOfWeek(DayOfWeek.Sunday));
						break;
					case ScorecardPeriod.Quarterly:
						scorecardRange = new DateRange(DateTime.UtcNow.AddMonths(-36).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddMonths(1).StartOfWeek(DayOfWeek.Sunday));
						lookupRange = new DateRange(DateTime.UtcNow.AddMonths(-36).AddDays(-37).AddMonths(-37).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddMonths(1).StartOfWeek(DayOfWeek.Sunday));
						break;
					default:
						scorecardRange = new DateRange(DateTime.UtcNow.AddDays(-7 * 13).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddDays(9).StartOfWeek(DayOfWeek.Sunday));
						lookupRange = new DateRange(DateTime.UtcNow.AddDays(-7 * 14).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddDays(9).StartOfWeek(DayOfWeek.Sunday));
						break;
				}
			}
			var scores = new List<ScoreModel>();

			var scoresAndMeasurables = await GetOrGenerateScorecardDataForRecurrence(s, perms, recurrenceId, true, range: lookupRange, getMeasurables: true, getScores: includeScores, forceIncludeTodoCompletion: forceIncludeTodoCompletion, queryCache: recurrence._CacheQueries);

			if (includeScores) {
				scores = scoresAndMeasurables.Scores;
			}

			var measurables = scoresAndMeasurables.MeasurablesAndDividers.Select(x => {
				if (x.IsDivider) {
					var m = AngularMeasurable.CreateDivider(x._Ordering, x.Id);
					m.RecurrenceId = x.L10Recurrence.Id;
					return m;
				} else {
					var m = new AngularMeasurable(x.Measurable, false);
					m.Ordering = x._Ordering;
					m.RecurrenceId = x.L10Recurrence.Id;
					return m;
				}
			}).ToList();

			if (recurrence.IncludeAggregateTodoCompletion || forceIncludeTodoCompletion) {
				measurables.Add(new AngularMeasurable(TodoMeasurable) {
					Ordering = -2
				});
			}

			var ts = perms.GetCaller().GetTimeSettings();
			ts.WeekStart = recurrence.StartOfWeekOverride ?? ts.WeekStart;
			recur.Scorecard = new AngularScorecard(recurrenceId, ts, measurables, scores, DateTime.UtcNow, scorecardRange, reverseScorecard: recurrence.ReverseScorecard);

			var allRocks = recurrence._DefaultRocks.Select(x => new AngularRock(x)).ToList();

			if (range != null) {
				RockModel rockAlias = null;
				var histRock = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
					.Where(x => x.DeleteTime != null && x.L10Recurrence.Id == recurrenceId)
					.Where(range.Filter<L10Recurrence.L10Recurrence_Rocks>())
					.List();

				allRocks.AddRange(histRock.Select(x => new AngularRock(x)));
			}
			recur.Rocks = allRocks.Distinct(x => x.Id);
			recur.Todos = GetAllTodosForRecurrence(s, perms, recurrenceId, includeClosed: includeHistorical, range: range).Select(x => new AngularTodo(x)).OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ToList();
			recur.IssuesList.Issues = GetAllIssuesForRecurrence(s, perms, recurrenceId, includeCompleted: includeHistorical, range: range).Select(x => new AngularIssue(x)).OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ToList();
			recur.Headlines = GetAllHeadlinesForRecurrence(s, perms, recurrenceId, includeClosed: includeHistorical, range: range).Select(x => new AngularHeadline(x)).OrderByDescending(x => x.CloseTime ?? DateTime.MaxValue).ToList();
			recur.Notes = recurrence._MeetingNotes.Select(x => new AngularMeetingNotes(x)).ToList();

			recur.ShowSegue = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Segue);
			recur.ShowScorecard = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Scorecard);
			recur.ShowRockReview = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Rocks);
			recur.ShowHeadlines = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Headlines);
			recur.ShowTodos = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Todo);
			recur.ShowIDS = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.IDS);
			recur.ShowConclude = recurrence._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Conclude);

			recur.SegueMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Segue).NotNull(x => (decimal?)x.Minutes);
			recur.ScorecardMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Scorecard).NotNull(x => (decimal?)x.Minutes);
			recur.RockReviewMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Rocks).NotNull(x => (decimal?)x.Minutes);
			recur.HeadlinesMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Headlines).NotNull(x => (decimal?)x.Minutes);
			recur.TodosMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Todo).NotNull(x => (decimal?)x.Minutes);
			recur.IDSMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.IDS).NotNull(x => (decimal?)x.Minutes);
			recur.ConcludeMinutes = recurrence._Pages.FirstOrDefault(x => x.PageType == L10Recurrence.L10PageType.Conclude).NotNull(x => (decimal?)x.Minutes);

			recur.MeetingType = recurrence.MeetingType;


			if (range == null) {
				recur.date = new AngularDateRange() {
					startDate = DateTime.UtcNow.Date.AddDays(-9),
					endDate = DateTime.UtcNow.Date.AddDays(1),
				};
			} else {
				recur.date = new AngularDateRange() {
					startDate = range.StartTime,
					endDate = range.EndTime,
				};
			}

			recur.HeadlinesUrl = Config.NotesUrl() + "p/" + recurrence.HeadlinesId + "?showControls=true&showChat=false";
			return recur;
		}

		public static async Task Remove(UserOrganizationModel caller, BaseAngular model, long recurrenceId, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create(connectionId)) {
						var perms = PermissionsUtility.Create(s, caller);
						perms.EditL10Recurrence(recurrenceId);

						if (model.Type == typeof(AngularIssue).Name) {
							await CompleteIssue(s, perms, rt, model.Id);
						} else if (model.Type == typeof(AngularTodo).Name) {
							await TodoAccessor.CompleteTodo(s, perms, model.Id);
						} else if (model.Type == typeof(AngularRock).Name) {
							await RemoveRock(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularMeasurable).Name) {
							await DetachMeasurable(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularUser).Name) {
							await RemoveAttendee(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularHeadline).Name) {
							await RemoveHeadline(s, perms, rt, model.Id);
						} else {
							throw new PermissionsException("Unhandled type: " + model.Type);
						}

						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static async Task UnArchive(UserOrganizationModel caller, BaseAngular model, long recurrenceId, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create(connectionId)) {
						var perms = PermissionsUtility.Create(s, caller);
						perms.EditL10Recurrence(recurrenceId);

						if (model.Type == typeof(AngularIssue).Name) {
							await UnarchiveIssue(s, perms, rt, model.Id);
						} else if (model.Type == typeof(AngularTodo).Name) {
							//await TodoAccessor.CompleteTodo(s, perms, model.Id);
						} else if (model.Type == typeof(AngularRock).Name) {
							await UnarchiveRock(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularMeasurable).Name) {
							//await DetachMeasurable(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularUser).Name) {
							//await RemoveAttendee(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularHeadline).Name) {
							await UnarchiveHeadline(s, perms, rt, model.Id);
						} else {
							throw new PermissionsException("Unhandled type: " + model.Type);
						}

						tx.Commit();
						s.Flush();
					}
				}
			}
		}



		public static async Task UnarchiveIssue(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceIssue) {
			var issue = s.Get<IssueModel.IssueModel_Recurrence>(recurrenceIssue);
			perm.EditL10Recurrence(issue.Recurrence.Id);
			if (issue.CloseTime == null)
				throw new PermissionsException("Issue already unarchived.");
			await IssuesAccessor.EditIssue(s, perm, recurrenceIssue, complete: false);
		}


		public static async Task UnarchiveHeadline(ISession s, PermissionsUtility perm, RealTimeUtility rt, long headlineId) {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

			perm.ViewHeadline(headlineId);

			var r = s.Get<PeopleHeadline>(headlineId);

			if (r.CloseTime == null)
				throw new PermissionsException("Headline already unarchived.");

			perm.EditL10Recurrence(r.RecurrenceId);

			var now = DateTime.UtcNow;
			r.CloseTime = null;
			s.Update(r);

			await HooksRegistry.Each<IHeadlineHook>((ses, x) => x.UnArchiveHeadline(ses, r));
		}

		//[Untested("Vto_Rocks",/* "Is the rock correctly removed in real-time from L10",/* "Is the rock correctly removed in real-time from VTO",*/ "Is rock correctly archived when existing in no meetings?")]
		public static async Task UnarchiveRock(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long rockId) {
			perm.AdminL10Recurrence(recurrenceId).EditRock(rockId);

			await RockAccessor.UnArchiveRock(s, perm, rockId);

			// attach rock
			await AttachRock(s, perm, recurrenceId, rockId, false);

			//perm.AdminL10Recurrence(recurrenceId).EditRock(rockId);
			//var rocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
			//    .Where(x => x.L10Recurrence.Id == recurrenceId && x.ForRock.Id == rockId)
			//    .List().ToList();
			//DateTime? now = null;                       
		}


		public static async Task Update(UserOrganizationModel caller, BaseAngular model, string connectionId) {
			if (model.Type == typeof(AngularIssue).Name) {
				var m = (AngularIssue)model;
				//UpdateIssue(caller, (long)model.GetOrDefault("Id", null), (string)model.GetOrDefault("Name", null), (string)model.GetOrDefault("Details", null), (bool?)model.GetOrDefault("Complete", null), connectionId);
				await IssuesAccessor.EditIssue(caller, m.Id, m.Name ?? "", m.Complete, priority: m.Priority, owner: m.Owner.NotNull(x => (long?)x.Id));
			} else if (model.Type == typeof(AngularTodo).Name) {
				var m = (AngularTodo)model;
				if (m.TodoType == TodoType.Milestone) {
					RockAccessor.EditMilestone(caller, -m.Id, m.Name, m.DueDate, status: m.Complete == true ? MilestoneStatus.Done : MilestoneStatus.NotDone, connectionId: connectionId);
				} else {
					//await UpdateTodo(caller, m.Id, m.Name ?? "", null, m.DueDate, m.Owner.NotNull(x => (long?)x.Id), m.Complete, connectionId);
					await TodoAccessor.UpdateTodo(caller, m.Id, m.Name, m.DueDate, m.Owner.NotNull(x => (long?)x.Id), m.Complete);// null, m.DueDate, m.Owner.NotNull(x => (long?)x.Id), m.Complete, connectionId);
				}
			} else if (model.Type == typeof(AngularScore).Name) {
				var m = (AngularScore)model;
				await ScorecardAccessor.UpdateScore(caller, m.Id, m.Measurable.Id, TimingUtility.GetDateSinceEpoch(m.ForWeek), m.Measured);

				//if (m.Id > 0)
				//	UpdateScore(caller, m.Id, m.Measured, connectionId, /*true*/ false);
				////else
				////	throw new Exception("Shouldn't get here");
				//else
				//	await UpdateScore(caller, m.Measurable.Id, m.ForWeek, m.Measured, connectionId, false);
			} else if (model.Type == typeof(AngularMeetingNotes).Name) {
				var m = (AngularMeetingNotes)model;
				EditNote(caller, m.Id,/* m.Contents,*/ m.Title, connectionId);
			} else if (model.Type == typeof(AngularRock).Name) {
				var m = (AngularRock)model;
				//TODO re-add company rock
				await UpdateRock(caller, m.Id, m.Name, m.Completion, m.Owner.NotNull(x => (long?)x.Id), connectionId, recurrenceRockId: m.RecurrenceRockId, vtoRock: m.VtoRock);
			} /*else if (model.Type == typeof(AngularMeasurable).Name) {
                var m = (AngularMeasurable)model;
                await ScorecardAccessor.UpdateMeasurable(caller, m.Id, m.Name, m.Direction, m.Target, m.Owner.NotNull(x => (long?)x.Id), m.Admin.NotNull(x => (long?)x.Id));
                //UpdateArchiveMeasurable(caller, m.Id, m.Name, m.Direction, m.Target, m.Owner.NotNull(x => (long?)x.Id), m.Admin.NotNull(x => (long?)x.Id), connectionId);
            }*/ else if (model.Type == typeof(AngularBasics).Name) {
				var m = (AngularBasics)model;
				await UpdateRecurrence(caller, m.Id, m.Name, m.TeamType, connectionId);
			} else if (model.Type == typeof(AngularHeadline).Name) {
				var m = (AngularHeadline)model;
				await HeadlineAccessor.UpdateHeadline(caller, m.Id, m.Name, connectionId);
			} else {
				throw new PermissionsException("Unhandled type: " + model.Type);
			}
		}
		public static async Task UpdateRecurrence(UserOrganizationModel caller, long recurrenceId, string name = null, L10TeamType? teamType = null, string connectionId = null) {
			using (var rt = RealTimeUtility.Create(connectionId)) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {

						var perms = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);
						var recurrence = s.Get<L10Recurrence>(recurrenceId);

						if (recurrence.DeleteTime != null)
							throw new PermissionsException();

						var angular = new AngularBasics(recurrenceId);

						if (name != null && recurrence.Name != name) {
							recurrence.Name = name;
							angular.Name = name;
							await Depristine_Unsafe(s, caller, recurrence);
						}

						if (teamType != null && recurrence.TeamType != teamType) {
							recurrence.TeamType = teamType.Value;
							angular.TeamType = teamType;
							await Depristine_Unsafe(s, caller, recurrence);
						}

						s.Update(recurrence);
						rt.UpdateRecurrences(recurrenceId).Update(angular);

						tx.Commit();
						s.Flush();
					}
				}
			}
		}
		#endregion
	}
}