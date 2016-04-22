using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CsvHelper;
using RadialReview.Utilities;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using System.Net;
using System.Globalization;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Issues;

namespace RadialReview.Controllers {
    public partial class UploadController : BaseController {

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public async Task<PartialViewResult> ProcessIssuesSelection(IEnumerable<int> users, IEnumerable<int> issues, IEnumerable<int> details, long recurrenceId, string path, FileType fileType)
        {
            var ui = await UploadAccessor.DownloadAndParse(GetUser(), path);

            var issueRect = new Rect(issues);

            issueRect.EnsureRowOrColumn();

            var m = new UploadIssuesSelectedDataVM() { };
            var orgId = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, false).OrganizationId;
            var allUsers = OrganizationAccessor.GetMembers_Tiny(GetUser(), orgId);
            //var allUsers = OrganizationAccessor.GetMembers_Tiny(GetUser(), GetUser().Organization.Id);
            m.AllUsers = allUsers.ToSelectList(x => x.Item1 + " " + x.Item2, x => x.Item3);
            if (fileType == FileType.CSV && (users != null || details != null)) {
                var csvData = ui.Csv;

                if (users != null) {
                    var userRect = new Rect(users);
                    userRect.EnsureSameRangeAs(issueRect);
                    var userStrings = userRect.GetArray1D(csvData);
                    m.UserLookup = DistanceUtility.TryMatch(userStrings, allUsers);
                    //data = csvData;
                    m.IncludeUsers = true;
                    m.Users = userStrings;
                }

                if (details != null) {
                    var detailsRect = new Rect(details);
                    detailsRect.EnsureSameRangeAs(issueRect);
                    var detailsStrings = detailsRect.GetArray1D(csvData);

                    m.IncludeDetails = true;
                    m.DetailsStrings = detailsStrings;
                }


                m.Issues = issueRect.GetArray1D(csvData);
            } else {
                var data = ui.Lines.Select(x => x.AsList()).ToList();
                m.Issues = issueRect.GetArray1D(data);
            }
            m.DetailsStrings = m.DetailsStrings ?? m.Issues.Select(x => (string)null).ToList();
            m.Path = path;

            return PartialView("UploadIssuesSelected", m);

        }



        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public async Task<JsonResult> SubmitIssues(FormCollection model)
        {
            //var useAws = model["UseAWS"].ToBoolean();
            var path = model["Path"].ToString();
            var recurrence = model["recurrenceId"].ToLong();

            _PermissionsAccessor.Permitted(GetUser(), x => x.AdminL10Recurrence(recurrence));

            var keys = model.Keys.OfType<string>();
            var issues = keys.Where(x => x.StartsWith("m_issue_"))
                .Where(x => !String.IsNullOrWhiteSpace(model[x]))
                .ToDictionary(x => x.SubstringAfter("m_issue_").ToInt(), x => (string)model[x]);

            var users = keys.Where(x => x.StartsWith("m_user_"))
                .ToDictionary(x => x.SubstringAfter("m_user_").ToInt(), x => model[x].ToLong());

            var details = keys.Where(x => x.StartsWith("m_details_"))
                .ToDictionary(x => x.SubstringAfter("m_details_").ToInt(), x => model[x]);

            var caller = GetUser();
            var now = DateTime.UtcNow;
            var measurableLookup = new Dictionary<int, MeasurableModel>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var org = s.Get<L10Recurrence>(recurrence).Organization;
                    var perms = PermissionsUtility.Create(s, caller).ViewOrganization(org.Id);
                    foreach (var m in issues) {
                        var ident = m.Key;
                        long? owner = null;
                        if (users.ContainsKey(ident))
                            owner = users[ident];
                        string dets = null;
                        if (details.ContainsKey(ident))
                            dets = details[ident];

                        await IssuesAccessor.CreateIssue(s, perms, recurrence, owner ?? caller.Id, new IssueModel() {
                            CreateTime = now,
                            Message = issues[ident],
                            OrganizationId = org.Id,
                            CreatedById = caller.Id,
                            Description = dets,
                        });

                    }
                    var existing = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
                        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrence)
                        .Select(x => x.User.Id)
                        .List<long>().ToList();

                    foreach (var u in users.Where(x => !existing.Any(y => y == x.Value)).Select(x => x.Value).Distinct()) {
                        s.Save(new L10Recurrence.L10Recurrence_Attendee() {
                            User = s.Load<UserOrganizationModel>(u),
                            L10Recurrence = s.Load<L10Recurrence>(recurrence),
                            CreateTime = now,
                        });
                    }
                    tx.Commit();
                    s.Flush();
                }
            }

            //ShowAlert("Uploaded Scorecard", AlertType.Success);

            return Json(ResultObject.CreateRedirect("/l10/wizard/" + recurrence + "#Issues", "Uploaded Issues"));
        }
        public class UploadIssuesSelectedDataVM {
            public List<string> Issues { get; set; }
            public List<string> Users { get; set; }
            public List<string> DetailsStrings { get; set; }
            public Dictionary<string, DiscreteDistribution<Tuple<string, string, long>>> UserLookup { get; set; }

            public bool IncludeUsers { get; set; }
            public bool IncludeDetails { get; set; }


            public List<SelectListItem> AllUsers { get; set; }

            public string Path { get; set; }
        }
    }
}