using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Json;
using RadialReview.Models.Survey;
using RadialReview.Utilities;
using System.Threading.Tasks;

namespace RadialReview.Controllers
{
    public class SurveysController : BaseController
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
			var s = OldSurveyAccessor.GetAllSurveyContainersForOrganization(GetUser(),GetUser().Organization.Id);
	        var q = OldSurveyAccessor.GetAllSurveyQuestionGroupsForOrganization(GetUser(), GetUser().Organization.Id);
	        var r = OldSurveyAccessor.GetAllSurveyRespondentGroupsForOrganization(GetUser(), GetUser().Organization.Id);
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
					OrgId = GetUser().Organization.Id,
					CreatorId = GetUser().Id
		        };
	        }else{
		        container = OldSurveyAccessor.GetSurveyContainer(GetUser(), id);
				PermissionsAccessor.Permitted(GetUser(),x=>x.EditOldSurvey(id));
	        }

			return View(container);
        }

		[Access(AccessLevel.Any)]
		public ActionResult Take(string id=null)
		{
			//SurveyContainerModel container;
			if (id == null){
				throw new PermissionsException("This id does not exist.");
			}

			var takeSurvey = OldSurveyAccessor.LoadSurvey(id,Request.UserAgent,Request.UserHostAddress,Request.UrlReferrer.NotNull(x=>x.ToString()));

			return View(takeSurvey);
		}


	    [Access(AccessLevel.Any)]
	    public ActionResult OpenEnded(string id,string respondent=null,bool embedded=false)
	    {
			var takeSurvey = OldSurveyAccessor.LoadOpenEndedSurvey(respondent, id, Request.UserAgent, Request.UserHostAddress, Request.UrlReferrer.NotNull(x=>x.ToString()));
		    if (embedded){
			    return PartialView("Embedded", takeSurvey);
			}
			return View("Take", takeSurvey);
	    }

	    [Access(AccessLevel.Any)]
		
		public JsonResult Set(string id,int? value=null,string str=null)
		{
			OldSurveyAccessor.SetValue(id,value,str);
			return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
	    public ActionResult Results(long id)
	    {
			//_PermissionsAccessor.Permitted(GetUser(),x=>x.ViewSurveyContainer(id));
			//var survey= SurveyAccessor.GetSurveyContainer(GetUser(), id);
			var results = OldSurveyAccessor.GetResults(GetUser(), id);
			return View(results);
	    }

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Edit(SurveyContainerModel model)
		{
			ValidateValues(model, x => x.Id, x => x.OrgId, x => x.CreateTime, x => x.DeleteTime, x => x.CreatorId);
			if (ModelState.IsValid){
				OldSurveyAccessor.EditSurvey(GetUser(), model);
				if (Request.Form["Submit"].Contains("Issue"))
				{
					if (!model.QuestionGroup._Questions.Any())
						ModelState.AddModelError("Questions", "You must add at least one question.");
					if (!model.RespondentGroup._Respondents.Any() &&  !model.OpenEnded)
						ModelState.AddModelError("Questions", "You must add at least one respondent or it must be embeddable.");

					if (ModelState.IsValid)
						await OldSurveyAccessor.IssueSurvey(GetUser(), model.Id);
					else
						return View(model);
				}
				return RedirectToAction("Index");
			}
			return View(model);
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public ActionResult ImportRespondents(string emails)
		{
			var sb = new StringBuilder();
			foreach (var e in emails.Split('\n'))
			{
				var ee = e.Trim();
				if (!String.IsNullOrWhiteSpace(ee))
				{
					sb.Append(ViewUtility.RenderPartial("~/Views/Survey/_SurveyRespondentRow.cshtml", new SurveyRespondentModel() { Email = ee }));
				}
			}
			return Content(sb.ToString());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult ImportQuestions(string questions)
		{
			var sb = new StringBuilder();
			foreach (var e in questions.Split('\n'))
			{
				var ee = e.Trim();
				if (!String.IsNullOrWhiteSpace(ee))
				{
					sb.Append(ViewUtility.RenderPartial("~/Views/Survey/_SurveyQuestionRow.cshtml", new SurveyQuestionModel() { Question = ee,QuestionType = SurveyQuestionType.Radio}));
				}
			}
			return Content(sb.ToString());
		}


		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult BlankQuestionEditorRow()
		{
			return PartialView("_SurveyQuestionRow", new SurveyQuestionModel());
		}
		
		[Access(AccessLevel.Manager)]
        public PartialViewResult BlankRespondentEditorRow()
		{
			return PartialView("_SurveyRespondentRow", new SurveyRespondentModel());
		}

    }
}