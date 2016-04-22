using System.Web.Script.Serialization;
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

namespace RadialReview.Controllers
{
    [SessionState(SessionStateBehavior.ReadOnly)]
    public class DashboardDataController : BaseController
    {
        [Access(AccessLevel.UserOrganization)]
        //[OutputCache(Duration = 3, VaryByParam = "id", Location = OutputCacheLocation.Client, NoStore = true)]
        //[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public JsonResult Data2(long id, bool completed = false, string name = null)
		{
            //Response.AddHeader("Content-Encoding", "gzip");
			var userId = id;
            var dash = DashboardAccessor.GetPrimaryDashboardForUser(GetUser(), id);
            var tiles = DashboardAccessor.GetTiles(GetUser(), dash.Id);
            
			var start = TimingUtility.PeriodsAgo(DateTime.UtcNow,13, GetUser().Organization.Settings.ScorecardPeriod);
			var end = DateTime.UtcNow.AddDays(14);
			if (completed){
				start = DateTime.UtcNow.AddDays(-1);
				end = DateTime.UtcNow.AddDays(2);
			}

            var output = new DashboardController.ListDataVM(id)
            {
                Name = name,
                //Todos = todos.OrderByDescending(x=>x.CompleteTime??DateTime.MaxValue).ThenBy(x=>x.DueDate),
                //Scorecard = sc,
                //Rocks = rocks,
                //Members = directReports,
                date = new AngularDateRange() { startDate = start, endDate = end }

                //Name = "All to-dos for " + m.GetName()
            };

            if (tiles.Any(x => x.Type == TileType.Todo || (x.DataUrl??"").Contains("UserTodo"))) { 
			    //Todos
			    var todos = TodoAccessor.GetTodosForUser(GetUser(), id, !completed).Select(x => new AngularTodo(x));
			    var m = _UserAccessor.GetUserOrganization(GetUser(), id, false, true, PermissionType.ViewTodos);
                output.Todos = todos.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ThenBy(x => x.DueDate);

            }

            if (tiles.Any(x => x.Type == TileType.Scorecard|| (x.DataUrl??"").Contains("UserScorecard")))
            {
                //Scorecard
                var measurables = ScorecardAccessor.GetUserMeasurables(GetUser(), userId, ordered: true, includeAdmin: true);

                var scores = ScorecardAccessor.GetUserScores(GetUser(), GetUser().Id, start, end, includeAdmin: true);
                output.Scorecard = new AngularScorecard(-1,
                    GetUser().Organization.Settings.WeekStart,
                    GetUser().Organization.GetTimezoneOffset(),
                    measurables.Select(x => new AngularMeasurable(x) { }),
                    scores.ToList(),
                    DateTime.UtcNow,
                    GetUser().Organization.Settings.ScorecardPeriod,
                    new YearStart(GetUser().Organization)
                    );
            }

            if (tiles.Any(x => x.Type == TileType.Rocks || (x.DataUrl ?? "").Contains("UserRock")))
            {
                var now = DateTime.UtcNow;
                var currentPeriod = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).Where(x => x.StartTime <= now && now <= x.EndTime).FirstOrDefault().NotNull(x=>x.Id);
                var rocks = L10Accessor.GetAllMyL10Rocks(GetUser(), GetUser().Id, currentPeriod).Select(x => new AngularRock(x));
              //  var rocks = _RockAccessor.GetAllRocks(GetUser(), GetUser().Id)
               //     .Where(x => currentPeriods.Any(y => y.Id == x.PeriodId))
             //       .Select(x => new AngularRock(x));

                output.Rocks = rocks;
            }

