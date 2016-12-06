﻿using System.Web.Script.Serialization;
using System.Web.SessionState;
using System.Web.UI;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Application;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Json;
using RadialReview.Models.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Exceptions;
using RadialReview.Models.Scorecard;
using RadialReview.Notifications;
using RadialReview.Models.Angular.Notifications;
using RadialReview.Models.Angular.DataType;

namespace RadialReview.Controllers {
    [SessionState(SessionStateBehavior.ReadOnly)]
    public class DashboardDataController : BaseController {

        protected void ProcessDeadTile(Exception e) {
            //  int a = 0;
        }


        [Access(AccessLevel.UserOrganization)]
        [OutputCache(NoStore = true, Duration = 0)]

        //[OutputCache(Duration = 3, VaryByParam = "id", Location = OutputCacheLocation.Client, NoStore = true)]
        //[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public JsonResult Data2(long id, bool completed = false, string name = null, long? start = null, long? end = null, bool fullScorecard = false) {
            //Response.AddHeader("Content-Encoding", "gzip");
            var userId = id;
            var dash = DashboardAccessor.GetPrimaryDashboardForUser(GetUser(), id);
            var tiles = DashboardAccessor.GetTiles(GetUser(), dash.Id);
            DateTime startRange;
            DateTime endRange;

            if (start == null)
                startRange = TimingUtility.PeriodsAgo(DateTime.UtcNow, 13, GetUser().Organization.Settings.ScorecardPeriod);
            else
                startRange = start.Value.ToDateTime();

            if (end == null)
                endRange = DateTime.UtcNow.AddDays(14);
            else
                endRange = end.Value.ToDateTime();

            if (completed) {
                startRange = Math2.Min(DateTime.UtcNow.AddDays(-1), startRange);
                endRange = Math2.Max(DateTime.UtcNow.AddDays(2), endRange);
            }
            var dateRange = new DateRange(startRange, endRange);

            var output = new DashboardController.ListDataVM(id) {
                Name = name,
                date = new AngularDateRange() { startDate = startRange, endDate = endRange }
            };

            if (tiles.Any(x => x.Type == TileType.Todo || (x.DataUrl ?? "").Contains("UserTodo"))) {
                try {
                    //Todos
                    var todos = TodoAccessor.GetMyTodos(GetUser(), id, !completed, dateRange).Select(x => new AngularTodo(x));
                    var m = _UserAccessor.GetUserOrganization(GetUser(), id, false, true, PermissionType.ViewTodos);
                    output.Todos = todos.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ThenBy(x => x.DueDate);
                } catch (Exception e) {
                    ProcessDeadTile(e);
                }

            }

            if (tiles.Any(x => x.Type == TileType.Scorecard || (x.DataUrl ?? "").Contains("UserScorecard"))) {
                var startEnd = "";
                if (start != null)
                    startEnd += "&start=" + start;
                if (end != null)
                    startEnd += "&end=" + end;

                output.LoadUrls.Add(new AngularString(-15291127 * userId, $"/DashboardData/UserScorecardData/{id}?userId={userId}&completed={completed}&fullScorecard={fullScorecard}" + startEnd));
            }

            if (tiles.Any(x => x.Type == TileType.Rocks || (x.DataUrl ?? "").Contains("UserRock"))) {
                try {
                    var now = DateTime.UtcNow;
                    var rocks = L10Accessor.GetAllMyL10Rocks(GetUser(), GetUser().Id).Select(x => new AngularRock(x));

                    output.Rocks = rocks;
                } catch (Exception e) {
                    ProcessDeadTile(e);
                }
            }

            if (tiles.Any(x => x.Type == TileType.Manage || (x.DataUrl ?? "").Contains("UserManage"))) {
                try {
                    var directReports = _OrganizationAccessor.GetOrganizationMembersLookup(GetUser(), GetUser().Organization.Id, true, PermissionType.EditEmployeeDetails)
                        .Select(x => AngularUser.CreateUser(x, managing: true)).ToList();
                    var managingIds = DeepAccessor.Users.GetSubordinatesAndSelf(GetUser(), GetUser().Id);
                    directReports = directReports.Where(x => managingIds.Contains(x.Id)).ToList();
                    output.Members = directReports;
                } catch (Exception e) {
                    ProcessDeadTile(e);
                }
            }

