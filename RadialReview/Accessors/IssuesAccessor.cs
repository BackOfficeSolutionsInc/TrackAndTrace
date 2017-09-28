using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Base;
using RadialReview.Utilities.DataTypes;
using System.Text;
using System.Web;
using RadialReview.Utilities.RealTime;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Todo;
using SpreadsheetLight;

namespace RadialReview.Accessors {
    public class IssuesAccessor : BaseAccessor {


        public static async Task<StringBuilder> BuildIssuesSolvedTable(List<IssueModel.IssueModel_Recurrence> issues, string title = null, long? recurrenceId = null, bool showDetails = false, Dictionary<string, HtmlString> padLookup = null) {
            title = title.NotNull(x => x.Trim()) ?? "Issues";
            var table = new StringBuilder();
            try {

                table.Append(@"<table width=""100%""  border=""0"" cellpadding=""0"" cellspacing=""0"">");
                table.Append(@"<tr><th colspan=""2"" align=""left"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;"">" + title + @"</th></tr>");
                var i = 1;
                if (issues.Any()) {
                    var org = issues.FirstOrDefault().NotNull(x => x.Issue.Organization);
                    var now = issues.FirstOrDefault().NotNull(x => x.Issue.Organization.ConvertFromUTC(DateTime.UtcNow).Date);
                    var format = org.NotNull(x => x.Settings.NotNull(y => y.GetDateFormat())) ?? "MM-dd-yyyy";
                    foreach (var issue in issues.OrderBy(x => x.CloseTime)) {
                        var url = "#";
                        if (recurrenceId != null)
                            url = Config.BaseUrl(org) + @"L10/Details/" + recurrenceId + "#/Issues";

                        table.Append(@" <tr><td width=""1px"" style=""vertical-align: top;""><b><a style=""color:#333333;text-decoration:none;"" href=""" + url + @""">")
                            .Append(i).Append(@". </a></b></td><td align=""left""><b><a style=""color:#333333;text-decoration:none;"" href=""" + url + @""">")
                            .Append(issue.Issue.Message).Append(@"</a></b></td></tr>");

                        if (showDetails) {
                            HtmlString details = null;
                            if (padLookup == null || !padLookup.ContainsKey(issue.Issue.PadId)) {
                                details = await PadAccessor.GetHtml(issue.Issue.PadId);
                            } else {
                                details = padLookup[issue.Issue.PadId];
                            }

                            if (!String.IsNullOrWhiteSpace(details.ToHtmlString())) {
                                table.Append(@"<tr><td></td><td><i style=""font-size:12px;"">&nbsp;&nbsp;<a style=""color:#333333;text-decoration: none;"" href=""" + url + @""">").Append(details.ToHtmlString()).Append("</a></i></td></tr>");
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

        public class IssueOutput {
            public IssueModel IssueModel { get; set; }
            public IssueModel.IssueModel_Recurrence IssueRecurrenceModel { get; set; }
        }

		[Untested("hook")]
        public static async Task<IssueOutput> CreateIssue(ISession s, PermissionsUtility perms, long recurrenceId, long ownerId, IssueModel issue) {
            var o = new IssueOutput();
            perms.EditL10Recurrence(recurrenceId);
            //perms.ViewL10Recurrence(recurrenceId);

            if (issue.Id != 0)
                throw new PermissionsException("Id was not zero");

            perms.ViewUserOrganization(ownerId, false);

            if (issue.CreatedDuringMeetingId == -1)
                issue.CreatedDuringMeetingId = null;
            perms.ConfirmAndFix(issue,
                x => x.CreatedDuringMeetingId,
                x => x.CreatedDuringMeeting,
                x => x.ViewL10Meeting);

            if (issue.OrganizationId == 0 && issue.Organization == null)
                issue.OrganizationId = perms.GetCaller().Organization.Id;
            perms.ConfirmAndFix(issue,
                x => x.OrganizationId,
                x => x.Organization,
                x => x.ViewOrganization);

            if (issue.CreatedById == 0 && issue.CreatedBy == null)
                issue.CreatedById = perms.GetCaller().Id;
            perms.ConfirmAndFix(issue,
                x => x.CreatedById,
                x => x.CreatedBy,
                x => y => x.ViewUserOrganization(y, false));
            /*if (issue.CreatedDuringMeetingId != null)
                issue.CreatedDuringMeeting = s.Get<L10Meeting>(issue.CreatedDuringMeetingId);
            issue.MeetingRecurrence = s.Get<L10Recurrence>(issue.MeetingRecurrenceId);
            issue.CreatedBy = s.Get<UserOrganizationModel>(issue.CreatedById);
            */

            if (String.IsNullOrWhiteSpace(issue.PadId))
                issue.PadId = Guid.NewGuid().ToString();

            if (!string.IsNullOrWhiteSpace(issue.Description))
                await PadAccessor.CreatePad(issue.PadId, issue.Description);


            s.Save(issue);
            o.IssueModel = issue;
            var r = s.Get<L10Recurrence>(recurrenceId);

            // r.Pristine = false;
            await L10Accessor.Depristine_Unsafe(s, perms.GetCaller(), r);
            s.Update(r);

            var recur = new IssueModel.IssueModel_Recurrence() {
                CopiedFrom = null,
                Issue = issue,
                CreatedBy = issue.CreatedBy,
                Recurrence = r,
                CreateTime = issue.CreateTime,
                Owner = s.Load<UserOrganizationModel>(ownerId),
                Priority = issue._Priority

            };
            s.Save(recur);
            o.IssueRecurrenceModel = recur;
            if (r.OrderIssueBy == "data-priority") {
                var order = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                    .Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null && x.CloseTime == null && x.Priority > issue._Priority && x.ParentRecurrenceIssue == null)
                    .Select(x => x.Ordering).List<long?>().Where(x => x != null).ToList();
                var max = -1L;
                if (order.Any())
                    max = order.Max() ?? -1;
                max += 1;
                recur.Ordering = max;
                s.Update(recur);
            }
            if (r.OrderIssueBy == "data-rank") {
                var order = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                    .Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null && x.CloseTime == null && x.Rank > issue._Rank && x.ParentRecurrenceIssue == null)
                    .Select(x => x.Ordering).List<long?>().Where(x => x != null).ToList();
                var max = -1L;
                if (order.Any())
                    max = order.Max() ?? -1;
                max += 1;
                recur.Ordering = max;
                s.Update(recur);
            }
            var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
            var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));

            meetingHub.appendIssue(".issues-list", IssuesData.FromIssueRecurrence(recur), r.OrderIssueBy);
            var message = "Created issue.";
            var showWhoCreatedDetails = true;
            if (showWhoCreatedDetails) {
                try {
                    if (perms.GetCaller() != null && perms.GetCaller().GetFirstName() != null) {
                        message = perms.GetCaller().GetFirstName() + " created an issue.";
                    }
                } catch (Exception) {
                }
            }

            meetingHub.showAlert(message, 1500);

            var updates = new AngularRecurrence(recurrenceId) {
                Focus = "[data-issue='" + recur.Id + "'] input:visible:first"
            };
            updates.IssuesList.Issues = AngularList.Create<AngularIssue>(AngularListType.Add, new[] { new AngularIssue(recur) });
            meetingHub.update(updates);

            //rt.UpdateRecurrences(recurrenceId).SetFocus("");

            Audit.L10Log(s, perms.GetCaller(), recurrenceId, "CreateIssue", ForModel.Create(issue), issue.NotNull(x => x.Message));

            // Trigger webhook events
            await HooksRegistry.Each<IIssueHook>((ses, x) => x.CreateIssue(ses, o.IssueRecurrenceModel));

            return o;

        }

        //public static object EditIssue(UserOrganizationModel caller, long issueRecurrenceId, string message, long? accountableUserId=null, int? priority=null) {
        //	using (var s = HibernateSession.GetCurrentSession()) {
        //		using (var tx = s.BeginTransaction()) {
        //			using (var rt = RealTimeUtility.Create()) {

        //				var perm = PermissionsUtility.Create(s, caller).EditIssueRecurrence(issueRecurrenceId);

        //				var found = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);

        //				if (message != null)
        //					found.Issue.Message = message;
        //				if (accountableUserId > 0) {
        //					perm.EditIssueRecurrence(found.Id).ViewUserOrganization(accountableUserId.Value,false);
        //					found.Owner = s.Load<UserOrganizationModel>(accountableUserId.Value);
        //				}
        //				if (priority != null) {
        //					found.Priority = priority.Value;
        //				}

        //				s.Update(found);

        //				if (found.Recurrence!=null && found.Recurrence.Id > 0)
        //					rt.UpdateRecurrences(found.Recurrence.Id).Update(new AngularIssue(found));


        //				tx.Commit();
        //				s.Flush();
        //				return found;

        //			}
        //		}
        //	}
        //}

        public static async Task<IssueOutput> CreateIssue(UserOrganizationModel caller, long recurrenceId, long ownerId, IssueModel issue) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);

                    var o = await CreateIssue(s, perms, recurrenceId, ownerId, issue);

                    tx.Commit();
                    s.Flush();

                    return o;
                }
            }
        }

        public static IssueModel GetIssue(UserOrganizationModel caller, long issueId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ViewIssue(issueId);
                    return s.Get<IssueModel>(issueId);
                }
            }
        }

        public static List<IssueModel.IssueModel_Recurrence> GetVisibleIssuesForUser(UserOrganizationModel caller, long userId) {
            //throw new NotImplementedException();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);

                    // only get meetings visible to me.
                    var list = L10Accessor.GetVisibleL10Meetings_Tiny(s, perms, caller.Id, true, false).Select(x => x.Id).ToList();

                    return s.QueryOver<IssueModel.IssueModel_Recurrence>()
                        .Where(x => x.DeleteTime == null
                        && x.CloseTime == null
                        && x.Owner.Id == userId).WhereRestrictionOn(x => x.Recurrence.Id).IsIn(list).Fetch(x => x.Issue).Eager.List().ToList();
                }
            }
        }


        // [Obsolete("Method is broken",true)]
        public static List<IssueModel.IssueModel_Recurrence> GetRecurrenceIssuesForUser(UserOrganizationModel caller, long userId, long recurrenceId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    // throw new NotImplementedException("query is incorrect");
                    PermissionsUtility.Create(s, caller).ViewRecurrenceIssuesForUser(userId, recurrenceId);

                    return s.QueryOver<IssueModel.IssueModel_Recurrence>()
                        .Where(x => x.DeleteTime == null
                        && x.Recurrence.Id == recurrenceId
                        && x.CloseTime == null
                        && x.Owner.Id == userId).Fetch(x => x.Issue).Eager.List().ToList();
                }
            }
        }

        public static IssueModel.IssueModel_Recurrence GetIssue_Recurrence(UserOrganizationModel caller, long recurrence_issue) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var found = s.Get<IssueModel.IssueModel_Recurrence>(recurrence_issue);

                    PermissionsUtility.Create(s, caller)
                        .ViewL10Recurrence(found.Recurrence.Id)
                        .ViewIssue(found.Issue.Id);

                    found.Issue = s.Get<IssueModel>(found.Issue.Id);
                    found.Recurrence = s.Get<L10Recurrence>(found.Recurrence.Id);

                    return found;
                }
            }
        }

        public static IssueModel.IssueModel_Recurrence CopyIssue(UserOrganizationModel caller, long parentIssue_RecurrenceId, long childRecurrenceId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var now = DateTime.UtcNow;

                    var parent = s.Get<IssueModel.IssueModel_Recurrence>(parentIssue_RecurrenceId);

                    PermissionsUtility.Create(s, caller)
                        .ViewL10Recurrence(parent.Recurrence.Id)
                        .ViewIssue(parent.Issue.Id);

                    var childRecur = s.Get<L10Recurrence>(childRecurrenceId);

                    if (childRecur.Organization.Id != caller.Organization.Id)
                        throw new PermissionsException("You cannot copy an issue into this meeting.");
                    if (parent.DeleteTime != null)
                        throw new PermissionsException("Issue does not exist.");

                    var possible = L10Accessor._GetAllL10RecurrenceAtOrganization(s, caller, caller.Organization.Id);
                    if (possible.All(x => x.Id != childRecurrenceId)) {
                        throw new PermissionsException("You do not have permission to copy this issue.");
                    }

                    var issue_recur = new IssueModel.IssueModel_Recurrence() {
                        ParentRecurrenceIssue = null,
                        CreateTime = now,
                        CopiedFrom = parent,
                        CreatedBy = caller,
                        Issue = s.Load<IssueModel>(parent.Issue.Id),
                        Recurrence = s.Load<L10Recurrence>(childRecurrenceId),
                        Owner = parent.Owner
                    };
                    s.Save(issue_recur);
                    var viewModel = IssuesData.FromIssueRecurrence(issue_recur);
                    _RecurseCopy(s, viewModel, caller, parent, issue_recur, now);
                    tx.Commit();
                    s.Flush();

                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
                    var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(childRecurrenceId));

                    meetingHub.appendIssue(".issues-list", viewModel);
                    var issue = s.Get<IssueModel>(parent.Issue.Id);
                    Audit.L10Log(s, caller, parent.Recurrence.Id, "CopyIssue", ForModel.Create(issue_recur), issue.NotNull(x => x.Message) + " copied into " + childRecur.NotNull(x => x.Name));
                    return issue_recur;
                }
            }
        }

        private static void _RecurseCopy(ISession s, IssuesData viewModel, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence copiedFrom, IssueModel.IssueModel_Recurrence parent, DateTime now) {
            var children = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                .Where(x => x.DeleteTime == null && x.ParentRecurrenceIssue.Id == copiedFrom.Id)
                .List();
            var childrenVMs = new List<IssuesData>();
            foreach (var child in children) {
                var issue_recur = new IssueModel.IssueModel_Recurrence() {
                    ParentRecurrenceIssue = parent,
                    CreateTime = now,
                    CopiedFrom = child,
                    CreatedBy = caller,
                    Issue = s.Load<IssueModel>(child.Issue.Id),
                    Recurrence = s.Load<L10Recurrence>(parent.Recurrence.Id),
                    Owner = s.Load<UserOrganizationModel>(parent.Owner.Id)
                };
                s.Save(issue_recur);
                var childVM = IssuesData.FromIssueRecurrence(issue_recur);
                childrenVMs.Add(childVM);
                _RecurseCopy(s, childVM, caller, child, issue_recur, now);
            }
            viewModel.children = childrenVMs.ToArray();
        }
        //private static void RecurseIssue(StringBuilder sb, int index, IssueModel.IssueModel_Recurrence parent, int depth, bool includeDetails) {
        //	var time = "";
        //	if (parent.CloseTime != null)
        //		time = parent.CloseTime.Value.ToShortDateString();
        //	sb.Append(index).Append(",")
        //		.Append(depth).Append(",")
        //		.Append(Csv.CsvQuote(parent.Owner.NotNull(x => x.GetName()))).Append(",")
        //		.Append(parent.CreateTime.ToShortDateString()).Append(",")
        //		.Append(time).Append(",");
        //	sb.Append(Csv.CsvQuote(parent.Issue.Message)).Append(",");

        //	sb.AppendLine();
        //	foreach (var child in parent._ChildIssues)
        //		RecurseIssue(sb, index, child, depth + 1, includeDetails);
        //}
        public static Csv Listing(UserOrganizationModel caller, long organizationId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    // var p = s.Get<PeriodModel>(period);

                    PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

                    var sb = new StringBuilder();

                    sb.Append("Id,Depth,Owner,Created,Closed,Issue");

                    var csv = new Csv();

                    IssueModel issueA = null;

                    //var id = 0;
                    var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                        .JoinAlias(x => x.Issue, () => issueA)
                        .Where(x => x.DeleteTime == null)
                        .Where(x => issueA.OrganizationId == organizationId)
                        .Fetch(x => x.Issue).Eager
                        .List().ToList();

                    foreach (var t in issues) {
                        var time = "";
                        csv.Add("" + t.Id, "Owner", t.Owner.NotNull(x => x.GetName()));
                        csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
                        if (t.CloseTime != null)
                            time = t.CloseTime.Value.ToShortDateString();
                        csv.Add("" + t.Id, "Completed", time);
                        csv.Add("" + t.Id, "Issue", "" + t.Issue.Message);

                        //if (false /*&& includeDetails*/) {
                        //	var padDetails = await PadAccessor.GetText(t.PadId);
                        //	csv.Add("" + t.Id, "Details", "" + padDetails);
                        //}
                    }


                    csv.SetTitle("Issues");

                    return csv;
                }
            }
        }

        public class IssueAndTodos {
            public IssueModel.IssueModel_Recurrence Issue { get; set; }
            public List<TodoModel> Todos { get; set; }
        }

        public static async Task<SLDocument> GetIssuesAndTodosSpreadsheetAtOrganization(UserOrganizationModel caller, long orgId, bool loadDetails = false) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ManagingOrganization(orgId);

                    IssueModel issueAlias = null;
                    //var issues = s.QueryOver<IssueModel>().Where(x => x.OrganizationId == ordId && x.DeleteTime == null);
                    var issuesQ = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                        .JoinAlias(x => x.Issue, () => issueAlias)
                        .Where(x => x.DeleteTime == null && issueAlias.OrganizationId == orgId && issueAlias.DeleteTime == null)
                        .Future();


                    var todosQ = s.QueryOver<TodoModel>()
                        .Where(x => x.ForModel == "IssueModel" && x.DeleteTime == null && x.OrganizationId == orgId)
                        .Future();

                    var result = new List<IssueAndTodos>();

                    var allTodos = todosQ.ToList();


                    var pads = new List<string>();
                    foreach (var issue in issuesQ) {
                        var iat = new IssueAndTodos();
                        iat.Issue = issue;
                        iat.Todos = allTodos.Where(x => x.ForModelId == issue.Issue.Id && x.ForModel == "IssueModel").ToList();
                        pads.Add(issue.Issue.PadId);
                        pads.AddRange(iat.Todos.Select(x => x.PadId));
                        result.Add(iat);
                    }

                    var padLookup = new Dictionary<string, string>();

                    if (loadDetails) {
                        padLookup = await PadAccessor.GetTexts(pads);
                    }

                    var issuesSheet = new Csv("Issues");
                    foreach (var iat in result) {
                        var ir = iat.Issue;
                        var issue = ir.Issue;
                        var id = issue.Id;
                        issuesSheet.Add("" + id, "Issue", issue.Message);
                        if (loadDetails) {
                            var details = padLookup.GetOrDefault(issue.PadId, "");
                            issuesSheet.Add("" + id, "Details", details);
                        }
                        issuesSheet.Add("" + id, "Owner", ir.Owner.Name);
                        issuesSheet.Add("" + id, "Completed", ir.CloseTime.NotNull(x => x.Value.ToShortDateString()));
                        issuesSheet.Add("" + id, "Created", issue.CreateTime.ToShortDateString());

                        issuesSheet.Add("" + id, "# Todos", ""+iat.Todos.Count());

                    }

                    var todoSheet= new Csv("Todos");
                    foreach (var todo in result.SelectMany(x=>x.Todos)) {
                        var id = todo.Id;
                        todoSheet.Add("" + id, "Todo", todo.Message);
                        if (loadDetails) {
                            var details = padLookup.GetOrDefault(todo.PadId, "");
                            todoSheet.Add("" + id, "Details", details);
                        }
                        todoSheet.Add("" + id, "Owner", todo.AccountableUser.Name);
                        todoSheet.Add("" + id, "Completed", todo.CompleteTime.NotNull(x => x.Value.ToShortDateString()));
                        todoSheet.Add("" + id, "Created", todo.CreateTime.ToShortDateString());
                        todoSheet.Add("" + id, "IssueId", ""+todo.ForModelId);
                    }



                    return CsvUtility.ToXls(issuesSheet, todoSheet);
                    
                }
            }
        }
    }
}
