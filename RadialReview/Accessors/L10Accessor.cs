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
using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
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

namespace RadialReview.Accessors {
	public class L10Accessor : BaseAccessor {

		public static void UpdateMeasurablePast(UserOrganizationModel caller, long id) {
			var m = ScorecardAccessor.GetMeasurable(caller, id);
			UpdateArchiveMeasurable(caller, id, m.Title, m.GoalDirection, m.Goal, m.AccountableUserId, m.AdminUserId, updateFutureOnly: false);
		}


		#region Attendees
		public static List<UserOrganizationModel> GetAttendees(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

						var usersRecur = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
							.Fetch(x => x.User).Eager
							.List().ToList();
						var users = usersRecur.Select(x => x.User).ToList();
						foreach (var u in users) {
							try {
								var a = u.GetName();
							} catch (Exception) {

							}
						}
						return users;
					}
				}
			}
		}

		public static void OrderAngularMeasurable(UserOrganizationModel caller, long measurableId, long recurrenceId, int oldOrder, int newOrder) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);
						perms.EditL10Recurrence(recurrenceId);
						perms.EditMeasurable(measurableId);

						var recurMeasureables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
										.Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null)
										.List().ToList();

						var ctx = Reordering.Create(recurMeasureables, measurableId, recurrenceId, oldOrder, newOrder, x => x._Ordering, x => (x.Measurable == null) ? x.Id : x.Measurable.Id);
						ctx.ApplyReorder(rt, s, (id, order) => new AngularMeasurable(id) { Ordering = order });

						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static void AddAttendee(UserOrganizationModel caller, long recurrenceId, long userorgid) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);
						perms.AdminL10Recurrence(recurrenceId);
						perms.ViewUserOrganization(userorgid, false);
						var user = s.Get<UserOrganizationModel>(userorgid);

						var existing = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userorgid && x.L10Recurrence.Id == recurrenceId).List().ToList();
						if (existing.Any())
							throw new PermissionsException("User is already an attendee.");
						var recur = s.Get<L10Recurrence>(recurrenceId);
						recur.Pristine = false;
						s.Update(recur);

						var attendee = new L10Recurrence.L10Recurrence_Attendee() {
							L10Recurrence = recur,
							User = user,
						};

						s.Save(attendee);

						if (caller.Organization.Settings.DisableUpgradeUsers && user.EvalOnly) {
							throw new PermissionsException("This user is set to participate in " + Config.ReviewName() + " only.");
						}

						if (user.EvalOnly) {
							perms.CanUpgradeUser(user.Id);
							user.EvalOnly = false;
							s.Update(user);
							user.UpdateCache(s);
						}

						var curr = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
						if (curr != null) {
							s.Save(new L10Meeting.L10Meeting_Attendee() {
								L10Meeting = curr,
								User = user,
							});
						}
						var auser = AngularUser.CreateUser(user);
						auser.CreateTime = attendee.CreateTime;

						rt.UpdateRecurrences(recurrenceId).Update(new AngularRecurrence(recurrenceId) {
							Attendees = AngularList.CreateFrom(AngularListType.Add, auser)
						});
						tx.Commit();
						s.Flush();
					}

				}
			}
		}
		public static void RemoveAttendee(ISession s, PermissionsUtility perms, RealTimeUtility rt, long recurrenceId, long userorgid) {
			perms.AdminL10Recurrence(recurrenceId);
			perms.ViewUserOrganization(userorgid, false);
			var user = s.Get<UserOrganizationModel>(userorgid);

			var existing = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userorgid && x.L10Recurrence.Id == recurrenceId).List().ToList();
			if (!existing.Any())
				throw new PermissionsException("User is not an attendee.");

			foreach (var e in existing) {
				e.DeleteTime = DateTime.UtcNow;
				s.Update(e);
			}

			var curr = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
			if (curr != null) {
				var curAttendee = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userorgid && x.L10Meeting.Id == curr.Id).List().ToList();

				foreach (var e in curAttendee) {
					e.DeleteTime = DateTime.UtcNow;
					s.Update(e);
				}
			}

			rt.UpdateRecurrences(recurrenceId).Update(new AngularRecurrence(recurrenceId) {
				Attendees = AngularList.CreateFrom(AngularListType.Remove, new AngularUser(userorgid))
			});
		}
		public static void RemoveAttendee(UserOrganizationModel caller, long recurrenceId, long userorgid) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);
						RemoveAttendee(s, perms, rt, recurrenceId, userorgid);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}


		public static long GuessUserId(IssueModel issueModel, long deflt = 0) {
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						if (issueModel == null)
							return deflt;
						if (issueModel.ForModel != null && issueModel.ForModel.ToLower() == "issuemodel" && issueModel.Id == issueModel.ForModelId)
							return deflt;
						var found = GetModel_Unsafe(s, issueModel.ForModel, issueModel.ForModelId);
						if (found == null)
							return deflt;
						if (found is MeasurableModel)
							return ((MeasurableModel)found).AccountableUserId;
						if (found is TodoModel)
							return ((TodoModel)found).AccountableUserId;
						if (found is IssueModel)
							return GuessUserId((IssueModel)found, deflt);
						return deflt;
					}
				}
			} catch (Exception) {
				return deflt;
			}
		}
		#endregion

		#region Meeting Actions
		public static L10Recurrence CreateBlankRecurrence(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CreateL10Recurrence(orgId);
					var recur = new L10Recurrence() {
						OrganizationId = orgId,
						Pristine = true,
						VideoId = Guid.NewGuid().ToString(),
						EnableTranscription = false,
						HeadlinesId = Guid.NewGuid().ToString(),
						CountDown = true,
						CreatedById = caller.Id,
						CreateTime = DateTime.UtcNow
					};
					s.Save(recur);
					var vto = VtoAccessor.CreateRecurrenceVTO(s, perms, recur.Id);
					s.Save(new PermItem() {
						CanAdmin = true,
						CanEdit = true,
						CanView = true,
						AccessorType = PermItem.AccessType.Creator,
						AccessorId = caller.Id,
						ResType = PermItem.ResourceType.L10Recurrence,
						ResId = recur.Id,
						CreatorId = caller.Id,
						OrganizationId = caller.Organization.Id,
						IsArchtype = false,
					});
					s.Save(new PermItem() {
						CanAdmin = true,
						CanEdit = true,
						CanView = true,
						AccessorType = PermItem.AccessType.Members,
						AccessorId = -1,
						ResType = PermItem.ResourceType.L10Recurrence,
						ResId = recur.Id,
						CreatorId = caller.Id,
						OrganizationId = caller.Organization.Id,
						IsArchtype = false,
					});
					s.Save(new PermItem() {
						CanAdmin = true,
						CanEdit = true,
						CanView = true,
						AccessorType = PermItem.AccessType.Admins,
						AccessorId = -1,
						ResType = PermItem.ResourceType.L10Recurrence,
						ResId = recur.Id,
						CreatorId = caller.Id,
						OrganizationId = caller.Organization.Id,
						IsArchtype = false,
					});
					tx.Commit();
					s.Flush();
					return recur;
				}
			}
		}

		public static string GetDefaultStartPage(L10Recurrence recurrence) {
			var p = "segue";
			if (recurrence.SegueMinutes > 0)
				p = "segue";
			else if (recurrence.ScorecardMinutes > 0)
				p = "scorecard";
			else if (recurrence.RockReviewMinutes > 0)
				p = "rocks";
			else if (recurrence.HeadlinesMinutes > 0)
				p = "headlines";
			else if (recurrence.TodoListMinutes > 0)
				p = "todo";
			else if (recurrence.IDSMinutes > 0)
				p = "ids";
			else
				p = "conclusion";
			return p;
		}

		public static L10Meeting StartMeeting(UserOrganizationModel caller, UserOrganizationModel meetingLeader, long recurrenceId, List<long> attendees) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					if (caller.Id != meetingLeader.Id)
						PermissionsUtility.Create(s, meetingLeader).ViewL10Recurrence(recurrenceId);

					lock ("Recurrence_" + recurrenceId) {
						//Make sure we're unstarted
						try {
							var perms = PermissionsUtility.Create(s, caller);
							_GetCurrentL10Meeting(s, perms, recurrenceId, false);
							throw new MeetingException("Meeting has already started.", MeetingExceptionType.AlreadyStarted);
						} catch (MeetingException e) {
							if (e.MeetingExceptionType != MeetingExceptionType.Unstarted)
								throw;
						}

						var now = DateTime.UtcNow;
						var recurrence = s.Get<L10Recurrence>(recurrenceId);

						var meeting = new L10Meeting {
							CreateTime = now,
							StartTime = now,
							L10RecurrenceId = recurrenceId,
							L10Recurrence = recurrence,
							OrganizationId = recurrence.OrganizationId,
							MeetingLeader = meetingLeader,
							MeetingLeaderId = meetingLeader.Id
						};

						s.Save(meeting);

						recurrence.MeetingInProgress = meeting.Id;
						s.Update(recurrence);

						_LoadRecurrences(s, false, false, false, recurrence);

						foreach (var m in recurrence._DefaultMeasurables) {
							if (m.Id > 0) {
								var mm = new L10Meeting.L10Meeting_Measurable() {
									L10Meeting = meeting,
									Measurable = m.Measurable,
									_Ordering = m._Ordering,
									IsDivider = m.IsDivider
								};
								s.Save(mm);
								meeting._MeetingMeasurables.Add(mm);
							}
						}
						foreach (var m in attendees) {
							var mm = new L10Meeting.L10Meeting_Attendee() {
								L10Meeting = meeting,
								User = s.Load<UserOrganizationModel>(m),
							};
							s.Save(mm);
							meeting._MeetingAttendees.Add(mm);
						}

						foreach (var r in recurrence._DefaultRocks) {
							var state = RockState.Indeterminate;
							state = r.ForRock.Completion;
							var mm = new L10Meeting.L10Meeting_Rock() {
								ForRecurrence = recurrence,
								L10Meeting = meeting,
								ForRock = r.ForRock,
								Completion = state
							};
							s.Save(mm);
							meeting._MeetingRocks.Add(mm);
						}
						var perms2 = PermissionsUtility.Create(s, caller);
						var todos = GetTodosForRecurrence(s, perms2, recurrence.Id, meeting.Id);
						var i = 0;
						foreach (var t in todos.OrderBy(x => x.AccountableUser.NotNull(y => y.GetName()) ?? ("" + x.AccountableUserId)).ThenBy(x => x.Message)) {
							t.Ordering = i;
							s.Update(t);
							i += 1;
						}
						Audit.L10Log(s, caller, recurrenceId, "StartMeeting", ForModel.Create(meeting));
						tx.Commit();
						s.Flush();
						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting)).setupMeeting(meeting.CreateTime.ToJavascriptMilliseconds(), meetingLeader.Id);
						return meeting;
					}
				}
			}
		}
		public async static Task ConcludeMeeting(UserOrganizationModel caller, long recurrenceId, List<System.Tuple<long, decimal?>> ratingValues, bool sendEmail, bool closeTodos, bool closeHeadlines, string connectionId) {
			var unsent = new List<Mail>();
			L10Meeting meeting = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var now = DateTime.UtcNow;
					//Make sure we're unstarted
					var perms = PermissionsUtility.Create(s, caller);
					meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, false);
					perms.ViewL10Meeting(meeting.Id);

					var todoRatio = new Ratio();
					var todos = GetTodosForRecurrence(s, perms, recurrenceId, meeting.Id);

					foreach (var todo in todos) {
						if (todo.CreateTime < meeting.StartTime) {
							if (todo.CompleteTime != null) {
								todo.CompleteDuringMeetingId = meeting.Id;
								if (closeTodos) {
									todo.CloseTime = now;
								}
								s.Update(todo);
							}
							todoRatio.Add(todo.CompleteTime != null ? 1 : 0, 1);
						}
					}

					if (closeHeadlines) {
						var headlines = GetHeadlinesForMeeting(s, perms, recurrenceId);
						foreach (var headline in headlines) {
							if (headline.CloseTime == null) {
								headline.CloseDuringMeetingId = meeting.Id;
								headline.CloseTime = now;
							}
							s.Update(headline);
						}
					}


					var issuesToClose = s.QueryOver<IssueModel.IssueModel_Recurrence>()
											.Where(x => x.DeleteTime == null && x.MarkedForClose && x.Recurrence.Id == recurrenceId && x.CloseTime == null)
											.List().ToList();

					foreach (var i in issuesToClose) {
						i.CloseTime = now;
						s.Update(i);
					}



					meeting.CompleteTime = now;
					meeting.TodoCompletion = todoRatio;

					var recurrence = s.Get<L10Recurrence>(recurrenceId);
					s.Update(meeting);

					var ids = ratingValues.Select(x => x.Item1).ToArray();

					//Set rating for attendees
					var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
						.WhereRestrictionOn(x => x.User.Id)
						.IsIn(ids)
						.List().ToList();
					foreach (var a in attendees) {
						a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
						s.Update(a);
					}
					//End all logs 
					var logs = s.QueryOver<L10Meeting.L10Meeting_Log>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.EndTime == null)
						.List().ToList();
					foreach (var l in logs) {
						l.EndTime = now;
						s.Update(l);
					}

					//Close all sub issues
					IssueModel issueAlias = null;
					var issue_recurParents = s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x => x.DeleteTime == null && x.CloseTime >= meeting.StartTime && x.CloseTime <= meeting.CompleteTime && x.Recurrence.Id == recurrenceId)
						//.Select(x => x.Id)
						.List().ToList();
					_RecursiveCloseIssues(s, issue_recurParents.Select(x => x.Id).ToList(), now);

					recurrence.MeetingInProgress = null;
					recurrence.SelectedVideoProvider = null;

					s.Update(recurrence);

					//send emails
					if (sendEmail) {
						try {


							var todoList = s.QueryOver<TodoModel>().Where(x =>
								x.DeleteTime == null &&
								x.ForRecurrenceId == recurrenceId &&
								x.CompleteTime == null
								).List().ToList();

							//All awaitables 

							var issuesForTable = issue_recurParents.Where(x => !x.AwaitingSolve);
							var pads = issuesForTable.Select(x => x.Issue.PadId).ToList();
							pads.AddRange(todoList.Select(x => x.PadId));
							var padTexts = await PadAccessor.GetHtmls(pads);

							/////


							var issueTable = await IssuesAccessor.BuildIssuesSolvedTable(issuesForTable.ToList(), "Issues Solved", recurrenceId, true, padTexts);
							var todosTable = new DefaultDictionary<long, string>(x => "");

							var allUserIds = todoList.Select(x => x.AccountableUserId).ToList();
							allUserIds.AddRange(attendees.Select(x => x.User.Id));
							allUserIds = allUserIds.Distinct().ToList();
							var allUsers = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(allUserIds).List().ToList();

							var auLu = new DefaultDictionary<long, UserOrganizationModel>(x => null);
							foreach (var u in allUsers) {
								auLu[u.Id] = u;
							}

							foreach (var personTodos in todoList.GroupBy(x => x.AccountableUserId)) {
								var user = auLu[personTodos.First().AccountableUserId];
								//var email = user.GetEmail();

								var todoTable = await TodoAccessor.BuildTodoTable(personTodos.ToList(), "Outstanding To-dos", true, padLookup: padTexts);

								var output = new StringBuilder();

								output.Append(todoTable.ToString());
								output.Append("<br/>");

								todosTable[user.Id] = output.ToString();

							}

							foreach (var userAttendee in attendees) {
								var output = new StringBuilder();
								var user = auLu[userAttendee.User.Id];
								var email = user.GetEmail();

								output.Append(todosTable[user.Id]);
								if (issuesForTable.Any()) {
									output.Append(issueTable.ToString());
								}
								var mail = Mail.To(EmailTypes.L10Summary, email)
									.Subject(EmailStrings.MeetingSummary_Subject, recurrence.Name)
									.Body(EmailStrings.MeetingSummary_Body, user.GetName(), output.ToString(), Config.ProductName(meeting.Organization));
								unsent.Add(mail);
							}

						} catch (Exception e) {
							log.Error("Emailer issue(1):" + recurrence.Id, e);
						}
					}

					EventUtil.Trigger(x => x.Create(s, EventType.ConcludeMeeting, caller, recurrence, message: recurrence.Name + "(" + DateTime.UtcNow.Date.ToShortDateString() + ")"));

					Audit.L10Log(s, caller, recurrenceId, "ConcludeMeeting", ForModel.Create(meeting));
					tx.Commit();
					s.Flush();
				}
			}
			if (meeting != null) {
				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
				hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting), connectionId).concludeMeeting();
			}

			try {
				if (sendEmail && unsent != null) {
					await Emailer.SendEmails(unsent);
				}
			} catch (Exception e) {
				log.Error("Emailer issue(2):" + recurrenceId, e);
			}
		}

		public static L10Meeting.L10Meeting_Connection JoinL10Meeting(UserOrganizationModel caller, long recurrenceId, string connectionId) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (recurrenceId == -3) {
						var recurs = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == caller.Id)
							.Select(x => x.L10Recurrence.Id)
							.List<long>().ToList();
						foreach (var r in recurs) {
							hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(r));
						}
						hub.Groups.Add(connectionId, MeetingHub.GenerateUserId(caller.Id));
					} else {
						new PermissionsAccessor().Permitted(caller, x => x.ViewL10Recurrence(recurrenceId));
						hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(recurrenceId));
						Audit.L10Log(s, caller, recurrenceId, "JoinL10Meeting", ForModel.Create(caller));
						var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
						meetingHub.userEnterMeeting(caller.Id, connectionId, caller.GetName(), caller.ImageUrl(true));
					}
				}
			}

			return null;
		}
		#endregion

		#region Load Data
		public static void _LoadMeetingLogs(ISession s, params L10Meeting[] meetings) {
			var meetingIds = meetings.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();
			if (meetingIds.Any()) {
				var allLogs = s.QueryOver<L10Meeting.L10Meeting_Log>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();
				var now = DateTime.UtcNow;
				foreach (var m in meetings.Where(x => x != null)) {
					m._MeetingLogs = allLogs.Where(x => m.Id == x.L10Meeting.Id).ToList();

					m._MeetingLeaderPageDurations = m._MeetingLogs
						.Where(x => x.User.Id == m.MeetingLeader.Id && x.EndTime != null)
						.GroupBy(x => x.Page)
						.Select(x =>
							Tuple.Create(
								x.First().Page,
								x.Sum(y => ((y.EndTime ?? now) - y.StartTime).TotalMinutes)
								)).ToList();

					var curPage = m._MeetingLogs
						.Where(x => x.User.Id == m.MeetingLeader.Id && x.EndTime == null)
						.OrderByDescending(x => x.StartTime)
						.FirstOrDefault();

					if (curPage != null) {
						m._MeetingLeaderCurrentPage = curPage.Page;
						m._MeetingLeaderCurrentPageStartTime = curPage.StartTime;
						m._MeetingLeaderCurrentPageBaseMinutes = m._MeetingLeaderPageDurations.Where(x => x.Item1 == curPage.Page).Sum(x => x.Item2);
					}
				}
			}
		}

		public static void _LoadMeetings(ISession s, bool loadUsers, bool loadMeasurables, bool loadRocks, params L10Meeting[] meetings) {
			var meetingIds = meetings.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();

			if (meetingIds.Any()) {
				var allAttend = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();
				var allMeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();
				var allRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetingIds)
					.List().ToList();
				foreach (var m in meetings) {
					if (m.L10Recurrence.IncludeAggregateTodoCompletion) {
						allMeasurables.Add(new L10Meeting.L10Meeting_Measurable() {
							_Ordering = -2,
							Id = -1,
							L10Meeting = m,
							Measurable = TodoMeasurable
						});
					}
				}
				foreach (var m in meetings.Where(x => x != null)) {
					m._MeetingAttendees = allAttend.Where(x => m.Id == x.L10Meeting.Id).ToList();
					m._MeetingMeasurables = allMeasurables.Where(x => m.Id == x.L10Meeting.Id).ToList();
					m._MeetingRocks = allRocks.Where(x => m.Id == x.L10Meeting.Id).ToList();
					if (m.L10Recurrence.IncludeIndividualTodos) {
						foreach (var u in m._MeetingAttendees) {
							m._MeetingMeasurables.Add(new L10Meeting.L10Meeting_Measurable() {
								_Ordering = -1,
								Id = -1,
								L10Meeting = m,
								Measurable = GenerateTodoMeasureable(u.User)
							});
						}
					}
					if (loadUsers) {
						foreach (var u in m._MeetingAttendees) {
							try {
								u.User.GetName();
								u.User.ImageUrl();
							} catch (Exception) {
							}
						}
					}
					if (loadMeasurables) {
						foreach (var u in m._MeetingMeasurables) {
							try {
								if (u.Measurable.AccountableUser != null) {
									u.Measurable.AccountableUser.GetName();
									u.Measurable.AccountableUser.ImageUrl();
								}
								if (u.Measurable.AdminUser != null) {
									u.Measurable.AdminUser.GetName();
									u.Measurable.AdminUser.ImageUrl();
								}
							} catch (Exception) {
							}
						}
					}
					if (loadRocks) {
						foreach (var u in m._MeetingRocks) {
							try {
								u.ForRock.AccountableUser.GetName();
								u.ForRock.AccountableUser.ImageUrl();
							} catch (Exception) {
							}
						}
					}
				}
			}
		}
		public static void _LoadRecurrences(ISession s, bool loadUsers, bool loadMeasurables, bool loadRocks, params L10Recurrence[] all) {
			var recurrenceIds = all.Where(x => x != null).Select(x => x.Id).Distinct().ToArray();

			if (recurrenceIds.Any()) {
				var allAttend = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
					.Future<L10Recurrence.L10Recurrence_Attendee>();
				var allMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
					.Fetch(x => x.Measurable).Eager
					.Future<L10Recurrence.L10Recurrence_Measurable>();
				var allRocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
					.Future<L10Recurrence.L10Recurrence_Rocks>();
				var allNotes = s.QueryOver<L10Note>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.Recurrence.Id).IsIn(recurrenceIds)
					.Future<L10Note>();
				var allVCP = s.QueryOver<L10Recurrence.L10Recurrence_VideoConferenceProvider>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
					.Future<L10Recurrence.L10Recurrence_VideoConferenceProvider>();

				foreach (var a in all.Where(x => x != null)) {
					a._DefaultAttendees = allAttend.Where(x => a.Id == x.L10Recurrence.Id && x.User.DeleteTime == null).ToList();
					var dm = allMeasurables.Where(x => a.Id == x.L10Recurrence.Id && ((x.Measurable != null && x.Measurable.DeleteTime == null) || (x.Measurable == null && x.IsDivider))).ToList();
					a._DefaultRocks = allRocks.Where(x => a.Id == x.L10Recurrence.Id && x.ForRock.DeleteTime == null).ToList();
					a._MeetingNotes = allNotes.Where(x => a.Id == x.Recurrence.Id && x.DeleteTime == null).ToList();
					a._VideoConferenceProviders = allVCP.Where(x => a.Id == x.L10Recurrence.Id && x.DeleteTime == null).ToList();

					if (a.IncludeIndividualTodos) {
						foreach (var u in a._DefaultAttendees) {
							dm.Add(new L10Recurrence.L10Recurrence_Measurable() {
								_Ordering = -1,
								Id = -1,
								L10Recurrence = a,
								Measurable = GenerateTodoMeasureable(u.User)
							});
						}
					}
					a._DefaultMeasurables = dm;
					if (loadUsers) {
						foreach (var u in a._DefaultAttendees) {
							u.User.GetName();
							u.User.ImageUrl(true);
						}
					}
					if (loadMeasurables) {
						foreach (var u in a._DefaultMeasurables.Where(x => x.Measurable != null)) {
							if (u.Measurable.AccountableUser != null) {
								u.Measurable.AccountableUser.GetName();
								u.Measurable.AccountableUser.ImageUrl(true);
							}
							if (u.Measurable.AdminUser != null) {
								u.Measurable.AdminUser.GetName();
								u.Measurable.AdminUser.ImageUrl(true);
							}
						}
					}
					if (loadRocks) {
						foreach (var u in a._DefaultRocks) {
							var b = u.ForRock.Rock;
							var c = u.ForRock.Period.NotNull(x => x.Name);
						}
					}
					if (true) {//Load video
						foreach (var v in a._VideoConferenceProviders) {
							var aa = v.Provider.GetVideoConferenceType();
							var b = v.Provider.GetType();
							var c = v.Provider.GetUrl();
							var d = v.Provider.FriendlyName;
						}
					}
				}
			}
		}

		private static List<IssueModel.IssueModel_Recurrence> _PopulateChildrenIssues(List<IssueModel.IssueModel_Recurrence> list) {
			var output = list.Where(x => x.ParentRecurrenceIssue == null).ToList();
			foreach (var o in output) {
				_RecurseChildrenIssues(o, list);
			}
			foreach (var o in output) {
				try {
					if (o.Owner != null) {
						o.Owner.GetName();
						o.Owner.GetImageUrl();
					}
				} catch (Exception) {
				}
			}
			output = output.OrderBy(x => x.Ordering).ToList();
			return output;

		}
		private static void _RecurseChildrenIssues(IssueModel.IssueModel_Recurrence issue, IEnumerable<IssueModel.IssueModel_Recurrence> list) {
			if (issue._ChildIssues != null)
				return;
			issue._ChildIssues = list.Where(x => x.ParentRecurrenceIssue != null && x.ParentRecurrenceIssue.Id == issue.Id).ToList();
			foreach (var i in issue._ChildIssues) {
				_RecurseChildrenIssues(i, list);
			}
		}
		#endregion

		#region Session Methods
		public static L10Meeting.L10Meeting_Log _GetCurrentLog(ISession s, UserOrganizationModel caller, long meetingId, long userId, bool nullOnUnstarted = false) {
			var found = s.QueryOver<L10Meeting.L10Meeting_Log>()
				.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id == userId && x.EndTime == null)
				.List().OrderByDescending(x => x.StartTime)
				.FirstOrDefault();
			if (found == null && !nullOnUnstarted)
				throw new PermissionsException("Meeting log does not exist");
			return found;
		}
		public static L10Meeting _GetCurrentL10Meeting(ISession s, PermissionsUtility perms, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false) {
			var found = s.QueryOver<L10Meeting>().Where(x =>
					x.StartTime != null &&
					x.CompleteTime == null &&
					x.DeleteTime == null &&
					x.L10RecurrenceId == recurrenceId
				).List().ToList();

			if (!found.Any()) {
				if (nullOnUnstarted)
					return null;
				throw new MeetingException("Meeting has not been started.", MeetingExceptionType.Unstarted);
			}
			if (found.Count != 1) {
				//throw new MeetingException("Too many open meetings.", MeetingExceptionType.TooMany);
				found = found.OrderByDescending(x => x.StartTime).ToList();
			}
			var meeting = found.First();
			perms.ViewL10Meeting(meeting.Id);
			if (load)
				_LoadMeetings(s, true, true, true, meeting);

			if (loadLogs)
				_LoadMeetingLogs(s, meeting);

			return meeting;
		}
		private static void _RecursiveCloseIssues(ISession s, List<long> parentIssue_RecurIds, DateTime now) {
			if (parentIssue_RecurIds.Count == 0)
				return;

			var children = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.CloseTime == null)
				.WhereRestrictionOn(x => x.ParentRecurrenceIssue.Id)
				.IsIn(parentIssue_RecurIds)
				.List().ToList();
			foreach (var c in children) {
				c.CloseTime = now;

				//Needs updating for RealTime

				s.Update(c);
			}
			_RecursiveCloseIssues(s, children.Select(x => x.Id).ToList(), now);
		}

		public static List<PermItem> GetAdmins(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return perms.GetAdmins(PermItem.ResourceType.L10Recurrence, recurrenceId);
				}
			}
		}

		public static List<L10Recurrence> _GetAllL10RecurrenceAtOrganization(ISession s, UserOrganizationModel caller, long organizationId) {
			PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
			return s.QueryOver<L10Recurrence>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId)
				.List().ToList();
		}
		public static List<L10Recurrence> _GetAllConnectedL10Recurrence(ISession s, UserOrganizationModel caller, long recurrenceId) {
			var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

			var userIds = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
				.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
				.Select(x => x.User.Id)
				.List<long>().ToList();

			var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
				.Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.User.Id).IsIn(userIds)
				.Select(x => x.L10Recurrence.Id)
				.List<long>().ToList();

			return s.QueryOver<L10Recurrence>()
				.Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.Id).IsIn(recurrenceIds)
				.List().ToList();

		}
		#endregion

		#region Get Meeting Data
		public static L10Recurrence GetL10Recurrence(UserOrganizationModel caller, long recurrenceId, bool load) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetL10Recurrence(s, perms, recurrenceId, load);
				}
			}
		}
		public static L10Recurrence GetL10Recurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool load) {
			perms.ViewL10Recurrence(recurrenceId);
			var found = s.Get<L10Recurrence>(recurrenceId);
			if (load)
				_LoadRecurrences(s, true, true, true, found);
			return found;
		}
		public static L10Meeting GetPreviousMeeting(ISession s, PermissionsUtility perms, long recurrenceId) {
			perms.ViewL10Recurrence(recurrenceId);
			var previousMeeting = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.L10RecurrenceId == recurrenceId && x.CompleteTime != null).OrderBy(x => x.CompleteTime).Desc.Take(1).SingleOrDefault();
			return previousMeeting;
		}

		public static DateTime GetLastMeetingEndTime(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var last = GetPreviousMeeting(s, perms, recurrenceId);
					if (last == null || !last.CompleteTime.HasValue)
						return DateTime.MinValue;
					return last.CompleteTime.Value;
				}
			}
		}

		public static List<NameId> GetVisibleL10Meetings_Tiny(UserOrganizationModel caller, long userId, bool onlyPersonallyAttending = false,bool onlyDashboardRecurrences=false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetVisibleL10Meetings_Tiny(s, perms, userId, onlyPersonallyAttending,onlyDashboardRecurrences);
				}
			}
		}
		public static List<NameId> GetVisibleL10Meetings_Tiny(ISession s, PermissionsUtility perms, long userId, bool onlyPersonallyAttending = false, bool onlyDashboardRecurrences = false) {
            List<long> personallyAttending;
            List<long> dashRecurs;
            var meetings = GetVisibleL10Meetings_Tiny(s, perms, userId, out personallyAttending,out dashRecurs);
            if (onlyPersonallyAttending) {
                meetings = meetings.Where(x => personallyAttending.Contains(x.Id)).ToList();
            }
            if (onlyDashboardRecurrences) {
                meetings = meetings.Where(x => dashRecurs.Contains(x.Id)).ToList();
            }
            return meetings;
		}
		public static List<NameId> GetVisibleL10Meetings_Tiny(ISession s, PermissionsUtility perms, long userId, out List<long> recurrencesPersonallyAttending, out List<long> recurrencesVisibleOnDashboard) {

			//IMPORTANT. Make sure the pristine flag is being set correctly on L10Recurrence.

			var caller = perms.GetCaller();
			perms.ViewUsersL10Meetings(userId);
			
			//Who should we get this data for? Just Self, or also subordiantes?
			var accessibleUserIds = new[] { userId };
			var user = s.Get<UserOrganizationModel>(userId);
			if (user.Organization.Settings.ManagersCanViewSubordinateL10)
				accessibleUserIds = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, userId).ToArray(); //DeepSubordianteAccessor.GetSubordinatesAndSelf(s, caller, userId).ToArray();

			L10Recurrence alias = null;
			//var allRecurrences = new List<L10Recurrence>();
			var allRecurrenceIds = new List<NameId>();
			IEnumerable<object[]> orgRecurrences = null ;
			if (caller.ManagingOrganization) {
				orgRecurrences = s.QueryOver<L10Recurrence>().Where(x => x.OrganizationId == caller.Organization.Id && x.DeleteTime == null && !x.Pristine)
					.Select(x => x.Name, x => x.Id)
					.Future<object[]>();
			}
			
			var attendee_ReccurenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
				.Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.User.Id).IsIn(accessibleUserIds)
				.Left.JoinQueryOver(x => x.L10Recurrence, () => alias)
				.Where(x => alias.DeleteTime == null)
				.Select(x => alias.Name, x => alias.Id, x => x.User.Id)
				.Future<object[]>();

			//Actually load the Recurrences

			var admin_MeasurableIds = s.QueryOver<MeasurableModel>().Where(x => x.AdminUserId == userId && x.DeleteTime == null).Select(x => x.Id).List<long>().ToList();
			var admin_RecurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.Measurable.Id).IsIn(admin_MeasurableIds)
				.Left.JoinQueryOver(x => x.L10Recurrence, () => alias)
				.Where(x => alias.DeleteTime == null)
				.Select(x => alias.Name, x => alias.Id)
				.List<object[]>().Select(x => new NameId((string)x[0], (long)x[1])).ToList();

			



			//From future
			var attendee_recurrences = attendee_ReccurenceIds.ToList().Select(x => new NameId((string)x[0], (long)x[1])).ToList();
			recurrencesPersonallyAttending = attendee_ReccurenceIds.Where(x => (long)x[2] == userId).Select(x => (long)x[1]).ToList();
			recurrencesPersonallyAttending = recurrencesPersonallyAttending.Distinct().ToList();
            recurrencesVisibleOnDashboard = recurrencesPersonallyAttending.ToList();
            


			allRecurrenceIds.AddRange(attendee_recurrences);
			allRecurrenceIds.AddRange(admin_RecurrenceIds);
			

			var allViewPerms = PermissionsAccessor.GetExplicitPermItemsForUser(s, perms, userId, PermItem.ResourceType.L10Recurrence).Where(x => x.CanView);
			var allViewPermsRecurrences = allRecurrenceIds.Where(allRecurrenceId => allViewPerms.Any(y => allRecurrenceId.Id == y.ResId)).ToList();
			recurrencesVisibleOnDashboard.AddRange(allViewPermsRecurrences.Select(x=> x.Id));

			//Outside the company
			var additionalRecurrenceIdsFromPerms = allViewPerms.Where(allViewPermId => !allRecurrenceIds.Any(y => y.Id == allViewPermId.ResId)).ToList();
			var additionalRecurrenceFromViewPerms = s.QueryOver<L10Recurrence>()
				.Where(x => !x.Pristine && x.DeleteTime == null)
				.WhereRestrictionOn(x => x.Id).IsIn(additionalRecurrenceIdsFromPerms.Select(x => x.ResId).ToArray())
				.Select(x => x.Name, x => x.Id)
				.List<object[]>().Select(x => new NameId((string)x[0], (long)x[1])).ToList();
			allRecurrenceIds.AddRange(additionalRecurrenceFromViewPerms);
            recurrencesVisibleOnDashboard.AddRange(additionalRecurrenceFromViewPerms.Select(x => x.Id));




			if (orgRecurrences != null) {
				allRecurrenceIds.AddRange(orgRecurrences.ToList().Select(x => new NameId((string)x[0], (long)x[1])));
			}

			allRecurrenceIds = allRecurrenceIds.Distinct(x => x.Id).ToList();
			recurrencesVisibleOnDashboard = recurrencesVisibleOnDashboard.Distinct().ToList();


			if (caller.ManagingOrganization) {
				return allRecurrenceIds;
			}

			var available = new List<NameId>();
			foreach (var r in allRecurrenceIds) {

				try {
					perms.CanView(PermItem.ResourceType.L10Recurrence, r.Id);
					available.Add(r);
				} catch {
				}
			}
			return available;
		}

		public static List<L10VM> GetVisibleL10Recurrences(UserOrganizationModel caller, long userId, bool loadUsers) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					List<long> attendee_recurrences; List<long> _nil;
                    var uniqueL10NameIds = GetVisibleL10Meetings_Tiny(s, perms, userId, out attendee_recurrences,out _nil);
					var uniqueL10Ids = uniqueL10NameIds.Select(x => x.Id).ToList();


					var allRecurrences = s.QueryOver<L10Recurrence>()
						.Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.Id).IsIn(uniqueL10Ids)
						.List().ToList();
					//allRecurrences.AddRange(loadedL10);


					//Load extra data
					//var allRecurrencesDistinct = allRecurrences.Distinct(x => x.Id).ToList();
					_LoadRecurrences(s, loadUsers, false, false, allRecurrences.ToArray());

					//Make a lookup for self attendance
					//var attending = attendee_recurrences.Where(x => userId == x.User.Id).Select(x => x.L10Recurrence.Id).ToArray();
					return allRecurrences.Select(x => new L10VM(x) {
						IsAttendee = attendee_recurrences.Any(y => y == x.Id),
						AdminMeeting = perms.IsPermitted(y => y.AdminL10Recurrence(x.Id))
					}).ToList();
				}
			}
		}
		public static string GetCurrentL10MeetingLeaderPage(UserOrganizationModel caller, long meetingId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var leaderId = s.Get<L10Meeting>(meetingId).MeetingLeader.Id;
					var leaderpage = s.QueryOver<L10Meeting.L10Meeting_Log>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id == leaderId && x.EndTime == null)
						.List().OrderByDescending(x => x.StartTime)
						.FirstOrDefault();
					return leaderpage.NotNull(x => x.Page);
				}
			}
		}

		public static L10Meeting GetCurrentL10Meeting(UserOrganizationModel caller, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return _GetCurrentL10Meeting(s, perms, recurrenceId, nullOnUnstarted, load, loadLogs);
				}
			}
		}
		public static List<L10Meeting> GetL10Meetings(UserOrganizationModel caller, long recurrenceId, bool load = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					var o = s.QueryOver<L10Meeting>()
						.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
						.List().ToList();
					if (load)
						_LoadMeetings(s, true, true, true, o.ToArray());

					return o;

				}
			}
		}

		//Finds all first degree connectioned L10Recurrences
		public static List<L10Recurrence> GetAllConnectedL10Recurrence(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return _GetAllConnectedL10Recurrence(s, caller, recurrenceId);
				}
			}
		}
		public static List<L10Recurrence> GetAllL10RecurrenceAtOrganization(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return _GetAllL10RecurrenceAtOrganization(s, caller, organizationId);
				}
			}
		}
		public static L10Recurrence GetCurrentL10RecurrenceFromMeeting(UserOrganizationModel caller, long l10MeetingId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewL10Meeting(l10MeetingId);
					var recurrence = s.Get<L10Meeting>(l10MeetingId).L10RecurrenceId;

					return GetL10Recurrence(s, perms, recurrence, true);
				}
			}
		}
		public static long GetLatestMeetingId(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var meeting = s.QueryOver<L10Meeting>().Where(x => x.L10RecurrenceId == recurrenceId && x.DeleteTime == null).OrderBy(x => x.Id).Desc.Take(1).List().ToList();
					var m = meeting.SingleOrDefault();
					return m.NotNull(x => x.Id);
				}
			}
		}

		#endregion

		#region Edit Meeting
		public static void EditL10Recurrence(UserOrganizationModel caller, L10Recurrence l10Recurrence) {
			bool wasCreated = false;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					if (l10Recurrence.Id == 0) {
						perm.CreateL10Recurrence(caller.Organization.Id);
						l10Recurrence.CreatedById = caller.Id;
						wasCreated = true;
						if (l10Recurrence.TeamType == L10TeamType.LeadershipTeam) {
							EventUtil.Trigger(x => x.Create(s, EventType.CreateLeadershipMeeting, caller, l10Recurrence, message: l10Recurrence.Name));
						} else if (l10Recurrence.TeamType == L10TeamType.DepartmentalTeam) {
							EventUtil.Trigger(x => x.Create(s, EventType.CreateDepartmentMeeting, caller, l10Recurrence, message: l10Recurrence.Name));
						}
					} else
						perm.AdminL10Recurrence(l10Recurrence.Id);

					//s.UpdateLists(l10Recurrence,DateTime.UtcNow,x=>x.DefaultAttendees,x=>x.DefaultMeasurables);
					/*if (l10Recurrence.Id != 0){
                        var old = s.Get<L10Recurrence>(l10Recurrence.Id);
                        //SetUtility.AddRemove(old.DefaultAttendees,l10Recurrence.DefaultAttendees,x=>x.)
                    }*/
					var oldRecur = s.Get<L10Recurrence>(l10Recurrence.Id);
					_LoadRecurrences(s, false, false, false, oldRecur);

					var oldMeeting = _GetCurrentL10Meeting(s, perm, l10Recurrence.Id, true, true);
					SetUtility.AddedRemoved<MeasurableModel> updateMeasurables = null;
					VtoModel vto = null;
					if (oldMeeting != null) {
						updateMeasurables = SetUtility.AddRemove(oldMeeting._MeetingMeasurables.Where(x => !x.IsDivider).Select(x => x.Measurable), l10Recurrence._DefaultMeasurables.Select(x => x.Measurable), x => x.Id);
						var updateableMeasurables = ScorecardAccessor.GetVisibleMeasurables(s, perm, l10Recurrence.OrganizationId, false);
						if (!updateMeasurables.AddedValues.All(x => updateableMeasurables.Any(y => y.Id == x.Id)))
							throw new PermissionsException("You do not have access to add one or more measurables.");
						if (oldMeeting.L10Recurrence.VtoId != 0) {
							vto = s.Get<VtoModel>(oldMeeting.L10Recurrence.VtoId);
						}
					}
					SetUtility.AddedRemoved<RockModel> updateRocks = null;
					if (oldMeeting != null) {
						updateRocks = SetUtility.AddRemove(
							oldMeeting._MeetingRocks.Select(x => x.ForRock),
							l10Recurrence._DefaultRocks.Select(x => x.ForRock),
							x => x.Id);

						var updatedRocks = RockAccessor.GetAllVisibleRocksAtOrganization(s, perm, l10Recurrence.OrganizationId, false);
						if (!updateRocks.AddedValues.All(x => updatedRocks.Any(y => y.Id == x.Id)))
							throw new PermissionsException("You do not have access to add one or more rock.");
					}

					SetUtility.AddedRemoved<UserOrganizationModel> updateAttendees = null;
					if (oldMeeting != null) {
						updateAttendees = SetUtility.AddRemove(
							oldMeeting._MeetingAttendees.Select(x => x.User),
							l10Recurrence._DefaultAttendees.Select(x => x.User),
							x => x.Id);


					}

					var now = DateTime.UtcNow;

					if (oldRecur != null) {
						foreach (var m in l10Recurrence._DefaultMeasurables) {
							m._Ordering = oldRecur._DefaultMeasurables.FirstOrDefault(x => x.Measurable != null && m.Measurable != null && x.Measurable.Id == m.Measurable.Id).NotNull(x => x._Ordering);
						}
					}


					/// match up attendees, measureables, and rocks
					/// 

					l10Recurrence._DefaultAttendees.ToList().ForEach(a => {
						if (oldRecur != null)
							a.Id = oldRecur._DefaultAttendees.FirstOrDefault(x => x.User.Id == a.User.Id).NotNull(x => x.Id);
					});
					l10Recurrence._DefaultRocks.ToList().ForEach(a => {
						if (oldRecur != null)
							a.Id = oldRecur._DefaultRocks.FirstOrDefault(x => x.ForRock.Id == a.ForRock.Id).NotNull(x => x.Id);
					});
					l10Recurrence._DefaultMeasurables.ToList().ForEach(a => {
						if (oldRecur != null) {
							var found = oldRecur._DefaultMeasurables.FirstOrDefault(x => ((x.Measurable == null && a.Measurable == null) || (x.Measurable != null && a.Measurable != null && x.Measurable.Id == a.Measurable.Id)) && !x._Used);
							if (found != null) {
								a.Id = found.Id;
								found._Used = true;
							}
						}
					});


					//Update new measurablse, attendees, rocks

					s.UpdateList(oldRecur.NotNull(x => x._DefaultAttendees), l10Recurrence._DefaultAttendees, now);
					s.UpdateList(oldRecur.NotNull(x => x._DefaultMeasurables.Where(y => !y.IsDivider)), l10Recurrence._DefaultMeasurables, now);
					s.UpdateList(oldRecur.NotNull(x => x._DefaultRocks), l10Recurrence._DefaultRocks, now);


					/////////////
					//Update rocks on the VTO also

					//we need to make sure to set this or the rocks is duplicated.
					foreach (var r in l10Recurrence._DefaultRocks)
						r.ForRock._AddedToL10 = true;


					if (oldRecur != null && vto != null) {

						var updateRocksRecur = SetUtility.AddRemove(oldRecur._DefaultRocks.Select(y => y.ForRock), l10Recurrence._DefaultRocks.Select(y => y.ForRock), x => x.Id);

						foreach (var a in updateRocksRecur.AddedValues) {
							VtoAccessor.AddRock(s, perm, vto.Id, a);
						}
						foreach (var a in updateRocksRecur.RemovedValues) {
							var vtoRocks = s.QueryOver<Vto_Rocks>().Where(x => x.Vto.Id == vto.Id && x.Rock.Id == a.Id && x.DeleteTime == null).List().ToList();

							foreach (var r in vtoRocks) {
								r.DeleteTime = now;
								s.Update(r);
							}
						}
					}
					////////////


					s.Evict(oldRecur);

					s.SaveOrUpdate(l10Recurrence);

					if (wasCreated) {
						vto = VtoAccessor.CreateRecurrenceVTO(s, perm, l10Recurrence.Id);

						s.Save(new PermItem() {
							CanAdmin = true,
							CanEdit = true,
							CanView = true,
							AccessorType = PermItem.AccessType.Creator,
							AccessorId = caller.Id,
							ResType = PermItem.ResourceType.L10Recurrence,
							ResId = l10Recurrence.Id,
							CreatorId = caller.Id,
							OrganizationId = caller.Organization.Id,
							IsArchtype = false,
						});
						s.Save(new PermItem() {
							CanAdmin = true,
							CanEdit = true,
							CanView = true,
							AccessorType = PermItem.AccessType.Members,
							AccessorId = -1,
							ResType = PermItem.ResourceType.L10Recurrence,
							ResId = l10Recurrence.Id,
							CreatorId = caller.Id,
							OrganizationId = caller.Organization.Id,
							IsArchtype = false,
						});
						s.Save(new PermItem() {
							CanAdmin = true,
							CanEdit = true,
							CanView = true,
							AccessorType = PermItem.AccessType.Admins,
							AccessorId = -1,
							ResType = PermItem.ResourceType.L10Recurrence,
							ResId = l10Recurrence.Id,
							CreatorId = caller.Id,
							OrganizationId = caller.Organization.Id,
							IsArchtype = false,
						});
					}



					if (updateMeasurables != null) {
						//Add new values.. probably shouldn't remove stale ones..
						foreach (var a in updateMeasurables.AddedValues) {
							s.Save(new L10Meeting.L10Meeting_Measurable() {
								L10Meeting = oldMeeting,
								Measurable = a,
							});

						}
						foreach (var a in updateMeasurables.RemovedValues) {
							if (a.Id > 0) { //Todo Completion is -10001
								var o = oldMeeting._MeetingMeasurables.First(x => x.Measurable != null && x.Measurable.Id == a.Id);
								if (!o.IsDivider) {
									o.DeleteTime = now;
									s.Update(o);
								}
							}
						}
					}
					if (updateRocks != null) {
						//Add new values.. probably shouldn't remove stale ones..
						foreach (var a in updateRocks.AddedValues) {
							s.Save(new L10Meeting.L10Meeting_Rock() {
								L10Meeting = oldMeeting,
								ForRock = a,
								ForRecurrence = oldMeeting.L10Recurrence,
							});



						}
						foreach (var a in updateRocks.RemovedValues) {
							var o = oldMeeting._MeetingRocks.First(x => x.ForRock != null && x.ForRock.Id == a.Id);
							o.DeleteTime = now;
							s.Update(o);
						}
					}

					if (updateAttendees != null) {
						//Add new values.. probably shouldn't remove stale ones..
						foreach (var a in updateAttendees.AddedValues) {
							s.Save(new L10Meeting.L10Meeting_Attendee() {
								L10Meeting = oldMeeting,
								User = a,
							});
						}
						foreach (var a in updateAttendees.RemovedValues) {
							var o = oldMeeting._MeetingAttendees.First(x => x.User != null && x.User.Id == a.Id);
							o.DeleteTime = now;
							s.Update(o);
						}
					}

					Audit.L10Log(s, caller, l10Recurrence.Id, "EditL10Recurrence", ForModel.Create(l10Recurrence));

					tx.Commit();
					s.Flush();
				}
			}

		}
		public static void UpdatePage(UserOrganizationModel caller, long forUserId, long recurrenceId, string pageName, string connection) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, true);
					if (meeting == null)
						return;
					//if (caller.Id != meeting.MeetingLeader.Id)	return;


					var forUser = s.Get<UserOrganizationModel>(forUserId);
					if (meeting.MeetingLeaderId == 0) {
						meeting.MeetingLeaderId = forUser.Id;
						meeting.MeetingLeader = forUser;
					}

					if (caller.Id != forUserId)
						PermissionsUtility.Create(s, forUser).ViewL10Meeting(meeting.Id);

					var log = _GetCurrentLog(s, caller, meeting.Id, forUserId, true);

					var now = DateTime.UtcNow;
					var addNew = true;
					if (log != null) {
						addNew = log.Page != pageName;
						if (addNew) {
							log.EndTime = now;//new DateTime(Math.Min(log.StartTime.AddMinutes(1).Ticks,now.Ticks));
							s.Update(log);
						}
					}

					if (addNew) {
						var newLog = new L10Meeting.L10Meeting_Log() {
							User = forUser,
							StartTime = now,
							L10Meeting = meeting,
							Page = pageName,
						};

						s.Save(newLog);



						if (meeting.MeetingLeader.NotNull(x => x.Id) == forUserId) {
							if (log != null) {
								//Add additional minutes from current page
								var cur = meeting._MeetingLeaderPageDurations.FirstOrDefault(x => x.Item1 == log.Page);
								var duration = (log.EndTime.Value - log.StartTime).TotalMinutes;
								if (cur == null) {
									meeting._MeetingLeaderPageDurations.Add(Tuple.Create(log.Page, duration));
								} else {
									for (var i = 0; i < meeting._MeetingLeaderPageDurations.Count; i++) {
										var x = meeting._MeetingLeaderPageDurations[i];
										if (x.Item1 == log.Page) {
											meeting._MeetingLeaderPageDurations[i] = Tuple.Create(x.Item1, x.Item2 + duration);
										}
									}
								}
							}
							var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
							var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(meeting));
							var baseMins = meeting._MeetingLeaderPageDurations.SingleOrDefault(x => x.Item1 == pageName).NotNull(x => x.Item2);
							meetingHub.setCurrentPage(pageName.ToLower(), now.ToJavascriptMilliseconds(), baseMins);

							meetingHub.update(new AngularMeeting(recurrenceId) { CurrentPage = pageName });

							foreach (var a in meeting._MeetingLeaderPageDurations) {
								if (a.Item1 != pageName) {
									meetingHub.setPageTime(a.Item1, a.Item2);
								}
							}
						}

					}

					var p = pageName;
					if (!string.IsNullOrEmpty(p))
						p = p.ToUpper()[0] + p.Substring(1);

					Audit.L10Log(s, caller, recurrenceId, "UpdatePage", ForModel.Create(meeting), p);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void DeleteL10(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).AdminL10Recurrence(recurrenceId);
					var r = s.Get<L10Recurrence>(recurrenceId);
					r.DeleteTime = DateTime.UtcNow;

					s.Update(r);

					EventUtil.Trigger(x => x.Create(s, EventType.DeleteMeeting, caller, r, message: r.Name + "(Deleted)"));

					Audit.L10Log(s, caller, recurrenceId, "DeleteL10", ForModel.Create(r), r.Name);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void UndeleteL10(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).AdminL10Recurrence(recurrenceId);
					var r = s.Get<L10Recurrence>(recurrenceId);
					r.DeleteTime = null;

					s.Update(r);

					EventUtil.Trigger(x => x.Create(s, EventType.UndeleteMeeting, caller, r, message: r.Name + "(Undeleted)"));

					Audit.L10Log(s, caller, recurrenceId, "UndeleteL10", ForModel.Create(r), r.Name);
					tx.Commit();
					s.Flush();
				}
			}
		}
		#endregion

		#region Notes
		public static L10Note GetNote(UserOrganizationModel caller, long noteId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Note(noteId);
					return s.Get<L10Note>(noteId);
				}
			}
		}
		public static List<L10Note> GetVisibleL10Notes_Unsafe(List<long> recurrences) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var notes = s.QueryOver<L10Note>().Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.Recurrence).IsIn(recurrences.ToArray())
						.List().ToList();
					return notes;
				}
			}
		}
		public static string CreateNote(UserOrganizationModel caller, long recurrenceId, string name) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var note = new L10Note() {
						Name = name,
						Contents = "",
						Recurrence = s.Load<L10Recurrence>(recurrenceId)
					};
					s.Save(note);
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
					group.createNote(note.Id, name);
					var rec = new AngularRecurrence(recurrenceId) {
						Notes = new List<AngularMeetingNotes>(){
							new AngularMeetingNotes(note)
						}
					};
					group.update(rec);

					Audit.L10Log(s, caller, recurrenceId, "CreateNote", ForModel.Create(note), name);
					tx.Commit();
					s.Flush();
					return note.PadId;
				}
			}
		}
		public static void EditNote(UserOrganizationModel caller, long noteId, string contents = null, string name = null, string connectionId = null, bool? delete = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var note = s.Get<L10Note>(noteId);
					PermissionsUtility.Create(s, caller).EditL10Recurrence(note.Recurrence.Id);
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var now = DateTime.UtcNow;
					if (contents != null) {
						note.Contents = contents;
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(note.Recurrence.Id), connectionId).updateNoteContents(noteId, contents, now.ToJavascriptMilliseconds());
					}
					if (name != null) {
						note.Name = name;
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(note.Recurrence.Id), connectionId).updateNoteName(noteId, name);
					}
					_ProcessDeleted(s, note, delete);
					s.Update(note);
					Audit.L10Log(s, caller, note.Recurrence.Id, "EditNote", ForModel.Create(note), note.Name + ":\n" + note.Contents);
					tx.Commit();
					s.Flush();
				}
			}
		}
		#endregion

		#region Rocks
		public static List<RockModel> GetAllMyL10Rocks(UserOrganizationModel caller, long userId, long? periodId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					perm.ViewUserOrganization(userId, true, PermissionType.EditEmployeeDetails);
					return s.QueryOver<RockModel>()
						.Where(x => x.AccountableUser.Id == userId && x.DeleteTime == null)
						.List().ToList();
				}
			}
		}


		public static List<L10Recurrence.L10Recurrence_Rocks> GetRocksForRecurrence(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetRocksForRecurrence(s, perms, recurrenceId);
				}
			}
		}
		public static List<L10Recurrence.L10Recurrence_Rocks> GetRocksForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId) {
			perms.ViewL10Recurrence(recurrenceId);
			RockModel rock = null;
			var found = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
				.JoinAlias(x => x.ForRock, () => rock)
				.Where(x => x.DeleteTime == null && rock.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
				.Fetch(x => x.ForRock).Eager
				.List().ToList();
			foreach (var f in found) {
				if (f.ForRock.AccountableUser != null) {
					var a = f.ForRock.AccountableUser.GetName();
					var b = f.ForRock.AccountableUser.ImageUrl(true, ImageSize._32);
				}
			}
			return found;
		}

		public class MeetingRockAndMilestones {
			public L10Meeting.L10Meeting_Rock Rock { get; set; }
			public List<Milestone> Milestones { get; set; }

			public MeetingRockAndMilestones() {
				Milestones = new List<Milestone>();
			}
		}
		public static List<MeetingRockAndMilestones> GetRocksForMeeting(ISession s, PermissionsUtility perms, long recurrenceId, long meetingId) {
			perms.ViewL10Recurrence(recurrenceId).ViewL10Meeting(meetingId);
			var meetingRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>()
				.Where(x => x.DeleteTime == null && x.ForRecurrence.Id == recurrenceId && x.L10Meeting.Id == meetingId)
				.Fetch(x => x.ForRock).Eager
				.List().ToList();

			var rockIds = meetingRocks.Select(x => x.ForRock.Id).ToList();

			//var rocks = s.QueryOver<RockModel>().WhereRestrictionOn(x => x.Id).IsIn(rockIds).Future();
			var milestones = s.QueryOver<Milestone>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.RockId).IsIn(rockIds).Future();


			var found = new List<MeetingRockAndMilestones>();

			foreach (var f in meetingRocks) {
				var toAdd = new MeetingRockAndMilestones();

				if (f.ForRock.AccountableUser == null)
					f.ForRock.AccountableUser = s.Load<UserOrganizationModel>(f.ForRock.ForUserId);
				var a = f.ForRock.AccountableUser.NotNull(x => x.GetName());
				var b = f.ForRock.AccountableUser.NotNull(x => x.ImageUrl(true, ImageSize._32));
				toAdd.Rock = f;
				toAdd.Milestones = milestones.Where(x => x.RockId == f.ForRock.Id).ToList();
				found.Add(toAdd);
			}
			return found;
		}

		public static List<MeetingRockAndMilestones> GetRocksForMeeting(UserOrganizationModel caller, long recurrenceId, long meetingId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetRocksForMeeting(s, perms, recurrenceId, meetingId);
				}
			}
		}
		


		public static void CreateRock(UserOrganizationModel caller, long recurrenceId, L10Controller.AddRockVm model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					AddRock(s, perm, recurrenceId, model);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void AddRock(ISession s, PermissionsUtility perm, long recurrenceId, RockModel rock, DateTime? nowTime = null) {
			if (rock._AddedToL10)
				throw new PermissionsException("Already added to l10");
			rock._AddedToL10 = true;
			var recur = s.Get<L10Recurrence>(recurrenceId);
			perm.EditL10Recurrence(recurrenceId);

			var current = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);
			var now = nowTime ?? DateTime.UtcNow;

			var rm = new L10Recurrence.L10Recurrence_Rocks() {
				CreateTime = now,
				L10Recurrence = recur,
				ForRock = rock,
			};
			s.Save(rm);

			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
			if (current != null) {
				var mm = new L10Meeting.L10Meeting_Rock() {
					ForRecurrence = recur,
					L10Meeting = current,
					ForRock = rock,
				};
				s.Save(mm);

				var rocksAndMilestones = L10Accessor.GetRocksForMeeting(s, perm, recurrenceId, current.Id);
				var builder = "";
				if (!recur.CombineRocks && rocksAndMilestones.Where(x => x.Rock.ForRock.CompanyRock).Any()) {
					var crow = ViewUtility.RenderPartial("~/Views/L10/partial/CompanyRockGroup.cshtml", rocksAndMilestones.Select(x=>x.Rock).Where(x => x.ForRock.CompanyRock).ToList());
					builder += " <div class='company-rock-container'> " + crow.Execute() + " <hr/> </div> ";
				}

				//Update L10 meeting
				var row = ViewUtility.RenderPartial("~/Views/L10/partial/RockGroup.cshtml", rocksAndMilestones.Select(x=>x.Rock).ToList());
				builder = builder + row.Execute();
				group.updateRocks(builder);

				//Update Angular
				var arecur = new AngularRecurrence(recurrenceId) {
					Rocks = AngularList.Create(AngularListType.Add, new[]{new AngularRock(mm.ForRock){
						ForceOrder =int.MaxValue,
					}}),
					Focus = "[data-rock='" + mm.ForRock.Id + "'] input:visible:first"
				};
				group.update(new AngularUpdate { arecur });
			} else {
				var recurRocks = L10Accessor.GetRocksForRecurrence(s, perm, recurrenceId);
				var arecur = new AngularRecurrence(recurrenceId) {
					Rocks = AngularList.Create(AngularListType.ReplaceAll, recurRocks.Select(x => new AngularRock(x.ForRock)).ToList()),
				};
				if (recurRocks.Any() && recurRocks.Last().ForRock != null) {
					arecur.Focus = "[data-rock='" + recurRocks.Last().ForRock.Id + "'] input:visible:first";
				}

				group.update(arecur);
			}
			Audit.L10Log(s, perm.GetCaller(), recurrenceId, "CreateRock", ForModel.Create(rm), rock.Rock);
			if (recur.VtoId != 0 && !rock._AddedToVTO) {
				VtoAccessor.AddRock(s, perm, recur.VtoId, rock);
			}
		}
		public static void AddRock(ISession s, PermissionsUtility perm, long recurrenceId, L10Controller.AddRockVm model) {
			var recur = s.Get<L10Recurrence>(recurrenceId);
			recur.Pristine = false;
			s.Update(recur);
			var now = DateTime.UtcNow;
			RockModel rock;

			if (model.SelectedRock == -3) {
				//Create new
				if (model.Rocks == null)
					throw new PermissionsException("You must include a rock to create.");

				rock = model.Rocks.SingleOrDefault();
				if (rock == null)
					throw new PermissionsException("You must include a rock to create.");

				perm.ViewUserOrganization(rock.ForUserId, false);

				rock.OrganizationId = recur.OrganizationId;
				if (rock.CreateTime == DateTime.MinValue)
					rock.CreateTime = now;
				rock.AccountableUser = s.Load<UserOrganizationModel>(rock.ForUserId);
				rock.Category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

				s.Save(rock);
				rock.AccountableUser.UpdateCache(s);
			} else {
				//Find Existing
				rock = s.Get<RockModel>(model.SelectedRock);
				if (rock == null)
					throw new PermissionsException("Rock does not exist.");
				perm.ViewRock(rock.Id);
			}
			AddRock(s, perm, recurrenceId, rock, now);
		}

		public static void UpdateRockCompletion(UserOrganizationModel caller, long recurrenceId, long meetingRockId, RockState state, string connectionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var rock = s.Get<L10Meeting.L10Meeting_Rock>(meetingRockId);
					if (rock == null)
						throw new PermissionsException("Rock does not exist.");
					var now = DateTime.UtcNow;
					var updated = false;
					perm.EditRock(rock.ForRock.Id);

					if (state != RockState.Indeterminate && rock.Completion != state) {
						if (state == RockState.Complete) {
							rock.CompleteTime = now;
							rock.ForRock.CompleteTime = now;
						}
						rock.Completion = state;
						rock.ForRock.Completion = state;
						s.Update(rock);
						s.Update(rock.ForRock);
						updated = true;
					} else if ((state == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate) {
						rock.Completion = RockState.Indeterminate;
						rock.CompleteTime = null;
						rock.ForRock.Completion = RockState.Indeterminate;
						rock.ForRock.CompleteTime = null;
						s.Update(rock);
						s.Update(rock.ForRock);
						updated = true;
					}

					if (updated) {
						Audit.L10Log(s, caller, recurrenceId, "UpdateRockCompletion", ForModel.Create(rock), "\"" + rock.ForRock.Rock + "\" set to \"" + state + "\"");
						tx.Commit();
						s.Flush();
						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId).updateRockCompletion(meetingRockId, state.ToString());

						UpdateRock(rock.ForRock.Id, state, connectionId, s, perm, rock.ForRock, hub, now);
					}
				}
			}
		}
		public static void UpdateRock(UserOrganizationModel caller, long id, String rockMessage, RockState? state, long? ownerId, string connectionId, bool? delete = null, bool? companyRock = null,DateTime? dueDate=null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create(connectionId)) {
						var perms = PermissionsUtility.Create(s, caller);
						var rock = s.Get<RockModel>(id);
						perms.EditRock(rock.Id);
						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();

						List<Tuple<long, long>> rockRecurrenceIds = null;
						var rockRecurs = new Func<List<Tuple<long, long>>>(() => {
							rockRecurrenceIds = rockRecurrenceIds ?? s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
							.Where(x => x.DeleteTime == null && x.ForRock.Id == id)
							.Select(x => x.L10Recurrence.Id, x => x.Id)
							.List<object[]>().Select(x => Tuple.Create((long)x[0], (long)x[1])).ToList();
							return rockRecurrenceIds;
						});
						List<Tuple<long, long>> rockVtoIds = null;
						var rockVtos = new Func<List<Tuple<long, long>>>(() => {
							rockVtoIds = rockVtoIds ?? s.QueryOver<Vto_Rocks>()
							.Where(x => x.DeleteTime == null && x.Rock.Id == id)
							.Select(x => x.Vto.Id, x => x.Id)
							.List<object[]>().Select(x => Tuple.Create((long)x[0], (long)x[1])).ToList();
							return rockVtoIds;
						});

						var now = DateTime.UtcNow;
						var updated = false;
						if (rockMessage != null && rockMessage != rock.Rock) {
							rock.Rock = rockMessage;
							s.Update(rock);
							updated = true;

							foreach (var r in rockRecurs()) {
								hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.Item1), connectionId).updateRockName(r.Item2, rockMessage);
							}
						}

						if (companyRock != null && companyRock != rock.CompanyRock) {
							rock.CompanyRock = companyRock.Value;
							s.Update(rock);
							updated = true;

						}

						if (dueDate != null && dueDate != rock.DueDate) {
							rock.DueDate = dueDate.Value;
							s.Update(rock);
							updated = true;
						}

						if (ownerId != null && ownerId != rock.ForUserId) {
							rock.ForUserId = ownerId.Value;
							perms.ViewUserOrganization(ownerId.Value, false);
							rock.AccountableUser = s.Get<UserOrganizationModel>(ownerId.Value);
							s.Update(rock);
							updated = true;
							//todo: update l10 rock page with new owner.

							//var rockRecurrences = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
							//.Where(x => x.DeleteTime == null && x.ForRock.Id == id)
							//.List().ToList();

							//foreach (var r in rockRecurrences) {
							//    hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId)
							//        .updateRockName(r.Id, rockMessage);
							//}
						}
						if (state != null && rock.Completion != state.Value) {
							SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateRockCompletion(id));
							if (state != RockState.Indeterminate && rock.Completion != state) {
								if (state == RockState.Complete) {
									rock.CompleteTime = now;
								}
							} else if ((state == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate) {
								rock.Completion = RockState.Indeterminate;
								rock.CompleteTime = null;
							}
							rock.Completion = state.Value;
							s.Update(rock);
							UpdateRock(id, state, connectionId, s, perms, rock, hub, now);
							updated = true;
						}
						updated = _ProcessDeleted(s, rock, delete) || updated;

						if (updated) {
							rt.UpdateRecurrences(rockRecurs().Select(x => x.Item1)).Update(rid => new AngularRock(rock));
							rt.UpdateVtos(rockVtos().Select(x => x.Item1)).Update(rid => new AngularVtoRock() {
								Rock = new AngularRock(rock)
							});
							tx.Commit();
							s.Flush();
						}
					}
				}
			}
		}
		private static void UpdateRock(long id, RockState? state, string connectionId, ISession s, PermissionsUtility perms, RockModel rock, IHubContext hub, DateTime now) {
			var rockRecurrences = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
								  .Where(x => x.DeleteTime == null && x.ForRock.Id == id)
								  .List().ToList();
			foreach (var r in rockRecurrences) {

				var curMeeting = _GetCurrentL10Meeting(s, perms, r.L10Recurrence.Id, true, false, false);
				if (curMeeting != null) {
					var meetingRock = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == curMeeting.Id && x.ForRock.Id == rock.Id).SingleOrDefault();
					if (meetingRock != null) {

						if (state != RockState.Indeterminate && meetingRock.Completion != state) {
							meetingRock.Completion = state.Value;
							if (state == RockState.Complete) {
								meetingRock.CompleteTime = now;
							}
							s.Update(meetingRock);
						} else if ((state == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate) {
							meetingRock.Completion = RockState.Indeterminate;
							meetingRock.CompleteTime = null;
							s.Update(meetingRock);
						}
						hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId)
							.updateRockCompletion(meetingRock.Id, state.ToString(), rock.Id);
					}
				} else {
					hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId)
						.updateRockCompletion(0, state.ToString(), rock.Id);
				}

				hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id), connectionId)
					.update(new AngularUpdate() { new AngularRock(rock) });

			}
		}

		public static void RemoveRock(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long rockId) {
			perm.AdminL10Recurrence(recurrenceId);
			var rocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.ForRock.Id == rockId).List().ToList();

			if (!rocks.Any())
				throw new PermissionsException("Rock does not exist.");
			var now = DateTime.UtcNow;
			foreach (var r in rocks) {
				r.DeleteTime = now;
				s.Update(r);
				rt.UpdateRecurrences(recurrenceId).Update(
					new AngularRecurrence(recurrenceId) {
						Rocks = AngularList.CreateFrom(AngularListType.Remove, new AngularRock(r.ForRock))
					}
				);

				if (r.L10Recurrence.VtoId > 0) {
					var vtoId = r.L10Recurrence.VtoId;
					var rocksInVTO = s.QueryOver<Vto_Rocks>().Where(x => x.DeleteTime == null && x.Rock.Id == rockId && x.Vto.Id == vtoId).List().ToList();
					foreach (var rv in rocksInVTO) {
						rv.DeleteTime = now;
						s.Update(rv);
					}
				}
			}

			var rocksInOthers = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.DeleteTime == null && x.ForRock.Id == rockId).RowCount();
			if (rocksInOthers == 0) {
				var rock = s.Get<RockModel>(rockId);

				if (rock.FromTemplateItemId == null) {
					rock.Archived = true;
					rock.DeleteTime = now;
					s.Update(rock);
#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
					if (rock.ForUserId != null) {
						s.Flush();
						s.GetFresh<UserOrganizationModel>(rock.ForUserId).UpdateCache(s);
					}
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
				}
			}
		}
		#endregion

		#region Scorecard		
		public static List<ScoreModel> GetScoresForRecurrence(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);//.ViewL10Recurrence(recurrenceId);
					return GetScoresForRecurrence(s, perm, recurrenceId);
				}
			}
		}
		public class ScoresAndMeasurables {
			public List<ScoreModel> Scores { get; set; }
			public List<MeasurableModel> Measurables { get; set; }
			public List<L10Recurrence.L10Recurrence_Measurable> MeasurablesAndDividers { get; set; }
		}

		public static ScoresAndMeasurables GetScoresAndMeasurablesForRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeAutoGenerated = true, DateTime? now = null, DateRange range = null, bool getMeasurables = false, bool getScores = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					return GetScoresAndMeasurablesForRecurrence(s, perm, recurrenceId, includeAutoGenerated: includeAutoGenerated, now: now, range: range, getMeasurables: getMeasurables, getScores: getScores);
				}
			}
		}

		public static ScoresAndMeasurables GetScoresAndMeasurablesForRecurrence(ISession s, PermissionsUtility perm, long recurrenceId, bool includeAutoGenerated = true,
			DateTime? now = null, DateRange range = null, bool getMeasurables = false, bool getScores = true) {
			var now1 = now ?? DateTime.UtcNow;
			perm.ViewL10Recurrence(recurrenceId);

			MeasurableModel mAlias = null;
			var r = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
						.JoinAlias(x => x.Measurable, () => mAlias, JoinType.LeftOuterJoin)
						.Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null && (x.Measurable == null || mAlias.DeleteTime == null))
						.List().ToList();

			var measurables = r.Where(x => x.Measurable != null).Distinct(x => x.Measurable.Id).Select(x => x.Measurable.Id).ToList();

			var scores = new List<ScoreModel>();
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
			List<MeasurableModel> measurableModels = null;
			if (getMeasurables) {
				measurableModels = s.QueryOver<MeasurableModel>().WhereRestrictionOn(x => x.Id).IsIn(measurables).Future().ToList();
			}
			if (getScores) {
				scores = scoresF.ToList();
			}

			var recur = s.Get<L10Recurrence>(recurrenceId);
			var currentTime = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false).NotNull(x => x.StartTime);
			if (includeAutoGenerated && (recur.IncludeAggregateTodoCompletion || recur.IncludeIndividualTodos)) {
				List<TodoModel> todoCompletion = null;
				todoCompletion = GetAllTodosForRecurrence(s, perm, recurrenceId);

				var ts = perm.GetCaller().GetTimeSettings();
				ts.Descending = recur.ReverseScorecard;

				var periods = TimingUtility.GetPeriods(ts, now1, currentTime, true);

				if (getScores && recur.IncludeAggregateTodoCompletion) {
					var todoScores = periods.Select(x => x.ForWeek).SelectMany(w => {
						try {
							var rangeTodos = TimingUtility.GetRange(perm.GetCaller().Organization, w.AddDays(-7));
							var ss = TodoCompletion(todoCompletion, rangeTodos.StartTime, rangeTodos.EndTime, currentTime);
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
					scores.AddRange(todoScores);
				}

				if (getScores && recur.IncludeIndividualTodos) {
					var individualTodoScores = periods.Select(x => x.ForWeek).SelectMany(ww => {
						return todoCompletion.GroupBy(x => x.AccountableUserId).SelectMany(todos => {
							var a = todos.First().AccountableUser;
							try {
								var rangeTodos = TimingUtility.GetRange(perm.GetCaller().Organization, ww.AddDays(-7));
								var ss = TodoCompletion(todos.ToList(), rangeTodos.StartTime, rangeTodos.EndTime, currentTime);
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
					scores.AddRange(individualTodoScores);
				}
			}

			var userQueries = scores.SelectMany(x => {
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
				//var cumulativeDict = new Dictionary<long, IEnumerable<object[]>>();
				//var startOfWeek = recur.StartOfWeekOverride ?? recur.Organization.Settings.WeekStart;
				////Grab Cumulative Values
				//foreach (var mm in measurableModels.Where(x => x.ShowCumulative && x.Id > 0).Distinct(x => x.Id)) {
				//	cumulativeDict[mm.Id] = s.QueryOver<ScoreModel>()
				//	.Where(x => x.MeasurableId == mm.Id && x.DeleteTime == null && x.Measured != null && x.ForWeek > mm.CumulativeRange.Value.AddDays(-(int)startOfWeek))
				//	.Select(x => x.ForWeek, x => x.Measured)
				//	.Future<object[]>();
				//}
				////Set Cumulative Values
				//foreach (var k in cumulativeDict.Keys) {
				//	measurableModels.Where(x => x.Id == k).ToList().ForEach(mm => {
				//		mm._Cumulative = cumulativeDict[k].GroupBy(x => (DateTime)x[0])
				//							.Select(x => (decimal?)x.FirstOrDefault(y => y[1] != null)[1])
				//							.Where(x => x != null)
				//							.Sum();
				//	});
				//}
			}
			//Touch 
			if (getScores) {
				foreach (var a in scores) {
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
			return new ScoresAndMeasurables() {
				Scores = scores,
				Measurables = measurableModels,
				MeasurablesAndDividers = r
			};
		}

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

		public static List<ScoreModel> GetScoresForRecurrence(ISession s, PermissionsUtility perm, long recurrenceId, bool includeAutoGenerated = true, DateTime? now = null, DateRange range = null) {
			var sam = GetScoresAndMeasurablesForRecurrence(s, perm, recurrenceId, includeAutoGenerated, now, range);
			return sam.Scores;
		}



		public static void UpdateScore(UserOrganizationModel caller, long scoreId, decimal? measured, string connectionId = null, bool noSyncException = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var score = s.Get<ScoreModel>(scoreId);
					if (score == null)
						throw new PermissionsException("Score does not exist.");

					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateScore(scoreId), noSyncException);

					var now = DateTime.UtcNow;
					PermissionsUtility.Create(s, caller).EditScore(scoreId);
					var all = s.QueryOver<ScoreModel>().Where(x => x.MeasurableId == score.MeasurableId && x.ForWeek == score.ForWeek).List().ToList();
					foreach (var sc in all) {
						sc.Measured = measured;
						sc.DateEntered = (measured == null) ? null : (DateTime?)now;
						s.Update(sc);
					}



					//L10Meeting meetingAlias = null;
					var possibleRecurrences = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
						.Where(x => x.DeleteTime == null && x.Measurable.Id == score.MeasurableId)
						.Select(x => x.L10Recurrence.Id)
						.List<long>().ToList();

					using (var rt = RealTimeUtility.Create()) { //Do not skip any users
						_RecalculateCumulative_Unsafe(s, rt, score.Measurable, possibleRecurrences, score);
					}

					foreach (var r in possibleRecurrences) {
						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r), connectionId);

						var n = score.Measurable.NotNull(x => x.AccountableUser.GetName());
						var n1 = score.Measurable.NotNull(x => x.AdminUser.GetName());
						var toUpdate = new AngularScore(score, false);


						toUpdate.DateEntered = score.Measured == null ? Removed.Date() : DateTime.UtcNow;
						toUpdate.Measured = toUpdate.Measured ?? Removed.Decimal();

						group.update(new AngularUpdate() { toUpdate });
						Audit.L10Log(s, caller, r, "UpdateScore", ForModel.Create(score), "\"" + score.Measurable.Title + "\" updated to \"" + measured + "\"");
					}

					tx.Commit();
					s.Flush();
				}
			}
		}
		public static ScoreModel _UpdateScore(ISession s, PermissionsUtility perms, RealTimeUtility rt, long measurableId, long weekNumber, decimal? measured, string connectionId, bool noSyncException = false, bool skipRealTime = false) {
			var now = DateTime.UtcNow;
			DateTime? nowQ = now;
			perms.EditMeasurable(measurableId);
			var m = s.Get<MeasurableModel>(measurableId);

			if (!skipRealTime) {

			}

			//adjust week..
			var week = TimingUtility.GetDateSinceEpoch(weekNumber).StartOfWeek(DayOfWeek.Sunday).Date;

			//See if we can find it given week.
			var score = s.QueryOver<ScoreModel>()
				.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId && x.ForWeek == week)
				//.OrderBy(x=>x.DateEntered).Desc.ThenBy(x=>x.Id).Desc
				.List().LastOrDefault();

			// var score = existingScores.SingleOrDefault(x => (x.ForWeek == week));

			if (score != null) {
				SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateScore(score.Id), noSyncException);
				//Found it with false id
				score.Measured = measured;
				score.DateEntered = (measured == null) ? null : nowQ;
				s.Update(score);

				//_RecalculateCumulative_Unsafe(s, score.Measurable, score);
			} else {
				var existingScores = s.QueryOver<ScoreModel>()
				.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
				.List().ToList();
				var ordered = existingScores.OrderBy(x => x.DateDue);
				var minDate = ordered.FirstOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now.AddDays(-7 * 13);
				var maxDate = ordered.LastOrDefault().NotNull(x => (DateTime?)x.ForWeek) ?? now;

				minDate = minDate.StartOfWeek(DayOfWeek.Sunday);
				maxDate = maxDate.StartOfWeek(DayOfWeek.Sunday);


				//DateTime start, end;

				if (week > maxDate) {
					//Create going up until sufficient
					var n = maxDate;
					ScoreModel curr = null;
					var measurable = s.Get<MeasurableModel>(m.Id);
					while (n < week) {
						var nextDue = n.StartOfWeek(DayOfWeek.Sunday).Date.AddDays(7).AddDays((int)m.DueDate).Add(m.DueTime);
						curr = new ScoreModel() {
							AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId),
							AccountableUserId = m.AccountableUserId,
							DateDue = nextDue,
							MeasurableId = m.Id,
							Measurable = measurable,
							OrganizationId = m.OrganizationId,
							ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday).Date,
							OriginalGoal = measurable.Goal,
							OriginalGoalDirection = measurable.GoalDirection
						};
						s.Save(curr);
						m.NextGeneration = nextDue;
						n = nextDue.StartOfWeek(DayOfWeek.Sunday).Date;
					}
					curr.DateEntered = (measured == null) ? null : nowQ;
					curr.Measured = measured;
					score = curr;
					//_RecalculateCumulative_Unsafe(s, m, curr);
				} else if (week < minDate) {
					var n = week;
					var first = true;
					var measurable = s.Get<MeasurableModel>(m.Id);
					while (n < minDate) {
						var nextDue = n.StartOfWeek(DayOfWeek.Sunday).Date.AddDays((int)m.DueDate).Add(m.DueTime);
						var curr = new ScoreModel() {
							AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId),
							AccountableUserId = m.AccountableUserId,
							DateDue = nextDue,
							MeasurableId = m.Id,
							Measurable = measurable,
							OrganizationId = m.OrganizationId,
							ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday).Date,
							OriginalGoal = measurable.Goal,
							OriginalGoalDirection = measurable.GoalDirection
						};
						if (first) {
							curr.Measured = measured;
							curr.DateEntered = (measured == null) ? null : nowQ;
							first = false;
							s.Save(curr);
							score = curr;
							//_RecalculateCumulative_Unsafe(s, m, curr);
						}

						//m.NextGeneration = nextDue;
						n = nextDue.AddDays(7).StartOfWeek(DayOfWeek.Sunday);
					}
				} else {
					// cant create scores between these dates..
					var measurable = s.Get<MeasurableModel>(m.Id);
					var curr = new ScoreModel() {
						AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId),
						AccountableUserId = m.AccountableUserId,
						DateDue = week.StartOfWeek(DayOfWeek.Sunday).Date.AddDays((int)m.DueDate).Add(m.DueTime),
						MeasurableId = m.Id,
						Measurable = measurable,
						OrganizationId = m.OrganizationId,
						ForWeek = week.StartOfWeek(DayOfWeek.Sunday).Date,
						Measured = measured,
						DateEntered = (measured == null) ? null : nowQ,
						OriginalGoal = measurable.Goal,
						OriginalGoalDirection = measurable.GoalDirection

					};
					s.Save(curr);
					score = curr;
					//_RecalculateCumulative_Unsafe(s, m, curr);
				}
				s.Update(m);
			}
			if (!skipRealTime) {


				var measurableRecurs = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
					.Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId)
					.Select(x => x.L10Recurrence.Id)
					.List<long>().ToList();

				_RecalculateCumulative_Unsafe(s, rt, score.Measurable, measurableRecurs, score);

				rt.UpdateRecurrences(measurableRecurs).UpdateScorecard(score.AsList());
				foreach (var recurrenceId in measurableRecurs) {
					//var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					//var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId);
					//var update = new AngularRecurrence(recurrenceId);
					//update.Scorecard = new AngularScorecard();
					////score.Measured = score.Measured ?? Removed.Decimal();
					//var angularScore = new AngularScore(score);
					//angularScore.Measured = angularScore.Measured ?? Removed.Decimal();
					//angularScore.ForWeek = TimingUtility.GetWeekSinceEpoch(angularScore.Week);
					//update.Scorecard.Scores = new List<AngularScore>() { angularScore };
					//group.update(update);

					Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateScore", ForModel.Create(score), "\"" + score.NotNull(x => x.Measurable.NotNull(y => y.Title)) + "\" updated to \"" + measured + "\"");
				}
			}
			return score;
		}
		public static void UpdateScore(UserOrganizationModel caller, long measurableId, long weekNumber, decimal? measured, string connectionId, bool noSyncException = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create(connectionId)) {
						var perms = PermissionsUtility.Create(s, caller);
						_UpdateScore(s, perms, rt, measurableId, weekNumber, measured, connectionId, noSyncException);

						tx.Commit();
						s.Flush();
					}
				}
			}
		}
		public static void UpdateArchiveMeasurable(UserOrganizationModel caller, long measurableId, string name = null,
			LessGreater? direction = null, decimal? target = null, long? accountableId = null, long? adminId = null,
			string connectionId = null, bool updateFutureOnly = true, decimal? altTarget = null, bool? showCumulative = null,
			DateTime? cumulativeRange = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create(connectionId)) {
						var measurable = s.Get<MeasurableModel>(measurableId);
						//var recurrence = s.Get<L10Recurrence>(recurrenceId);
						var scoresToUpdate = new List<ScoreModel>();

						if (measurable == null)
							throw new PermissionsException("Measurable does not exist.");


						var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
							.Where(x => x.Measurable.Id == measurableId && x.DeleteTime == null).Select(x => x.L10Recurrence.Id).List<long>().ToList();

						var rtRecur = rt.UpdateRecurrences(recurrenceIds);
						var checkEither = new List<Func<PermissionsUtility, PermissionsUtility>>{
							x => x.EditMeasurable(measurableId)
						};

						checkEither.AddRange(recurrenceIds.Select<long, Func<PermissionsUtility, PermissionsUtility>>(recurrenceId => (x => x.EditL10Recurrence(recurrenceId))));
						var perms = PermissionsUtility.Create(s, caller).Or(checkEither.ToArray());

						var updateText = new List<String>();

						var meetingMeasurableIds = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
							.Where(x => x.DeleteTime == null && x.Measurable.Id == measurable.Id)
							.Select(x => x.Id)
							.List<long>().ToList();

						if (name != null && measurable.Title != name) {
							measurable.Title = name;
							//group.updateArchiveMeasurable(measurableId, "title", name);
							updateText.Add("Title: " + measurable.Title);
							foreach (var mmid in meetingMeasurableIds)
								rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "title", name));
						}
						var updateCumulative = false;
						if (showCumulative != null && measurable.ShowCumulative != showCumulative) {
							measurable.ShowCumulative = showCumulative.Value;
							updateText.Add("Cumulative: " + showCumulative);
							foreach (var mmid in meetingMeasurableIds)
								rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "showCumulative", showCumulative));
							updateCumulative = true;
						}
						if (cumulativeRange != null && measurable.CumulativeRange != cumulativeRange) {
							measurable.CumulativeRange = cumulativeRange.Value;
							updateText.Add("Cumulative Start: " + cumulativeRange);
							foreach (var mmid in meetingMeasurableIds) {
								rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "cumulativeRange", cumulativeRange));
							}
							updateCumulative = true;
						}

						if (updateCumulative) {
							//Recalculate cumulative
							_RecalculateCumulative_Unsafe(s, rt, measurable, recurrenceIds);
						}

						if ((direction != null && measurable.GoalDirection != direction.Value) || !updateFutureOnly) {
							measurable.GoalDirection = direction.Value;
							updateText.Add("Goal Direction: " + measurable.GoalDirection.ToSymbol());

							var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
							if (updateFutureOnly) {
								var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
								scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
							}

							var scores = scoresQ.List().ToList();
							foreach (var score in scores) {
								score.OriginalGoalDirection = direction.Value;
								s.Update(score);
							}
							scoresToUpdate = scores;

							foreach (var mmid in meetingMeasurableIds)
								rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "direction", direction.Value.ToSymbol(), direction.Value.ToString()));
							//group.updateArchiveMeasurable(measurableId, "direction", direction.Value.ToSymbol(), direction.Value.ToString());

						}
						if ((target != null && measurable.Goal != target.Value) || !updateFutureOnly) {
							if (target != null) {
								measurable.Goal = target.Value;
								updateText.Add("Goal: " + measurable.Goal);
							}


							var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
							if (updateFutureOnly) {
								var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
								scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
							}
							var scores = scoresQ.List().ToList();
							foreach (var score in scores) {
								score.OriginalGoal = measurable.Goal;
								s.Update(score);
							}
							scoresToUpdate = scores;

							foreach (var mmid in meetingMeasurableIds)
								rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "target", measurable.Goal.ToString("0.#####")));
							//group.updateArchiveMeasurable(measurableId, "target", target.Value.ToString("0.#####"));
						}




						if ((altTarget != null && measurable.AlternateGoal != altTarget.Value) || !updateFutureOnly) {

							if (altTarget != null) {
								measurable.AlternateGoal = altTarget.Value;
								updateText.Add("AltGoal: " + measurable.AlternateGoal);
							}


							var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
							if (updateFutureOnly) {
								var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
								scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
							}
							var scores = scoresQ.List().ToList();
							foreach (var score in scores) {
								score.AlternateOriginalGoal = measurable.AlternateGoal;
								s.Update(score);
							}
							scoresToUpdate = scores;

							foreach (var mmid in meetingMeasurableIds)
								rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "altTarget", measurable.AlternateGoal.NotNull(x => x.Value.ToString("0.#####")) ?? ""));
							//group.updateArchiveMeasurable(measurableId, "target", target.Value.ToString("0.#####"));
						}

						if (accountableId != null && measurable.AccountableUserId != accountableId.Value) {
							perms.ViewUserOrganization(accountableId.Value, false);
							var user = s.Get<UserOrganizationModel>(accountableId.Value);
							if (user != null)
								user.UpdateCache(s);

							measurable.AccountableUserId = accountableId.Value;
							measurable.AccountableUser = user;
							updateText.Add("Accountable: " + user.GetName());

							foreach (var mmid in meetingMeasurableIds)
								rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "accountable", user.NotNull(x => x.GetName()), accountableId.Value));
							//group.updateArchiveMeasurable(measurableId, "accountable", user.NotNull(x => x.GetName()), accountableId.Value);
						}
						if (adminId != null) {
							perms.ViewUserOrganization(adminId.Value, false);
							var user = s.Get<UserOrganizationModel>(adminId.Value);
							if (user != null)
								user.UpdateCache(s);
							measurable.AdminUserId = adminId.Value;
							measurable.AdminUser = user;
							updateText.Add("Admin: " + user.GetName());

							foreach (var mmid in meetingMeasurableIds)
								rtRecur.AddLowLevelAction(g => g.updateMeasurable(mmid, "admin", user.NotNull(x => x.GetName()), adminId.Value));
							//group.updateArchiveMeasurable(measurableId, "admin", user.NotNull(x => x.GetName()), adminId.Value);
						}

						//var scorecard = new AngularScorecard();
						//scorecard.Measurables = new List<AngularMeasurable>() { };
						//var scoreList = new List<AngularScore>(); 

						//foreach (var ss in scores.Where(x => x.Measurable.Id == measurable.Id)) {
						//    scoreList.Add(new AngularScore(ss));
						//}
						//scorecard.Scores = AngularList.Create<AngularScore>(AngularListType.ReplaceAll, scoreList);
						//group.update(new AngularUpdate() { scorecard, new AngularMeasurable(measurable) });

						//_ProcessDeleted(s, measurable, delete);

						rtRecur.UpdateMeasurable(measurable, scoresToUpdate);

						var updatedText = "Updated Measurable: \"" + measurable.Title + "\" \n " + String.Join("\n", updateText);
						foreach (var recurrenceId in recurrenceIds) {
							Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateArchiveMeasurable", ForModel.Create(measurable), updatedText);
						}
						tx.Commit();
						s.Flush();
					}
				}
			}
		}
		public static void UpdateMeasurable(UserOrganizationModel caller, long meeting_measurableId,
			string name = null, LessGreater? direction = null, decimal? target = null,
			long? accountableId = null, long? adminId = null, UnitType? unitType = null,
			bool updateFutureOnly = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var measurable = s.Get<L10Meeting.L10Meeting_Measurable>(meeting_measurableId);
					if (measurable == null)
						throw new PermissionsException("Measurable does not exist.");

					var recurrenceId = measurable.L10Meeting.L10RecurrenceId;
					if (recurrenceId == 0)
						throw new PermissionsException("Meeting does not exist.");
					var perms = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

					var updateText = new List<String>();
					if (name != null && measurable.Measurable.Title != name) {
						measurable.Measurable.Title = name;
						group.updateMeasurable(meeting_measurableId, "title", name);
						updateText.Add("Title: " + measurable.Measurable.Title);
					}
					if (unitType != null && measurable.Measurable.UnitType != unitType.Value) {
						measurable.Measurable.UnitType = unitType.Value;
						group.updateMeasurable(meeting_measurableId, "unittype", unitType.Value.ToTypeString(), unitType.Value.ToString());
						updateText.Add("Unit Type: " + measurable.Measurable.UnitType);
					}
					//if (direction != null && measurable.Measurable.GoalDirection != direction.Value) {
					//    measurable.Measurable.GoalDirection = direction.Value;
					//    group.updateMeasurable(meeting_measurableId, "direction", direction.Value.ToSymbol(), direction.Value.ToString());
					//    updateText.Add("Goal Direction: " + measurable.Measurable.GoalDirection.ToSymbol());
					//}
					//if (target != null && measurable.Measurable.Goal != target.Value) {
					//    measurable.Measurable.Goal = target.Value;
					//    group.updateMeasurable(meeting_measurableId, "target", target.Value.ToString("0.#####"));
					//    updateText.Add("Goal: " + measurable.Measurable.Goal);
					//}
					var scoresToUpdate = new List<ScoreModel>();
					var meetingMeasurableIds = s.QueryOver<L10Meeting.L10Meeting_Measurable>()
						.Where(x => x.DeleteTime == null && x.Measurable.Id == measurable.Measurable.Id)
						.Select(x => x.Id)
						.List<long>().ToList();

					var l10MeetingStart = measurable.L10Meeting.StartTime ?? DateTime.UtcNow;

					if (direction != null && measurable.Measurable.GoalDirection != direction.Value) {
						measurable.Measurable.GoalDirection = direction.Value;
						updateText.Add("Goal Direction: " + measurable.Measurable.GoalDirection.ToSymbol());

						var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Measurable.Id);
						if (updateFutureOnly) {
							var nowSunday = l10MeetingStart.AddDays(-7).StartOfWeek(DayOfWeek.Sunday);
							scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
						}

						var scores = scoresQ.List().ToList();
						foreach (var score in scores) {
							score.OriginalGoalDirection = direction.Value;
							s.Update(score);
						}
						scoresToUpdate = scores;

						foreach (var mmid in meetingMeasurableIds)
							group.updateMeasurable(mmid, "direction", direction.Value.ToSymbol(), direction.Value.ToString());
						//group.updateArchiveMeasurable(measurableId, "direction", direction.Value.ToSymbol(), direction.Value.ToString());

					}
					if (target != null && measurable.Measurable.Goal != target.Value) {
						measurable.Measurable.Goal = target.Value;
						updateText.Add("Goal: " + measurable.Measurable.Goal);


						var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Measurable.Id);
						if (updateFutureOnly) {
							var nowSunday = l10MeetingStart.AddDays(-7).StartOfWeek(DayOfWeek.Sunday);
							scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
						}
						var scores = scoresQ.List().ToList();
						foreach (var score in scores) {
							score.OriginalGoal = target.Value;
							s.Update(score);
						}
						scoresToUpdate = scores;

						foreach (var mmid in meetingMeasurableIds)
							group.updateMeasurable(mmid, "target", target.Value.ToString("0.#####"));
						//group.updateArchiveMeasurable(measurableId, "target", target.Value.ToString("0.#####"));
					}

					if (accountableId != null && measurable.Measurable.AccountableUserId != accountableId.Value) {
						perms.ViewUserOrganization(accountableId.Value, false);
						var user = s.Get<UserOrganizationModel>(accountableId.Value);
						var oldUser = s.Get<UserOrganizationModel>(measurable.Measurable.AccountableUserId);
						if (user == null)
							throw new PermissionsException("Cannot Update User");
						user.UpdateCache(s);
						if (oldUser != null)
							oldUser.UpdateCache(s);

						measurable.Measurable.AccountableUserId = accountableId.Value;
						group.updateMeasurable(meeting_measurableId, "accountable", user.NotNull(x => x.GetName()), accountableId.Value);
						updateText.Add("Accountable: " + user.NotNull(x => x.GetName()));
						s.Update(measurable.Measurable);
					}
					if (adminId != null && measurable.Measurable.AdminUserId != adminId.Value) {
						perms.ViewUserOrganization(adminId.Value, false);
						var user = s.Get<UserOrganizationModel>(adminId.Value);
						var oldUser = s.Get<UserOrganizationModel>(measurable.Measurable.AdminUserId);
						if (user == null)
							throw new PermissionsException("Cannot Update User");
						user.UpdateCache(s);
						if (oldUser != null)
							oldUser.UpdateCache(s);
						measurable.Measurable.AdminUserId = adminId.Value;
						group.updateMeasurable(meeting_measurableId, "admin", user.NotNull(x => x.GetName()), adminId.Value);
						updateText.Add("Admin: " + user.NotNull(x => x.GetName()));
						s.Update(measurable.Measurable);
					}

					var updatedText = "Updated Measurable: \"" + measurable.Measurable.Title + "\" \n " + String.Join("\n", updateText);
					Audit.L10Log(s, perms.GetCaller(), recurrenceId, "UpdateMeasurable", ForModel.Create(measurable), updatedText);

					tx.Commit();
					s.Flush();
				}
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
					group.removeMeasurable(l10Meeting_measurableId);
				}
			}
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

						//var serial=new{
						//	id=measurable.Id,
						//	accountableId=measurable.AccountableUserId,
						//	accountableName=measurable.AccountableUser.GetName(),
						//	adminName = measurable.AdminUser.GetName(),
						//	title=measurable.Title,
						//	direction = measurable.GoalDirection.ToString(),
						//	directionName = measurable.GoalDirection.ToSymbol(),
						//	target = measurable.Goal,
						//	measurable.AdminUserId,
						//	scores=weekData
						//};

						var settings = current.Organization.Settings;
						var sow = settings.WeekStart;
						var offset = current.Organization.GetTimezoneOffset();
						var scorecardType = settings.ScorecardPeriod;

						var ts = current.Organization.GetTimeSettings();
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
		public static void AddMeasurable(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, L10Controller.AddMeasurableVm model, bool skipRealTime = false, int? rowNum = null) {
			perm.EditL10Recurrence(recurrenceId);

			var recur = s.Get<L10Recurrence>(recurrenceId);

			recur.Pristine = false;
			s.Update(recur);


			//var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			//var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

			var now = DateTime.UtcNow;
			MeasurableModel measurable;

			var scores = new List<ScoreModel>();
			var wasCreated = false;
			if (model.SelectedMeasurable == -3) {
				////Create new
				//if (model.Measurables == null)
				//    throw new PermissionsException("You must include a measurable to create.");

				//measurable = model.Measurables.SingleOrDefault();
				//if (measurable == null)
				//    throw new PermissionsException("You must include a measurable to create.");

				//perm.ViewUserOrganization(measurable.AccountableUserId, false);
				//perm.ViewUserOrganization(measurable.AdminUserId, false);

				//measurable.OrganizationId = recur.OrganizationId;
				//measurable.CreateTime = now;

				//measurable.AccountableUser = s.Load<UserOrganizationModel>(measurable.AccountableUserId);
				//measurable.AdminUser = s.Load<UserOrganizationModel>(measurable.AdminUserId);

				//s.Save(measurable);

				//measurable.AccountableUser.UpdateCache(s);
				//measurable.AdminUser.UpdateCache(s);
				measurable = model.Measurables.SingleOrDefault();
				measurable.OrganizationId = recur.OrganizationId;
				measurable.CreateTime = now;
				ScorecardAccessor.CreateMeasurable(s, perm, measurable, false);

				wasCreated = true;
			} else {
				//Find Existing
				measurable = s.Get<MeasurableModel>(model.SelectedMeasurable);
				if (measurable == null)
					throw new PermissionsException("Measurable does not exist.");
				perm.ViewMeasurable(measurable.Id);

				scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id).List().ToList();
				//weekData = scores.Select(x => new{week = x.ForWeek.ToJavascriptMilliseconds(), value = x.Measured}).ToList();

			}

			var rm = new L10Recurrence.L10Recurrence_Measurable() {
				CreateTime = now,
				L10Recurrence = recur,
				Measurable = measurable,
				_Ordering = rowNum ?? 0
			};
			s.Save(rm);

			if (wasCreated) {
				var week = TimingUtility.GetWeekSinceEpoch(DateTime.UtcNow);
				for (var i = 0; i < 4; i++) {
					scores.Add(_UpdateScore(s, perm, rt, measurable.Id, week - i, null, null, skipRealTime: true));
				}
			}

			var current = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);
			if (current != null) {

				//var l10Scores = L10Accessor.GetScoresForRecurrence(s, perm, recurrenceId,includeAutoGenerated:false);

				var mm = new L10Meeting.L10Meeting_Measurable() {
					L10Meeting = current,
					Measurable = measurable,
				};
				s.Save(mm);

				//var serial=new{
				//	id=measurable.Id,
				//	accountableId=measurable.AccountableUserId,
				//	accountableName=measurable.AccountableUser.GetName(),
				//	adminName = measurable.AdminUser.GetName(),
				//	title=measurable.Title,
				//	direction = measurable.GoalDirection.ToString(),
				//	directionName = measurable.GoalDirection.ToSymbol(),
				//	target = measurable.Goal,
				//	measurable.AdminUserId,
				//	scores=weekData
				//};
				if (!skipRealTime) {

					rt.UpdateRecurrences(recurrenceId).AddLowLevelAction(g => {
						var settings = current.Organization.Settings;
						var sow = settings.WeekStart;
						var offset = current.Organization.GetTimezoneOffset();
						var scorecardType = settings.ScorecardPeriod;

						var ts = current.Organization.GetTimeSettings();
						ts.Descending = recur.ReverseScorecard;

						var weeks = TimingUtility.GetPeriods(ts, now, current.StartTime, false);

						//if (recur.ReverseScorecard)
						//	weeks.Reverse();

						//var rowId = l10Scores.GroupBy(x => x.MeasurableId).Count();
						var rowId = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).RowCount();
						var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM {
							MeetingId = current.Id,
							RecurrenceId = recurrenceId,
							MeetingMeasurable = mm,
							Scores = scores,
							Weeks = weeks
						});
						row.ViewData["row"] = rowId - 1;

						var first = row.Execute();
						row.ViewData["ShowRow"] = false;
						var second = row.Execute();
						g.addMeasurable(first, second);
					});
				}
			}
			if (!skipRealTime) {

				rt.UpdateRecurrences(recurrenceId).UpdateScorecard(scores.Where(x => x.Measurable.Id == measurable.Id));

				rt.UpdateRecurrences(recurrenceId).SetFocus("[data-measurable='" + measurable.Id + "'] input:visible:first");
				//var scorecard = new AngularScorecard();
				//var measurablesList = new List<AngularMeasurable>() { new AngularMeasurable(measurable) };

				//scorecard.Measurables = AngularList.Create(AngularListType.Add, measurablesList);
				//var scoresList = new List<AngularScore>(); 
				//foreach (var ss in ) {
				//    scoresList.Add(new AngularScore(ss));
				//}
				//scorecard.Scores = AngularList.Create<AngularScore>(AngularListType.Add,scoresList);


				//group.update(new AngularUpdate() { scorecard });

			}
			Audit.L10Log(s, perm.GetCaller(), recurrenceId, "CreateMeasurable", ForModel.Create(measurable), measurable.Title);
		}
		//private static void AddMeasurable_RT(ISession s, long recurrenceId, dynamic group, DateTime now, List<ScoreModel> scores, L10Meeting current, L10Meeting.L10Meeting_Measurable mm) {
		//	var settings = current.Organization.Settings;
		//	var sow = settings.WeekStart;
		//	var offset = current.Organization.GetTimezoneOffset();
		//	var scorecardType = settings.ScorecardPeriod;

		//	var weeks = TimingUtility.GetPeriods(current.Organization, now, current.StartTime, false);

		//	if (current.L10Recurrence != null && current.L10Recurrence.ReverseScorecard)
		//		weeks = weeks.OrderByDescending(x => x.ForWeek).ToList();


		//	//var rowId = l10Scores.GroupBy(x => x.MeasurableId).Count();

		//	var rowId = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).RowCount();
		//	var row = ViewUtility.RenderPartial("~/Views/L10/partial/ScorecardRow.cshtml", new ScorecardRowVM {
		//		MeetingId = current.Id,
		//		RecurrenceId = recurrenceId,
		//		MeetingMeasurable = mm,
		//		Scores = scores,
		//		Weeks = weeks
		//	});
		//	row.ViewData["row"] = rowId - 1;

		//	var first = row.Execute();
		//	row.ViewData["ShowRow"] = false;
		//	var second = row.Execute();
		//	group.addMeasurable(first, second);
		//}
		public static void CreateMeasurable(UserOrganizationModel caller, long recurrenceId, L10Controller.AddMeasurableVm model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perm = PermissionsUtility.Create(s, caller);
						AddMeasurable(s, perm, rt, recurrenceId, model);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static void SetMeetingMeasurableOrdering(UserOrganizationModel caller, long recurrenceId, List<long> orderedL10Meeting_Measurables) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.MeasurableReorder(recurrenceId));

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

					tx.Commit();
					s.Flush();

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


				}
			}
		}
		public static void SetRecurrenceMeasurableOrdering(UserOrganizationModel caller, long recurrenceId, List<long> orderedL10Recurrene_Measurables) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);


					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.MeasurableReorder(recurrenceId));

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

					tx.Commit();
					s.Flush();



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


				}
			}
		}

		public static void RemoveMeasurable(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long measurableId) {
			perm.AdminL10Recurrence(recurrenceId);
			var measurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.Measurable.Id == measurableId).List().ToList();



			if (!measurables.Any())
				throw new PermissionsException("Measurable does not exist.");
			var now = DateTime.UtcNow;
			foreach (var r in measurables) {
				r.DeleteTime = now;
				s.Update(r);

				rt.UpdateRecurrences(recurrenceId).Update(
					new AngularRecurrence(recurrenceId) {
						Scorecard = new AngularScorecard(recurrenceId) {
							Id = recurrenceId,
							Measurables = AngularList.CreateFrom(AngularListType.Remove, new AngularMeasurable(r.Measurable))
						}
					}
				);
			}
			var cur = _GetCurrentL10Meeting(s, perm, recurrenceId, true, false, false);

			if (cur != null) {
				var mmeasurables = s.QueryOver<L10Meeting.L10Meeting_Measurable>().Where(x =>
					x.DeleteTime == null && x.L10Meeting.Id == cur.Id && x.Measurable.Id == measurableId).List().ToList();
				foreach (var r in mmeasurables) {
					r.DeleteTime = now;
					s.Update(r);
				}

			}

			var measurableInOthers = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId).RowCount();
			if (measurableInOthers == 0) {
				var measurable = s.Get<MeasurableModel>(measurableId);
				if (measurable.FromTemplateItemId == null) {
					measurable.Archived = true;
					measurable.DeleteTime = now;
					s.Update(measurable);

#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
					if (measurable.AccountableUserId != null) {
						s.Flush();
						s.GetFresh<UserOrganizationModel>(measurable.AccountableUserId).UpdateCache(s);
					}
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
				}
			}
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

		#region Todos	
		public static List<TodoModel> GetTodosForRecurrence(UserOrganizationModel caller, long recurrenceId, long meetingId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					return GetTodosForRecurrence(s, perms, recurrenceId, meetingId);
				}
			}
		}

		private static List<TodoModel> GetTodosForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, long meetingId) {
			perms.ViewL10Recurrence(recurrenceId).ViewL10Meeting(meetingId);

			var meeting = s.Get<L10Meeting>(meetingId);

			if (meeting.L10RecurrenceId != recurrenceId)
				throw new PermissionsException("Incorrect Recurrence Id");

			var previous = GetPreviousMeeting(s, perms, recurrenceId);

			var everythingAfter = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7));

			if (previous != null)
				everythingAfter = previous.CompleteTime.Value;

			var todoList = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId);

			if (meeting.CompleteTime != null)
				todoList = todoList.Where(x => x.CloseTime == null || (x.CloseTime <= meeting.CompleteTime && x.CreateTime < meeting.StartTime)); //todoList.Where(x => x.CompleteTime == null || (x.CompleteTime < meeting.CompleteTime && x.CreateTime < meeting.StartTime));
			else {
				todoList = todoList.Where(x => x.CloseTime == null || x.CloseTime > everythingAfter);//todoList.Where(x => x.CompleteTime == null || x.CompleteTime > everythingAfter);
			}
			var output = todoList.Fetch(x => x.AccountableUser).Eager.List().ToList();
			//var users = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(output.Select(x => x.AccountableUserId).Distinct().ToArray()).List().ToList();

			//foreach (var o in output) {
			//	o.AccountableUser = users.First(x=>x.Id==o.AccountableUserId
			//}
			return output;


		}
		public static MeasurableModel TodoMeasurable = new MeasurableModel() {
			Id = -10001,
			Title = "Aggregate To-Do Completion",
			_Editable = false,
			Goal = 90,
			GoalDirection = LessGreater.GreaterThan,
			UnitType = UnitType.Percent,
		};

		public static MeasurableModel GenerateTodoMeasureable(UserOrganizationModel forUser) {
			return new MeasurableModel() {
				Id = -10001 - forUser.Id,
				Title = "To-Do Completion " + forUser.GetName(),
				_Editable = false,
				Goal = 90,
				GoalDirection = LessGreater.GreaterThan,
				UnitType = UnitType.Percent,

			};
		}
		public static List<TodoModel> GetPreviousTodos(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

					var todos = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId).List().ToList();

					foreach (var t in todos) {
						var a = t.AccountableUser.GetName();
					}
					return todos;
				}
			}
		}
		public static void GetVisibleTodos(UserOrganizationModel caller, long[] forUsers, bool includeComplete) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var p = PermissionsUtility.Create(s, caller);
					//forUsers.Distinct().ForEach(x => p.ManagesUserOrganizationOrSelf(x));
					//s.QueryOver<TodoModel>().Where(x=>x.)
					throw new Exception("todo");
				}
			}
		}
		public static void UpdateTodo(UserOrganizationModel caller, long todoId, string message = null, string details = null, DateTime? dueDate = null, long? accountableUser = null, bool? complete = null, string connectionId = null, bool duringMeeting = false, bool? delete = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var todo = s.Get<TodoModel>(todoId);
					if (todo == null)
						throw new PermissionsException("To-do does not exist.");
					PermissionsUtility perm = PermissionsUtility.Create(s, caller);
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					dynamic group;
					if (todo.TodoType == TodoType.Recurrence) {
						if (todo.ForRecurrenceId == null || todo.ForRecurrenceId == 0)
							throw new PermissionsException("Meeting does not exist.");
						perm.EditTodo(todoId);//EditL10Recurrence(todo.ForRecurrenceId.Value);

						group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(todo.ForRecurrenceId.Value), connectionId);
					} else if (todo.TodoType == TodoType.Personal) {
						perm.EditTodo(todoId);
						group = hub.Clients.Group(MeetingHub.GenerateUserId(todo.AccountableUserId), connectionId);
					} else {
						throw new PermissionsException("unhandled TodoType");
					}

					var updatesText = new List<string>();

					if (message != null && todo.Message != message) {
						SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateTodoMessage(todo.Id));
						todo.Message = message;
						group.updateTodoMessage(todoId, message);
						updatesText.Add("Message: " + todo.Message);
					}
					if (details != null && todo.Details != details) {
						SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateTodoDetails(todo.Id));
						todo.Details = details;
						group.updateTodoDetails(todoId, details);
						updatesText.Add("Details: " + details);
					}
					if (dueDate != null && todo.DueDate != dueDate.Value) {
						todo.DueDate = dueDate.Value;
						group.updateTodoDueDate(todoId, dueDate.Value.ToJavascriptMilliseconds());
						updatesText.Add("Due-Date: " + dueDate.Value.ToShortDateString());
					}
					if (accountableUser != null && todo.AccountableUserId != accountableUser.Value && accountableUser>0) {
						todo.AccountableUserId = accountableUser.Value;
						todo.AccountableUser = s.Get<UserOrganizationModel>(accountableUser.Value);
						group.updateTodoAccountableUser(todoId, accountableUser.Value, todo.AccountableUser.GetName(), todo.AccountableUser.ImageUrl(true, ImageSize._32));
						updatesText.Add("Accountable: " + todo.AccountableUser.GetName());
					}

					if (complete != null) {
						SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateTodoCompletion(todo.Id));
						var now = DateTime.UtcNow;
						if (complete.Value && todo.CompleteTime == null) {
							if (duringMeeting && todo.ForRecurrenceId != null) {
								try {
									var meetingId = _GetCurrentL10Meeting(s, perm, todo.ForRecurrenceId.Value, true, false, false).NotNull(x => x.Id);
									if (meetingId != 0)
										todo.CompleteDuringMeetingId = meetingId;
								} catch (Exception) {

								}
							}

							todo.CompleteTime = now;
							s.Update(todo);
							updatesText.Add("Marked Complete");
							new Cache().InvalidateForUser(todo.AccountableUser, CacheKeys.UNSTARTED_TASKS);
						} else if (!complete.Value && todo.CompleteTime != null) {
							todo.CompleteTime = null;
							todo.CompleteDuringMeetingId = null;
							s.Update(todo);
							updatesText.Add("Marked Incomplete");
							new Cache().InvalidateForUser(todo.AccountableUser, CacheKeys.UNSTARTED_TASKS);
						}
						group.updateTodoCompletion(todoId, complete);
					}

					_ProcessDeleted(s, todo, delete);

					group.update(new AngularUpdate() { new AngularTodo(todo) });


					if (todo.ForRecurrenceId.HasValue) {
						var updatedText = "Updated To-Do \"" + todo.Message + "\" \n " + String.Join("\n", updatesText);
						Audit.L10Log(s, caller, todo.ForRecurrenceId.Value, "UpdateTodo", ForModel.Create(todo), updatedText);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void CompleteTodo(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long todoModel) {
			perm.EditTodo(todoModel);
			var todo = s.Get<TodoModel>(todoModel);
			if (todo.CompleteTime != null)
				throw new PermissionsException("Issue already deleted.");
			if (todo.ForRecurrence.Id != recurrenceId)
				throw new PermissionsException("You cannot edit this meeting.");
			todo.CompleteTime = DateTime.UtcNow;
			s.Update(todo);

			var recur = new AngularRecurrence(recurrenceId);
			recur.Todos = AngularList.CreateFrom(AngularListType.Remove, new AngularTodo(todo));
			rt.UpdateRecurrences(recurrenceId).Update(recur);
		}

		public static void MarkFireworks(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Recurrence(recurrenceId);

					var l10 = _GetCurrentL10Meeting(s, perms, recurrenceId, true, true, false);

					if (l10 != null && l10._MeetingAttendees != null) {
						var found = l10._MeetingAttendees.FirstOrDefault(x => x.User.Id == caller.Id);
						if (found != null) {
							var attendee = s.Get<L10Meeting.L10Meeting_Attendee>(found.Id);
							attendee.SeenTodoFireworks = true;
							s.Update(attendee);
							tx.Commit();
							s.Flush();
						}
					}
				}
			}
		}
		public static List<TodoModel> GetAllTodosForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool includeClosed = true, DateRange range = null) {
			perms.ViewL10Recurrence(recurrenceId);

			var todoListQ = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId);
			if (range != null && includeClosed) {
				var st = range.StartTime.AddDays(-1);
				var et = range.EndTime.AddDays(1);
				todoListQ = todoListQ.Where(x => x.CompleteTime == null || (x.CompleteTime >= st && x.CompleteTime <= et));
			}


			if (!includeClosed) {
				todoListQ = todoListQ.Where(x => x.CloseTime == null);
			}
			var todoList = todoListQ.List().ToList();
			foreach (var t in todoList) {
				if (t.AccountableUser != null) {
					var a = t.AccountableUser.GetName();
					var b = t.AccountableUser.ImageUrl(true);
				}
			}
			return todoList;
		}

		public static Ratio TodoCompletion(TodoModel todo, DateTime weekStart, DateTime weekEnd, DateTime? meetingStart = null) {
			//NUM= c<d && s<d<e
			//DEN= s<d<e
			var t = todo;
			var s = weekStart;
			var e = weekEnd;
			var c = t.CompleteTime ?? DateTime.MaxValue;
			var d = t.DueDate;

			if (d == d.Date) {
				if (todo.Organization != null)
					d = todo.Organization.ConvertFromUTC(d.Date.AddDays(1)).AddMilliseconds(-1);
				else
					d = d.Date.AddDays(1).AddMilliseconds(-1);
			}


			meetingStart = meetingStart ?? DateTime.UtcNow;
			if (s < c && c < d && d < e)
				return new Ratio(1, 1);
			if (weekStart < meetingStart && meetingStart < d && t.CompleteTime == null/* && d < weekEnd*/)
				return new Ratio(0, 0);

			if (/*meetingStart != null &&*/ todo.CompleteDuringMeetingId != null && weekStart < d && d < weekEnd &&
				weekStart < /*meetingStart && meetingStart <*/ todo.CompleteTime && todo.CompleteTime < weekEnd)
				return new Ratio(1, 1);

			if (meetingStart.Value.Date <= d && t.CompleteTime == null)
				return new Ratio(0, 0);
			if (d < s && s < c) { // use s for Q=0/1 and use e for Q=0/0
				if (c < e)
					return new Ratio(0, 0);
				else
					return new Ratio(0, 1);
			}

			var cd = c < d;
			var sde = (s < d) && (d < e);
			var num = cd && sde;
			var den = sde;

			return new Ratio(num.ToInt(), den.ToInt());
		}

		public static Ratio TodoCompletion(List<TodoModel> todos, DateTime weekStart, DateTime weekEnd, DateTime? meetingStart = null) {
			var ratio = new Ratio(0, 0);
			foreach (var t in todos) {
				var c = TodoCompletion(t, weekStart, weekEnd, meetingStart);
				ratio.Merge(c);
			}
			return ratio;
		}

		[Obsolete("Do not use", true)]
		public static Ratio TodoCompletion(List<TodoModel> todos, DateTime week, DateTime now, bool dontUse) {
			var ratio = new Ratio(0, 0);
			foreach (var t in todos) {
				if (t.CreateTime < week.AddDays(-7)) {
					if (t.CompleteTime == null || week < t.CompleteTime.Value) {
						ratio.Add(0, 1);
					} else if (week.AddDays(-7) <= t.CompleteTime.Value.StartOfWeek(DayOfWeek.Sunday) && t.CompleteTime.Value.StartOfWeek(DayOfWeek.Sunday) < week) {
						ratio.Add(1, 1);
					}
				}
				/*if (week.AddDays(-7) <= t.DueDate.StartOfWeek(DayOfWeek.Sunday) && t.DueDate.StartOfWeek(DayOfWeek.Sunday) < week)
                {

                    if (currentWeek){
                        //do something different...
                    }
                    else{

                        if (t.CompleteTime == null){
                            ratio.Add(0, 1);
                        }else{
                            if (t.CompleteTime.Value.StartOfWeek(DayOfWeek.Sunday) <= week)
                                ratio.Add(1, 1);
                            else
                                ratio.Add(0, 1);
                        }
                    }
                }*/
			}
			return ratio;


		}

		public static void UpdateTodos(UserOrganizationModel caller, long recurrenceId, L10Controller.UpdateTodoVM model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var ids = model.todos;// model.GetAllIds();
					var existingTodos = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId)
						.WhereRestrictionOn(x => x.Id).IsIn(ids)
						.List().ToList();

					var ar = SetUtility.AddRemove(ids, existingTodos.Select(x => x.Id));

					if (ar.RemovedValues.Any())
						throw new PermissionsException("You do not have permission to edit this issue.");
					if (ar.AddedValues.Any())
						throw new PermissionsException("Unreachable.");

					//var recurrenceIssues = existingTodos.ToList();
					var i = 0;
					foreach (var e in model.todos) {
						var f = existingTodos.First(x => x.Id == e);
						var update = false;
						/*if (f..NotNull(x => x.Id) != e.ParentRecurrenceIssueId)
                        {
                            f.ParentRecurrenceIssue = (e.ParentRecurrenceIssueId == null) ? null : recurrenceIssues.First(x => x.Id == e.ParentRecurrenceIssueId);
                            update = true;
                        }*/

						if (f.Ordering != i) {
							f.Ordering = i;
							update = true;
						}
						if (update)
							s.Update(f);
						i++;
					}

					var json = Json.Encode(model);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), model.connectionId);

					//group.deserializeTodos(".todo-list", model);
					group.setTodoOrder(model.todos);


					group.update(new AngularRecurrence(recurrenceId) {
						Todos = existingTodos.OrderBy(x => x.Ordering).Select(x => new AngularTodo(x)).ToList()
					});

					Audit.L10Log(s, caller, recurrenceId, "UpdateTodos", ForModel.Create<L10Recurrence>(recurrenceId));
					tx.Commit();
					s.Flush();
				}
			}
		}


		#endregion

		#region Issues			
		public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, DateTime? meetingStart = null) {
			var mstart = meetingStart ?? DateTime.MaxValue;
			perms.ViewL10Recurrence(recurrenceId);
			//TODO optimize this call. Some issueRecurrence's parents are closed, but children are not.

			var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x =>
					x.DeleteTime == null && x.Recurrence.Id == recurrenceId &&
					(x.CloseTime == null || x.CloseTime >= mstart)
				).Fetch(x => x.Issue).Eager
				.List().ToList();

			/*var query = s.QueryOver<IssueModel>();
            if (includeResolved)
                query = query.Where(x => x.DeleteTime == null);
            else
                query = query.Where(x => x.DeleteTime == null && x.CloseTime == null);					
            var issues =  query.WhereRestrictionOn(x => x.Id).IsIn(issueIds).List().ToList();*/

			return _PopulateChildrenIssues(issues);
		}

		public static List<IssueModel.IssueModel_Recurrence> GetSolvedIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, DateRange range) {
			perms.ViewL10Recurrence(recurrenceId);

			var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
				.Where(x => x.CloseTime >= range.StartTime && x.CloseTime <= range.EndTime)
				.Fetch(x => x.Issue).Eager
				.List().ToList();

			return _PopulateChildrenIssues(issues);
		}


		public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrence(UserOrganizationModel caller, long meetingId, bool includeResolved) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var meeting = s.Get<L10Meeting>(meetingId);
					var recurrenceId = meeting.L10RecurrenceId;
					var perms = PermissionsUtility.Create(s, caller);
					return GetIssuesForRecurrence(s, perms, recurrenceId, meeting.StartTime);
				}
			}
		}
		public static List<IssueModel.IssueModel_Recurrence> GetAllIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool includeCompleted = true, DateRange range = null) {
			perms.ViewL10Recurrence(recurrenceId);

			//TODO optimize this call. Some issueRecurrence's parents are closed, but children are not.

			var issuesQ = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId);

			if (range != null && includeCompleted) {
				var st = range.StartTime.AddDays(-1);
				var et = range.EndTime.AddDays(1);
				issuesQ = issuesQ.Where(x => x.CloseTime == null || (x.CloseTime >= st && x.CloseTime <= et));
			}

			if (!includeCompleted)
				issuesQ = issuesQ.Where(x => x.CloseTime == null);

			var issues = issuesQ.Fetch(x => x.Issue).Eager.List().ToList();

			return _PopulateChildrenIssues(issues);
		}

		public static void _UpdateIssueCompletion_Unsafe(ISession s, RealTimeUtility rt, IssueModel.IssueModel_Recurrence issue, bool complete, DateTime? now = null) {

			now = now ?? DateTime.UtcNow;

			bool? added = null;
			if (complete && issue.CloseTime == null) {
				issue.CloseTime = now;
				added = false;
			} else if (!complete && issue.CloseTime != null) {
				issue.CloseTime = null;
				added = true;
			}

			if (added != null) {
				s.Update(issue);
				var others = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.Issue.Id == issue.Issue.Id).List().ToList();

				foreach (var o in others) {
					if (o.Id != issue.Id) {
						o.MarkedForClose = complete;
						s.Update(o);
					}
					rt.UpdateRecurrences(o.Recurrence.Id).AddLowLevelAction(x => x.updateModedIssueSolve(o.Id, complete));

					var recur = new AngularRecurrence(o.Recurrence.Id);
					recur.IssuesList.Issues = AngularList.CreateFrom(added.Value ? AngularListType.Add : AngularListType.Remove, new AngularIssue(issue));
					rt.UpdateRecurrences(o.Recurrence.Id).Update(recur);
				}
			}
		}


		public static void UpdateIssue(UserOrganizationModel caller, long issueRecurrenceId, DateTime updateTime, string message = null, string details = null, bool? complete = null, string connectionId = null, long? owner = null, int? priority = null, int? rank = null, bool? delete = null, bool? awaitingSolve = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					updateTime = Math2.Min(DateTime.UtcNow.AddSeconds(3), updateTime);

					var issue = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);
					if (issue == null)
						throw new PermissionsException("Issue does not exist.");

					var recurrenceId = issue.Recurrence.Id;
					if (recurrenceId == 0)
						throw new PermissionsException("Meeting does not exist.");
					var perms = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), connectionId);

					var updatesText = new List<string>();
					if (message != null && message != issue.Issue.Message) {
						SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateIssueMessage(issue.Issue.Id));
						issue.Issue.Message = message;
						group.updateIssueMessage(issueRecurrenceId, message);
						updatesText.Add("Message: " + issue.Issue.Message);
					}
					if (details != null && details != issue.Issue.Description) {
						SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateIssueDetails(issue.Issue.Id));
						issue.Issue.Description = details;
						group.updateIssueDetails(issueRecurrenceId, details);
						updatesText.Add("Description: " + issue.Issue.Description);
					}
					if (owner != null && (issue.Owner == null || owner != issue.Owner.Id) && owner>0) {
						var any = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == issue.Recurrence.Id && x.User.Id == owner).Take(1).List().ToList();
						if (!any.Any())
							throw new PermissionsException("Specified Owner cannot see meeting");

						issue.Owner = s.Get<UserOrganizationModel>(owner);
						group.updateIssueOwner(issueRecurrenceId, owner, issue.Owner.GetName(), issue.Owner.ImageUrl(true, ImageSize._32));
						updatesText.Add("Owner: " + issue.Owner.GetName());
					}
					if (priority != null && priority != issue.Priority && issue.LastUpdate_Priority < updateTime) {
						issue.LastUpdate_Priority = updateTime;
						var old = issue.Priority;
						issue.Priority = priority.Value;
						group.updateIssuePriority(issueRecurrenceId, issue.Priority);
						updatesText.Add("Priority from " + old + " to " + issue.Priority);
						s.Update(issue);
					}
					if (rank != null && rank != issue.Rank && issue.LastUpdate_Priority < updateTime) {
						issue.LastUpdate_Priority = updateTime;
						var old = issue.Rank;
						issue.Rank = rank.Value;
						group.updateIssueRank(issueRecurrenceId, issue.Rank, true);
						updatesText.Add("Rank from " + old + " to " + issue.Rank);
						s.Update(issue);
					}


					_ProcessDeleted(s, issue, delete);

					var now = DateTime.UtcNow;
					if (complete != null) {
						using (var rt = RealTimeUtility.Create()) {
							_UpdateIssueCompletion_Unsafe(s, rt, issue, complete.Value, now);
						}
						if (complete.Value && issue.CloseTime == null) {
							updatesText.Add("Marked Closed");
						} else if (!complete.Value && issue.CloseTime != null) {
							updatesText.Add("Marked Open");
						}
					}


					if (awaitingSolve != null && awaitingSolve != issue.AwaitingSolve) {
						issue.AwaitingSolve = awaitingSolve.Value;
						s.Update(issue);

						group.updateIssueAwaitingSolve(issue.Id, awaitingSolve.Value);

					}
					group.update(new AngularUpdate() { new AngularIssue(issue) });


					var updatedText = "Updated Issue \"" + issue.Issue.Message + "\" \n " + String.Join("\n", updatesText);

					Audit.L10Log(s, caller, recurrenceId, "UpdateIssue", ForModel.Create(issue), updatedText);

					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void CompleteIssue(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long recurrenceIssue) {
			perm.EditL10Recurrence(recurrenceId);
			var issue = s.Get<IssueModel.IssueModel_Recurrence>(recurrenceIssue);
			if (issue.Recurrence.Id != recurrenceId)
				throw new PermissionsException("You cannot edit this meeting.");
			if (issue.CloseTime != null)
				throw new PermissionsException("Issue already deleted.");

			_UpdateIssueCompletion_Unsafe(s, rt, issue, true);
		}
		public static void UpdateIssues(UserOrganizationModel caller, long recurrenceId, /*IssuesDataList*/L10Controller.IssuesListVm model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var ids = model.GetAllIds();
					var found = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
						.WhereRestrictionOn(x => x.Id).IsIn(ids)
						//.Fetch(x=>x.Issue).Eager
						.List().ToList();

					if (model.orderby != null) {
						var recur = s.Get<L10Recurrence>(recurrenceId);
						recur.OrderIssueBy = model.orderby;
						s.Update(recur);
					}


					var ar = SetUtility.AddRemove(ids, found.Select(x => x.Id));

					if (ar.RemovedValues.Any())
						throw new PermissionsException("You do not have permission to edit this issue.");
					if (ar.AddedValues.Any())
						throw new PermissionsException("Unreachable.");

					var recurrenceIssues = found.ToList();

					foreach (var e in model.GetIssueEdits()) {
						var f = recurrenceIssues.First(x => x.Id == e.RecurrenceIssueId);
						var update = false;
						if (f.ParentRecurrenceIssue.NotNull(x => x.Id) != e.ParentRecurrenceIssueId) {
							f.ParentRecurrenceIssue = (e.ParentRecurrenceIssueId == null) ? null : recurrenceIssues.First(x => x.Id == e.ParentRecurrenceIssueId);
							update = true;
						}

						if (f.Ordering != e.Order) {
							f.Ordering = e.Order;
							update = true;
						}

						if (update)
							s.Update(f);
					}

					var json = Json.Encode(model);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId), model.connectionId);

					//group.deserializeIssues(".issues-list", model);
					group.setIssueOrder(model.issues);
					var issues = GetAllIssuesForRecurrence(s, perm, recurrenceId)
						.OrderBy(x => x.Ordering)
						.Select(x => new AngularIssue(x))
						.ToList();



					group.update(new AngularRecurrence(recurrenceId) {
						IssuesList = new AngularIssuesList(recurrenceId) {
							Issues = AngularList.Create(AngularListType.ReplaceAll, issues)
						}
					});

					Audit.L10Log(s, caller, recurrenceId, "UpdateIssues", ForModel.Create<L10Recurrence>(recurrenceId));

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static VtoItem_String MoveIssueToVto(UserOrganizationModel caller, long issue_recurrence) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					var recurIssue = s.Get<IssueModel.IssueModel_Recurrence>(issue_recurrence);

					perm.EditL10Recurrence(recurIssue.Recurrence.Id);

					recurIssue.DeleteTime = DateTime.UtcNow;
					s.Update(recurIssue);

					var recur = s.Get<L10Recurrence>(recurIssue.Recurrence.Id);
					perm.EditVTO(recur.VtoId);
					var vto = s.Get<VtoModel>(recur.VtoId);

					var str = VtoAccessor.AddString(s, perm, recur.VtoId, VtoItemType.List_Issues,
						(v, list) => new AngularVTO(v.Id) { Issues = list },
						true, forModel: ForModel.Create(recurIssue), value: recurIssue.Issue.Message);

					tx.Commit();
					s.Flush();
					return str;
				}
			}
		}
		public async static Task<IssueModel.IssueModel_Recurrence> MoveIssueFromVto(UserOrganizationModel caller, long vtoIssue) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var now = DateTime.UtcNow;
					var perm = PermissionsUtility.Create(s, caller);
					var vtoIssueStr = s.Get<VtoItem_String>(vtoIssue);

					IssueModel.IssueModel_Recurrence issueRecur;
					perm.EditVTO(vtoIssueStr.Vto.Id);

					vtoIssueStr.DeleteTime = now;
					s.Update(vtoIssueStr);

					if (vtoIssueStr.ForModel != null) {
						if (vtoIssueStr.ForModel.ModelType != ForModel.GetModelType<IssueModel.IssueModel_Recurrence>())
							throw new PermissionsException("ModelType was unexpected");
						issueRecur = s.Get<IssueModel.IssueModel_Recurrence>(vtoIssueStr.ForModel.ModelId);

						var recur = s.Get<L10Recurrence>(issueRecur.Recurrence.Id);

						perm.EditL10Recurrence(issueRecur.Recurrence.Id);

						issueRecur.DeleteTime = null;
						s.Update(issueRecur);
						//Add back to issues list (does not need to be added below. CreateIssue calls this.
						var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
						var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(issueRecur.Recurrence.Id));
						meetingHub.appendIssue(".issues-list", IssuesData.FromIssueRecurrence(issueRecur), recur.OrderIssueBy);
					} else {
						var vto = s.Get<VtoModel>(vtoIssueStr.Vto.Id);
						if (vto.L10Recurrence == null)
							throw new PermissionsException("Expected L10Recurrence was null");
						var issue = await IssuesAccessor.CreateIssue(s, perm, vto.L10Recurrence.Value, caller.Id, new IssueModel() {
							Message = vtoIssueStr.Data,
							OrganizationId = vto.Organization.Id,
							CreatedById = caller.Id
						});
						var recur = s.Get<L10Recurrence>(vto.L10Recurrence.Value);

						issueRecur = issue.IssueRecurrenceModel;
					}
					//Remove from vto
					var vtoHub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
					var group = vtoHub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoIssueStr.Vto.Id));
					vtoIssueStr.Vto = null;
					group.update(new AngularUpdate() { AngularVtoString.Create(vtoIssueStr) });

					tx.Commit();
					s.Flush();
					return issueRecur;
				}
			}
		}

		#endregion

		#region PeopleHeadlines		
		public static List<PeopleHeadline> GetHeadlinesForMeeting(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetHeadlinesForMeeting(s, perms, recurrenceId);
				}
			}
		}
		public static List<PeopleHeadline> GetHeadlinesForMeeting(ISession s, PermissionsUtility perms, long recurrenceId, bool includeClosed = false) {
			perms.ViewL10Recurrence(recurrenceId);

			var foundQ = s.QueryOver<PeopleHeadline>().Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId);
			if (!includeClosed)
				foundQ = foundQ.Where(x => x.CloseTime == null);

			var found = foundQ.Fetch(x => x.Owner).Eager
								.Fetch(x => x.About).Eager
								.List().ToList();

			foreach (var f in found) {
				if (f.Owner != null) {
					var a = f.Owner.GetName();
					var b = f.Owner.ImageUrl(true, ImageSize._32);
				}
				if (f.About != null) {
					var a = f.About.GetName();
					var b = f.About.GetImageUrl();
				}
			}
			return found;
		}

		public static void UpdateHeadline(UserOrganizationModel caller, long headlineId, string message, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var headline = s.Get<PeopleHeadline>(headlineId);
					perms.EditL10Recurrence(headline.RecurrenceId);

					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateHeadlineMessage(headlineId));
					headline.Message = message;
					s.Update(headline);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(headline.RecurrenceId), connectionId);
					group.updateHeadlineMessage(headlineId, message);

					group.update(new AngularUpdate() {
						new AngularHeadline(headlineId) {
							Name = message
						}
					});

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void RemoveHeadline(ISession s, PermissionsUtility perm, RealTimeUtility rt, long recurrenceId, long headlineId) {
			perm.EditL10Recurrence(recurrenceId);

			perm.ViewHeadline(headlineId);

			var r = s.Get<PeopleHeadline>(headlineId);

			var now = DateTime.UtcNow;
			r.CloseTime = now;
			s.Update(r);
			rt.UpdateRecurrences(recurrenceId).Update(
				new AngularRecurrence(recurrenceId) {
					Headlines = AngularList.CreateFrom(AngularListType.Remove, new AngularHeadline(r.Id))
				}
			);
		}
		#endregion

		#region Angular
		public static AngularRecurrence GetAngularRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeScores = true, bool includeHistorical = true, bool fullScorecard = true, DateRange range = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var recurrence = s.Get<L10Recurrence>(recurrenceId);
					_LoadRecurrences(s, true, true, true, recurrence);

					var recur = new AngularRecurrence(recurrence);

					recur.Attendees = recurrence._DefaultAttendees.Select(x => {
						var au = AngularUser.CreateUser(x.User);
						au.CreateTime = x.CreateTime;
						return au;
					}).ToList();

					var scorecardRange = range;
					if (fullScorecard)
						scorecardRange = new DateRange(DateTime.UtcNow.AddDays(-7 * 13).StartOfWeek(DayOfWeek.Sunday), DateTime.UtcNow.AddDays(9).StartOfWeek(DayOfWeek.Sunday));
					var scores = new List<ScoreModel>();

					var scoresAndMeasurables = GetScoresAndMeasurablesForRecurrence(s, perms, recurrenceId, true, range: scorecardRange, getMeasurables: true, getScores: includeScores);

					if (includeScores) {
						scores = scoresAndMeasurables.Scores;//L10Accessor.GetScoresForRecurrence(s, perms, recurrenceId, range: scorecardRange);
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
					//var measurables = recurrence._DefaultMeasurables.Select(x => {
					//	if (x.IsDivider) {
					//		var m = AngularMeasurable.CreateDivider(x._Ordering, x.Id);
					//		m.RecurrenceId = x.L10Recurrence.Id;
					//		return m;
					//	} else {
					//		var m = new AngularMeasurable(x.Measurable, false);
					//		m.Ordering = x._Ordering;
					//		m.RecurrenceId = x.L10Recurrence.Id;
					//		return m;
					//	}
					//}).ToList();

					if (recurrence.IncludeAggregateTodoCompletion) {
						measurables.Add(new AngularMeasurable(TodoMeasurable) {
							Ordering = -2
						});
					}

					var ts = caller.GetTimeSettings();
					ts.WeekStart = recurrence.StartOfWeekOverride ?? ts.WeekStart;
					recur.Scorecard = new AngularScorecard(recurrenceId, ts, measurables, scores, DateTime.UtcNow, scorecardRange, reverseScorecard: recurrence.ReverseScorecard);

					var allRocks = recurrence._DefaultRocks.Select(x => new AngularRock(x.ForRock)).ToList();

					if (range != null) {
						var histRock = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
							.Where(x => x.DeleteTime != null && x.L10Recurrence.Id == recurrenceId)
							.Where(range.Filter<L10Recurrence.L10Recurrence_Rocks>()).List();
						allRocks.AddRange(histRock.Select(x => new AngularRock(x.ForRock)));
					}
					recur.Rocks = allRocks.Distinct(x => x.Id);
					recur.Todos = GetAllTodosForRecurrence(s, perms, recurrenceId, includeClosed: includeHistorical, range: range).Select(x => new AngularTodo(x)).OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ToList();
					recur.IssuesList.Issues = GetAllIssuesForRecurrence(s, perms, recurrenceId, includeCompleted: includeHistorical, range: range).Select(x => new AngularIssue(x)).OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ToList();
					recur.Headlines = GetAllHeadlinesForRecurrence(s, perms, recurrenceId, includeClosed: includeHistorical, range: range).Select(x => new AngularHeadline(x)).OrderByDescending(x => x.CloseTime ?? DateTime.MaxValue).ToList();
					recur.Notes = recurrence._MeetingNotes.Select(x => new AngularMeetingNotes(x)).ToList();

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
			}
		}
		public static List<PeopleHeadline> GetAllHeadlinesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool includeClosed, DateRange range) {
			perms.ViewL10Recurrence(recurrenceId);

			var headlineListQ = s.QueryOver<PeopleHeadline>().Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId);
			if (range != null && includeClosed) {
				var st = range.StartTime.AddDays(-1);
				var et = range.EndTime.AddDays(1);
				headlineListQ = headlineListQ.Where(x => x.CloseTime == null || (x.CloseTime >= st && x.CloseTime <= et));
			}

			if (!includeClosed) {
				headlineListQ = headlineListQ.Where(x => x.CloseTime == null);
			}
			var headlineList = headlineListQ.List().ToList();
			foreach (var t in headlineList) {
				if (t.About != null) {
					var a = t.About.GetName();
					var b = t.About.GetImageUrl();
				}
				if (t.Owner != null) {
					var a = t.Owner.GetName();
					var b = t.Owner.GetImageUrl();
				}
			}
			return headlineList;
		}
		public static void Remove(UserOrganizationModel caller, BaseAngular model, long recurrenceId, string connectionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);
						perms.EditL10Recurrence(recurrenceId);

						if (model.Type == typeof(AngularIssue).Name) {
							CompleteIssue(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularTodo).Name) {
							CompleteTodo(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularRock).Name) {
							RemoveRock(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularMeasurable).Name) {
							RemoveMeasurable(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularUser).Name) {
							RemoveAttendee(s, perms, rt, recurrenceId, model.Id);
						} else if (model.Type == typeof(AngularHeadline).Name) {
							RemoveHeadline(s, perms, rt, recurrenceId, model.Id);
						} else {
							throw new PermissionsException("Unhandled type: " + model.Type);
						}

						tx.Commit();
						s.Flush();
					}
				}
			}

		}
		public static void Update(UserOrganizationModel caller, BaseAngular model, string connectionId) {
			if (model.Type == typeof(AngularIssue).Name) {
				var m = (AngularIssue)model;
				//UpdateIssue(caller, (long)model.GetOrDefault("Id", null), (string)model.GetOrDefault("Name", null), (string)model.GetOrDefault("Details", null), (bool?)model.GetOrDefault("Complete", null), connectionId);
				UpdateIssue(caller, m.Id, DateTime.UtcNow, m.Name ?? "", m.Details ?? "", m.Complete, connectionId, priority: m.Priority, owner: m.Owner.NotNull(x => x.Id));
			} else if (model.Type == typeof(AngularTodo).Name) {
				var m = (AngularTodo)model;
				UpdateTodo(caller, m.Id, m.Name ?? "", null, m.DueDate, m.Owner.NotNull(x => x.Id), m.Complete, connectionId);
			} else if (model.Type == typeof(AngularScore).Name) {
				var m = (AngularScore)model;
				if (m.Id > 0)
					UpdateScore(caller, m.Id, m.Measured, connectionId, /*true*/ false);
				//else
				//	throw new Exception("Shouldn't get here");
				else
					UpdateScore(caller, m.Measurable.Id, m.ForWeek, m.Measured, connectionId, false);
			} else if (model.Type == typeof(AngularMeetingNotes).Name) {
				var m = (AngularMeetingNotes)model;
				EditNote(caller, m.Id, m.Contents, m.Title, connectionId);
			} else if (model.Type == typeof(AngularRock).Name) {
				var m = (AngularRock)model;
				UpdateRock(caller, m.Id, m.Name, m.Completion, m.Owner.NotNull(x => x.Id), connectionId, companyRock: m.CompanyRock);
			} else if (model.Type == typeof(AngularMeasurable).Name) {
				var m = (AngularMeasurable)model;
				UpdateArchiveMeasurable(caller, m.Id, m.Name, m.Direction, m.Target, m.Owner.NotNull(x => x.Id), m.Admin.NotNull(x => x.Id), connectionId);
			} else if (model.Type == typeof(AngularBasics).Name) {
				var m = (AngularBasics)model;
				UpdateRecurrence(caller, m.Id, m.Name, m.TeamType, connectionId);
			} else if (model.Type == typeof(AngularHeadline).Name) {
				var m = (AngularHeadline)model;
				UpdateHeadline(caller, m.Id, m.Name, connectionId);
			} else {
				throw new PermissionsException("Unhandled type: " + model.Type);
			}
		}
		public static void UpdateRecurrence(UserOrganizationModel caller, long recurrenceId, string name = null, L10TeamType? teamType = null, string connectionId = null) {
			using (var rt = RealTimeUtility.Create(connectionId)) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {

						var perms = PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId);
						var recurrence = s.Get<L10Recurrence>(recurrenceId);

						var angular = new AngularBasics(recurrenceId);

						if (name != null && recurrence.Name != name) {
							recurrence.Name = name;
							angular.Name = name;
							recurrence.Pristine = false;
						}

						if (teamType != null && recurrence.TeamType != teamType) {
							recurrence.TeamType = teamType.Value;
							angular.TeamType = teamType;
							recurrence.Pristine = false;
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

										return (new KeyValuePair<DateTime, decimal>(x.First().StartTime.Value, num / den * 100)).AsList();
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
										return (new KeyValuePair<DateTime, decimal>(x.First().CloseTime.Value, count)).AsList();
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
										return (new KeyValuePair<DateTime, decimal>(x.First().L10Meeting.StartTime.Value, x.Average(y => y.Rating.Value))).AsList();
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
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var recurrence = s.Get<L10Recurrence>(recurrenceId);
					var o = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).List().ToList();
					var meeting = o.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).FirstOrDefault();
					var prevMeeting = o.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).Take(2).LastOrDefault();


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
						if (prevMeeting != null && prevMeeting.CompleteTime != null)
							todoComplete = s.QueryOver<TodoModel>().Where(x => x.ForRecurrenceId == recurrenceId && x.CompleteTime > prevMeeting.CompleteTime).List().Count;
					} else {
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
					if (oldTodos.Count() > 0) {
						completion = (decimal)oldTodos.Count(x => x.CompleteTime != null) / (decimal)oldTodos.Count() * 100m;
					}
					if (meeting.TodoCompletion != null)
						completion = meeting.TodoCompletion.GetValue(0) * 100m;

					var stats = new L10MeetingStatsVM() {
						IssuesSolved = issuesSolved,
						TodosCreated = todosCreated,
						AllMeetings = o,
						StartTime = meeting.NotNull(x => x.StartTime),
						EndTime = meeting.NotNull(x => x.CompleteTime),
						TodoCompleted = todoComplete,
						AverageRating = rating,
						AllTodos = allTodos,
						TodoCompletionPercentage = completion
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

		#region Audit

		public static List<L10AuditModel> GetL10Audit(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var audits = s.QueryOver<L10AuditModel>().Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
						.Fetch(x => x.UserOrganization).Eager
						.TransformUsing(Transformers.DistinctRootEntity)
						.List().ToList();
					return audits;
				}
			}
		}
		#endregion

		#region Helpers
		public static List<AbstractTodoCreds> GetExternalLinksForRecurrence(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return ExternalTodoAccessor.GetExternalLinksForModel(s, PermissionsUtility.Create(s, caller), ForModel.Create<L10Recurrence>(recurrenceId));
				}
			}
		}

		private static bool _ProcessDeleted(ISession s, IDeletable item, bool? delete) {
			if (delete != null) {
				if (delete == true && item.DeleteTime == null) {
					item.DeleteTime = DateTime.UtcNow;
					s.Update(item);
					return true;
				} else if (delete == false && item.DeleteTime != null) {
					item.DeleteTime = null;
					s.Update(item);
					return true;
				}
			}
			return false;
		}
		public static object GetModel_Unsafe(ISession s, string type, long id) {
			if (id <= 0)
				return null;

			switch (type.ToLower()) {
				case "measurablemodel":
					return s.Get<MeasurableModel>(id);
				case "todomodel":
					return s.Get<TodoModel>(id);
				case "issuemodel":
					return s.Get<IssueModel>(id);
			}
			return null;
		}

		#endregion

		#region Video

		public static void SetVideoProvider(UserOrganizationModel caller, long recurrenceId, long vcProviderId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

						var found = s.Get<AbstractVCProvider>(vcProviderId);
						if (found.DeleteTime != null)
							throw new PermissionsException("Video Provider does not exist");
						perms.ViewUserOrganization(found.OwnerId, false);

						var user = s.Get<UserOrganizationModel>(found.OwnerId);
						if (user.DeleteTime != null)
							throw new PermissionsException("Owner of the Video Conference Provider no longer exists");

						found.LastUsed = DateTime.UtcNow;
						s.Update(found);

						var l10Meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true);
						if (l10Meeting != null) {
							l10Meeting.SelectedVideoProvider = found;

							s.Update(l10Meeting);
						}

						rt.UpdateRecurrences(recurrenceId)
						  .AddLowLevelAction(x => {
							  var resolved = (AbstractVCProvider)s.GetSessionImplementation().PersistenceContext.Unproxy(found);
							  x.setSelectedVideoProvider(resolved);
						  });

						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static void SetJoinedVideo(UserOrganizationModel caller, long userId, long recurrenceId, long vcProviderId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller)
							.ViewL10Recurrence(recurrenceId)
							.Self(userId);

						var found = s.Get<AbstractVCProvider>(vcProviderId);
						if (found.DeleteTime != null)
							throw new PermissionsException("Video Provider does not exist");
						perms.ViewUserOrganization(found.OwnerId, false);

						var user = s.Get<UserOrganizationModel>(found.OwnerId);
						if (user.DeleteTime != null)
							throw new PermissionsException("Owner of the Video Conference Provider no longer exists");

						found.LastUsed = DateTime.UtcNow;
						s.Update(found);

						var link = new JoinedVideo() {
							RecurrenceId = recurrenceId,
							UserId = userId,
							VideoProvider = vcProviderId,
						};

						var recur = s.Get<L10Recurrence>(recurrenceId);
						recur.SelectedVideoProviderId = found.Id;
						s.Update(recur);

						var l10Meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true);
						if (l10Meeting != null) {
							link.MeetingId = l10Meeting.Id;
						}

						rt.UpdateRecurrences(recurrenceId).AddLowLevelAction(x => {
							x.setSelectedVideoProvider(found);
						});

						s.Save(link);

						tx.Commit();
						s.Flush();
					}
				}
			}
		}


		#endregion
	}
}