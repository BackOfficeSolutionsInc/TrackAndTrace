﻿using System;
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
		#region Todos	
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
		public static Ratio GetTodoCompletion(TodoModel todo, DateTime weekStart, DateTime weekEnd, DateTime? meetingStart = null) {
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

			// DueDate < WeekStart < CompleteTime
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
		public static Ratio GetTodoCompletion(List<TodoModel> todos, DateTime weekStart, DateTime weekEnd, DateTime? meetingStart = null) {
			var ratio = new Ratio(0, 0);
			foreach (var t in todos) {
				var c = GetTodoCompletion(t, weekStart, weekEnd, meetingStart);
				ratio.Merge(c);
			}
			return ratio;
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
		public static List<TodoModel> GetAllTodosForRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeClosed = true, DateRange range = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					return GetAllTodosForRecurrence(s, perms, recurrenceId, includeClosed, range);
				}
			}
		}


		public static void UpdateTodoOrder(UserOrganizationModel caller, long recurrenceId, L10Controller.UpdateTodoVM model) {
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

		#region Deleted

		//[Untested("Hooks")]
		//[Obsolete("Do not use", true)]
		//public static async Task UpdateTodo(UserOrganizationModel caller, long todoId, string message = null, DateTime? dueDate = null, long? accountableUser = null, bool? complete = null, string connectionId = null, bool duringMeeting = false, bool? delete = null) {
		//	throw new NotImplementedException();
		//using (var s = HibernateSession.GetCurrentSession()) {
		//	using (var tx = s.BeginTransaction()) {
		//		var todo = s.Get<TodoModel>(todoId);
		//		if (todo == null)
		//			throw new PermissionsException("To-do does not exist.");
		//		PermissionsUtility perm = PermissionsUtility.Create(s, caller);
		//		var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
		//		dynamic group;
		//		if (todo.TodoType == TodoType.Recurrence) {
		//			if (todo.ForRecurrenceId == null || todo.ForRecurrenceId == 0)
		//				throw new PermissionsException("Meeting does not exist.");
		//			perm.EditTodo(todoId);//EditL10Recurrence(todo.ForRecurrenceId.Value);

		//			group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(todo.ForRecurrenceId.Value), connectionId);
		//		} else if (todo.TodoType == TodoType.Personal) {
		//			perm.EditTodo(todoId);
		//			group = hub.Clients.Group(MeetingHub.GenerateUserId(todo.AccountableUserId), connectionId);
		//		} else {
		//			throw new PermissionsException("unhandled TodoType");
		//		}

		//		var updatesText = new List<string>();

		//		bool IsTodoUpdate = false;
		//		if (message != null && todo.Message != message) {
		//			SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateTodoMessage(todo.Id));
		//			todo.Message = message;
		//			group.updateTodoMessage(todoId, message);
		//			updatesText.Add("Message: " + todo.Message);
		//			IsTodoUpdate = true;
		//		}
		//		if (details != null && todo.Details != details) {
		//			SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateTodoDetails(todo.Id));
		//			todo.Details = details;
		//			group.updateTodoDetails(todoId, details);
		//			updatesText.Add("Details: " + details);
		//		}
		//		if (dueDate != null && todo.DueDate != dueDate.Value) {
		//			todo.DueDate = dueDate.Value;
		//			group.updateTodoDueDate(todoId, dueDate.Value.ToJavascriptMilliseconds());
		//			updatesText.Add("Due-Date: " + dueDate.Value.ToShortDateString());
		//			IsTodoUpdate = true;
		//		}
		//		if (accountableUser != null && todo.AccountableUserId != accountableUser.Value && accountableUser > 0) {
		//			todo.AccountableUserId = accountableUser.Value;
		//			todo.AccountableUser = s.Get<UserOrganizationModel>(accountableUser.Value);
		//			group.updateTodoAccountableUser(todoId, accountableUser.Value, todo.AccountableUser.GetName(), todo.AccountableUser.ImageUrl(true, ImageSize._32));
		//			updatesText.Add("Accountable: " + todo.AccountableUser.GetName());
		//			IsTodoUpdate = true;
		//		}

		//		bool IsTodoStatusUpdated = false;
		//		if (complete != null) {
		//			IsTodoStatusUpdated = true;
		//			SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateTodoCompletion(todo.Id));
		//			var now = DateTime.UtcNow;
		//			if (complete.Value && todo.CompleteTime == null) {
		//				if (duringMeeting && todo.ForRecurrenceId != null) {
		//					try {
		//						var meetingId = _GetCurrentL10Meeting(s, perm, todo.ForRecurrenceId.Value, true, false, false).NotNull(x => x.Id);
		//						if (meetingId != 0)
		//							todo.CompleteDuringMeetingId = meetingId;
		//					} catch (Exception) {

		//					}
		//				}

		//				todo.CompleteTime = now;
		//				s.Update(todo);
		//				updatesText.Add("Marked Complete");
		//				new Cache().InvalidateForUser(todo.AccountableUser, CacheKeys.UNSTARTED_TASKS);
		//			} else if (!complete.Value && todo.CompleteTime != null) {
		//				todo.CompleteTime = null;
		//				todo.CompleteDuringMeetingId = null;
		//				s.Update(todo);
		//				updatesText.Add("Marked Incomplete");
		//				new Cache().InvalidateForUser(todo.AccountableUser, CacheKeys.UNSTARTED_TASKS);
		//			}
		//			group.updateTodoCompletion(todoId, complete);
		//		}

		//		_ProcessDeleted(s, todo, delete);

		//		group.update(new AngularUpdate() { new AngularTodo(todo) });


		//		if (todo.ForRecurrenceId.HasValue) {
		//			var updatedText = "Updated To-Do \"" + todo.Message + "\" \n " + String.Join("\n", updatesText);
		//			Audit.L10Log(s, caller, todo.ForRecurrenceId.Value, "UpdateTodo", ForModel.Create(todo), updatedText);
		//		}

		//		// Webhook event trigger
		//		if (IsTodoUpdate) {
		//			await HooksRegistry.Each<ITodoHook>((ses, x) => x.UpdateMessage(ses, todo));
		//		}

		//		// Webhook register Marking complete for TODO
		//		if (IsTodoStatusUpdated) {
		//			await HooksRegistry.Each<ITodoHook>((ses, x) => x.UpdateCompletion(ses, todo));
		//		}

		//		tx.Commit();
		//		s.Flush();
		//	}
		//}
		//}
		//[Untested("Hooks")]
		//[Obsolete("Use TodoAccessor", true)]
		//public static async Task CompleteTodo(ISession s, PermissionsUtility perm, RealTimeUtility rt, long todoModel) {
		//	throw new NotImplementedException();
		//	//perm.EditTodo(todoModel);
		//	//var todo = s.Get<TodoModel>(todoModel);
		//	//if (todo.CompleteTime != null)
		//	//	throw new PermissionsException("To-do already deleted.");
		//	////if (todo.ForRecurrence.Id != recurrenceId)
		//	////    throw new PermissionsException("You cannot edit this meeting.");
		//	//todo.CompleteTime = DateTime.UtcNow;
		//	//s.Update(todo);

		//	//var recur = new AngularRecurrence(todo.ForRecurrence.Id);
		//	//recur.Todos = AngularList.CreateFrom(AngularListType.Remove, new AngularTodo(todo));
		//	//rt.UpdateRecurrences(todo.ForRecurrence.Id).Update(recur);

		//	//// Webhook register Marking complete for TODO
		//	////? added await
		//	//await HooksRegistry.Each<ITodoHook>((ses, x) => x.UpdateCompletion(ses, todo));
		//}


		//public static List<TodoModel> GetPreviousTodos(UserOrganizationModel caller, long recurrenceId) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

		//			var todos = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.ForRecurrenceId == recurrenceId).List().ToList();

		//			foreach (var t in todos) {
		//				var a = t.AccountableUser.GetName();
		//			}
		//			return todos;
		//		}
		//	}
		//}
		//[Obsolete("Do not use", true)]
		//public static Ratio TodoCompletion(List<TodoModel> todos, DateTime week, DateTime now, bool dontUse) {
		//	var ratio = new Ratio(0, 0);
		//	foreach (var t in todos) {
		//		if (t.CreateTime < week.AddDays(-7)) {
		//			if (t.CompleteTime == null || week < t.CompleteTime.Value) {
		//				ratio.Add(0, 1);
		//			} else if (week.AddDays(-7) <= t.CompleteTime.Value.StartOfWeek(DayOfWeek.Sunday) && t.CompleteTime.Value.StartOfWeek(DayOfWeek.Sunday) < week) {
		//				ratio.Add(1, 1);
		//			}
		//		}
		//		/*if (week.AddDays(-7) <= t.DueDate.StartOfWeek(DayOfWeek.Sunday) && t.DueDate.StartOfWeek(DayOfWeek.Sunday) < week)
		//              {

		//                  if (currentWeek){
		//                      //do something different...
		//                  }
		//                  else{

		//                      if (t.CompleteTime == null){
		//                          ratio.Add(0, 1);
		//                      }else{
		//                          if (t.CompleteTime.Value.StartOfWeek(DayOfWeek.Sunday) <= week)
		//                              ratio.Add(1, 1);
		//                          else
		//                              ratio.Add(0, 1);
		//                      }
		//                  }
		//              }*/
		//	}
		//	return ratio;


		//}		
		//public static void GetVisibleTodos(UserOrganizationModel caller, long[] forUsers, bool includeComplete) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			var p = PermissionsUtility.Create(s, caller);
		//			//forUsers.Distinct().ForEach(x => p.ManagesUserOrganizationOrSelf(x));
		//			//s.QueryOver<TodoModel>().Where(x=>x.)
		//			throw new Exception("todo");
		//		}
		//	}
		//}
		#endregion

		#endregion
	}
}