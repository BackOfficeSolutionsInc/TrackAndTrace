using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Accessors.TodoIntegrations;

namespace RadialReview.Accessors
{
	public class TodoAccessor
	{

		public static void CreateTodo(UserOrganizationModel caller, long recurrenceId, TodoModel todo)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					//.ViewL10Recurrence(recurrenceId); //Tested below
					//perms.ViewL10Recurrence(recurrenceId);

					if (todo.Id != 0)
						throw new PermissionsException("Id was not zero");


					perms.ConfirmAndFix(todo,
						x => x.CreatedDuringMeetingId,
						x => x.CreatedDuringMeeting,
						x => x.ViewL10Meeting);

					perms.ConfirmAndFix(todo,
						x => x.OrganizationId,
						x => x.Organization,
						x => x.ViewOrganization);

					perms.ConfirmAndFix(todo,
						x => x.ForRecurrenceId,
						x => x.ForRecurrence,
						x => x.ViewL10Recurrence);

					perms.ConfirmAndFix(todo,
						x => x.CreatedById,
						x => x.CreatedBy,
						x => y => x.ManagesUserOrganization(y, false));

					perms.ConfirmAndFix(todo,
						x => x.AccountableUserId,
						x => x.AccountableUser,
						x => y => x.ViewUserOrganization(y, false));

					var r = s.Get<L10Recurrence>(recurrenceId);
					ExternalTodoAccessor.AddLink(s,perms,ForModel.Create(r),todo.AccountableUserId,todo);

					s.Save(todo);

					tx.Commit();
					s.Flush();
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
					meetingHub.appendTodo(".todo-list", TodoData.FromTodo(todo));
				}
			}
		}

		public static TodoModel GetTodo(UserOrganizationModel caller, long todoId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewTodo(todoId);
					var found = s.Get<TodoModel>(todoId);
					var a=found.AccountableUser.GetName();
					var b=found.AccountableUser.ImageUrl(true);
					var c = found.GetIssueMessage();
					var d = found.GetIssueDetails();

					return found;
				}
			}
		}
	}
}