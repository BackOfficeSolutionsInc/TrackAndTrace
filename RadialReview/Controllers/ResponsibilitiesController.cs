﻿using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ResponsibilitiesController : BaseController
    {

        protected ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();
        protected CategoryAccessor _CategoryAccessor = new CategoryAccessor();

        //
        // GET: /Responsibilities/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Modal(long responsibilityGroupId=0,long id=0)
        {
            var caller=GetUser();

            var responsiblity=new ResponsibilityModel();
            if (id != 0)
            {
                responsiblity = _ResponsibilitiesAccessor.GetResponsibility(caller, id);
                responsibilityGroupId = responsiblity.ForResponsibilityGroup;
            }


            var categories=_CategoryAccessor.GetCategories(caller,caller.Organization.Id);
            var model = new ResponsibilityViewModel(responsibilityGroupId, responsiblity, categories);
            return PartialView(model);
        }

        /*[HttpPost]
        public JsonResult Add(ResponsibilityViewModel model)
        {
            var caller = GetUser();
            _ResponsibilitiesAccessor.EditResponsibility(caller, 0, model.Responsibility, model.CategoryId, model.ResponsiblityGroupId);
            return Json(JsonObject.Success);
        }*/

        [HttpPost]
        public JsonResult Modal(ResponsibilityViewModel model)
        {
            var caller = GetUser();
            if (model.CategoryId == -1)
            {
                var category=_CategoryAccessor.Edit(caller, 0, new Origin(OriginType.Organization, caller.Organization.Id), new LocalizedStringModel(model.NewCategory), true);
                model.CategoryId = category.Id;
            }
            _ResponsibilitiesAccessor.EditResponsibility(caller, model.Id, model.Responsibility, model.CategoryId, model.ResponsibilityGroupId);
            return Json(JsonObject.Success);
        }

        public ActionResult Edit(long id)
        {
            var responsibilityGroupId = id;
            var r = _ResponsibilitiesAccessor.GetResponsibilityGroup(GetUser(), responsibilityGroupId);
            var model = new ResponsibilityTablesViewModel(r);


            if (r is UserOrganizationModel)
            {
                var userResponsibility = ((UserOrganizationModel)r).Hydrate().Position().Teams().Execute();
                foreach (var rgId in userResponsibility.Positions.ToListAlive().Select(x => x.Position.Id))
                {
                    var positionResp = _ResponsibilitiesAccessor.GetResponsibilityGroup(GetUser(), rgId);
                    model.ResponsibilityTables.Add(new ResponsibilityTableViewModel(false, positionResp));
                }
                foreach (var teamId in userResponsibility.Teams.ToListAlive().Select(x => x.Team.Id))
                {
                    var teamResp = _ResponsibilitiesAccessor.GetResponsibilityGroup(GetUser(), teamId);
                    model.ResponsibilityTables.Add(new ResponsibilityTableViewModel(false, teamResp));
                }
            }





            return View(model);
        }
	}
}