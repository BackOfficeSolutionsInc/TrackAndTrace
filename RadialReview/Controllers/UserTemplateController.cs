using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using RadialReview.Models.UserTemplate;
using RadialReview.Models.ViewModels;
using System.Threading.Tasks;

namespace RadialReview.Controllers
{
    public class UserTemplateController : BaseController
    {
		// GET: UserTemplate
		[Access(AccessLevel.Manager)]
		public async Task<ActionResult> Create(long id, string type) {
			var ttype = AttachType.Invalid;

			Enum.TryParse(type, true, out ttype);

			var ut = new UserTemplate() {
				AttachId = id,
				AttachType = ttype,
				OrganizationId = GetUser().Organization.Id,
			};

			await UserTemplateAccessor.CreateTemplate(GetUser(), ut);

			return RedirectToAction("Edit", new { id = ut.Id });
		}
		[Access(AccessLevel.Manager)]
		public ActionResult Edit(long id)
		{
			var ut = UserTemplateAccessor.GetUserTemplate(GetUser(), id,true,true,true);

			var edit = PermissionsAccessor.IsPermitted(GetUser(), x => x.EditTemplate(id));

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