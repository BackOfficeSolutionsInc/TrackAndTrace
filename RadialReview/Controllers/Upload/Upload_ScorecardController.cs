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
using RadialReview.Utilities.RealTime;

namespace RadialReview.Controllers {
    public partial class UploadController : BaseController {

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public async Task<PartialViewResult> ProcessScorecardSelection(IEnumerable<int> users, IEnumerable<int> dates, IEnumerable<int> measurables, IEnumerable<int> goals, long recurrenceId, string path)
        {
            var ui = await UploadAccessor.DownloadAndParse(GetUser(), path);
            var csvData = ui.Csv;
            var userRect = new Rect(users);
            var measurableRect = new Rect(measurables);
            var goalsRect = new Rect(goals);

            userRect.EnsureRowOrColumn();
            userRect.EnsureSameRangeAs(measurableRect);
            userRect.EnsureSameRangeAs(goalsRect);

            var dateRect = new Rect(dates);

            if (userRect.GetType() != dateRect.GetType())
                throw new ArgumentOutOfRangeException("rect", "Date selection and owner selection must be of different selection types (either row or column)");

            var userStrings = userRect.GetArray1D(csvData);
            var measurableStrings = measurableRect.GetArray1D(csvData);
            var goals1 = goalsRect.GetArray1D(csvData, x => x.TryParseDecimal() ?? 0m);

            var dateStrings = dateRect.GetArray1D(csvData);
            var dates1 = TimingUtility.FixOrderedDates(dateStrings, new CultureInfo("en-US"));


            var orgId = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, false).OrganizationId;
            var allUsers = OrganizationAccessor.GetMembers_Tiny(GetUser(), orgId);
           // var allUsers = OrganizationAccessor.GetMembers_Tiny(GetUser(), GetUser().Organization.Id);
            var userLookups = DistanceUtility.TryMatch(userStrings, allUsers);

            Rect scoreRect = null;
            if (dateRect.GetRectType() == RectType.Row) {
                scoreRect = new Rect(dateRect.MinX, userRect.MinY, dateRect.MaxX, userRect.MaxY);
            } else {
                scoreRect = new Rect(userRect.MinX, dateRect.MinY, userRect.MaxX, dateRect.MaxY);
            }

            var scores = scoreRect.GetArray(csvData, x => x.TryParseDecimal());

