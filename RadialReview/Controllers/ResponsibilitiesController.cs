﻿using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Askables;
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

        //
        // GET: /Responsibilities/
        [Access(AccessLevel.Manager)]
        public ActionResult Index()
        {
            return View();
        }

        [Access(AccessLevel.Manager)]
        public PartialViewResult Modal(long responsibilityGroupId = 0, long id = 0)
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
        [Access(AccessLevel.Manager)]
        public JsonResult Modal(ResponsibilityViewModel model)
        {
            var caller = GetUser();
			/*if (model.CategoryId == -1)
            {
                var category=_CategoryAccessor.Edit(caller, 0, new Origin(OriginType.Organization, caller.Organization.Id), new LocalizedStringModel(model.NewCategory), true);
                model.CategoryId = category.Id;
            }
	        var catagoryId = 0L;
	        var questionType = QuestionType.Invalid;
	        if (model.TypeSelected > 0){
		        catagoryId = model.TypeSelected;
				questionType=QuestionType.Slider;
			}
			if (model.TypeSelected == -9) {// Thumbs
				questionType = QuestionType.Thumbs;
			}
			if (model.TypeSelected == -10) {// Feedback
				questionType = QuestionType.Feedback;
			}
			*/
			var oa = AboutType.NoRelationship;
			foreach (var onlyAsk in model.OnlyAsk) {
				oa = oa | onlyAsk;
			}


			
	        _ResponsibilitiesAccessor.EditResponsibility(caller, model.Id, model.Responsibility, model.CategoryId, model.ResponsibilityGroupId,model.Active,model.Weight,model.Required, oa, model.UpdateOutstandingReviews);
            return Json(ResultObject.Success("Responsibility updated."));
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Edit(long id)
        {
            var responsibilityGroupId = id;
            var r = _ResponsibilitiesAccessor.GetResponsibilityGroup(GetUser(), responsibilityGroupId).HydrateResponsibilityGroup().PersonallyManaging(GetUser()).Execute();
            var model = new ResponsibilityTablesViewModel(r);

            if (r is UserOrganizationModel)
            {
                ViewBag.Page = "Members";
                var teams = TeamAccessor.GetUsersTeams(GetUser(), responsibilityGroupId);
                var userResponsibility = ((UserOrganizationModel)r).Hydrate().Position().SetTeams(teams).Execute();
                foreach (var rgId in userResponsibility.Positions.ToListAlive().Select(x => x.Position.Id))
                {
                    var positionResp = _ResponsibilitiesAccessor.GetResponsibilityGroup(GetUser(), rgId).HydrateResponsibilityGroup().PersonallyManaging(GetUser()).Execute();
                    model.ResponsibilityTables.Add(new ResponsibilityTableViewModel(false, positionResp));
                }
                foreach (var teamId in userResponsibility.Teams.ToListAlive().Select(x => x.Team.Id))
                {
                    var teamResp = _ResponsibilitiesAccessor.GetResponsibilityGroup(GetUser(), teamId).HydrateResponsibilityGroup().PersonallyManaging(GetUser()).Execute();
                    model.ResponsibilityTables.Add(new ResponsibilityTableViewModel(false, teamResp));
                }
            }
            else if (r is OrganizationPositionModel)
            {
                ViewBag.Page = "Positions";
            }
            else if (r is OrganizationTeamModel)
            {
                ViewBag.Page = "Teams";
            }

            return View(model);
        }


        [Access(AccessLevel.Manager)]
        public JsonResult ToggleActive(long id,bool active)
        {
            return Json(ResultObject.SilentSuccess(_ResponsibilitiesAccessor.SetActive(GetUser(), id, active)),JsonRequestBehavior.AllowGet);
        }
	}
}