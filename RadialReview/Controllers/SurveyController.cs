using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Json;
using RadialReview.Models.Survey;

namespace RadialReview.Controllers
{
    public class SurveyController : BaseController
    {
	    public class SurveyListing
	    {
			public List<SurveyContainerModel> Issued { get; set; }
			public List<SurveyRespondentGroupModel> RespondentGroups { get; set; }
			public List<SurveyQuestionGroupModel> QuestionGroups { get; set; }
	    }


        //
        // GET: /Survey/
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
		{
			var s = SurveyAccessor.GetAllSurveyContainersForOrganization(GetUser(),GetUser().Organization.Id);
	        var q = SurveyAccessor.GetAllSurveyQuestionGroupsForOrganization(GetUser(), GetUser().Organization.Id);
	        var r = SurveyAccessor.GetAllSurveyRespondentGroupsForOrganization(GetUser(), GetUser().Organization.Id);
            return View(new SurveyListing(){
				Issued = s,
				RespondentGroups =r,
				QuestionGroups = q
			});
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Edit(long id =0)
        {
	        SurveyContainerModel container;
	        if (id == 0){
		        container = new SurveyContainerModel(){
			        Organization = GetUser().Organization,
					Creator = GetUser()
		        };
	        }else{
		        container = SurveyAccessor.GetSurveyContainer(GetUser(), id);
	        }

			return View(container);
        }

		[Access(AccessLevel.Any)]
		public ActionResult Take(string id=null)
		{
			SurveyContainerModel container;
			if (id == null){
				throw new PermissionsException("This id does not exist.");
			}

			var takeSurvey = SurveyAccessor.LoadSurvey(id,Request.UserAgent,Request.UserHostAddress);

			return View(takeSurvey);
		}

		[Access(AccessLevel.Any)]
		
		public JsonResult Set(string id,int? value)
		{
			SurveyAccessor.SetValue(id,value);
			return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
		}


		[HttpPost]
		[Access(AccessLevel.Manager)]
		public ActionResult Edit(SurveyContainerModel model)
		{
			ValidateValues(model,x=>x.Id,x=>x.Organization.Id,x=>x.CreateTime,x=>x.DeleteTime,x=>x.Creator.Id);
			if (ModelState.IsValid){
				SurveyAccessor.EditSurvey(GetUser(), model);
				return RedirectToAction("Index");
			}
			return View(model);
		}

		[Access(AccessLevel.Manager)]
		public ActionResult BlankQuestionEditorRow()
		{
			return PartialView("_SurveyQuestionRow", new SurveyQuestionModel());
		}
		
		[Access(AccessLevel.Manager)]
		public ActionResult BlankRespondentEditorRow()
		{
			return PartialView("_SurveyRespondentRow", new SurveyRespondentModel());
		}

    }
}