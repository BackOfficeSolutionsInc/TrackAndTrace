using RadialReview.Accessors;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ManageController : BaseController
    {
        protected OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        //
        // GET: /Manage/
        public ActionResult Index()
        {
            //Main page

            return View();
        }

        public ActionResult Positions()
        {
            var orgPos = _OrganizationAccessor.GetOrganizationPositions(GetUser(), GetUser().Organization.Id);
            var model = new OrgPositionsViewModel() { Positions = orgPos.Select(x=>new OrgPosViewModel(x,0)).ToList() };
            return View(model);
        }

        public ActionResult Teams()
        {
            var orgTeams = _OrganizationAccessor.GetOrganizationTeams(GetUser(), GetUser().Organization.Id);
            var model = new OrganizationTeamViewModel() { Teams = orgTeams };
            return View(model);
        }

        public ActionResult Members()
        {
            var members = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id);
            for(int i=0;i<members.Count();i++)
            {
                members[i] = members[i].Hydrate().Teams().Execute();
            }
            var model = new OrgMembersViewModel(members);
            return View(model);
        }
    }
}