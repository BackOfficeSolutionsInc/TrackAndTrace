using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Angular.CompanyValue;
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

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularVto(AngularVTO model, string connectionId = null)
		{
			VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddCompanyValue(long vto, string connectionId = null)
		{
			VtoAccessor.AddCompanyValue(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult DeleteCompanyValue(long value, string connectionId = null)
		{
			VtoAccessor.UpdateCompanyValue(GetUser(), value, null, null, true, connectionId);
			return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularCompanyValue(AngularCompanyValue model, string connectionId = null)
		{
			VtoAccessor.Update(GetUser(), model,connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}


    }
}