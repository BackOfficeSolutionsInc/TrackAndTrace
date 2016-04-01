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

namespace RadialReview.Accessors
{
    public class IssuesAccessor
    {
        public class IssueOutput
        {
            public IssueModel IssueModel { get; set; }
            public IssueModel.IssueModel_Recurrence IssueRecurrenceModel { get; set; }
        }
        public static async Task<IssueOutput> CreateIssue(ISession s, PermissionsUtility perms, long recurrenceId, long ownerId, IssueModel issue)
        {
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

            perms.ConfirmAndFix(issue,
                x => x.OrganizationId,
                x => x.Organization,
                x => x.ViewOrganization);

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

            var recur = new IssueModel.IssueModel_Recurrence()
            {
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

            var updates = new AngularRecurrence(recurrenceId);
            updates.Issues = new List<AngularIssue>() { new AngularIssue(recur) };
            meetingHub.update(updates);
            Audit.L10Log(s, perms.GetCaller(), recurrenceId, "CreateIssue", ForModel.Create(issue), issue.NotNull(x => x.Message));
            return o;

        }


        public static async Task<bool> CreateIssue(UserOrganizationModel caller, long recurrenceId, long ownerId, IssueModel issue)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);

                    var o = await CreateIssue(s, perms, recurrenceId, ownerId, issue);

                    tx.Commit();
                    s.Flush();

                    return true;
                }
            }
        }

        public static IssueModel GetIssue(UserOrganizationModel caller, long issueId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewIssue(issueId);
                    return s.Get<IssueModel>(issueId);
                }
            }
        }

        public static IssueModel.IssueModel_Recurrence GetIssue_Recurrence(UserOrganizationModel caller, long recurrence_issue)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
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




        public static void CopyIssue(UserOrganizationModel caller, long parentIssue_RecurrenceId, long childRecurrenceId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var now = DateTime.UtcNow;

                    var parent = s.Get<IssueModel.IssueModel_Recurrence>(parentIssue_RecurrenceId);


                    PermissionsUtility.Create(s, caller)
                        .ViewL10Recurrence(parent.Recurrence.Id)
                        .ViewIssue(parent.Issue.Id);

                    var childRecur = s.Get<L10Recurrence>(childRecurrenceId);

                    if (childRecur.Organization.Id != caller.Organization.Id)
                        throw new PermissionsException("You cannot copy an issue into this meeting.");

                    /*var parent = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                        .Where(x => x.DeleteTime == null && x.Issue.Id == issueId && x.Recurrence.Id == parentRecurrenceId)
                        .SingleOrDefault();*/

                    if (parent.DeleteTime != null)
                        throw new PermissionsException("Issue does not exist.");

                    //var issue = s.Get<IssueModel>(issueId);
                    //var parentRecurrence = s.Get<L10Recurrence>(parentRecurrenceId);

                    var possible = L10Accessor._GetAllL10RecurrenceAtOrganization(s, caller, caller.Organization.Id);

                    if (possible.All(x => x.Id != childRecurrenceId))
                    {
                        throw new PermissionsException("You do not have permission to copy this issue.");
                    }


                    var issue_recur = new IssueModel.IssueModel_Recurrence()
                    {
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

                }
            }
        }

        private static void _RecurseCopy(ISession s, IssuesData viewModel, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence copiedFrom, IssueModel.IssueModel_Recurrence parent, DateTime now)
        {
            var children = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                .Where(x => x.DeleteTime == null && x.ParentRecurrenceIssue.Id == copiedFrom.Id)
                .List();
            var childrenVMs = new List<IssuesData>();
            foreach (var child in children)
            {
                var issue_recur = new IssueModel.IssueModel_Recurrence()
                {
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
    }
}