            var m = new UploadScorecardSelectedDataVM() {
                Rows = csvData,
                Users = userStrings,
                UserLookup = userLookups,
                Measurables = measurableStrings,
                Dates = dates1,
                Goals = goals1,
                Scores = scores,
                RecurrenceId = recurrenceId,
                Path = path,
                //UseAWS = useAWS,
                ScoreRange = string.Join(",", scoreRect.ToString()),
                MeasurableRectType = "" + dateRect.GetRectType(),
                DateRange = string.Join(",", dateRect.ToString()),
                AllUsers = allUsers.Select(x => new SelectListItem() { Text=x.Item1+" "+x.Item2, Value=x.Item3+""}).ToList()
            };
            return PartialView("UploadScorecardSelected",m);

        }
        

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public async Task<JsonResult> SubmitScorecard(FormCollection model)
        {
            //var useAws = model["UseAWS"].ToBoolean();
            var path = model["Path"].ToString();
            var recurrence = model["recurrenceId"].ToLong();
            var measurableRectType = model["MeasurableRectType"].ToString();
            var scoreRect = new Rect(model["ScoreRange"].ToString().Split(',').Select(x => x.ToInt()).ToList());
            var dateRect = new Rect(model["DateRange"].ToString().Split(',').Select(x => x.ToInt()).ToList());

            _PermissionsAccessor.Permitted(GetUser(), x => x.AdminL10Recurrence(recurrence));
            var ui = await UploadAccessor.DownloadAndParse(GetUser(), path);
            var csvData = ui.Csv;

            var keys = model.Keys.OfType<string>();
            var measurables = keys.Where(x => x.StartsWith("m_measurable_"))
                .ToDictionary(x => x.SubstringAfter("m_measurable_").ToInt(), x => (string)model[x]);
            var goals = keys.Where(x => x.StartsWith("m_goal_"))
                .ToDictionary(x => x.SubstringAfter("m_goal_").ToInt(), x => model[x].TryParseDecimal(0));
            var users = keys.Where(x => x.StartsWith("m_user_"))
                .ToDictionary(x => x.SubstringAfter("m_user_").ToInt(), x => model[x].ToLong());
            var goalDirs = keys.Where(x => x.StartsWith("m_goaldir_"))
                .ToDictionary(x => x.SubstringAfter("m_goaldir_").ToInt(), x => (LessGreater)Enum.Parse(typeof(LessGreater), model[x]));

            var scores = scoreRect.GetArray(csvData, (x, c) => x.TryParseDecimal());


            var dateStrings = dateRect.GetArray1D(csvData);
            var dates = TimingUtility.FixOrderedDates(dateStrings, new CultureInfo("en-US"));

            var caller = GetUser();
            var now = DateTime.UtcNow;
            var measurableLookup = new Dictionary<int, MeasurableModel>();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    using (var rt = RealTimeUtility.Create(false)) {
                        var org = s.Get<L10Recurrence>(recurrence).Organization;
                        var perms = PermissionsUtility.Create(s, caller).ViewOrganization(org.Id);
                        foreach (var m in measurables) {
                            var ident = m.Key;
                            var owner = users[ident];
                            var goal = goals[ident];
                            var goaldir = goalDirs[ident];
                            var measurable = new MeasurableModel() {
                                Title = m.Value,
                                OrganizationId = org.Id,
                                Goal = goal,
                                GoalDirection = goaldir,
                                AccountableUserId = owner,
                                AdminUserId = owner,
                                CreateTime = now
                            };

                            L10Accessor.AddMeasurable(s, perms,rt, recurrence, L10Controller.AddMeasurableVm.CreateNewMeasurable(recurrence, measurable), skipRealTime: true);
                            measurableLookup[ident] = measurable;
                            var scoreRow = measurableRectType == "Row"
                                ? new Rect(scoreRect.MinX, scoreRect.MinY + ident, scoreRect.MaxX, scoreRect.MinY + ident)
                                : new Rect(scoreRect.MinX + ident, scoreRect.MinY, scoreRect.MinX + ident, scoreRect.MaxY);

                            var scoresFound = scoreRow.GetArray1D(csvData, x => x.TryParseDecimal());

                            for (var i = 0; i < dates.Count; i++) {
                                var week = TimingUtility.GetWeekSinceEpoch(dates[i].AddDays(7).AddDays(6).StartOfWeek(DayOfWeek.Sunday));
                                var score = scoresFound[i];
                                L10Accessor._UpdateScore(s, perms, rt, measurable.Id, week, score, null, noSyncException: true, skipRealTime: true);
                            }



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
            }

            //ShowAlert("Uploaded Scorecard", AlertType.Success);

            return Json(ResultObject.CreateRedirect("/l10/wizard/" + recurrence + "#Scorecard", "Uploaded Scorecard"));
        }

        public class UploadScorecardSelectedDataVM {
            public List<string> Measurables { get; set; }
            public List<string> Users { get; set; }
            public List<decimal> Goals { get; set; }
            public List<DateTime> Dates { get; set; }

            public List<List<String>> Rows { get; set; }

            public List<Tuple<long, long>> Errors { get; set; }

            public Dictionary<string, DiscreteDistribution<Tuple<string, string, long>>> UserLookup { get; set; }

            public List<List<decimal?>> Scores { get; set; }

            public long RecurrenceId { get; set; }

            public bool UseAWS { get; set; }

            public string Path { get; set; }

            public string ScoreRange { get; set; }

            public string MeasurableRectType { get; set; }

            public string DateRange { get; set; }

            public List<SelectListItem> AllUsers { get; set; }
        }

    }
}