using RadialReview.Accessors;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.ViewModels;

namespace RadialReview.Controllers
{
    public class TeamController : BaseController
    {
        protected TeamAccessor _TeamAccessor = new TeamAccessor();
        protected ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();
        //
        // GET: /Team/
        public ActionResult Index()
        {
            return View();
        }
        
        public ActionResult Modal(long id=0)
        {
            var user = GetUser();
            var team = _TeamAccessor.GetTeam(user, id);
            return PartialView("Modal",team);
        }

        [HttpPost]
        public JsonResult Modal(OrganizationTeamModel model)
        {
            var user = GetUser();
            var team = _TeamAccessor.EditTeam(user, model.ResponsibilityGroupId,model.Name,model.OnlyManagersEdit);
            return Json(JsonObject.Success);
        }

        public ActionResult Responsibilities(long id)
        {
            var teamId = id;
            var team = _TeamAccessor.GetTeam(GetUser(),id);
            var responsibilities=_ResponsibilitiesAccessor.GetResponsibilities(GetUser(), teamId);

            var model=new TeamResponsibilitiesViewModel()
            {
                Responsibilities=responsibilities,
                Team=team,
            };
            return View(model);
        }
        
        /*
        public ActionResult Create()
        {
            return View("Edit", new OrganizationTeamModel());
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
        }*/

        public JsonResult AddMember(long teamId,long userId)
        {
            var user=GetUser();
            _TeamAccessor.AddMember(user,teamId,userId);
            return Json(JsonObject.Success, JsonRequestBehavior.AllowGet);
        }

	}
}