            if (tiles.Any(x => x.Type == TileType.Roles || (x.DataUrl ?? "").Contains("UserRoles"))) {
                try {
                    var roles = _RoleAccessor.GetRoles(GetUser(), GetUser().Id).Select(x => new AngularRole(x)).ToList();
                    output.Roles = roles;
                } catch (Exception e) {
                    ProcessDeadTile(e);
                }
            }

            if (tiles.Any(x => x.Type == TileType.Values || (x.DataUrl ?? "").Contains("OrganizationValues"))) {
                try {
                    var values = _OrganizationAccessor.GetCompanyValues(GetUser(), GetUser().Organization.Id).Select(x => AngularCompanyValue.Create(x)).ToList();
                    output.CoreValues = values;
                } catch (Exception e) {
                    ProcessDeadTile(e);
                }
            }

            if (tiles.Any(x => x.Type == TileType.Notifications || (x.DataUrl ?? "").Contains("UserNotifications"))) {
                try {
                    var notifications = AngularNotification.Create(PubSub.ListUnseen(GetUser(), GetUser().Id)).ToList();
                    output.Notifications = notifications;
                } catch (Exception e) {
                    ProcessDeadTile(e);
                }
            }

            var caller = GetUser();
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var l10Lookup = new DefaultDictionary<long, L10Recurrence>(x => L10Accessor.GetL10Recurrence(caller, x, false));
                    var perms = PermissionsUtility.Create(s, caller);

                    //L10 Todos
                    foreach (var todo in tiles.Where(x => x.Type == TileType.L10Todos || (x.DataUrl ?? "").Contains("L10Todos")).Distinct(x => x.KeyId)) {
                        long l10Id = 0;
                        if (long.TryParse(todo.KeyId, out l10Id)) {
                            try {
                                var tile = new DashboardController.AngularTileId<List<AngularTodo>>(todo.Id, l10Id, l10Lookup[l10Id].Name + " to-dos");
                                tile.Contents = L10Accessor.GetAllTodosForRecurrence(s, perms, l10Id, false).Select(x => new AngularTodo(x)).ToList();
                                output.L10Todos.Add(tile);
                            } catch (Exception e) {
                                output.L10Todos.Add(DashboardController.AngularTileId<List<AngularTodo>>.Error(todo.Id, l10Id, e));
                            }
                        }
                    }

                    //L10 Issues
                    foreach (var issue in tiles.Where(x => x.Type == TileType.L10Issues || (x.DataUrl ?? "").Contains("L10Issues")).Distinct(x => x.KeyId)) {
                        long l10Id = 0;
                        if (long.TryParse(issue.KeyId, out l10Id)) {
                            try {
                                var tile = new DashboardController.AngularTileId<AngularIssuesList>(issue.Id, l10Id, l10Lookup[l10Id].Name + " issues");
                                tile.Contents = new AngularIssuesList(l10Id) {
                                    Issues = L10Accessor.GetIssuesForRecurrence(s, perms, l10Id).Select(x => new AngularIssue(x)).ToList(),
                                    Prioritization = l10Lookup[l10Id].Prioritization,
                                };
                                output.L10Issues.Add(tile);
                            } catch (Exception e) {
                                output.L10Issues.Add(DashboardController.AngularTileId<AngularIssuesList>.Error(issue.Id, l10Id, e));
                            }
                        }
                    }

