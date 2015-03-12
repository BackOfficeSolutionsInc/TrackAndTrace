using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
    public class JobDescriptionController : BaseController
    {
	    public class JobDescriptionVM
		{
			public long TemplateId { get; set; }
			public long UserId { get; set; }
			public String JobDescription { get; set; }
			public bool Locked { get; set; }
			public bool Override { get; set; }
	    }

		[Access(AccessLevel.Manager)]
		public PartialViewResult Modal(long id) {
			var user = _UserAccessor.GetUserOrganization(GetUser(), id, false, false);
			return PartialView(new JobDescriptionVM{JobDescription = user.JobDescription, UserId = user.Id, Locked = user.JobDescriptionFromTemplateId!=null});
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult Modal(JobDescriptionVM model)
		{
			_UserAccessor.EditJobDescription(GetUser(), model.UserId, model.JobDescription);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult TemplateModal(long id)
		{
			var template = UserTemplateAccessor.GetUserTemplate(GetUser(), id);
			return PartialView(new JobDescriptionVM { JobDescription = template.JobDescription,TemplateId = id});
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult TemplateModal(JobDescriptionVM model)
		{
			ValidateValues(model,x=>x.TemplateId);
			UserTemplateAccessor.UpdateJobDescription(GetUser(), model.TemplateId, model.JobDescription,model.Override);
			return Json(ResultObject.SilentSuccess());
		}
    }
}