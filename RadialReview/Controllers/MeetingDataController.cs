using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
    public partial class MeetingController : BaseController
    {
		[Access(AccessLevel.UserOrganization)]
	    public JsonResult SetPage(long id, string connection, string page=null)
	    {
			var recurrenceId = id;
			//page = page.ToLower();
			if (!String.IsNullOrEmpty(page))
				L10Accessor.UpdatePage(GetUser(), GetUser().Id, recurrenceId, page, connection);
			
		    return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
	    }

    }
}