                    //L10 Rocks
                    foreach (var rock in tiles.Where(x => x.Type == TileType.L10Rocks || (x.DataUrl ?? "").Contains("L10Rocks")).Distinct(x => x.KeyId)) {
                        long l10Id = 0;
                        if (long.TryParse(rock.KeyId, out l10Id)) {
                            try {
                                var tile = new DashboardController.AngularTileId<List<AngularRock>>(rock.Id, l10Id, l10Lookup[l10Id].Name + " rocks");
                                tile.Contents = L10Accessor.GetRocksForRecurrence(s, perms, l10Id).Select(x => new AngularRock(x.ForRock)).ToList();
                                output.L10Rocks.Add(tile);
                            } catch (Exception e) {
                                output.L10Rocks.Add(DashboardController.AngularTileId<List<AngularRock>>.Error(rock.Id, l10Id, e));
                            }
                        }
                    }

                    //L10 Scorecard
                    foreach (var scorecard in tiles.Where(x => x.Type == TileType.L10Scorecard || (x.DataUrl ?? "").Contains("L10Scorecard")).Distinct(x => x.KeyId)) {
                        long l10Id = 0;
                        if (long.TryParse(scorecard.KeyId, out l10Id)) {
                            try {
                                var scname = "L10 Scorecard";
                                try {
                                    scname=l10Lookup[l10Id].Name;
                                } catch (Exception) {
                                }
                                var startEnd = "";
                                if (start != null)
                                    startEnd += "&start=" + start;
                                if (end != null)
                                    startEnd += "&end=" + end;
                                //random prime
                                output.LoadUrls.Add(new AngularString(15291127 * l10Id, $"/DashboardData/L10ScorecardData/{id}?name={scname}&scorecardTileId={scorecard.Id}&l10Id={l10Id}&completed={completed}&fullScorecard={fullScorecard}" + startEnd));
                            } catch (Exception e) {
                                output.L10Scorecards.Add(DashboardController.AngularTileId<AngularScorecard>.Error(scorecard.Id, l10Id, e));
                            }
                        }
                    }
                }
            }

            return Json(output, JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
        [OutputCache(NoStore = true, Duration = 0)]
        public JsonResult L10ScorecardData(long id, string name, long scorecardTileId, long l10Id, bool completed = false, bool fullScorecard = false, long? start = null, long? end = null) {
            DateTime startRange;
            DateTime endRange;

            if (start == null)
                startRange = TimingUtility.PeriodsAgo(DateTime.UtcNow, 13, GetUser().Organization.Settings.ScorecardPeriod);
            else
                startRange = start.Value.ToDateTime();

            if (end == null)
                endRange = DateTime.UtcNow.AddDays(14);
            else
                endRange = end.Value.ToDateTime();

            if (completed) {
                startRange = Math2.Min(DateTime.UtcNow.AddDays(-1), startRange);
                endRange = Math2.Max(DateTime.UtcNow.AddDays(2), endRange);
            }
            var dateRange = new DateRange(startRange, endRange);

            var output = new DashboardController.ListDataVM(id) {
                date = new AngularDateRange() { startDate = startRange, endDate = endRange }
            };
            try {
                var tile = new DashboardController.AngularTileId<AngularScorecard>(scorecardTileId, l10Id, name + " scorecard");
                using (var s = HibernateSession.GetCurrentSession()) {
                    using (var tx = s.BeginTransaction()) {
                        var perms = PermissionsUtility.Create(s, GetUser());
                        var sam = L10Accessor.GetScoresAndMeasurablesForRecurrence(s, perms, l10Id, false, getMeasurables: true);
                        var scores = sam.Scores;
                        var measurables = sam.Measurables;

                        var orders = L10Accessor.GetMeasurableOrdering(GetUser(), l10Id);
                        var ts = GetUser().GetTimeSettings();
                        ts.WeekStart = L10Accessor.GetL10Recurrence(GetUser(), l10Id, false).StartOfWeekOverride ?? ts.WeekStart;
                        tile.Contents = AngularScorecard.Create(scorecardTileId, ts,
                            sam.MeasurablesAndDividers,
                            scores.ToList(), DateTime.UtcNow);
                        output.L10Scorecards.Add(tile);
                    }
                }
            } catch (Exception e) {
                output.L10Scorecards.Add(DashboardController.AngularTileId<AngularScorecard>.Error(scorecardTileId, l10Id, e));
            }

            return Json(output, JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
        [OutputCache(NoStore = true, Duration = 0)]
        public JsonResult UserScorecardData(long id, long userId, bool completed = false, bool fullScorecard = false, long? start = null, long? end = null) {
            DateTime startRange;
            DateTime endRange;

            if (start == null)
                startRange = TimingUtility.PeriodsAgo(DateTime.UtcNow, 13, GetUser().Organization.Settings.ScorecardPeriod);
            else
                startRange = start.Value.ToDateTime();

            if (end == null)
                endRange = DateTime.UtcNow.AddDays(14);
            else
                endRange = end.Value.ToDateTime();

            if (completed) {
                startRange = Math2.Min(DateTime.UtcNow.AddDays(-1), startRange);
                endRange = Math2.Max(DateTime.UtcNow.AddDays(2), endRange);
            }
            var dateRange = new DateRange(startRange, endRange);

            var output = new DashboardController.ListDataVM(id) {
                date = new AngularDateRange() { startDate = startRange, endDate = endRange }
            };
            try {//Scorecard

                var scorecardStart = fullScorecard ? TimingUtility.PeriodsAgo(DateTime.UtcNow, 13, GetUser().Organization.Settings.ScorecardPeriod) : startRange;
                var scorecardEnd = fullScorecard ? DateTime.UtcNow.AddDays(14) : endRange;
                output.Scorecard = ScorecardAccessor.GetAngularScorecardForUser(GetUser(), userId, new DateRange(scorecardStart, scorecardEnd), true, now: DateTime.UtcNow);
            } catch (Exception e) {
                ProcessDeadTile(e);
            }
            return Json(output, JsonRequestBehavior.AllowGet);
        }
    }

    public class DashboardController : BaseController {
        public class AngularTileId<T> : BaseAngular {
            public long KeyId { get; set; }
            public string Title { get; set; }
            public T Contents { get; set; }
            public bool HasError { get; set; }
            public string Message { get; set; }

            public AngularTileId(long tile, long keyId, string title)
                : base(tile) {
                KeyId = keyId;
                Title = title;
            }

            public static AngularTileId<T> Error(long tile, long keyId, Exception e) {
                var message = "Could not load tile";
                if (e is PermissionsException)
                    message = (e as PermissionsException).Message;

                return new AngularTileId<T>(tile, keyId, "Error") {
                    HasError = true,
                    Message = message,
                };
            }
        }

        public class ListDataVM : BaseAngular {
            public string Name { get; set; }
            public IEnumerable<AngularTodo> Todos { get; set; }
            public AngularScorecard Scorecard { get; set; }
            public IEnumerable<AngularRock> Rocks { get; set; }
            public IEnumerable<AngularUser> Members { get; set; }

            public AngularDateRange date { get; set; }

            public class DateVM {
                public DateTime startDate { get; set; }
                public DateTime endDate { get; set; }
            }

            public IEnumerable<AngularRole> Roles { get; set; }
            public IEnumerable<AngularCompanyValue> CoreValues { get; set; }
            public IEnumerable<AngularNotification> Notifications { get; set; }


            public List<AngularTileId<AngularScorecard>> L10Scorecards { get; set; }
            public List<AngularTileId<List<AngularRock>>> L10Rocks { get; set; }
            public List<AngularTileId<AngularIssuesList>> L10Issues { get; set; }
            public List<AngularTileId<List<AngularTodo>>> L10Todos { get; set; }

            public List<AngularString> LoadUrls { get; set; }

            public ListDataVM(long id)
                : base(id) {
                L10Scorecards = new List<AngularTileId<AngularScorecard>>();
                L10Rocks = new List<AngularTileId<List<AngularRock>>>();
                L10Issues = new List<AngularTileId<AngularIssuesList>>();
                L10Todos = new List<AngularTileId<List<AngularTodo>>>();

                LoadUrls = new List<AngularString>();
            }
        }

        public class TileVM {
            public int w { get; set; }
            public int h { get; set; }
            public int x { get; set; }
            public int y { get; set; }
            public long id { get; set; }
        }
        [Access(AccessLevel.UserOrganization)]
        public JsonResult Tiles(long id) {
            var dashboardId = id;
            var tiles = DashboardAccessor.GetTiles(GetUser(), dashboardId);
            return Json(ResultObject.SilentSuccess(tiles), JsonRequestBehavior.AllowGet);
        }
        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public JsonResult UpdateTiles(long id, IEnumerable<TileVM> model) {
            var dashboardId = id;
            DashboardAccessor.EditTiles(GetUser(), dashboardId, model);
            return Json(ResultObject.SilentSuccess());
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult Tile(long id, bool? hidden = null, int? w = null, int? h = null, int? x = null, int? y = null, string dataurl = null, string title = null) {
            var tile = DashboardAccessor.EditTile(GetUser(), id, h, w, x, y, hidden, dataurl, title);
            tile.ForUser = null;
            tile.Dashboard = null;
            return Json(ResultObject.SilentSuccess(tile), JsonRequestBehavior.AllowGet);
        }
        [Access(AccessLevel.UserOrganization)]
        public ActionResult CreateDashboard(string title = null, bool primary = false, bool prepopulate = false) {
            var dash = DashboardAccessor.CreateDashboard(GetUser(), title, primary, prepopulate);
            return RedirectToAction("Index", new { id = dash.Id });
        }

        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateTileModal() {
            return PartialView();
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult CreateTile(long id, bool? hidden = null, int w = 1, int h = 1, int x = 0, int y = 0, TileType type = TileType.Invalid, string dataurl = null, string title = null, string keyId = null) {
            var tile = DashboardAccessor.CreateTile(GetUser(), id, w, h, x, y, dataurl, title, type, keyId);
            tile.ForUser = null;
            tile.Dashboard = null;
            return Json(ResultObject.SilentSuccess(tile), JsonRequestBehavior.AllowGet);
        }

        public class DashboardVM {
            public long DashboardId { get; set; }
            public String TileJson { get; set; }
            public List<L10> L10s { get; set; }

            public class L10 {
                public bool Selected { get; set; }
                public string Text { get; set; }
                public string Value { get; set; }
                public List<SelectListItem> Notes { get; set; }
                public L10() {
                    Notes = new List<SelectListItem>();
                }
            }

            public DashboardVM() {
                L10s = new List<L10>();
            }
        }


        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index(long? id = null) {
            if (id == null)
                id = DashboardAccessor.GetPrimaryDashboardForUser(GetUser(), GetUser().Id).NotNull(x => x.Id);
            if (id == 0) {
                id = DashboardAccessor.CreateDashboard(GetUser(), null, false, true).Id;
                return RedirectToAction("Index", new { id = id });
            }

            var tiles = DashboardAccessor.GetTiles(GetUser(), id.Value);

            var l10s = L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id, true);

            var notes = L10Accessor.GetVisibleL10Notes_Unsafe(l10s.Select(x => x.Id).ToList());


            var jsonTiles = Json(ResultObject.SilentSuccess(tiles), JsonRequestBehavior.AllowGet);
            var jsonTilesStr = new JavaScriptSerializer().Serialize(jsonTiles.Data);



            ViewBag.UserId = GetUser().Id;
            return View(new DashboardVM() {
                DashboardId = id.Value,
                TileJson = jsonTilesStr,
                L10s = l10s.Select(x => new DashboardVM.L10() {
                    Value = "" + x.Id,
                    Text = x.Name,
                    Notes = notes.Where(y => y.Recurrence.Id == x.Id).Select(z => new SelectListItem() {
                        Text = z.Name,
                        Value = "" + z.Id
                    }).ToList()
                }).ToList()
            });
        }



        //
        // GET: /Dashboard/

    }
}