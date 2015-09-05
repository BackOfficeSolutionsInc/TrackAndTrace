using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
    public partial class VTOController : BaseController
    {
        // GET: VtoData
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularVtoString(AngularVtoString model, string connectionId = null)
		{
			VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
        }
    }
}