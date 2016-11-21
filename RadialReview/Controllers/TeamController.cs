using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.ViewModels;
using RadialReview.Models.UserModels;
using RadialReview.Models;

namespace RadialReview.Controllers
{
    public class TeamController : BaseController
    {       
        
        //
        // GET: /Team/
        [Access(AccessLevel.Manager)]
        public ActionResult Index()
        {
            return View();
        }

        [Access(AccessLevel.Manager)]
        public PartialViewResult Modal(long id = 0)
        {
            var user = GetUser();
            var team = TeamAccessor.GetTeam(user, id);
            var managers = _OrganizationAccessor.GetOrganizationManagers(GetUser(), GetUser().Organization.Id)
                                                .ToListAlive()
                                                .ToSelectList(x=>x.GetNameAndTitle(2,user.Id),x=>x.Id)
                                                .ToList();

            var modal = new OrganizationTeamCreateViewModel(GetUser(),team, managers);
            return PartialView("Modal", modal);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult Modal(OrganizationTeamCreateViewModel model)
        {
            var user = GetUser();
            var team = TeamAccessor.EditTeam(user, model.TeamId,model.TeamName,model.InterReview,true,model.ManagerId);
			return Json(ResultObject.Success("Team has been updated.").ForceRefresh());
        }
        /*
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
        */
        /*
        public ActionResult Create()
        {
            return View("Edit", new OrganizationTeamModel());
        }

        */

        public class TeamViewModel
        {
            public OrganizationTeamModel Team { get;set;}
            public List<TeamDurationModel> Members { get; set; }
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Edit(long id)
        {
            var teamId = id;

            var team = TeamAccessor.GetTeam(GetUser(), teamId);
			var members = TeamAccessor.GetTeamMembers(GetUser(), teamId, false);

            var model = new TeamViewModel()
            {
                Members = members,
                Team = team
            };

            return View(model);
        }

        public class AddModalViewModel
        {
            public long TeamId { get;set;}
            public List<SelectListItem> Users { get; set; }

            public long SelectedUserId { get; set; }
        }

        [Access(AccessLevel.Manager)]
        public PartialViewResult AddModal(long id)
        {
            var teamId = id;
            var members = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id,false,false);
			var alreadyMember = TeamAccessor.GetTeamMembers(GetUser(), teamId, false).ToListAlive();

            var notMembers=members.Where(x=>alreadyMember.All(y => y.User.Id != x.Id)).OrderBy(x=>x.GetName());

            var model = new AddModalViewModel(){
                TeamId = id,
                Users = notMembers.ToSelectList(x => x.GetNameAndTitle(), x => x.Id).ToList(),
            };
            return PartialView(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult AddModal(AddModalViewModel model)
        {
            if (model.SelectedUserId == 0)
                return Json(new ResultObject(true, "Id of zero is not allowed."));
            TeamAccessor.AddMember(GetUser(), model.TeamId, model.SelectedUserId);
            return Json(ResultObject.Success("Added member.").ForceRefresh());
        }
        /*
        [HttpPost]
        public ActionResult Edit(long id, String name, long? organizationId)
        {
            var user = GetOneUserOrganization(organizationId);
            var team = _TeamAccessor.EditTeam(user, id);
            return View(team);
        }*/

        [Access(AccessLevel.Manager)]
        public JsonResult AddMember(long teamId,long userId)
        {
            var user=GetUser();
            TeamAccessor.AddMember(user,teamId,userId);
            return Json(ResultObject.Success("Added member.").ForceRefresh(), JsonRequestBehavior.AllowGet);
        }

	}
}