			// var directReports = _UserAccessor.GetDirectSubordinates(GetUser(), GetUser().Id).Select(x=>AngularUser.CreateUser(x));
            if (tiles.Any(x => x.Type == TileType.Manage || (x.DataUrl ?? "").Contains("UserManage")))
            {
                var directReports = _OrganizationAccessor.GetOrganizationMembersLookup(GetUser(), GetUser().Organization.Id, true, PermissionType.EditEmployeeDetails)
                    .Select(x => AngularUser.CreateUser(x, managing: true)).ToList();
                //if (!GetUser().IsRadialAdmin)
                //{
                var managingIds = _DeepSubordianteAccessor.GetSubordinatesAndSelf(GetUser(), GetUser().Id);
                directReports = directReports.Where(x => managingIds.Contains(x.Id)).ToList();
                //}
                output.Members = directReports;
            }
            var caller = GetUser();
            using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
                    var l10Lookup = new DefaultDictionary<long,L10Recurrence>(x=>L10Accessor.GetL10Recurrence(caller,x,false));
                    var perms = PermissionsUtility.Create(s,caller);
                    //L10 Todos
                    foreach (var todo in tiles.Where(x => x.Type == TileType.L10Todos|| (x.DataUrl ?? "").Contains("L10Todos")).Distinct(x => x.KeyId))
                    {
                        long l10Id = 0;
                        if (long.TryParse(todo.KeyId, out l10Id))
                        {
                            var tile = new DashboardController.AngularTileId<List<AngularTodo>>(todo.Id,l10Id, l10Lookup[l10Id].Name + " to-dos");
                            tile.Contents = L10Accessor.GetAllTodosForRecurrence(s, perms, l10Id,false).Select(x => new AngularTodo(x)).ToList();
                            output.L10Todos.Add(tile);
                        }
                    }
                    //L10 Issues
                    foreach (var issue in tiles.Where(x => x.Type == TileType.L10Issues|| (x.DataUrl ?? "").Contains("L10Issues")).Distinct(x => x.KeyId))
                    {
                        long l10Id = 0;
                        if (long.TryParse(issue.KeyId, out l10Id))
                        {
                            var tile = new DashboardController.AngularTileId<AngularIssuesList>(issue.Id,l10Id, l10Lookup[l10Id].Name + " issues");
                            tile.Contents = new AngularIssuesList(l10Id) {
                                Issues = L10Accessor.GetIssuesForRecurrence(s, perms, l10Id).Select(x => new AngularIssue(x)).ToList(),
                                Prioritization = l10Lookup[l10Id].Prioritization,
                            };
                            output.L10Issues.Add(tile);
                        }
                    }
                    //L10 Rocks
                    foreach (var rock in tiles.Where(x => x.Type == TileType.L10Rocks|| (x.DataUrl ?? "").Contains("L10Rocks")).Distinct(x => x.KeyId))
                    {
                        long l10Id = 0;
                        if (long.TryParse(rock.KeyId, out l10Id))
                        {
                            var tile = new DashboardController.AngularTileId<List<AngularRock>>(rock.Id,l10Id, l10Lookup[l10Id].Name + " rocks");
                            tile.Contents = L10Accessor.GetRocksForRecurrence(s, perms, l10Id).Select(x => new AngularRock(x.ForRock)).ToList();
                            output.L10Rocks.Add(tile);
                        }
                    }
                    //L10 Scorecard
                    foreach (var scorecard in tiles.Where(x => x.Type == TileType.L10Scorecard || (x.DataUrl ?? "").Contains("L10Scorecard")).Distinct(x => x.KeyId))
                    {
                        long l10Id = 0;
                        if (long.TryParse(scorecard.KeyId, out l10Id))
                        {
                            var tile = new DashboardController.AngularTileId<AngularScorecard>(scorecard.Id, l10Id, l10Lookup[l10Id].Name + " scorecard");
                            //tile.Contents;

                            var scores = L10Accessor.GetScoresForRecurrence(s,perms,l10Id, false);
                            var measurables = scores.Select(x => x.Measurable).Distinct(x => x.Id).ToList();

                            //var scores = ScorecardAccessor.GetUserScores(GetUser(), GetUser().Id, start, end, includeAdmin: true);
                            tile.Contents= new AngularScorecard(scorecard.Id,
                                GetUser().Organization.Settings.WeekStart,
                                GetUser().Organization.GetTimezoneOffset(),
                                measurables.Select(x => new AngularMeasurable(x) { }),
                                scores.ToList(),
                                DateTime.UtcNow,
                                GetUser().Organization.Settings.ScorecardPeriod,
                                new YearStart(GetUser().Organization)
                                );
                            output.L10Scorecards.Add(tile);
                        }
                    }
                }
           }

			return Json(output, JsonRequestBehavior.AllowGet);
		}
    }

    public class DashboardController : BaseController
    {
        public class AngularTileId<T> : BaseAngular
        {
            public long KeyId { get; set; }
            public string Title { get; set; }
            public T Contents { get; set; }

            public AngularTileId(long tile, long keyId, string title) : base(tile)
            {
                KeyId = keyId;
                Title = title;
            }
        }

        public class ListDataVM : BaseAngular
        {
            public string Name { get; set; }
            public IEnumerable<AngularTodo> Todos { get; set; }
            public AngularScorecard Scorecard { get; set; }
            public IEnumerable<AngularRock> Rocks { get; set; }
            public IEnumerable<AngularUser> Members { get; set; }

            public AngularDateRange date { get; set; }

            public class DateVM
            {
                public DateTime startDate { get; set; }
                public DateTime endDate { get; set; }
            }

            public List<AngularTileId<AngularScorecard>> L10Scorecards { get; set; }
            public List<AngularTileId<List<AngularRock>>> L10Rocks { get; set; }
            public List<AngularTileId<AngularIssuesList>> L10Issues { get; set; }
            public List<AngularTileId<List<AngularTodo>>> L10Todos { get; set; }


            public ListDataVM(long id) : base(id)
            {
                L10Scorecards = new List<AngularTileId<AngularScorecard>>();
                L10Rocks = new List<AngularTileId<List<AngularRock>>>();
                L10Issues = new List<AngularTileId<AngularIssuesList>>();
                L10Todos = new List<AngularTileId<List<AngularTodo>>>();
            }
        }

        public class TileVM
        {
            public int w { get; set; }
            public int h { get; set; }
            public int x { get; set; }
            public int y { get; set; }
            public long id { get; set; }
        }
        [Access(AccessLevel.UserOrganization)]
        public JsonResult Tiles(long id)
        {
            var dashboardId = id;
            var tiles = DashboardAccessor.GetTiles(GetUser(), dashboardId);
            return Json(ResultObject.SilentSuccess(tiles), JsonRequestBehavior.AllowGet);
        }
        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public JsonResult UpdateTiles(long id, IEnumerable<TileVM> model)
        {
            var dashboardId = id;
            DashboardAccessor.EditTiles(GetUser(), dashboardId, model);
            return Json(ResultObject.SilentSuccess());
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult Tile(long id, bool? hidden = null, int? w = null, int? h = null, int? x = null, int? y = null, string dataurl = null, string title = null)
        {
            var tile = DashboardAccessor.EditTile(GetUser(), id, h, w, x, y, hidden, dataurl, title);
            tile.ForUser = null;
            tile.Dashboard = null;
            return Json(ResultObject.SilentSuccess(tile), JsonRequestBehavior.AllowGet);
        }
        [Access(AccessLevel.UserOrganization)]
        public ActionResult CreateDashboard(string title = null, bool primary = false)
        {
            var dash = DashboardAccessor.CreateDashboard(GetUser(), title, primary);
            return RedirectToAction("Index", new { id = dash.Id });
        }

        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateTileModal()
        {
            return PartialView();
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult CreateTile(long id, bool? hidden = null, int w = 1, int h = 1, int x = 0, int y = 0, TileType type = TileType.Invalid, string dataurl = null, string title = null,string keyId=null)
        {
            var tile = DashboardAccessor.CreateTile(GetUser(), id, w, h, x, y, dataurl, title, type, keyId);
            tile.ForUser = null;
            tile.Dashboard = null;
            return Json(ResultObject.SilentSuccess(tile), JsonRequestBehavior.AllowGet);
        }

        public class DashboardVM
        {
            public long DashboardId { get; set; }
            public String TileJson { get; set; }
            public List<SelectListItem> L10s { get; set; }

            public DashboardVM()
            {
                L10s = new List<SelectListItem>();
            }
        }


        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index(long? id = null)
        {
            if (id == null)
                id = DashboardAccessor.GetPrimaryDashboardForUser(GetUser(), GetUser().Id).NotNull(x => x.Id);
            if (id == 0)
            {
                id = DashboardAccessor.CreateDashboard(GetUser(), null, false, true).Id;
                return RedirectToAction("Index", new { id = id });
            }

            var tiles = DashboardAccessor.GetTiles(GetUser(), id.Value);

            var l10s = L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id,true);


            var jsonTiles = Json(ResultObject.SilentSuccess(tiles), JsonRequestBehavior.AllowGet);
            var jsonTilesStr = new JavaScriptSerializer().Serialize(jsonTiles.Data);



            ViewBag.UserId = GetUser().Id;
            return View(new DashboardVM()
            {
                DashboardId = id.Value,
                TileJson = jsonTilesStr,
                L10s = l10s.Select(x => new SelectListItem() { Value =""+x.Id,Text = x.Name}).ToList()
            });
        }



        //
        // GET: /Dashboard/

    }
}