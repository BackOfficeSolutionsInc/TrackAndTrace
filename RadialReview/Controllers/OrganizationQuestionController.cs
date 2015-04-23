using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Controllers
{
    public class OrganizationQuestionController : BaseController
    {
	    public class OrgQuestionVM
	    {
			public long OrganizationId { get; set; }
		    public List<AboutCompanyAskable> Questions { get; set; } 
			public DateTime CurrentTime { get; set; }
			public bool UpdateOutstandingReviews { get; set; }
		    public OrgQuestionVM()
		    {
			    CurrentTime = DateTime.UtcNow;
		    }

	    }

		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankEditorRow()
		{
			var qt = new List<SelectListItem>();

			var vals = new[]{QuestionType.Slider, QuestionType.Thumbs, QuestionType.Feedback};
			qt = vals.Select(x => new SelectListItem(){
				Text =x.ToString(), 
				Value = x.ToString()
			}).ToList();

			ViewBag.QuestionTypes = qt;
			
			return PartialView("_CompanyQuestionRow", new AboutCompanyAskable(){
				CreateTime = DateTime.UtcNow,
				Organization =  GetUser().Organization,
				OrganizationId = GetUser().Organization.Id,
			});
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult Modal()
		{
			var questions = OrganizationAccessor.GetQuestionsAboutCompany(GetUser(),GetUser().Organization.Id,null);

			var qt = new List<SelectListItem>();

			var vals = new[] { QuestionType.Slider, QuestionType.Thumbs, QuestionType.Feedback };
			qt = vals.Select(x => new SelectListItem(){
				Text = x.ToString(),
				Value = x.ToString()
			}).ToList();

			ViewBag.QuestionTypes = qt;

			var vm = new OrgQuestionVM(){
				CurrentTime = DateTime.UtcNow,
				OrganizationId = GetUser().Id,
				Questions = questions,
			};

			return PartialView(vm);
		}
		[Access(AccessLevel.Manager)]
		[HttpPost]
		public JsonResult Modal(OrgQuestionVM model)
		{
			OrganizationAccessor.EditQuestionsAboutCompany(GetUser(), model.Questions);
			return Json(ResultObject.SilentSuccess(false));
		}

    }
}