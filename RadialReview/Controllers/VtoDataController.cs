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
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularVtoRock(AngularVtoRock model, string connectionId = null)
		{
			VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularOneYearPlan(AngularOneYearPlan model, string connectionId = null)
		{
			VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularQuarterlyRocks(AngularQuarterlyRocks model, string connectionId = null)
		{
			VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularThreeYearPicture(AngularThreeYearPicture model, string connectionId = null)
		{
			VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularStrategy(AngularStrategy model, string connectionId = null)
		{
			VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddThreeYear(long vto, string connectionId = null)
		{
			VtoAccessor.AddThreeYear(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddYearGoal(long vto, string connectionId = null)
		{
			VtoAccessor.AddYearGoal(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}	
		
		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddCompanyValue(long vto, string connectionId = null)
		{
			VtoAccessor.AddCompanyValue(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddRock(long vto, string connectionId = null)
		{
			VtoAccessor.CreateNewRock(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult DeleteRock(long value, string connectionId = null)
		{
			VtoAccessor.UpdateRock(GetUser(), value,null,null,true,connectionId);
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
			VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularCoreFocus(AngularCoreFocus model, string connectionId = null)
		{
			VtoAccessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddUniques(long vto, string connectionId = null)
		{
			VtoAccessor.AddUniques(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddIssue(long vto, string connectionId = null)
		{
			VtoAccessor.AddIssue(GetUser(), vto);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult DeleteString(long value, string connectionId = null)
		{
			VtoAccessor.UpdateVtoString(GetUser(), value, null, true, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
	    public JsonResult XUpdateRock(string pk,string name,string value)
	    {
		    switch(name.ToLower()){
				case "accountable": VtoAccessor.UpdateRockAccountable(GetUser(), pk.ToLong(), value.ToLong());
				    break;
				default: throw new ArgumentOutOfRangeException(name.ToLower());
		    }
		    return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
	    }


    }
}