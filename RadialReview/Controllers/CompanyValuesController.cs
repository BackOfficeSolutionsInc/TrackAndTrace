using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using RadialReview.Models.Askables;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
	public class CompanyValuesController : BaseController
	{
		public class CompanyValueVM
		{
			public long OrgId { get; set; }
			public List<CompanyValueModel> CompanyValues { get; set; }
			public DateTime CurrentTime = DateTime.UtcNow;
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankEditorRow()
		{
			return PartialView("_ValueRow", new CompanyValueModel());
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult Modal(long id)
		{
			var companyValues = _OrganizationAccessor.GetCompanyValues(GetUser(), id);
			return PartialView(new CompanyValueVM { CompanyValues = companyValues, OrgId = id });
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult Modal(CompanyValueVM model)
		{
			foreach (var r in model.CompanyValues){
				r.OrganizationId = model.OrgId;
			}
			_OrganizationAccessor.EditCompanyValues(GetUser(), model.OrgId, model.CompanyValues);
			return Json(ResultObject.SilentSuccess());
		}
	}
}