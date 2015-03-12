using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using RadialReview.Models.UserTemplate;
using RadialReview.Models.ViewModels;

namespace RadialReview.Controllers
{
    public class UserTemplateController : BaseController
    {
        // GET: UserTemplate
		[Access(AccessLevel.Manager)]
		public ActionResult Create(long id, AttachType type)
		{
			var ut = new UserTemplate(){
				AttachId = id,
				AttachType = AttachType.Position,
				OrganizationId = GetUser().Organization.Id,
			};

			UserTemplateAccessor.CreateTemplate(GetUser(),ut);
			
			return RedirectToAction("Edit",new {id=ut.Id});
		}
		[Access(AccessLevel.Manager)]
		public ActionResult Edit(long id)
		{
			var ut = UserTemplateAccessor.GetUserTemplate(GetUser(), id,true,true,true);

			var edit = _PermissionsAccessor.IsPermitted(GetUser(), x => x.EditTemplate(id));

			var model = new UserTemplateVM(ut, edit);
			return View(model);
		}

	    [Access(AccessLevel.Manager)]
	    public bool Test()
	    {
		    return false;
	    }

    }
}