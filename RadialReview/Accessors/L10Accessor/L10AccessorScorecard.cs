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
		#region Scorecard	

		#region Models   
		public class ScorecardData {
			public List<ScoreModel> Scores { get; set; }
			public List<MeasurableModel> Measurables { get; set; }
			public List<L10Recurrence.L10Recurrence_Measurable> MeasurablesAndDividers { get; set; }
			public TimeData TimeSettings { get; set; }

			public ScorecardData() { }
			public static ScorecardData FromScores(List<ScoreModel> scores) {
				return new ScorecardData() {
					Scores = scores,
					Measurables = scores.GroupBy(x => x.MeasurableId).Select(x => x.First().Measurable).ToList(),
					MeasurablesAndDividers = scores.GroupBy(x => x.MeasurableId).Select(x => new L10Recurrence.L10Recurrence_Measurable() {
						Measurable = x.First().Measurable
					}).ToList(),

				};
			}
		}
		#endregion

		#region Attach

		public static async Task AttachMeasurable(UserOrganizationModel caller, long recurrenceId, long measurableId, bool skipRealTime = false, int? rowNum = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);

						await AttachMeasurable(s, perms, recurrenceId, measurableId, skipRealTime, rowNum);

						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static async Task AttachMeasurable(ISession s, PermissionsUtility perm, long recurrenceId, long measurableId, bool skipRealTime = false, int? rowNum = null, DateTime? now = null) {
			perm.AdminL10Recurrence(recurrenceId);
			var measurable = s.Get<MeasurableModel>(measurableId);
			if (measurable == null)
				throw new PermissionsException("Measurable does not exist.");
			perm.ViewMeasurable(measurable.Id);

			var alreadyExist = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.Measurable.Id == measurableId).RowCount() > 0;
			if (alreadyExist) {
				throw new PermissionsException("Measurable already attached to meeting");
			}

			if (rowNum == null) {
				var orders = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).Select(x => x._Ordering).List<int>().ToList();
				if (orders.Any()) {
					rowNum = orders.Max() + 1;
				}
			}


			var rm = new L10Recurrence.L10Recurrence_Measurable() {
				CreateTime = now ?? DateTime.UtcNow,
				L10Recurrence = s.Load<L10Recurrence>(recurrenceId),
				Measurable = measurable,
				_Ordering = rowNum ?? 0
			};
			s.Save(rm);

			await HooksRegistry.Each<IMeetingMeasurableHook>((ses, x) => x.AttachMeasurable(ses, perm.GetCaller(), measurable, rm));
		}
		public static void CreateMeasurableDivider(UserOrganizationModel caller, long recurrenceId, int ordering = -1) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);
					var recur = s.Get<L10Recurrence>(recurrenceId);
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

					var now = DateTime.UtcNow;

					var divider = new L10Recurrence.L10Recurrence_Measurable() {
						_Ordering = ordering,
						IsDivider = true,
						L10Recurrence = recur,
						Measurable = null,
					};

					s.Save(divider);


					var current = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);
					//var l10Scores = L10Accessor.GetScoresForRecurrence(s, perm, recurrenceId);
					if (current != null) {


						var mm = new L10Meeting.L10Meeting_Measurable() {
							L10Meeting = current,
							Measurable = null,
							IsDivider = true,

						};
						s.Save(mm);

						var settings = current.Organization.Settings;
						var sow = settings.WeekStart;
						var offset = current.Organization.GetTimezoneOffset();
						var scorecardType = settings.ScorecardPeriod;

#pragma warning disable CS0618 // Type or member is obsolete
						var ts = current.Organization.GetTimeSettings();
#pragma warning restore CS0618 // Type or member is obsolete
						ts.Descending = recur.ReverseScorecard;

						var weeks = TimingUtility.GetPeriods(ts, now, current.StartTime, true);


						var rowId = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).RowCount();
						// var rowId = l10Scores.GroupBy(x => x.MeasurableId).Count();

						var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM {
							MeetingId = current.Id,
							RecurrenceId = recurrenceId,
							MeetingMeasurable = mm,
							IsDivider = true,
							Weeks = weeks
						});
						row.ViewData["row"] = rowId - 1;

						var first = row.Execute();
						row.ViewData["ShowRow"] = false;
						var second = row.Execute();
						group.addMeasurable(first, second);
					}
					var scorecard = new AngularScorecard(recurrenceId);
					scorecard.Measurables = new List<AngularMeasurable>() { AngularMeasurable.CreateDivider(divider._Ordering, divider.Id) };
					scorecard.Scores = new List<AngularScore>();

					group.update(new AngularUpdate() { scorecard });

					Audit.L10Log(s, caller, recurrenceId, "CreateMeasurableDivider", ForModel.Create(divider));


					tx.Commit();
					s.Flush();
				}
			}
		}
		#endregion

		#region Get

		public static async Task<ScorecardData> GetOrGenerateScorecardDataForRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeAutoGenerated = true, DateTime? now = null, DateRange range = null, bool getMeasurables = false, bool getScores = true, L10Recurrence.L10LookupCache queryCache = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					var ang = await GetOrGenerateScorecardDataForRecurrence(s, perm, recurrenceId, includeAutoGenerated: includeAutoGenerated, now: now, range: range, getMeasurables: getMeasurables, getScores: getScores, queryCache: queryCache);
					tx.Commit();
					s.Flush();
					return ang;
				}
			}
		}


		public static async Task<List<ScoreModel>> GetOrGenerateScoresForRecurrence(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);//.ViewL10Recurrence(recurrenceId);
					var angulareRecur = await GetOrGenerateScoresForRecurrence(s, perm, recurrenceId);
					tx.Commit();
					s.Flush();
					return angulareRecur;
				}
			}
		}

		[Obsolete("Must call commit")]
		public static async Task<ScorecardData> GetOrGenerateScorecardDataForRecurrence(ISession s, PermissionsUtility perm, long recurrenceId,
					bool includeAutoGenerated = true, DateTime? now = null, DateRange range = null, bool getMeasurables = false, bool getScores = true, bool forceIncludeTodoCompletion = false, L10Recurrence.L10LookupCache queryCache = null) {

			queryCache = queryCache ?? new L10Recurrence.L10LookupCache(recurrenceId);

			if (queryCache.RecurrenceId != recurrenceId)
				throw new PermissionsException("Id does not match");

			var now1 = now ?? DateTime.UtcNow;
			perm.ViewL10Recurrence(recurrenceId);

			if (forceIncludeTodoCompletion) {
				includeAutoGenerated = true;
			}

			var recurrenceMeasurables = queryCache.GetAllMeasurablesAndDividers(() => {
				MeasurableModel mAlias = null;
				return s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
							.JoinAlias(x => x.Measurable, () => mAlias, JoinType.LeftOuterJoin)
							.Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null && (x.Measurable == null || mAlias.DeleteTime == null))
							.List().ToList();
			});

			var measurableModels = recurrenceMeasurables.Where(x => x.Measurable != null).Distinct(x => x.Measurable.Id).Select(x => x.Measurable).ToList();
			var measurables = measurableModels.Select(x => x.Id).ToList();

			var scoreModels = new List<ScoreModel>();
			IEnumerable<ScoreModel> scoresF = null;

			if (getScores) {
				var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null);
				if (range != null) {
					var st = range.StartTime.StartOfWeek(DayOfWeek.Sunday);
					var et = range.EndTime.AddDays(7).StartOfWeek(DayOfWeek.Sunday);
					scoresQ = scoresQ.Where(x => x.ForWeek >= st && x.ForWeek <= et);
				}
				scoresF = scoresQ.WhereRestrictionOn(x => x.MeasurableId).IsIn(measurables).Future();
			}
			//List<MeasurableModel> measurableModels = null;
			//if (getMeasurables) {
			//    measurableModels = s.QueryOver<MeasurableModel>().WhereRestrictionOn(x => x.Id).IsIn(measurables).Future().ToList();
			//}
			if (getScores) {
				scoreModels = scoresF.ToList();
				if (scoreModels.Any() || range != null) {


					var rangeTemp = range;
					if (rangeTemp == null) {
						var minDate = Math2.Max(new DateTime(2013, 1, 1), scoreModels.Select(x => x.ForWeek).Min());
						var maxDate = Math2.Min(DateTime.UtcNow.AddDays(14), scoreModels.Select(x => x.ForWeek).Max());
						rangeTemp = new DateRange(minDate, maxDate);
					}

					var extra = await ScorecardAccessor._GenerateScoreModels_AddMissingScores_Unsafe(s, rangeTemp, measurables, scoreModels);
					scoreModels.AddRange(extra);
				}
			}

			var recur = s.Get<L10Recurrence>(recurrenceId);

			var ts = perm.GetCaller().GetTimeSettings();
			ts.WeekStart = recur.StartOfWeekOverride ?? ts.WeekStart;
			ts.Descending = recur.ReverseScorecard;

			if (includeAutoGenerated && (recur.IncludeAggregateTodoCompletion || recur.IncludeIndividualTodos || forceIncludeTodoCompletion)) {
				var currentTime = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false).NotNull(x => x.StartTime);
				List<TodoModel> todoCompletion = null;
				todoCompletion = GetAllTodosForRecurrence(s, perm, recurrenceId);

				var periods = TimingUtility.GetPeriods(ts, now1, currentTime, true);

				if (getScores && (recur.IncludeAggregateTodoCompletion || forceIncludeTodoCompletion)) {
					var todoScores = periods.Select(x => x.ForWeek).SelectMany(w => {
						try {
							var rangeTodos = TimingUtility.GetRange(perm.GetCaller().Organization, w.AddDays(-7));
							var ss = GetTodoCompletion(todoCompletion, rangeTodos.StartTime, rangeTodos.EndTime, currentTime);
							decimal? percent = null;
							if (ss.IsValid()) {
								percent = Math.Round(ss.GetValue(0) * 100m, 1);
							}
							return new ScoreModel() {
								_Editable = false,
								AccountableUserId = -1,
								ForWeek = w,
								Measurable = TodoMeasurable,
								Measured = percent,
								MeasurableId = TodoMeasurable.Id,
								OriginalGoalDirection = TodoMeasurable.GoalDirection,
								OriginalGoal = TodoMeasurable.Goal
							}.AsList();
						} catch (Exception) {
							return new List<ScoreModel>();
						}
					});
					scoreModels.AddRange(todoScores);
				}

				if (getScores && (recur.IncludeIndividualTodos || forceIncludeTodoCompletion)) {
					var individualTodoScores = periods.Select(x => x.ForWeek).SelectMany(ww => {
						return todoCompletion.GroupBy(x => x.AccountableUserId).SelectMany(todos => {
							var a = todos.First().AccountableUser;
							try {
								var rangeTodos = TimingUtility.GetRange(perm.GetCaller().Organization, ww.AddDays(-7));
								var ss = GetTodoCompletion(todos.ToList(), rangeTodos.StartTime, rangeTodos.EndTime, currentTime);
								decimal? percent = null;
								if (ss.IsValid()) {
									percent = Math.Round(ss.GetValue(0) * 100m, 1);
								}
								var mm = GenerateTodoMeasureable(a);
								return new ScoreModel() {
									_Editable = false,
									AccountableUserId = a.Id,
									ForWeek = ww,
									Measurable = mm,
									Measured = percent,
									MeasurableId = mm.Id,
									OriginalGoal = mm.Goal,
									OriginalGoalDirection = mm.GoalDirection

								}.AsList();
							} catch (Exception) {
								return new List<ScoreModel>();
							}
						});
					});
					scoreModels.AddRange(individualTodoScores);
				}
			}

			var userQueries = scoreModels.SelectMany(x => {
				var o = new List<long>(){
					x.Measurable.AccountableUser.NotNull(y => y.Id),
					x.AccountableUser.NotNull(y => y.Id),
					x.Measurable.AdminUser.NotNull(y => y.Id),
				};
				return o;
			}).Distinct().ToList();

			//CUMULATIVE
			if (getMeasurables) {
				_RecalculateCumulative_Unsafe(s, null, measurableModels, recur.AsList());
			}

			//Touch 
			if (getScores) {
				var allUserIds = userQueries;
				var __allUsers = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(allUserIds).List().ToList();


				foreach (var a in scoreModels) {
					try {
						if (a.Measurable != null) {
							var i = a.Measurable.Goal;
							if (a.Measurable.AccountableUser != null) {
								var u = a.Measurable.AccountableUser.GetName();
								var v = a.Measurable.AccountableUser.ImageUrl(true);
							}
							if (a.Measurable.AdminUser != null) {
								var u1 = a.Measurable.AdminUser.GetName();
								var v1 = a.Measurable.AdminUser.ImageUrl(true);
							}
						}
						if (a.AccountableUser != null) {
							var j = a.AccountableUser.GetName();
							var k = a.AccountableUser.ImageUrl(true);
						}
					} catch (Exception) {
						//Opps
					}
				}
			}

			if (recur.PreventEditingUnownedMeasurables) {
				var userId = perm.GetCaller().Id;
				scoreModels.ForEach(x => {
					if (x.Measurable != null) {
						x._Editable = x.Measurable.AccountableUserId == userId || x.Measurable.AdminUserId == userId;
					}
				});
				if (getMeasurables) {
					measurableModels.ForEach(x => x._Editable = x.AccountableUserId == userId || x.AdminUserId == userId);
				}
				recurrenceMeasurables.ForEach(x => {
					if (x.Measurable != null) {
						x.Measurable._Editable = x.Measurable.AccountableUserId == userId || x.Measurable.AdminUserId == userId;
					}
				});
			}

			return new ScorecardData() {
				Scores = scoreModels,
				Measurables = measurableModels,
				MeasurablesAndDividers = recurrenceMeasurables,
				TimeSettings = ts
			};
		}

		[Obsolete("Must call commit")]
		public static async Task<List<ScoreModel>> GetOrGenerateScoresForRecurrence(ISession s, PermissionsUtility perm, long recurrenceId, bool includeAutoGenerated = true, DateTime? now = null, DateRange range = null) {
			var sam = await GetOrGenerateScorecardDataForRecurrence(s, perm, recurrenceId, includeAutoGenerated, now, range);
			return sam.Scores;
		}

		public static List<Tuple<long, int?, bool>> GetMeasurableOrdering(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					return s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null)
						.Select(x => x.Measurable.Id, x => x._Ordering, x => x.IsDivider)
						.List<object[]>()
						.Where(x => x[0] != null)
						.Select(x => {
							return Tuple.Create((long)x[0], (int?)x[1], (bool)x[2]);
						}).ToList();

				}
			}
		}

		#endregion

		#region Update
        [Untested("ESA")]
		public static async Task SetMeetingMeasurableOrdering(UserOrganizationModel caller, long recurrenceId, List<long> orderedL10Meeting_Measurables) {
            //using (var s = HibernateSession.GetCurrentSession()) {
            //	using (var tx = s.BeginTransaction()) {
            await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.MeasurableReorder(recurrenceId), async s => {
                PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

                //SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.MeasurableReorder(recurrenceId));

                var l10measurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>().WhereRestrictionOn(x => x.Id).IsIn(orderedL10Meeting_Measurables).Where(x => x.DeleteTime == null).List().ToList();

                if (!l10measurables.Any())
                    throw new PermissionsException("None found.");
                if (l10measurables.GroupBy(x => x.L10Meeting.Id).Count() > 1)
                    throw new PermissionsException("Measurables must be part of the same meeting");
                if (l10measurables.First().L10Meeting.L10RecurrenceId != recurrenceId)
                    throw new PermissionsException("Not part of the specified L10");
                var recurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null).List().ToList();

                for (var i = 0; i < orderedL10Meeting_Measurables.Count; i++) {
                    var id = orderedL10Meeting_Measurables[i];
                    var f = l10measurables.FirstOrDefault(x => x.Id == id);
                    if (f != null) {
                        f._Ordering = i;
                        s.Update(f);
                        var g = recurMeasurables.FirstOrDefault(x => (x.Measurable != null && f.Measurable != null && x.Measurable.Id == f.Measurable.Id) || ((x.Measurable == null && f.Measurable == null) && !x._WasModified));
                        if (g != null) {
                            g._WasModified = true;
                            g._Ordering = i;
                            s.Update(g);
                        }
                    }
                }

                Audit.L10Log(s, caller, recurrenceId, "SetMeasurableOrdering", null);

                //tx.Commit();
                //s.Flush();

                var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

                group.reorderMeasurables(orderedL10Meeting_Measurables);

                var updates = new AngularUpdate();
                foreach (var x in recurMeasurables) {
                    if (x.IsDivider) {
                        updates.Add(AngularMeasurable.CreateDivider(x._Ordering, x.Id));
                    } else {
                        updates.Add(new AngularMeasurable(x.Measurable) { Ordering = x._Ordering });
                    }
                }
                group.update(updates);
                //	}
                //}
            });
		}
		public static async Task SetRecurrenceMeasurableOrdering(UserOrganizationModel caller, long recurrenceId, List<long> orderedL10Recurrene_Measurables) {
            //using (var s = HibernateSession.GetCurrentSession()) {
            //	using (var tx = s.BeginTransaction()) {
            await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.MeasurableReorder(recurrenceId), async s => {
                var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);


                //SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.MeasurableReorder(recurrenceId));

                /*var l10measurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                    .WhereRestrictionOn(x => x.Measurable.Id).IsIn(orderedL10Recurrene_Measurables)
                    .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
                    .List().ToList();*/
                MeasurableModel mm = null;

                var l10RecurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().JoinAlias(p => p.Measurable, () => mm)
                    .WhereRestrictionOn(() => mm.Id)
                    .IsIn(orderedL10Recurrene_Measurables.Where(x => x >= 0).ToArray())
                    .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
                    .List<L10Recurrence.L10Recurrence_Measurable>();

                var dividers = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                    .WhereRestrictionOn(x => x.Id)
                    .IsIn(orderedL10Recurrene_Measurables.Where(x => x < 0).Select(x => -x).ToArray())
                    .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
                    .List<L10Recurrence.L10Recurrence_Measurable>();



                if (!l10RecurMeasurables.Any())
                    throw new PermissionsException("None found.");
                if (l10RecurMeasurables.GroupBy(x => x.L10Recurrence.Id).Count() > 1)
                    throw new PermissionsException("Measurables must be part of the same meeting");
                if (l10RecurMeasurables.First().L10Recurrence.Id != recurrenceId)
                    throw new PermissionsException("Not part of the specified L10");
                var recurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null).List().ToList();

                var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

                var meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
                if (meeting != null) {
                    var l10MeetingMeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
                        .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
                        .List().ToList();/*.JoinAlias(p => p.Measurable, () => mm)
							.WhereRestrictionOn(() => mm.Id)
							.IsIn(orderedL10Recurrene_Measurables)
							.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
							.List<L10Meeting.L10Meeting_Measurable>();*/




                    var orderedL10Meeting_Measurables = new List<long>();
                    for (var i = 0; i < orderedL10Recurrene_Measurables.Count; i++) {
                        var id = orderedL10Recurrene_Measurables[i];
                        var f = l10MeetingMeasurables.FirstOrDefault(x => (x.Measurable != null && x.Measurable.Id == id) || (x.Measurable == null && !x._WasModified));
                        if (f != null) {
                            f._WasModified = true;
                            f._Ordering = i;
                            s.Update(f);
                            /*var g = l10MeetingMeasurables.FirstOrDefault(x => 
                                (x.Measurable != null && f.Measurable != null && x.Measurable.Id == f.Measurable.Id) 
                                || ((x.Measurable == null && f.Measurable == null) && !x._WasModified));
                            if (g != null)
                            {
                                g._WasModified = true;
                                g._Ordering = i;
                                s.Update(g);
                            }*/
                            orderedL10Meeting_Measurables.Add(f.Id);

                        }
                    }

                    group.reorderMeasurables(orderedL10Meeting_Measurables);
                }

                for (var i = 0; i < orderedL10Recurrene_Measurables.Count; i++) {
                    var id = orderedL10Recurrene_Measurables[i];
                    var f = l10RecurMeasurables.FirstOrDefault(x => x.Measurable.Id == id) ?? dividers.FirstOrDefault(x => x.Id == -id);
                    if (f != null) {
                        f._Ordering = i;
                        s.Update(f);
                        /*var g = recurMeasurables.FirstOrDefault(x => (x.Measurable != null && f.Measurable != null && x.Measurable.Id == f.Measurable.Id) || (x.Measurable == null && f.Measurable == null && x.Id==f.Id));
                        if (g != null)
                        {
                            g._Ordering = i;
                            s.Update(g);
                        }*/
                    } else {
                        //int a = 0;
                    }
                }

                Audit.L10Log(s, caller, recurrenceId, "SetMeasurableOrdering", null);

                //tx.Commit();
                //s.Flush();



                group.reorderRecurrenceMeasurables(orderedL10Recurrene_Measurables);

                var updates = new AngularUpdate();
                foreach (var x in recurMeasurables) {
                    if (x.IsDivider) {
                        updates.Add(AngularMeasurable.CreateDivider(x._Ordering, x.Id));
                    } else {
                        updates.Add(new AngularMeasurable(x.Measurable) { Ordering = x._Ordering });
                    }
                }
                group.update(updates);

            });
            //	}
            //}
        }
		#endregion

		#region Detatch
		public static async Task DetachMeasurable(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long measurableId) {
			perm.AdminL10Recurrence(recurrenceId);
			//Probably only one...
			var meetingMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
				.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.Measurable.Id == measurableId)
				.List().ToList();

			if (!meetingMeasurables.Any())
				throw new PermissionsException("Measurable does not exist.");
			var now = DateTime.UtcNow;
			foreach (var r in meetingMeasurables) {
				r.DeleteTime = now;
				s.Update(r);
			}

			var cur = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);

			if (cur != null) {
				var mmeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
					.Where(x => x.DeleteTime == null && x.L10Meeting.Id == cur.Id && x.Measurable.Id == measurableId)
					.List().ToList();
				foreach (var r in mmeasurables) {
					r.DeleteTime = now;
					s.Update(r);
				}
			}

			s.Flush();

			var measurableInOthers = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId).RowCount();
			if (measurableInOthers == 0) {
				var measurable = s.Get<MeasurableModel>(measurableId);
				if (measurable.FromTemplateItemId == null)
					await ScorecardAccessor.DeleteMeasurable(s, perm, measurableId);
			}

			foreach (var r in meetingMeasurables) {
				await HooksRegistry.Each<IMeetingMeasurableHook>((ses, x) => x.DetachMeasurable(ses, perm.GetCaller(), r.Measurable, recurrenceId));
			}
		}
		public static void DeleteMeetingMeasurableDivider(UserOrganizationModel caller, long l10Meeting_measurableId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var divider = s.Get<L10Meeting.L10Meeting_Measurable>(l10Meeting_measurableId);
					if (divider == null)
						throw new PermissionsException("Divider does not exist");

					var recurrenceId = divider.L10Meeting.L10RecurrenceId;
					var perm = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);
					if (!divider.IsDivider)
						throw new PermissionsException("Not a divider");
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

					var matchingMeasurable = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
						.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.IsDivider && x._Ordering == divider._Ordering)
						.List().FirstOrDefault();

					var now = DateTime.UtcNow;
					divider.DeleteTime = now;

					if (matchingMeasurable != null) {
						matchingMeasurable.DeleteTime = now;
						s.Update(matchingMeasurable);
					} else {
					}

					s.Update(divider);
					tx.Commit();
					s.Flush();
					group.removeDivider(l10Meeting_measurableId);
				}
			}
		}
		#endregion

		#region Helpers

		public static void _RecalculateCumulative_Unsafe(ISession s, RealTimeUtility rt, MeasurableModel measurable, List<long> recurIds, ScoreModel updatedScore = null, bool forceNoSkip = true) {
			var recurs = s.QueryOver<L10Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(recurIds).List().ToList();
			_RecalculateCumulative_Unsafe(s, rt, measurable.AsList(), recurs, updatedScore);
		}

		public static void _RecalculateCumulative_Unsafe(ISession s, RealTimeUtility rt, List<MeasurableModel> measurables, List<L10Recurrence> recurs, ScoreModel updatedScore = null, bool forceNoSkip = true) {
			var cumulativeByMeasurable = new Dictionary<long, IEnumerable<object[]>>();
			//Grab Cumulative Values
			foreach (var mm in measurables.Where(x => x.ShowCumulative && x.Id > 0).Distinct(x => x.Id)) {
				cumulativeByMeasurable[mm.Id] = s.QueryOver<ScoreModel>()
				.Where(x => x.MeasurableId == mm.Id && x.DeleteTime == null && x.Measured != null && x.ForWeek > mm.CumulativeRange.Value.AddDays(-7))
				.Select(x => x.ForWeek, x => x.Measured)
				.Future<object[]>();
			}

			var defaultDay = measurables.FirstOrDefault().NotNull(x => x.Organization.NotNull(y => y.Settings.WeekStart));

			//Set Cumulative Values
			if (recurs == null || recurs.Count == 0) {
				recurs = new List<L10Recurrence>() { null };
			}
			foreach (var recur in recurs) {
				var startOfWeek = defaultDay;
				if (recur != null) {
					startOfWeek = recur.StartOfWeekOverride ?? recur.Organization.Settings.WeekStart;
				}
				foreach (var k in cumulativeByMeasurable.Keys) {
					foreach (var mm in measurables.Where(x => x.Id == k).ToList()) {
						var foundScores = cumulativeByMeasurable[k].Select(x => new {
							ForWeek = (DateTime)x[0],
							Measured = (decimal?)x[1]
						}).Where(x => x.ForWeek > mm.CumulativeRange.Value.AddDays(-(int)startOfWeek)).ToList();

						//Use the updated score if we have it.
						if (updatedScore != null) {
							for (var i = 0; i < foundScores.Count; i++) {
								if (updatedScore.ForWeek == foundScores[i].ForWeek)
									foundScores[i] = new { ForWeek = updatedScore.ForWeek, Measured = updatedScore.Measured };
							}
						}

						mm._Cumulative = foundScores.GroupBy(x => x.ForWeek)
											.Select(x => x.FirstOrDefault(y => y.Measured != null).NotNull(y => y.Measured))
											.Where(x => x != null)
											.Sum();
					}
				}
			}

			if (rt != null) {
				foreach (var mm in measurables.Where(x => x.ShowCumulative && x.Id > 0).Distinct(x => x.Id)) {
					rt.UpdateRecurrences(recurs.Select(x => x.Id)).UpdateMeasurable(mm, forceNoSkip: forceNoSkip);
				}
			}

		}

		#endregion

		#region Deleted
		//[Obsolete("Use ScorcardAcessor", true)]
		//public static void UpdateScore(UserOrganizationModel caller, long scoreId, decimal? measured, string connectionId = null, bool noSyncException = false) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			var score = s.Get<ScoreModel>(scoreId);
		//			if (score == null)
		//				throw new PermissionsException("Score does not exist.");

		//			SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateScore(scoreId), noSyncException);

		//			var now = DateTime.UtcNow;
		//			PermissionsUtility.Create(s, caller).EditScore(scoreId);
		//			var all = s.QueryOver<ScoreModel>().Where(x => x.MeasurableId == score.MeasurableId && x.ForWeek == score.ForWeek).List().ToList();
		//			foreach (var sc in all) {
		//				sc.Measured = measured;
		//				sc.DateEntered = (measured == null) ? null : (DateTime?)now;
		//				s.Update(sc);
		//			}



		//			//L10Meeting meetingAlias = null;
		//			var possibleRecurrences = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
		//				.Where(x => x.DeleteTime == null && x.Measurable.Id == score.MeasurableId)
		//				.Select(x => x.L10Recurrence.Id)
		//				.List<long>().ToList();

		//			using (var rt = RealTimeUtility.Create()) { //Do not skip any users
		//				_RecalculateCumulative_Unsafe(s, rt, score.Measurable, possibleRecurrences, score);
		//			}

		//			foreach (var r in possibleRecurrences) {
		//				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
		//				var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r), connectionId);

		//				var n = score.Measurable.NotNull(x => x.AccountableUser.GetName());
		//				var n1 = score.Measurable.NotNull(x => x.AdminUser.GetName());
		//				var toUpdate = new AngularScore(score, false);


		//				toUpdate.DateEntered = score.Measured == null ? Removed.Date() : DateTime.UtcNow;
		//				toUpdate.Measured = toUpdate.Measured ?? Removed.Decimal();

		//				group.update(new AngularUpdate() { toUpdate });
		//				Audit.L10Log(s, caller, r, "UpdateScore", ForModel.Create(score), "\"" + score.Measurable.Title + "\" updated to \"" + measured + "\"");
		//			}

		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//}
		//[Obsolete("Use ScorcardAcessor", true)]
		//public static async Task<ScoreModel> _UpdateScore(ISession s, PermissionsUtility perms, RealTimeUtility rt, long measurableId, long weekNumber, decimal? measured, string connectionId, bool noSyncException = false, bool skipRealTime = false) {
		//	var now = DateTime.UtcNow;
		//	DateTime? nowQ = now;
		//	perms.EditMeasurable(measurableId);
		//	var m = s.Get<MeasurableModel>(measurableId);

		//	await ScorecardAccessor.GenerateScoreModels_Unsafe(s, TimingUtility.GetDateSinceEpoch(weekNumber).AsList(), measurableId.AsList());

		//	//adjust week..
		//	var week = TimingUtility.GetDateSinceEpoch(weekNumber).StartOfWeek(DayOfWeek.Sunday).Date;

		//	//See if we can find it given week.
		//	var score = s.QueryOver<ScoreModel>()
		//		.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId && x.ForWeek == week)
		//		//.OrderBy(x=>x.DateEntered).Desc.ThenBy(x=>x.Id).Desc
		//		.List().LastOrDefault();

		//	// var score = existingScores.SingleOrDefault(x => (x.ForWeek == week));

		//	if (score != null) {
		//		SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateScore(score.Id), noSyncException);
		//		//Found it with false id
		//		score.Measured = measured;
		//		score.DateEntered = (measured == null) ? null : nowQ;
		//		s.Update(score);

		//		//_RecalculateCumulative_Unsafe(s, score.Measurable, score);
		//	} else {
		//		var existingScores = s.QueryOver<ScoreModel>()
		//		.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
		//		.List().ToList();
		//		var ordered = existingScores.OrderBy(x => x.DateDue);
		//		var minDate = ordered.FirstOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now.AddDays(-7 * 13);
		//		var maxDate = ordered.LastOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;

		//		minDate = minDate.StartOfWeek(DayOfWeek.Sunday);
		//		maxDate = maxDate.StartOfWeek(DayOfWeek.Sunday);


		//		//DateTime start, end;

		//		if (week > maxDate) {
		//			//Create going up until sufficient
		//			var n = maxDate;
		//			ScoreModel curr = null;
		//			var measurable = s.Get<MeasurableModel>(m.Id);
		//			while (n < week) {
		//				var nextDue = n.StartOfWeek(DayOfWeek.Sunday).Date.AddDays(7).AddDays((int)m.DueDate).Add(m.DueTime);
		//				curr = new ScoreModel() {
		//					AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId),
		//					AccountableUserId = m.AccountableUserId,
		//					DateDue = nextDue,
		//					MeasurableId = m.Id,
		//					Measurable = measurable,
		//					OrganizationId = m.OrganizationId,
		//					ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday).Date,
		//					OriginalGoal = measurable.Goal,
		//					OriginalGoalDirection = measurable.GoalDirection
		//				};
		//				s.Save(curr);
		//				m.NextGeneration = nextDue;
		//				n = nextDue.StartOfWeek(DayOfWeek.Sunday).Date;
		//			}
		//			curr.DateEntered = (measured == null) ? null : nowQ;
		//			curr.Measured = measured;
		//			score = curr;
		//			//_RecalculateCumulative_Unsafe(s, m, curr);
		//		} else if (week < minDate) {
		//			var n = week;
		//			var first = true;
		//			var measurable = s.Get<MeasurableModel>(m.Id);
		//			while (n < minDate) {
		//				var nextDue = n.StartOfWeek(DayOfWeek.Sunday).Date.AddDays((int)m.DueDate).Add(m.DueTime);
		//				var curr = new ScoreModel() {
		//					AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId),
		//					AccountableUserId = m.AccountableUserId,
		//					DateDue = nextDue,
		//					MeasurableId = m.Id,
		//					Measurable = measurable,
		//					OrganizationId = m.OrganizationId,
		//					ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday).Date,
		//					OriginalGoal = measurable.Goal,
		//					OriginalGoalDirection = measurable.GoalDirection
		//				};
		//				if (first) {
		//					curr.Measured = measured;
		//					curr.DateEntered = (measured == null) ? null : nowQ;
		//					first = false;
		//					s.Save(curr);
		//					score = curr;
		//					//_RecalculateCumulative_Unsafe(s, m, curr);
		//				}

		//				//m.NextGeneration = nextDue;
		//				n = nextDue.AddDays(7).StartOfWeek(DayOfWeek.Sunday);
		//			}
		//		} else {
		//			// cant create scores between these dates..
		//			var measurable = s.Get<MeasurableModel>(m.Id);
		//			var curr = new ScoreModel() {
		//				AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId),
		//				AccountableUserId = m.AccountableUserId,
		//				DateDue = week.StartOfWeek(DayOfWeek.Sunday).Date.AddDays((int)m.DueDate).Add(m.DueTime),
		//				MeasurableId = m.Id,
		//				Measurable = measurable,
		//				OrganizationId = m.OrganizationId,
		//				ForWeek = week.StartOfWeek(DayOfWeek.Sunday).Date,
		//				Measured = measured,
		//				DateEntered = (measured == null) ? null : nowQ,
		//				OriginalGoal = measurable.Goal,
		//				OriginalGoalDirection = measurable.GoalDirection

		//			};
		//			s.Save(curr);
		//			score = curr;
		//			//_RecalculateCumulative_Unsafe(s, m, curr);
		//		}
		//		s.Update(m);
		//	}
		//	if (!skipRealTime) {


		//		var measurableRecurs = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
		//			.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
		//			.Select(x => x.L10Recurrence.Id)
		//			.List<long>().ToList();

		//		_RecalculateCumulative_Unsafe(s, rt, score.Measurable, measurableRecurs, score);

		//		rt.UpdateRecurrences(measurableRecurs).UpdateScorecard(score.AsList());
		//		foreach (var recurrenceId in measurableRecurs) {
		//			Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateScore", ForModel.Create(score), "\"" + score.NotNull(x => x.Measurable.NotNull(y => y.Title)) + "\" updated to \"" + measured + "\"");
		//		}
		//	}
		//	return score;
		//}
		//[Obsolete("Use ScorcardAcessor", true)]
		//public static async Task<ScoreModel> UpdateScore(UserOrganizationModel caller, long measurableId, long weekNumber, decimal? measured, string connectionId, bool noSyncException = false) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			using (var rt = RealTimeUtility.Create(connectionId)) {
		//				var perms = PermissionsUtility.Create(s, caller);
		//				var score = await _UpdateScore(s, perms, rt, measurableId, weekNumber, measured, connectionId, noSyncException);
		//				tx.Commit();
		//				s.Flush();
		//				return score;
		//			}
		//		}
		//	}
		//}
		//[Obsolete("Use ScorcardAcessor", true)]
		//public static void UpdateArchiveMeasurable(UserOrganizationModel caller, long measurableId, string name = null,
		//	LessGreater? direction = null, decimal? target = null, long? accountableId = null, long? adminId = null,
		//	string connectionId = null, bool updateFutureOnly = true, decimal? altTarget = null, bool? showCumulative = null,
		//	DateTime? cumulativeRange = null, UnitType? modifiers = null) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			using (var rt = RealTimeUtility.Create(connectionId)) {
		//				var measurable = s.Get<MeasurableModel>(measurableId);
		//				//var recurrence = s.Get<L10Recurrence>(recurrenceId);
		//				var scoresToUpdate = new List<ScoreModel>();

		//				if (measurable == null)
		//					throw new PermissionsException("Measurable does not exist.");


		//				var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
		//					.Where(x => x.Measurable.Id == measurableId && x.DeleteTime == null).Select(x => x.L10Recurrence.Id).List<long>().ToList();

		//				var rtRecur = rt.UpdateRecurrences(recurrenceIds);
		//				var checkEither = new List<Func<PermissionsUtility, PermissionsUtility>>{
		//					x => x.EditMeasurable(measurableId)
		//				};

		//				checkEither.AddRange(recurrenceIds.Select<long, Func<PermissionsUtility, PermissionsUtility>>(recurrenceId => (x => x.EditL10Recurrence(recurrenceId))));
		//				var perms = PermissionsUtility.Create(s, caller).Or(checkEither.ToArray());

		//				var updateText = new List<String>();

		//				var meetingMeasurableIds = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
		//					.Where(x => x.DeleteTime == null && x.Measurable.Id == measurable.Id)
		//					.Select(x => x.Id)
		//					.List<long>().ToList();

		//				if (name != null && measurable.Title != name) {
		//					measurable.Title = name;
		//					//group.updateArchiveMeasurable(measurableId, "title", name);
		//					updateText.Add("Title: " + measurable.Title);
		//					foreach (var mmid in meetingMeasurableIds)
		//						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "title", name));
		//				}
		//				var updateCumulative = false;
		//				if (showCumulative != null && measurable.ShowCumulative != showCumulative) {
		//					measurable.ShowCumulative = showCumulative.Value;
		//					updateText.Add("Cumulative: " + showCumulative);
		//					foreach (var mmid in meetingMeasurableIds)
		//						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "showCumulative", showCumulative));
		//					updateCumulative = true;
		//				}
		//				if (cumulativeRange != null && measurable.CumulativeRange != cumulativeRange) {
		//					measurable.CumulativeRange = cumulativeRange.Value;
		//					updateText.Add("Cumulative Start: " + cumulativeRange);
		//					foreach (var mmid in meetingMeasurableIds) {
		//						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "cumulativeRange", cumulativeRange));
		//					}
		//					updateCumulative = true;
		//				}

		//				if (updateCumulative) {
		//					//Recalculate cumulative
		//					_RecalculateCumulative_Unsafe(s, rt, measurable, recurrenceIds);
		//				}

		//				if ((direction != null && measurable.GoalDirection != direction.Value) || !updateFutureOnly) {
		//					measurable.GoalDirection = direction.Value;
		//					updateText.Add("Goal Direction: " + measurable.GoalDirection.ToSymbol());

		//					var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
		//					if (updateFutureOnly) {
		//						var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
		//						scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
		//					}

		//					var scores = scoresQ.List().ToList();
		//					foreach (var score in scores) {
		//						score.OriginalGoalDirection = direction.Value;
		//						s.Update(score);
		//					}
		//					scoresToUpdate = scores;

		//					foreach (var mmid in meetingMeasurableIds)
		//						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "direction", direction.Value.ToSymbol(), direction.Value.ToString()));
		//					//group.updateArchiveMeasurable(measurableId, "direction", direction.Value.ToSymbol(), direction.Value.ToString());

		//				}
		//				if ((target != null && measurable.Goal != target.Value) || !updateFutureOnly) {
		//					if (target != null) {
		//						measurable.Goal = target.Value;
		//						updateText.Add("Goal: " + measurable.Goal);
		//					}


		//					var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
		//					if (updateFutureOnly) {
		//						var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
		//						scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
		//					}
		//					var scores = scoresQ.List().ToList();
		//					foreach (var score in scores) {
		//						score.OriginalGoal = measurable.Goal;
		//						s.Update(score);
		//					}
		//					scoresToUpdate = scores;

		//					foreach (var mmid in meetingMeasurableIds)
		//						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "target", measurable.Goal.ToString("0.#####")));
		//					//group.updateArchiveMeasurable(measurableId, "target", target.Value.ToString("0.#####"));
		//				}




		//				if ((altTarget != null && measurable.AlternateGoal != altTarget.Value) || !updateFutureOnly) {

		//					if (altTarget != null) {
		//						measurable.AlternateGoal = altTarget.Value;
		//						updateText.Add("AltGoal: " + measurable.AlternateGoal);
		//					}


		//					var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
		//					if (updateFutureOnly) {
		//						var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
		//						scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
		//					}
		//					var scores = scoresQ.List().ToList();
		//					foreach (var score in scores) {
		//						score.AlternateOriginalGoal = measurable.AlternateGoal;
		//						s.Update(score);
		//					}
		//					scoresToUpdate = scores;

		//					foreach (var mmid in meetingMeasurableIds)
		//						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "altTarget", measurable.AlternateGoal.NotNull(x => x.Value.ToString("0.#####")) ?? ""));
		//					//group.updateArchiveMeasurable(measurableId, "target", target.Value.ToString("0.#####"));
		//				}

		//				if (accountableId != null && measurable.AccountableUserId != accountableId.Value) {
		//					perms.ViewUserOrganization(accountableId.Value, false);
		//					var user = s.Get<UserOrganizationModel>(accountableId.Value);
		//					if (user != null)
		//						user.UpdateCache(s);

		//					measurable.AccountableUserId = accountableId.Value;
		//					measurable.AccountableUser = user;
		//					updateText.Add("Accountable: " + user.GetName());

		//					foreach (var mmid in meetingMeasurableIds)
		//						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "accountable", user.NotNull(x => x.GetName()), accountableId.Value));
		//					//group.updateArchiveMeasurable(measurableId, "accountable", user.NotNull(x => x.GetName()), accountableId.Value);
		//				}
		//				if (adminId != null) {
		//					perms.ViewUserOrganization(adminId.Value, false);
		//					var user = s.Get<UserOrganizationModel>(adminId.Value);
		//					if (user != null)
		//						user.UpdateCache(s);
		//					measurable.AdminUserId = adminId.Value;
		//					measurable.AdminUser = user;
		//					updateText.Add("Admin: " + user.GetName());

		//					foreach (var mmid in meetingMeasurableIds)
		//						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "admin", user.NotNull(x => x.GetName()), adminId.Value));
		//					//group.updateArchiveMeasurable(measurableId, "admin", user.NotNull(x => x.GetName()), adminId.Value);
		//				}
		//				var applySelf = false;
		//				if (modifiers != null && measurable.UnitType != modifiers.Value) {
		//					//perms.ViewUserOrganization(accountableId.Value, false);
		//					//var user = s.Get<UserOrganizationModel>(accountableId.Value);
		//					//if (user != null)
		//					//	user.UpdateCache(s);

		//					measurable.UnitType = modifiers.Value;
		//					s.Update(measurable);

		//					applySelf = true;
		//					var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);

		//					var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);

		//					scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
		//					var scores = scoresQ.List().ToList();
		//					scoresToUpdate = scores;

		//					foreach (var mmid in meetingMeasurableIds)
		//						rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "unittype", measurable.UnitType.ToTypeString(), measurable.UnitType));
		//					//group.updateArchiveMeasurable(measurableId, "accountable", user.NotNull(x => x.GetName()), accountableId.Value);
		//				}
		//				//var scorecard = new AngularScorecard();
		//				//scorecard.Measurables = new List<AngularMeasurable>() { };
		//				//var scoreList = new List<AngularScore>(); 

		//				//foreach (var ss in scores.Where(x => x.Measurable.Id == measurable.Id)) {
		//				//    scoreList.Add(new AngularScore(ss));
		//				//}
		//				//scorecard.Scores = AngularList.Create<AngularScore>(AngularListType.ReplaceAll, scoreList);
		//				//group.update(new AngularUpdate() { scorecard, new AngularMeasurable(measurable) });

		//				//_ProcessDeleted(s, measurable, delete);

		//				rtRecur.UpdateMeasurable(measurable, scoresToUpdate, forceNoSkip: applySelf);

		//				var updatedText = "Updated Measurable: \"" + measurable.Title + "\" \n " + String.Join("\n", updateText);
		//				foreach (var recurrenceId in recurrenceIds) {
		//					Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateArchiveMeasurable", ForModel.Create(measurable), updatedText);
		//				}
		//				tx.Commit();
		//				s.Flush();
		//			}
		//		}
		//	}
		//}
		//[Obsolete("Use ScorcardAcessor", true)]
		//public static void UpdateMeasurable(UserOrganizationModel caller, long meeting_measurableId,
		//	string name = null, LessGreater? direction = null, decimal? target = null,
		//	long? accountableId = null, long? adminId = null, UnitType? unitType = null,
		//	bool updateFutureOnly = true) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			var measurable = s.Get<L10Meeting.L10Meeting_Measurable>(meeting_measurableId);
		//			if (measurable == null)
		//				throw new PermissionsException("Measurable does not exist.");

		//			var recurrenceId = measurable.L10Meeting.L10RecurrenceId;
		//			if (recurrenceId == 0)
		//				throw new PermissionsException("Meeting does not exist.");
		//			var perms = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);

		//			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
		//			var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

		//			var updateText = new List<String>();
		//			if (name != null && measurable.Measurable.Title != name) {
		//				measurable.Measurable.Title = name;
		//				group.updateMeasurable(meeting_measurableId, "title", name);
		//				updateText.Add("Title: " + measurable.Measurable.Title);
		//			}
		//			if (unitType != null && measurable.Measurable.UnitType != unitType.Value) {
		//				measurable.Measurable.UnitType = unitType.Value;
		//				group.updateMeasurable(meeting_measurableId, "unittype", unitType.Value.ToTypeString(), unitType.Value.ToString());
		//				updateText.Add("Unit Type: " + measurable.Measurable.UnitType);
		//			}
		//			//if (direction != null && measurable.Measurable.GoalDirection != direction.Value) {
		//			//    measurable.Measurable.GoalDirection = direction.Value;
		//			//    group.updateMeasurable(meeting_measurableId, "direction", direction.Value.ToSymbol(), direction.Value.ToString());
		//			//    updateText.Add("Goal Direction: " + measurable.Measurable.GoalDirection.ToSymbol());
		//			//}
		//			//if (target != null && measurable.Measurable.Goal != target.Value) {
		//			//    measurable.Measurable.Goal = target.Value;
		//			//    group.updateMeasurable(meeting_measurableId, "target", target.Value.ToString("0.#####"));
		//			//    updateText.Add("Goal: " + measurable.Measurable.Goal);
		//			//}
		//			var scoresToUpdate = new List<ScoreModel>();
		//			var meetingMeasurableIds = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
		//				.Where(x => x.DeleteTime == null && x.Measurable.Id == measurable.Measurable.Id)
		//				.Select(x => x.Id)
		//				.List<long>().ToList();

		//			var l10MeetingStart = measurable.L10Meeting.StartTime ?? DateTime.UtcNow;

		//			if (direction != null && measurable.Measurable.GoalDirection != direction.Value) {
		//				measurable.Measurable.GoalDirection = direction.Value;
		//				updateText.Add("Goal Direction: " + measurable.Measurable.GoalDirection.ToSymbol());

		//				var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Measurable.Id);
		//				if (updateFutureOnly) {
		//					var nowSunday = l10MeetingStart.AddDays(-7).StartOfWeek(DayOfWeek.Sunday);
		//					scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
		//				}

		//				var scores = scoresQ.List().ToList();
		//				foreach (var score in scores) {
		//					score.OriginalGoalDirection = direction.Value;
		//					s.Update(score);
		//				}
		//				scoresToUpdate = scores;

		//				foreach (var mmid in meetingMeasurableIds)
		//					group.updateMeasurable(mmid, "direction", direction.Value.ToSymbol(), direction.Value.ToString());
		//				//group.updateArchiveMeasurable(measurableId, "direction", direction.Value.ToSymbol(), direction.Value.ToString());

		//			}
		//			if (target != null && measurable.Measurable.Goal != target.Value) {
		//				measurable.Measurable.Goal = target.Value;
		//				updateText.Add("Goal: " + measurable.Measurable.Goal);


		//				var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Measurable.Id);
		//				if (updateFutureOnly) {
		//					var nowSunday = l10MeetingStart.AddDays(-7).StartOfWeek(DayOfWeek.Sunday);
		//					scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
		//				}
		//				var scores = scoresQ.List().ToList();
		//				foreach (var score in scores) {
		//					score.OriginalGoal = target.Value;
		//					s.Update(score);
		//				}
		//				scoresToUpdate = scores;

		//				foreach (var mmid in meetingMeasurableIds)
		//					group.updateMeasurable(mmid, "target", target.Value.ToString("0.#####"));
		//				//group.updateArchiveMeasurable(measurableId, "target", target.Value.ToString("0.#####"));
		//			}

		//			if (accountableId != null && measurable.Measurable.AccountableUserId != accountableId.Value) {
		//				perms.ViewUserOrganization(accountableId.Value, false);
		//				var user = s.Get<UserOrganizationModel>(accountableId.Value);
		//				var oldUser = s.Get<UserOrganizationModel>(measurable.Measurable.AccountableUserId);
		//				if (user == null)
		//					throw new PermissionsException("Cannot Update User");
		//				user.UpdateCache(s);
		//				if (oldUser != null)
		//					oldUser.UpdateCache(s);

		//				measurable.Measurable.AccountableUserId = accountableId.Value;
		//				group.updateMeasurable(meeting_measurableId, "accountable", user.NotNull(x => x.GetName()), accountableId.Value);
		//				updateText.Add("Accountable: " + user.NotNull(x => x.GetName()));
		//				s.Update(measurable.Measurable);
		//			}
		//			if (adminId != null && measurable.Measurable.AdminUserId != adminId.Value) {
		//				perms.ViewUserOrganization(adminId.Value, false);
		//				var user = s.Get<UserOrganizationModel>(adminId.Value);
		//				var oldUser = s.Get<UserOrganizationModel>(measurable.Measurable.AdminUserId);
		//				if (user == null)
		//					throw new PermissionsException("Cannot Update User");
		//				user.UpdateCache(s);
		//				if (oldUser != null)
		//					oldUser.UpdateCache(s);
		//				measurable.Measurable.AdminUserId = adminId.Value;
		//				group.updateMeasurable(meeting_measurableId, "admin", user.NotNull(x => x.GetName()), adminId.Value);
		//				updateText.Add("Admin: " + user.NotNull(x => x.GetName()));
		//				s.Update(measurable.Measurable);
		//			}

		//			var updatedText = "Updated Measurable: \"" + measurable.Measurable.Title + "\" \n " + String.Join("\n", updateText);
		//			Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateMeasurable", ForModel.Create(measurable), updatedText);

		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//}

		//[Obsolete("Compress", true)]
		//public static async Task AddMeasurable(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, L10Controller.AddMeasurableVm model, bool skipRealTime = false, int? rowNum = null) {
		//	throw new NotImplementedException();
		//	//			perm.EditL10Recurrence(recurrenceId);
		//	//			rt = rt ?? RealTimeUtility.Create(false);
		//	//			var recur = s.Get<L10Recurrence>(recurrenceId);
		//	//			await L10Accessor.Depristine_Unsafe(s, perm.GetCaller(), recur);
		//	//			s.Update(recur);
		//	//			var now = DateTime.UtcNow;
		//	//			MeasurableModel measurable;
		//	//			var scores = new List<ScoreModel>();
		//	//			var wasCreated = false;
		//	//			if (model.SelectedMeasurable == -3) {
		//	//				measurable = model.Measurables.SingleOrDefault();
		//	//				measurable.OrganizationId = recur.OrganizationId;
		//	//				measurable.CreateTime = now;
		//	//				await ScorecardAccessor.CreateMeasurable(s, perm, measurable, false);
		//	//				wasCreated = true;
		//	//			} else {
		//	//				//Find Existing
		//	//				measurable = s.Get<MeasurableModel>(model.SelectedMeasurable);
		//	//				if (measurable == null)
		//	//					throw new PermissionsException("Measurable does not exist.");
		//	//				perm.ViewMeasurable(measurable.Id);

		//	//				scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id).List().ToList();

		//	//			}

		//	//			var rm = new L10Recurrence.L10Recurrence_Measurable() {
		//	//				CreateTime = now,
		//	//				L10Recurrence = recur,
		//	//				Measurable = measurable,
		//	//				_Ordering = rowNum ?? 0
		//	//			};
		//	//			s.Save(rm);

		//	//			if (wasCreated) {
		//	//				var week = TimingUtility.GetWeekSinceEpoch(DateTime.UtcNow);
		//	//				for (var i = 0; i < 4; i++) {
		//	//					scores.Add(await _UpdateScore(s, perm, rt, measurable.Id, week - i, null, null, skipRealTime: true));
		//	//				}
		//	//			}

		//	//			var current = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);
		//	//			if (current != null) {

		//	//				var mm = new L10Meeting.L10Meeting_Measurable() {
		//	//					L10Meeting = current,
		//	//					Measurable = measurable,
		//	//				};
		//	//				s.Save(mm);

		//	//				if (!skipRealTime) {

		//	//					rt.UpdateRecurrences(recurrenceId).AddLowLevelAction(g => {
		//	//						var settings = current.Organization.Settings;
		//	//						var sow = settings.WeekStart;
		//	//						var offset = current.Organization.GetTimezoneOffset();
		//	//						var scorecardType = settings.ScorecardPeriod;

		//	//#pragma warning disable CS0618 // Type or member is obsolete
		//	//						var ts = current.Organization.GetTimeSettings();
		//	//#pragma warning restore CS0618 // Type or member is obsolete
		//	//						ts.Descending = recur.ReverseScorecard;

		//	//						var weeks = TimingUtility.GetPeriods(ts, now, current.StartTime, false);

		//	//						//if (recur.ReverseScorecard)
		//	//						//	weeks.Reverse();S:\repos\Radial\RadialReview\RadialReview\Hooks\Realtime\L10\Realtime_L10Scorecard.cs

		//	//						//var rowId = l10Scores.GroupBy(x => x.MeasurableId).Count();
		//	//						var rowId = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).RowCount();
		//	//						var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM {
		//	//							MeetingId = current.Id,
		//	//							RecurrenceId = recurrenceId,
		//	//							MeetingMeasurable = mm,
		//	//							Scores = scores,
		//	//							Weeks = weeks
		//	//						});
		//	//						row.ViewData["row"] = rowId - 1;

		//	//						var first = row.Execute();
		//	//						row.ViewData["ShowRow"] = false;
		//	//						var second = row.Execute();
		//	//						g.addMeasurable(first, second);
		//	//					});
		//	//				}
		//	//			}
		//	//			if (!skipRealTime) {
		//	//				rt.UpdateRecurrences(recurrenceId).UpdateScorecard(scores.Where(x => x.Measurable.Id == measurable.Id));
		//	//				rt.UpdateRecurrences(recurrenceId).SetFocus("[data-measurable='" + measurable.Id + "'] input:visible:first");
		//	//			}
		//	//			Audit.L10Log(s, perm.GetCaller(), recurrenceId, "CreateMeasurable", ForModel.Create(measurable), measurable.Title);
		//}
		//[Obsolete("Use ScorcardAcessor", true)]
		//public static async Task CreateMeasurable(UserOrganizationModel caller, long recurrenceId, L10Controller.AddMeasurableVm model) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			using (var rt = RealTimeUtility.Create()) {
		//				var perm = PermissionsUtility.Create(s, caller);
		//				await AddMeasurable(s, perm, rt, recurrenceId, model);
		//				tx.Commit();
		//				s.Flush();
		//			}
		//		}
		//	}
		//} 
		#endregion

		#endregion
	}
}