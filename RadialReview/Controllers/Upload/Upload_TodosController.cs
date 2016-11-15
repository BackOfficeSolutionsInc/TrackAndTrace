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
using RadialReview.Models.Todo;

namespace RadialReview.Controllers {
    public partial class UploadController : BaseController {

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public async Task<PartialViewResult> ProcessTodosSelection(IEnumerable<int> users, IEnumerable<int> todos, IEnumerable<int> details, IEnumerable<int> duedate, long recurrenceId, string path, FileType fileType)
        {
            try {
                var ui = await UploadAccessor.DownloadAndParse(GetUser(), path);

                var todosRect = new Rect(todos);

                todosRect.EnsureRowOrColumn();

                var m = new UploadTodosSelectedDataVM() { };
                var orgId = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, false).OrganizationId;
                var allUsers = TinyUserAccessor.GetOrganizationMembers(GetUser(), orgId);
                m.AllUsers = allUsers.ToSelectList(x => x.FirstName + " " + x.LastName, x => x.UserOrgId);
                var now = DateTime.UtcNow;
                if (fileType == FileType.CSV && (users != null || details != null || duedate != null)) {
                    var csvData = ui.Csv;

                    if (users != null) {
                        var userRect = new Rect(users);
                        userRect.EnsureSameRangeAs(todosRect);
                        var userStrings = userRect.GetArray1D(csvData);
                        m.UserLookup = DistanceUtility.TryMatch(userStrings, allUsers);
                        //data = csvData;
                        m.IncludeUsers = true;
                        m.Users = userStrings;
                    }

                    if (details != null) {
                        var detailsRect = new Rect(details);
                        detailsRect.EnsureSameRangeAs(todosRect);
                        var detailsStrings = detailsRect.GetArray1D(csvData);

                        m.IncludeDetails = true;
                        m.DetailsStrings = detailsStrings;
                    }
                    if (duedate != null) {
                        var duedateRect = new Rect(duedate);
                        duedateRect.EnsureSameRangeAs(todosRect);
                        var duedates = duedateRect.GetArray1D(csvData, x => { DateTime d = now.AddDays(7); DateTime.TryParse(x, out d); return d; });

                        m.IncludeDueDates = true;
                        m.DueDates = duedates;
                    }


                    m.Todos = todosRect.GetArray1D(csvData);
                } else {
                    var data = ui.Lines.Select(x => x.AsList()).ToList();
                    m.Todos = todosRect.GetArray1D(data);
                }
                m.DetailsStrings = m.DetailsStrings ?? m.Todos.Select(x => (string)null).ToList();
                m.DueDates = m.DueDates ?? m.Todos.Select(x => now.AddDays(7)).ToList();
                m.Path = path;

                return PartialView("UploadTodosSelected", m);
            } catch (Exception e) {
                //e.Data.Add("AWS_ID", path);
                throw new Exception(e.Message + "[" + path + "]", e);
            }
        }



        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public async Task<JsonResult> SubmitTodos(FormCollection model)
        {
                var path = model["Path"].ToString();
            try {
                //var useAws = model["UseAWS"].ToBoolean();
                var recurrence = model["recurrenceId"].ToLong();

                _PermissionsAccessor.Permitted(GetUser(), x => x.AdminL10Recurrence(recurrence));

                var now = DateTime.UtcNow;
                var keys = model.Keys.OfType<string>();
                var todos = keys.Where(x => x.StartsWith("m_todo_"))
                    .Where(x => !String.IsNullOrWhiteSpace(model[x]))
                    .ToDictionary(x => x.SubstringAfter("m_todo_").ToInt(), x => (string)model[x]);

                var users = keys.Where(x => x.StartsWith("m_user_"))
                    .ToDictionary(x => x.SubstringAfter("m_user_").ToInt(), x => model[x].ToLong());

                var details = keys.Where(x => x.StartsWith("m_details_"))
                    .ToDictionary(x => x.SubstringAfter("m_details_").ToInt(), x => model[x]);

                var due = keys.Where(x => x.StartsWith("m_due_"))
                               .ToDictionary(x => x.SubstringAfter("m_due_").ToInt(), x => { var o = now.AddDays(7); DateTime.TryParse(model[x], out o); return o; });

                var caller = GetUser();
                var measurableLookup = new Dictionary<int, MeasurableModel>();
                using (var s = HibernateSession.GetCurrentSession()) {
                    using (var tx = s.BeginTransaction()) {
                        var org = s.Get<L10Recurrence>(recurrence).Organization;
                        var perms = PermissionsUtility.Create(s, caller).ViewOrganization(org.Id);
                        foreach (var m in todos) {
                            var ident = m.Key;
                            long? owner = null;
                            if (users.ContainsKey(ident))
                                owner = users[ident];
                            string dets = null;
                            if (details.ContainsKey(ident))
                                dets = details[ident];

                            DateTime dued = now.AddDays(7);
                            if (details.ContainsKey(ident))
                                dued = due[ident];

                            await TodoAccessor.CreateTodo(s, perms, recurrence, new TodoModel() {
                                CreateTime = now,
                                Message = todos[ident],
                                OrganizationId = org.Id,
                                CreatedById = caller.Id,
                                Details = dets,
                                DueDate = dued,
                                AccountableUserId = owner ?? caller.Id,
                                ForRecurrenceId=recurrence,
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

                return Json(ResultObject.CreateRedirect("/l10/wizard/" + recurrence + "#Todos", "Uploaded To-dos"));
            } catch (Exception e) {
                //e.Data.Add("AWS_ID", path);
                throw new Exception(e.Message + "[" + path + "]", e);
            }
        }
        public class UploadTodosSelectedDataVM {
            public List<string> Todos { get; set; }
            public List<string> Users { get; set; }
            public List<string> DetailsStrings { get; set; }
            public List<DateTime> DueDates { get; set; }
            public Dictionary<string, DiscreteDistribution<TinyUser>> UserLookup { get; set; }

            public bool IncludeUsers { get; set; }
            public bool IncludeDetails { get; set; }
            public bool IncludeDueDates { get; set; }


            public List<SelectListItem> AllUsers { get; set; }

            public string Path { get; set; }
        }
    }
}