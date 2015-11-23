using System.Web.Script.Serialization;
using System.Web.SessionState;
using System.Web.UI;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Json;
using RadialReview.Models.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
	[SessionState(SessionStateBehavior.ReadOnly)]
	public class DashboardDataController : BaseController
	{
		[Access(AccessLevel.UserOrganization)]
		//[OutputCache(Duration = 3, VaryByParam = "id", Location = OutputCacheLocation.Client, NoStore = true)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public JsonResult Data2(long id, bool completed = true, string name = null)
		{
			var userId = id;
			//Todos
			var todos = TodoAccessor.GetTodosForUser(GetUser(), id, !completed).Where(x=>x.CompleteTime==null).Select(x => new AngularTodo(x));
			var m = _UserAccessor.GetUserOrganization(GetUser(), id, false, true, PermissionType.ViewTodos);

			//Scorecard
			var measurables = ScorecardAccessor.GetUserMeasurables(GetUser(), GetUser().Id,ordered:true, includeAdmin:true);

			
			var scores = ScorecardAccessor.GetUserScores(GetUser(), GetUser().Id, DateTime.UtcNow.AddDays(-7 * 13), DateTime.UtcNow.AddDays(14),includeAdmin:true);
			var sc = new AngularScorecard(
				GetUser().Organization.Settings.WeekStart,
				GetUser().Organization.GetTimezoneOffset(),
				measurables.Select(x => new AngularMeasurable(x){}),
				scores.ToList(),
				DateTime.UtcNow);
			var now = DateTime.UtcNow;
			var currentPeriods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).Where(x => x.StartTime <= now && now <= x.EndTime);

			var rocks = _RockAccessor.GetAllRocks(GetUser(), GetUser().Id)
				.Where(x => currentPeriods.Any(y => y.Id == x.PeriodId))
				.Select(x => new AngularRock(x));


			// var directReports = _UserAccessor.GetDirectSubordinates(GetUser(), GetUser().Id).Select(x=>AngularUser.CreateUser(x));

			var directReports = _OrganizationAccessor.GetOrganizationMembersLookup(GetUser(), GetUser().Organization.Id, true, PermissionType.EditEmployeeDetails)
				.Select(x => AngularUser.CreateUser(x, managing: true)).ToList();



			//if (!GetUser().IsRadialAdmin)
			//{
				var managingIds = _DeepSubordianteAccessor.GetSubordinatesAndSelf(GetUser(), GetUser().Id);
				directReports = directReports.Where(x => managingIds.Contains(x.Id)).ToList();
			//}

			return Json(new DashboardController.ListDataVM(id)
			{
				Name = name,
				Todos = todos,
				Scorecard = sc,
				Rocks = rocks,
				Members = directReports
				//Name = "All to-dos for " + m.GetName()
			}, JsonRequestBehavior.AllowGet);
		}
	}

    public class DashboardController : BaseController
    {
        public class ListDataVM : BaseAngular
        {
            public string Name { get; set; }
			public IEnumerable<AngularTodo> Todos { get; set; }
			public AngularScorecard Scorecard { get; set; }
			public IEnumerable<AngularRock> Rocks { get; set; }
			public IEnumerable<AngularUser> Members { get; set; }
            public ListDataVM(long id) : base(id) { }
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
			return Json(ResultObject.SilentSuccess(tiles),JsonRequestBehavior.AllowGet);
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
		public ActionResult CreateDashboard(string title = null,bool primary=false)
		{
			var dash = DashboardAccessor.CreateDashboard(GetUser(), title, primary);
			return RedirectToAction("Index",new {id=dash.Id});
		}

	    [Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateTileModal()
	    {
		    return PartialView();
	    }

		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateTile(long id, bool? hidden = null, int w = 1, int h = 1, int x = 0, int y = 0,TileType type=TileType.Invalid, string dataurl = null, string title = null)
		{
			var tile = DashboardAccessor.CreateTile(GetUser(), id, w, h, x, y, dataurl, title,type);
			tile.ForUser = null;
			tile.Dashboard = null;
			return Json(ResultObject.SilentSuccess(tile), JsonRequestBehavior.AllowGet);
		}

	    public class DashboardVM
	    {
		    public long DashboardId { get; set; }
			public String TileJson { get; set; }
	    }


		[Access(AccessLevel.UserOrganization)]
	    public ActionResult Index(long? id=null)
		{
			if (id == null)
				id = DashboardAccessor.GetPrimaryDashboardForUser(GetUser(), GetUser().Id).NotNull(x=>x.Id);
			if (id == 0){
				id = DashboardAccessor.CreateDashboard(GetUser(), null, false, true).Id;
				return RedirectToAction("Index", new{id = id});
			}

			var tiles = DashboardAccessor.GetTiles(GetUser(), id.Value);

			var jsonTiles = Json(ResultObject.SilentSuccess(tiles), JsonRequestBehavior.AllowGet);
			var jsonTilesStr = new JavaScriptSerializer().Serialize(jsonTiles.Data);
			
			

			ViewBag.UserId = GetUser().Id;
			return View(new DashboardVM(){
				DashboardId = id.Value,
				TileJson = jsonTilesStr
			});
		}
    


        //
        // GET: /Dashboard/
	
	}
}