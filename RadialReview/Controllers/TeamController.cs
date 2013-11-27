using RadialReview.Accessors;
using RadialReview.Models.AccountabilityGroupModels;
using RadialReview.Models.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class TeamController : BaseController
    {
        protected TeamAccessor _TeamAccessor = new TeamAccessor();
        //
        // GET: /Team/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            return View("Edit", new TeamModel());
        }

        public ActionResult Edit(long id, long? organizationId)
        {
            var user=GetOneUserOrganization(organizationId);
            var team = _TeamAccessor.GetTeam(user, id);
            return View(team);
        }

        [HttpPost]
        public ActionResult Edit(long id, String name, long? organizationId)
        {
            var user = GetOneUserOrganization(organizationId);
            var team = _TeamAccessor.EditTeam(user, id);
            return View(team);
        }

        public JsonResult AddMember(long teamId,long userId,long? organizationId)
        {
            var user=GetOneUserOrganization(organizationId);
            _TeamAccessor.AddMember(user,teamId,userId);
            return Json(JsonObject.Success, JsonRequestBehavior.AllowGet);
        }

	}
}