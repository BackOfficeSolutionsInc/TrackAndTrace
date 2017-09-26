using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FluentNHibernate.Utils;
using Microsoft.AspNet.SignalR;
using NHibernate.Hql.Ast.ANTLR;
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
using RadialReview.Utilities.Encrypt;
using RadialReview.Controllers;
using NHibernate;
using RadialReview.Utilities.DataTypes;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using static RadialReview.Controllers.DashboardController;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Dashboard;
using NHibernate.Criterion;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Rocks;
using RadialReview.Models.Askables;

namespace RadialReview.Accessors {
    public class TodoAccessor : BaseAccessor {

        public static string _SharedSecretTodoPrefix(long userId) {
            return "402F5DE7-DB3C-40D3-B634-42EF5E7D9118+" + userId;
        }


        public static async Task<StringBuilder> BuildTodoTable(List<TodoModel> todos, string title = null, bool showDetails = false, Dictionary<string, HtmlString> padLookup = null) {
            title = title.NotNull(x => x.Trim()) ?? "To-do";
            var table = new StringBuilder();
            try {

                table.Append(@"<table width=""100%""  border=""0"" cellpadding=""0"" cellspacing=""0"">");
                table.Append(@"<tr><th colspan=""3"" align=""left"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;"">" + title + @"</th><th align=""right"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;width: 80px;"">Due Date</th></tr>");
                var i = 1;
                if (todos.Any()) {
                    var org = todos.FirstOrDefault().NotNull(x => x.Organization);
                    var now = todos.FirstOrDefault().NotNull(x => x.Organization.ConvertFromUTC(DateTime.UtcNow).Date);
                    var format = org.NotNull(x => x.Settings.NotNull(y => y.GetDateFormat())) ?? "MM-dd-yyyy";
                    foreach (var todo in todos.OrderBy(x => x.DueDate.Date).ThenBy(x => x.Message)) {
                        var color = todo.DueDate.Date <= now ? "color:#F22659;" : "color: #34AD00;";
                        var completionIcon = Config.BaseUrl(org) + @"Image/TodoCompletion?id=" + HttpUtility.UrlEncode(Crypto.EncryptStringAES("" + todo.Id, _SharedSecretTodoPrefix(todo.AccountableUserId))) + "&userId=" + todo.AccountableUserId;
                        var duedate = todo.DueDate;
                        if (org != null) {
                            duedate = org.ConvertFromUTC(todo.DueDate);
                        }

                        table.Append(@"<tr><td width=""16px"" valign=""top"" style=""padding: 3px 0 0 0;""><img src='").Append(completionIcon).Append("' width='15' height='15'/>").Append(@"</td><td width=""1px"" style=""vertical-align: top;""><b><a style=""color:#333333;text-decoration:none;"" href=""" + Config.BaseUrl(org) + @"Todo/List"">")
                            .Append(i).Append(@". </a></b></td><td align=""left""><b><a style=""color:#333333;text-decoration:none;"" href=""" + Config.BaseUrl(org) + @"Todo/List?todo=" + todo.Id + @""">")
                            .Append(todo.Message).Append(@"</a></b></td><td  align=""right"" valign=""top"" style=""" + color + @""">")
                            .Append(duedate.ToString(format)).Append("</td></tr>");

                        if (showDetails) {

                            HtmlString details = null;
                            if (padLookup == null || !padLookup.ContainsKey(todo.PadId)) {
                                details = await PadAccessor.GetHtml(todo.PadId);
                            } else {
                                details = padLookup[todo.PadId];
                            }

                            //var details = await PadAccessor.GetHtml(todo.PadId);

                            if (!String.IsNullOrWhiteSpace(details.ToHtmlString())) {
                                table.Append(@"<tr><td colspan=""2""></td><td><i style=""font-size:12px;"">&nbsp;&nbsp;<a style=""color:#333333;text-decoration: none;"" href=""" + Config.BaseUrl(org) + @"Todo/List"">").Append(details.ToHtmlString()).Append("</a></i></td><td></td></tr>");
                            }
                        }

                        i++;
                    }
                }
            } catch (Exception e) {
                log.Error(e);
            }
            table.Append("</table>");
            return table;
        }

        public static async Task<bool> CreateTodo(ISession s, PermissionsUtility perms, long recurrenceId, TodoModel todo) {
            if (todo.Id != 0)
                throw new PermissionsException("Id was not zero");

            if (todo.CreatedDuringMeetingId == -1)
                todo.CreatedDuringMeetingId = null;
            perms.ConfirmAndFix(todo,
                x => x.CreatedDuringMeetingId,
                x => x.CreatedDuringMeeting,
                x => x.ViewL10Meeting);

            if (todo.OrganizationId == 0 && todo.Organization == null)
                todo.OrganizationId = perms.GetCaller().Organization.Id;
            perms.ConfirmAndFix(todo,
                x => x.OrganizationId,
                x => x.Organization,
                x => x.ViewOrganization);

            perms.ConfirmAndFix(todo,
                x => x.ForRecurrenceId,
                x => x.ForRecurrence,
                x => x.EditL10Recurrence);

            if ((todo.ForRecurrenceId == null || todo.ForRecurrence == null) && todo.TodoType == TodoType.Recurrence)
                throw new PermissionsException("Recurrence Id is required to create a meeting todo.");


            if (todo.CreatedById == 0 && todo.CreatedBy == null)
                todo.CreatedById = perms.GetCaller().Id;
            perms.ConfirmAndFix(todo,
                x => x.CreatedById,
                x => x.CreatedBy,
                x => y => x.ViewUserOrganization(y, false));

            if (todo.AccountableUserId == 0 && todo.AccountableUser == null)
                todo.AccountableUserId = perms.GetCaller().Id;
            perms.ConfirmAndFix(todo,
                x => x.AccountableUserId,
                x => x.AccountableUser,
                x => y => x.ViewUserOrganization(y, false));

            L10Recurrence r = null;
            if (recurrenceId > 0) {
                r = s.Get<L10Recurrence>(recurrenceId);
                //r.Pristine = false;
                await L10Accessor.Depristine_Unsafe(s, perms.GetCaller(), r);
                s.Update(r);
            }
            if (todo.TodoType == TodoType.Recurrence)
                ExternalTodoAccessor.AddLink(s, perms, ForModel.Create(r), todo.AccountableUserId, todo);
            else if (todo.TodoType == TodoType.Personal)
                ExternalTodoAccessor.AddLink(s, perms, ForModel.Create(todo.AccountableUser), todo.AccountableUserId, todo);
            else
                throw new PermissionsException("unhandled TodoType");

            if (String.IsNullOrWhiteSpace(todo.PadId))
                todo.PadId = Guid.NewGuid().ToString();

            if (!string.IsNullOrWhiteSpace(todo.Details))
                await PadAccessor.CreatePad(todo.PadId, todo.Details);
            if (recurrenceId > 0) {
                todo.ForRecurrenceId = recurrenceId;
                todo.ForRecurrence = r;
            }

            s.Save(todo);
            //if (todo.DueDate == todo.DueDate.Date)
            //    todo.DueDate = todo.DueDate.Date.AddDays(1).AddMinutes(-todo.Organization.GetTimezoneOffset()).AddMilliseconds(-1);
            todo.Ordering = -todo.Id;
            s.Update(todo);

            await HooksRegistry.Each<ITodoHook>(x => x.CreateTodo(s, todo));

            if (todo.TodoType == TodoType.Personal) {

                var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                var userMeetingHub = hub.Clients.Group(MeetingHub.GenerateUserId(todo.AccountableUserId));
                var todoData = TodoData.FromTodo(todo);
                userMeetingHub.appendTodo(".todo-list", todoData);
                var updates = new AngularRecurrence(recurrenceId);
                updates.Todos = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(todo));
                userMeetingHub.update(updates);
            }

            if (recurrenceId > 0) {
                var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
                var todoData = TodoData.FromTodo(todo);

                if (todo.CreatedDuringMeetingId != null)
                    todoData.isNew = true;
                meetingHub.appendTodo(".todo-list", todoData);

                var message = "Created to-do.";

                var showWhoCreatedDetails = true;
                if (showWhoCreatedDetails) {
                    try {
                        if (perms.GetCaller() != null && perms.GetCaller().GetFirstName() != null) {
                            message = perms.GetCaller().GetFirstName() + " created a to-do.";
                        }
                    } catch (Exception) {
                    }
                }

                meetingHub.showAlert(message, 1500);

                var updates = new AngularRecurrence(recurrenceId);
                updates.Todos = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(todo));
                updates.Focus = "[data-todo='" + todo.Id + "'] input:visible:first";
                meetingHub.update(updates);

                #region Add to L10 Tile
                try {
                    Dashboard dashboardAlias = null;
                    var dashs = s.QueryOver<TileModel>()
                        .JoinAlias(x => x.Dashboard, () => dashboardAlias)
                        .Where(x => x.DeleteTime == null && x.Type == TileType.Url && x.DataUrl == "/TileData/L10Todos/" + recurrenceId)
                        .Select(x => x.Dashboard.Id, x => x.Id, x => dashboardAlias.ForUser.Id)
                        .List<object[]>()
                        .Select(x => new {
                            DashboardId = (long)x[0],
                            TileId = (long)x[1],
                            UserId = (string)x[2]
                        }).ToList();

                    if (dashs.Any()) {
                        //Only do if there are tiles
                        var dashUserIds = dashs.Select(x => x.UserId).Distinct().ToArray();
                        var dashUsers = s.QueryOver<UserOrganizationModel>()
                                            .Where(x => x.DeleteTime == null)
                                            .WhereRestrictionOn(x => x.User.Id)
                                            .IsIn(dashUserIds)
                                            .List().ToList();

                        var canView = new DefaultDictionary<string, bool>(x => false);
                        foreach (var u in dashUsers) {
                            if (canView[u.User.Id] == false) {
                                try {
                                    PermissionsUtility.Create(s, u).ViewL10Recurrence(recurrenceId);
                                    canView[u.User.Id] = true;
                                } catch (PermissionsException) {
                                }
                            }
                        }

                        foreach (var d in dashs) {
                            if (canView[d.UserId]) {
                                var tile = new AngularTileId<IEnumerable<AngularTodo>>(d.TileId, recurrenceId, null) {
                                    Contents = AngularList.Create(AngularListType.Add, new[] { new AngularTodo(todo) })
                                };
                                meetingHub.update(new AngularUpdate() { tile });
                            }
                        }
                    }
                } catch (Exception e) {
                    //Special stuff,
                    log.Error(e);
                }
                #endregion
                Audit.L10Log(s, perms.GetCaller(), recurrenceId, "CreateTodo", ForModel.Create(todo), todo.NotNull(x => x.Message));
            }

            return true;
        }

        public static async Task<bool> CreateTodo(UserOrganizationModel caller, long recurrenceId, TodoModel todo) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    //.ViewL10Recurrence(recurrenceId); //Tested below
                    //perms.ViewL10Recurrence(recurrenceId);
                    var created = await CreateTodo(s, perms, recurrenceId, todo);

                    tx.Commit();
                    s.Flush();

                    return created;
                }
            }
        }

        public static TodoModel GetTodo(UserOrganizationModel caller, long todoId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewTodo(todoId);
                    var found = s.Get<TodoModel>(todoId);
                    var a = found.AccountableUser.GetName();
                    var b = found.AccountableUser.ImageUrl(true);
                    var c = found.NotNull(x => x.GetIssueMessage());
                    var d = found.NotNull(x => x.GetIssueDetails());

                    return found;
                }
            }
        }

        public static List<AngularTodo> GetMyTodosAndMilestones(UserOrganizationModel caller, long userId, bool excludeCompleteDuringMeeting = false, DateRange range = null,bool includeTodos=true,bool includeMilestones=true) {

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.Self(userId);

                    var allSelf = s.QueryOver<UserOrganizationModel>()
                        .Where(x => x.DeleteTime == null && x.User.Id == caller.User.Id)
                        .Select(x => x.Id)
                        .List<long>().ToArray();
                    return GetTodosForUsers_Unsafe(s, allSelf, excludeCompleteDuringMeeting, range,includeTodos,includeMilestones);
                }
            }
        }


        public static List<AngularTodo> GetTodosForUser(UserOrganizationModel caller, long userId, bool excludeCompleteDuringMeeting = false, DateRange range = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ManagesUserOrganizationOrSelf(userId);
                    var found = GetTodosForUsers_Unsafe(s, new[] { userId }, excludeCompleteDuringMeeting, range);
                    return found;
                }
            }
        }

        private static List<AngularTodo> GetTodosForUsers_Unsafe(ISession s, long[] userIds, bool excludeCompleteDuringMeeting, DateRange range, bool includeTodos = true, bool includeMilestones = false) {
            // List<TodoModel> found;
            var weekAgo = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday).AddDays(-7);
            IEnumerable<TodoModel> todos = null;
            if (includeTodos) {
                var q = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null)
                    .WhereRestrictionOn(x => x.AccountableUserId)
                    .IsIn(userIds);
                if (excludeCompleteDuringMeeting)
                    q = q.Where(x => ((x.CompleteTime != null && x.CompleteTime > weekAgo && x.CompleteDuringMeetingId == null) || x.CompleteTime == null));
                if (range != null)
                    q = q.Where(x => x.CompleteTime == null || (x.CompleteTime != null && x.CompleteTime >= range.StartTime && x.CompleteTime <= range.EndTime));
                todos = q.Future();
            }

            //Add milestones
            var milestones = new List<Milestone>();
            var rockName = new Dictionary<long,string>();
            var rockOwnerLookup = new Dictionary<long, long>();
            if (includeMilestones) {
                var rockAndOwnerIds = s.QueryOver<RockModel>()
                    .Where(x => x.DeleteTime == null)
                    .WhereRestrictionOn(x => x.AccountableUser.Id).IsIn(userIds)
                    .Select(x => x.Id, x => x.AccountableUser.Id, x=>x.Rock)
                    .Future<object[]>()
                    .Select(x => new {
                        RockId = (long)x[0],
                        UserId = (long)x[1],
                        Name = (string)x[2]
                    }).ToList();

                var rockIds = rockAndOwnerIds.Select(x => x.RockId).ToArray();
                rockOwnerLookup = rockAndOwnerIds.ToDictionary(x => x.RockId, x => x.UserId);
                rockName = rockAndOwnerIds.ToDictionary(x => x.RockId, x => x.Name);
                var mq = s.QueryOver<Milestone>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.RockId).IsIn(rockIds);
                if (range != null) {
                    mq = mq.Where(x => x.CompleteTime == null || (x.CompleteTime != null && x.CompleteTime >= range.StartTime && x.CompleteTime <= range.EndTime));
                }
                milestones = mq.List().ToList();
            }

            var angular = new List<AngularTodo>();

            var ownerLookup = new DefaultDictionary<long, UserOrganizationModel>(x => {
                var user = s.Get<UserOrganizationModel>(x);
                var a = user.GetName();
                var b = user.ImageUrl(true, ImageSize._32);
                return user;
            });

            if (includeTodos) {
                foreach (var f in todos) {
                    var a = f.ForRecurrence.NotNull(x => x.Id);
                    var b = f.AccountableUser.NotNull(x => x.GetName());
                    var c = f.AccountableUser.NotNull(x => x.ImageUrl(true, ImageSize._32));
                    var d = f.CreatedDuringMeeting.NotNull(x => x.Id);
                    var e = f.ForRecurrence.NotNull(x => x.Name);
                }
                //Populate dictionary
                foreach (var u in todos.Select(x => x.AccountableUser)) {
                    ownerLookup[u.Id] = u;
                }
                angular.AddRange(todos.Select(x => new AngularTodo(x)));
            }

            if (includeMilestones) {
                angular.AddRange(milestones.Select(milestone => {
                    var rockId = milestone.RockId;
                    var ownerId = rockOwnerLookup[rockId];
                    var owner = ownerLookup[ownerId];
                    var rock = rockName[rockId];
                    return new AngularTodo(milestone, owner, origin: rock);
                }));
            }
            return angular;
        }


        public static Csv Listing(UserOrganizationModel caller, long organizationId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    // var p = s.Get<PeriodModel>(period);

                    PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

                    var csv = new Csv();

                    var todos = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId).List().ToList();
                    foreach (var t in todos) {
                        csv.Add("" + t.Id, "Owner", t.AccountableUser.NotNull(x => x.GetName()));
                        csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
                        csv.Add("" + t.Id, "Due Date", t.DueDate.ToShortDateString());
                        var time = "";
                        if (t.CompleteTime != null)
                            time = t.CompleteTime.Value.ToShortDateString();
                        csv.Add("" + t.Id, "Completed", time);
                        csv.Add("" + t.Id, "To-Do", "" + t.Message);

                        //if (false /*&& includeDetails*/) {
                        //	var padDetails = await PadAccessor.GetText(t.PadId);
                        //	csv.Add("" + t.Id, "Details", "" + padDetails);
                        //}
                    }

                    csv.SetTitle("Todos");
                    return csv;
                }
            }
        }

        public static TodoModel MarkComplete(UserOrganizationModel caller, long todoId, DateTime completeTime) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perm = PermissionsUtility.Create(s, caller).EditTodo(todoId);
                    var found = s.Get<TodoModel>(todoId);

                    found.CompleteTime = completeTime;
                    s.Update(found);

                    tx.Commit();
                    s.Flush();
                    return found;
                }
            }
        }

        public static List<TodoModel> GetRecurrenceTodos(UserOrganizationModel caller, long recurrenceId, bool excludeCompleteDuringMeeting = false, DateRange range = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    return s.QueryOver<TodoModel>()
                        .Where(x => x.DeleteTime == null
                        && x.ForRecurrenceId == recurrenceId).List().ToList();
                }
            }
        }
    }
}