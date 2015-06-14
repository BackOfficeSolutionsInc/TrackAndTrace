using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using FluentNHibernate.Utils;
using Microsoft.AspNet.SignalR;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Accessors.TodoIntegrations;

namespace RadialReview.Accessors
{
	public class TodoAccessor : BaseAccessor
	{

		public static StringBuilder BuildTodoTable(List<TodoModel> todos,string title=null)
		{
			title = title.NotNull(x => x.Trim()) ?? "To-do";
			var table = new StringBuilder();
			try
			{

				table.Append(@"<table width=""100%""  border=""0"" cellpadding=""0"" cellspacing=""0"">");
				table.Append(@"<tr><th colspan=""3"" align=""left"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;"">"+title+@"</th><th align=""right"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;"">Due Date</th></tr>");
				var i = 1;
				if (todos.Any()){
					var org = todos.FirstOrDefault().NotNull(x => x.Organization);
					var now = todos.FirstOrDefault().NotNull(x => x.Organization.ConvertFromUTC(DateTime.UtcNow).Date);
					foreach (var todo in todos.OrderBy(x => x.DueDate.Date).ThenBy(x => x.Message)){
						var color = todo.DueDate.Date <= now ? "color:#F22659;" : "color: #34AD00;";

						table.Append(@"<tr><td width=""8px""></td><td width=""1px""><b><a style=""color:#333333;text-decoration:none;"" href=""" + Config.BaseUrl(org) + @"Todo/List"">")
							.Append(i).Append(@". </a></b></td><td align=""left""><b><a style=""color:#333333;text-decoration:none;"" href=""" + Config.BaseUrl(org) + @"Todo/List"">")
							.Append(todo.Message).Append(@"</a></b></td><td  align=""right"" style=""" + color + @""">")
							.Append(todo.DueDate.ToShortDateString()).Append("</td></tr>");
						if (!String.IsNullOrWhiteSpace(todo.Details)){
							table.Append(@"<tr><td colspan=""2""></td><td><i style=""font-size:12px;"">&nbsp;&nbsp;<a style=""color:#333333;text-decoration: none;"" href=""" + Config.BaseUrl(org) + @"Todo/List"">").Append(todo.Details).Append("</a></i></td><td></td></tr>");
						}

						i++;
					}
				}
				table.Append("</table>");
			}catch (Exception e){
				log.Error(e);
			}
			return table;
		}

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

					if (todo.CreatedDuringMeetingId == -1)
						todo.CreatedDuringMeetingId = null;
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
						x => y => x.ViewUserOrganization(y, false));

					perms.ConfirmAndFix(todo,
						x => x.AccountableUserId,
						x => x.AccountableUser,
						x => y => x.ViewUserOrganization(y, false));

					var r = s.Get<L10Recurrence>(recurrenceId);
					ExternalTodoAccessor.AddLink(s, perms, ForModel.Create(r), todo.AccountableUserId, todo);

					s.Save(todo);

					tx.Commit();
					s.Flush();
					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
					meetingHub.appendTodo(".todo-list", TodoData.FromTodo(todo));

					var updates = new AngularRecurrence(recurrenceId);
					updates.Todos = new List<AngularTodo>() { new AngularTodo(todo) };
					meetingHub.update(updates);
				}
			}
		}

		public static TodoModel GetTodo(UserOrganizationModel caller, long todoId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewTodo(todoId);
					var found = s.Get<TodoModel>(todoId);
					var a = found.AccountableUser.GetName();
					var b = found.AccountableUser.ImageUrl(true);
					var c = found.GetIssueMessage();
					var d = found.GetIssueDetails();

					return found;
				}
			}
		}

		public static List<TodoModel> GetTodosForUser(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).Self(userId);

					var found = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.AccountableUserId == userId).List().ToList();
					foreach (var f in found)
					{
						var a = f.ForRecurrence.Id;
						var b = f.AccountableUser.GetName();
						var c = f.AccountableUser.ImageUrl(true,ImageSize._32);
						var d = f.CreatedDuringMeeting.Id;
					}
					return found;
				}
			}
		}
	}
}