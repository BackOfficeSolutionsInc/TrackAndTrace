﻿using RadialReview.Accessors;
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
        protected TeamAccessor _TeamAccessor = new TeamAccessor();
        protected PositionAccessor _PositionAccessor = new PositionAccessor();
        //
        // GET: /Manage/
        [Access(AccessLevel.Manager)]
        public ActionResult Index()
        {
            //Main page
            return View();
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Positions()
        {
            var orgPos = _OrganizationAccessor.GetOrganizationPositions(GetUser(), GetUser().Organization.Id);

            var positions = orgPos.Select(x=>
                new OrgPosViewModel(x, _PositionAccessor.GetUsersWithPosition(GetUser(), x.Id).Count())
            ).ToList();


            var model = new OrgPositionsViewModel() { Positions = positions};
            return View(model);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Teams()
        {
            var orgTeams = _TeamAccessor.GetOrganizationTeams(GetUser(), GetUser().Organization.Id);
            var model = new OrganizationTeamViewModel() { Teams = orgTeams };
            return View(model);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Members()
        {
            var members = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id);
            for(int i=0;i<members.Count();i++)
            {
                var u = members[i];
                var teams = _TeamAccessor.GetUsersTeams(GetUser(), u.Id);
                members[i] = members[i].Hydrate().SetTeams(teams).PersonallyManaging(GetUser()).Execute();
            }
            var model = new OrgMembersViewModel(members);
            return View(model);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Organization()
        {
            var user = GetUser().Hydrate().Organization().Execute();

            var model=new OrganizationViewModel(){
                Id=user.Organization.Id,
                ManagersCanEdit = user.Organization.ManagersCanEdit,
                OrganizationName = user.Organization.Name.Default.Value,
                StrictHierarchy = user.Organization.StrictHierarchy
            };

            return View(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public ActionResult Organization(OrganizationViewModel model)
        {
            _OrganizationAccessor.Edit(GetUser(),model.Id, model.OrganizationName, model.ManagersCanEdit, model.StrictHierarchy);
            ViewBag.Success = "Successfully Saved.";
            return View(model);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Reviews()
        {


            return View();
        }
    }
}