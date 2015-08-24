using RadialReview.Accessors;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
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

    
        //
        // GET: /Dashboard/
        [Access(AccessLevel.UserOrganization)]
        public JsonResult Data(long id,bool completed=true,string name =null)
        {
            var userId = id;
            //Todos
			var todos = TodoAccessor.GetTodosForUser(GetUser(), id, !completed).Select(x => new AngularTodo(x));
            var m = _UserAccessor.GetUserOrganization(GetUser(), id, false, true, PermissionType.ViewTodos);

            //Scorecard
            var measurables = ScorecardAccessor.GetUserMeasurables(GetUser(), GetUser().Id);
            var scores = ScorecardAccessor.GetUserScores(GetUser(), GetUser().Id, DateTime.UtcNow.AddDays(-7 * 13), DateTime.UtcNow.AddDays(14));
            var sc = new AngularScorecard(
                GetUser().Organization.Settings.WeekStart,
                GetUser().Organization.GetTimezoneOffset(),
                measurables.Select(x => new AngularMeasurable(x)),
                scores.ToList());
	        var now = DateTime.UtcNow;
			var currentPeriods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).Where(x => x.StartTime <= now && now <= x.EndTime);

	        var rocks = _RockAccessor.GetAllRocks(GetUser(), GetUser().Id)
				.Where(x=>currentPeriods.Any(y=>y.Id==x.PeriodId))
				.Select(x=>new AngularRock(x));


	       // var directReports = _UserAccessor.GetDirectSubordinates(GetUser(), GetUser().Id).Select(x=>AngularUser.CreateUser(x));

	        var directReports = _OrganizationAccessor.GetOrganizationMembersLookup(GetUser(), GetUser().Organization.Id, true, PermissionType.EditEmployeeDetails)
		        .Select(x => AngularUser.CreateUser(x,managing:true)).ToList();



	        if (!GetUser().IsRadialAdmin){
				var managingIds = _DeepSubordianteAccessor.GetSubordinatesAndSelf(GetUser(), GetUser().Id);
				directReports = directReports.Where(x => managingIds.Contains(x.Id)).ToList();
	        }

	        return Json(new ListDataVM(id)
            {
				Name = name,
                Todos = todos,
                Scorecard=sc,
				Rocks = rocks,
				Members = directReports
                //Name = "All to-dos for " + m.GetName()
            }, JsonRequestBehavior.AllowGet);
        }
	}
}