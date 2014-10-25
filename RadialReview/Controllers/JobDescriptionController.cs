using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
    public class JobDescriptionController : BaseController
    {
	    public class JobDescriptionVM
	    {
			public long UserId { get; set; }
			public String JobDescription { get; set; }
	    }

		[Access(AccessLevel.Manager)]
		public PartialViewResult Modal(long id) {
			var user = _UserAccessor.GetUserOrganization(GetUser(), id, false, false);
			return PartialView(new JobDescriptionVM{JobDescription = user.JobDescription, UserId = user.Id});
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult Modal(JobDescriptionVM model)
		{

			_UserAccessor.EditJobDescription(GetUser(), model.UserId, model.JobDescription);

			return Json(ResultObject.SilentSuccess());
		}
    }
}