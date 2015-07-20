using RadialReview.Accessors;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class DashboardController : BaseController
    {
        public class ListDataVM : BaseAngular
        {
            public string Name { get; set; }
            public IEnumerable<AngularTodo> Todos { get; set; }
            public AngularScorecard Scorecard { get; set; }
            public ListDataVM(long id) : base(id) { }
        }

    
        //
        // GET: /Dashboard/
        [Access(AccessLevel.UserOrganization)]
        public JsonResult Data(long id)
        {
            var userId = id;
            //Todos
            var todos = TodoAccessor.GetTodosForUser(GetUser(), id).Select(x => new AngularTodo(x));
            var m = _UserAccessor.GetUserOrganization(GetUser(), id, false, true, PermissionType.ViewTodos);

            //Scorecard
            var measurables = ScorecardAccessor.GetUserMeasurables(GetUser(), GetUser().Id);
            var scores = ScorecardAccessor.GetUserScores(GetUser(), GetUser().Id, DateTime.UtcNow.AddDays(-7 * 13), DateTime.UtcNow.AddDays(9));
            var sc = new AngularScorecard(
                GetUser().Organization.Settings.WeekStart,
                GetUser().Organization.GetTimezoneOffset(),
                measurables.Select(x => new AngularMeasurable(x)),
                scores.ToList());

            return Json(new ListDataVM(id)
            {
                Todos = todos,
                Scorecard=sc,
                Name = "All to-dos for " + m.GetName()
            }, JsonRequestBehavior.AllowGet);
        }
	}
}