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

		#region Meeting Actions

		private static IEnumerable<L10Recurrence.L10Recurrence_Page> GenerateMeetingPages(long recurrenceId, MeetingType meetingType, DateTime createTime) {

			if (meetingType == MeetingType.L10) {
				#region L10 Pages
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Segue",
					Subheading = "Share good news from the last 7 days.<br/> One personal and one professional.",
					PageType = L10Recurrence.L10PageType.Segue,
					_Ordering = 0,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Scorecard",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.Scorecard,
					_Ordering = 1,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Rock Review",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.Rocks,
					_Ordering = 2,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "People Headlines",
					Subheading = "Share headlines about customers/clients and people in the company.<br/> Good and bad. Drop down (to the issues list) anything that needs discussion.",
					PageType = L10Recurrence.L10PageType.Headlines,
					_Ordering = 3,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "To-do List",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.Todo,
					_Ordering = 4,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 60,
					Title = "IDS",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.IDS,
					_Ordering = 5,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Conclude",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.Conclude,
					_Ordering = 6,
					AutoGen = true
				};
				#endregion
			} else if (meetingType == MeetingType.SamePage) {
				#region Same Page Meeting pages
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Check In",
					Subheading = "How are you doing? State of mind?</br> Business and personal stuff?",
					PageType = L10Recurrence.L10PageType.Empty,
					_Ordering = 0,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Build Issues List",
					Subheading = "List all of your issues, concerns, ideas and disconnects.",
					PageType = L10Recurrence.L10PageType.Empty,
					_Ordering = 1,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 50,
					Title = "IDS",
					Subheading = "IDS all of your issues.",
					PageType = L10Recurrence.L10PageType.IDS,
					_Ordering = 2,
					AutoGen = true
				};
				yield return new L10Recurrence.L10Recurrence_Page() {
					CreateTime = createTime,
					L10RecurrenceId = recurrenceId,
					Minutes = 5,
					Title = "Conclude",
					Subheading = "",
					PageType = L10Recurrence.L10PageType.Conclude,
					_Ordering = 3,
					AutoGen = true
				};
				#endregion
			}
		}

		public static async Task<L10Recurrence> CreateBlankRecurrence(UserOrganizationModel caller, long orgId, MeetingType meetingType = MeetingType.L10) {
			L10Recurrence recur;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					recur = await CreateBlankRecurrence(s, perms, orgId, meetingType);
					tx.Commit();
					s.Flush();
				}
			}
			return recur;
		}

		public static async Task<L10Recurrence> CreateBlankRecurrence(ISession s, PermissionsUtility perms, long orgId, MeetingType meetingType = MeetingType.L10) {
			L10Recurrence recur;
			var caller = perms.GetCaller();
			perms.CreateL10Recurrence(orgId);
			recur = new L10Recurrence() {
				OrganizationId = orgId,
				Pristine = true,
				VideoId = Guid.NewGuid().ToString(),
				EnableTranscription = false,
				HeadlinesId = Guid.NewGuid().ToString(),
				CountDown = true,
				CreatedById = caller.Id,
				CreateTime = DateTime.UtcNow
			};

			if (meetingType == MeetingType.SamePage) {
				recur.TeamType = L10TeamType.SamePageMeeting;
			}

			s.Save(recur);

			foreach (var page in GenerateMeetingPages(recur.Id, meetingType, recur.CreateTime)) {
				s.Save(page);
			}


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
				AccessorId = -1,
				AccessorType = PermItem.AccessType.Admins,
				ResType = PermItem.ResourceType.L10Recurrence,
				ResId = recur.Id,
				CreatorId = caller.Id,
				OrganizationId = caller.Organization.Id,
				IsArchtype = false,
			});

			await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.CreateRecurrence(ses, recur));

			return recur;
		}

		public static async Task Depristine_Unsafe(ISession s, UserOrganizationModel caller, L10Recurrence recur) {
			if (recur.Pristine == true) {
				recur.Pristine = false;
				s.Update(recur);
				await Trigger(x => x.Create(s, EventType.CreateMeeting, caller, recur, message: recur.Name + "(" + DateTime.UtcNow.Date.ToShortDateString() + ")"));
			}
		}

		public static async Task<MvcHtmlString> GetMeetingSummary(UserOrganizationModel caller, long meetingId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meetingId);

					var meeting = s.Get<L10Meeting>(meetingId);
					var completeTime = meeting.CompleteTime;

					var completedIssues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
											.Where(x => x.DeleteTime == null && x.CloseTime == completeTime && x.Recurrence.Id == meeting.L10RecurrenceId)
											.List().ToList();

					var pads = completedIssues.Select(x => x.Issue.PadId).ToList();
					var padTexts = await PadAccessor.GetHtmls(pads);

					return new MvcHtmlString((await IssuesAccessor.BuildIssuesSolvedTable(completedIssues, showDetails: true, padLookup: padTexts)).ToString());
				}
			}
		}

		public static string GetDefaultStartPage(L10Recurrence recurrence) {

			var page = recurrence._Pages.FirstOrDefault();
			if (page != null) {
				return "page-" + page.Id;
			} else {
				return "nopage";
			}
			////UNREACHABLE...
			/*var p = "segue";
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
			return p;*/
		}

		public static async Task<L10Meeting> StartMeeting(UserOrganizationModel caller, UserOrganizationModel meetingLeader, long recurrenceId, List<long> attendees, bool preview) {
			L10Recurrence recurrence;
			L10Meeting meeting;

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
						recurrence = s.Get<L10Recurrence>(recurrenceId);

						meeting = new L10Meeting {
							CreateTime = now,
							StartTime = now,
							L10RecurrenceId = recurrenceId,
							L10Recurrence = recurrence,
							OrganizationId = recurrence.OrganizationId,
							MeetingLeader = meetingLeader,
							MeetingLeaderId = meetingLeader.Id,
							Preview = preview,
						};

						s.Save(meeting);

						recurrence.MeetingInProgress = meeting.Id;
						s.Update(recurrence);

						_LoadRecurrences(s, false, false, false, false, recurrence);

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
								Completion = state,
								VtoRock = r.VtoRock,
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
					}
				}
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.StartMeeting(ses, recurrence, meeting));
					if (recurrence.TeamType == L10TeamType.LeadershipTeam)
						await Trigger(x => x.Create(s, EventType.StartLeadershipMeeting, caller, recurrence, message: recurrence.Name));
					if (recurrence.TeamType == L10TeamType.DepartmentalTeam)
						await Trigger(x => x.Create(s, EventType.StartDepartmentMeeting, caller, recurrence, message: recurrence.Name));

					tx.Commit();
					s.Flush();
				}
			}

			return meeting;
		}
		public async static Task ConcludeMeeting(UserOrganizationModel caller, long recurrenceId, List<System.Tuple<long, decimal?>> ratingValues, ConcludeSendEmail sendEmail, bool closeTodos, bool closeHeadlines, string connectionId) {
			var unsent = new List<Mail>();
			L10Recurrence recurrence = null;
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

					var headlines = GetHeadlinesForMeeting(s, perms, recurrenceId);
					if (closeHeadlines) {
						foreach (var headline in headlines) {
							if (headline.CloseTime == null) {
								headline.CloseDuringMeetingId = meeting.Id;
								headline.CloseTime = now;
							}
							s.Update(headline);
						}
					}


					//Conclude the forum
					recurrence = s.Get<L10Recurrence>(recurrenceId);
					var externalForumNumbers = s.QueryOver<ExternalUserPhone>()
											.Where(x => x.DeleteTime > now && x.ForModel.ModelId == recurrenceId && x.ForModel.ModelType == ForModel.GetModelType<L10Recurrence>())
											.List().ToList();
					if (externalForumNumbers.Any()) {
						try {
							var twilioData = Config.Twilio();
							TwilioClient.Init(twilioData.Sid, twilioData.AuthToken);

							var allMessages = new List<Task<MessageResource>>();
							foreach (var number in externalForumNumbers) {
								try {
									if (twilioData.ShouldSendText) {

										var to = new PhoneNumber(number.UserNumber);
										var from = new PhoneNumber(number.SystemNumber);

										var url = Config.BaseUrl(null, "/su?id=" + number.LookupGuid);
										var message = MessageResource.CreateAsync(to, from: from,
											body: "Thanks for participating in the " + recurrence.Name + "!\nWant a demo of Traction Tools? Click here\n" + url
										);
										allMessages.Add(message);
									}
								} catch (Exception e) {
									log.Error("Particular Forum text was not sent", e);
								}

								number.DeleteTime = now;
								s.Update(number);
							}
							await Task.WhenAll(allMessages);

						} catch (Exception e) {
							log.Error("Forum texts were not sent", e);
						}
					}

					//CONNECTIONS AUTOMATICALLY CLOSE with the DeleteTime var
					//var connectionsToClose = s.QueryOver<L10Recurrence.L10Recurrence_Connection>().Where(x => x.DeleteTime <= DateTime.UtcNow.Add(MeetingHub.PingTimeout).AddMinutes(5) && x.RecurrenceId == recurrenceId).List().ToList();
					//foreach (var c in connectionsToClose) {
					//	c.DeleteTime = now.AddMinutes(5);
					//}


					var issuesToClose = s.QueryOver<IssueModel.IssueModel_Recurrence>()
											.Where(x => x.DeleteTime == null && x.MarkedForClose && x.Recurrence.Id == recurrenceId && x.CloseTime == null)
											.List().ToList();

					foreach (var i in issuesToClose) {
						i.CloseTime = now;
						s.Update(i);
					}

					meeting.CompleteTime = now;
					meeting.TodoCompletion = todoRatio;


					s.Update(meeting);

					var ids = ratingValues.Select(x => x.Item1).ToArray();

					//Set rating for attendees
					var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
						.List().ToList();
					var raters = attendees.Where(x => ids.Any(y => y == x.User.Id));
					var raterCount = 0m;
					var raterValue = 0m;

					foreach (var a in raters) {
						a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
						s.Update(a);

						if (a.Rating != null) {
							raterCount += 1;
							raterValue += a.Rating.Value;
						}
					}

					meeting.AverageMeetingRating = new Ratio(raterValue, raterCount);
					s.Update(meeting);


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
					if (sendEmail != ConcludeSendEmail.None) {
						try {

							var todoList = s.QueryOver<TodoModel>().Where(x =>
								x.DeleteTime == null &&
								x.ForRecurrenceId == recurrenceId &&
								x.CompleteTime == null
								).List().ToList();

							//All awaitables 
							//headline.CloseDuringMeetingId = meeting.Id;

							var issuesForTable = issue_recurParents.Where(x => !x.AwaitingSolve);

							var pads = issuesForTable.Select(x => x.Issue.PadId).ToList();
							pads.AddRange(todoList.Select(x => x.PadId));
							pads.AddRange(headlines.Select(x => x.HeadlinePadId));
							var padTexts = await PadAccessor.GetHtmls(pads);

							/////
							var headlineTable = await HeadlineAccessor.BuildHeadlineTable(headlines.ToList(), "Headlines", recurrenceId, true, padTexts);

							var issueTable = await IssuesAccessor.BuildIssuesSolvedTable(issuesForTable.ToList(), "Issues Solved", recurrenceId, true, padTexts);
							var todosTable = new DefaultDictionary<long, string>(x => "");
							var hasTodos = new DefaultDictionary<long, bool>(x => false);

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

								if (personTodos.Any())
									hasTodos[personTodos.First().AccountableUserId] = true;

								var todoTable = await TodoAccessor.BuildTodoTable(personTodos.ToList(), "Outstanding To-dos", true, padLookup: padTexts);


								var output = new StringBuilder();

								output.Append(todoTable.ToString());
								output.Append("<br/>");
								todosTable[user.Id] = output.ToString();
							}


							IEnumerable<L10Meeting.L10Meeting_Attendee> sendEmailTo = new List<L10Meeting.L10Meeting_Attendee>();

							switch (sendEmail) {
								case ConcludeSendEmail.AllAttendees:
									sendEmailTo = attendees;
									break;
								case ConcludeSendEmail.AllRaters:
									sendEmailTo = raters;
									break;
								default:
									break;
							}

							foreach (var userAttendee in sendEmailTo) {
								var output = new StringBuilder();
								var user = auLu[userAttendee.User.Id];
								var email = user.GetEmail();
								var toSend = false;

								if (hasTodos[userAttendee.User.Id]) {
									toSend = true;
								}

								output.Append(todosTable[user.Id]);
								if (issuesForTable.Any()) {
									output.Append(issueTable.ToString());
									toSend = true;
								}


								if (headlines.Any()) {
									output.Append(headlineTable.ToString());
									output.Append("<br/>");
									toSend = true;
								}


								var mail = Mail.To(EmailTypes.L10Summary, email)
									.Subject(EmailStrings.MeetingSummary_Subject, recurrence.Name)
									.Body(EmailStrings.MeetingSummary_Body, user.GetName(), output.ToString(), Config.ProductName(meeting.Organization));
								if (toSend) {
									unsent.Add(mail);
								}
							}

						} catch (Exception e) {
							log.Error("Emailer issue(1):" + recurrence.Id, e);
						}
					}

					await Trigger(x => x.Create(s, EventType.ConcludeMeeting, caller, recurrence, message: recurrence.Name + "(" + DateTime.UtcNow.Date.ToShortDateString() + ")"));

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
				if (sendEmail != ConcludeSendEmail.None && unsent != null) {
					await Emailer.SendEmails(unsent);
				}
			} catch (Exception e) {
				log.Error("Emailer issue(2):" + recurrenceId, e);
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.ConcludeMeeting(ses, recurrence, meeting));
					tx.Commit();
					s.Flush();
				}
			}
		}


		public async static Task UpdateRating(UserOrganizationModel caller, List<System.Tuple<long, decimal?>> ratingValues, long meetingId, string connectionId) {

			L10Meeting meeting = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var now = DateTime.UtcNow;
					//Make sure we're unstarted
					var perms = PermissionsUtility.Create(s, caller);
					meeting = s.QueryOver<L10Meeting>().Where(t => t.Id == meetingId).SingleOrDefault();
					perms.ViewL10Meeting(meeting.Id);


					var ids = ratingValues.Select(x => x.Item1).ToArray();

					//Set rating for attendees
					var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id)
						.List().ToList();
					var raters = attendees.Where(x => ids.Any(y => y == x.User.Id));

					foreach (var a in raters) {
						a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
						s.Update(a);
					}

					Audit.L10Log(s, caller, meeting.L10RecurrenceId, "UpdateL10Rating", ForModel.Create(meeting));
					tx.Commit();
					s.Flush();
				}
			}
		}



		public static IEnumerable<L10Recurrence.L10Recurrence_Connection> GetConnected(UserOrganizationModel caller, long recurrenceId, bool load = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					var connections = s.QueryOver<L10Recurrence.L10Recurrence_Connection>().Where(x => x.DeleteTime >= DateTime.UtcNow && x.RecurrenceId == recurrenceId).List().ToList();
					if (load) {
						var userIds = connections.Select(x => x.UserId).Distinct().ToArray();
						var tiny = TinyUserAccessor.GetUsers_Unsafe(s, userIds).ToDefaultDictionary(x => x.UserOrgId, x => x, null);
						foreach (var c in connections) {
							c._User = tiny[c.UserId];
						}
					}
					return connections;
				}
			}
		}

		public static L10Meeting.L10Meeting_Connection JoinL10Meeting(UserOrganizationModel caller, long recurrenceId, string connectionId) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//var perms = PermissionsUtility.
					if (recurrenceId == -3) {
						var recurs = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null)
							.WhereRestrictionOn(x=>x.User.Id).IsIn(caller.UserIds)
							.Select(x => x.L10Recurrence.Id)
							.List<long>().ToList();
						//Hey.. this doesnt grab all visible meetings.. it should be adjusted when we know that GetVisibleL10Meetings_Tiny is optimized
						//GetVisibleL10Meetings_Tiny(s, perms, caller.Id);
						foreach (var r in recurs) {
							hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(r));
						}
						hub.Groups.Add(connectionId, MeetingHub.GenerateUserId(caller.Id));
					} else {
						new PermissionsAccessor().Permitted(caller, x => x.ViewL10Recurrence(recurrenceId));
						hub.Groups.Add(connectionId, MeetingHub.GenerateMeetingGroupId(recurrenceId));
						Audit.L10Log(s, caller, recurrenceId, "JoinL10Meeting", ForModel.Create(caller));

						//s.QueryOver<L10Recurrence.L10Recurrence_Connection>().where
#pragma warning disable CS0618 // Type or member is obsolete
						var connection = new L10Recurrence.L10Recurrence_Connection() { Id = connectionId, RecurrenceId = recurrenceId, UserId = caller.Id };
#pragma warning restore CS0618 // Type or member is obsolete

						s.SaveOrUpdate(connection);

						connection._User = TinyUser.FromUserOrganization(caller);

						var perms = PermissionsUtility.Create(s, caller);
						var currentMeeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
						if (currentMeeting != null) {
							var isAttendee = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.L10Meeting.Id == currentMeeting.Id && x.User.Id == caller.Id && x.DeleteTime == null).RowCount() > 0;
							if (!isAttendee) {
								var potentialAttendee = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == caller.Id && x.L10Recurrence.Id == recurrenceId).RowCount() > 0;
								if (potentialAttendee) {
									s.Save(new L10Meeting.L10Meeting_Attendee() {
										L10Meeting = currentMeeting,
										User = caller,
									});
								}
							}
						}

						tx.Commit();
						s.Flush();

						var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
						meetingHub.userEnterMeeting(connection);
						//?meetingHub.userEnterMeeting(caller.Id, connectionId, caller.GetName(), caller.ImageUrl(true));
					}
				}
			}

			return null;
		}
		#endregion
	}
}