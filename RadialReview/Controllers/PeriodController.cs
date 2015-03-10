using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Json;
using RadialReview.Models.Periods;

namespace RadialReview.Controllers
{
    public class PeriodController : BaseController
    {
	    public class PeriodVM
	    {
			public bool CanEdit { get; set; }
			public long PeriodId { get; set; }
			public long OrgId { get; set; }

			public String Name { get; set; }
			public DateTime StartTime { get; set; }
		    public DateTime EndTime { get; set; }
			public double Offset { get; set; }

			public PeriodVM(){
				
			}

		    public PeriodVM(PeriodModel period)
		    {
				StartTime = period.StartTime;
				EndTime = period.EndTime;
			    Name = period.Name;
			    OrgId = period.OrganizationId;
			    PeriodId = period.Id;
		    }
	    }


        // GET: Period
		[Access(AccessLevel.Manager)]
        public ActionResult Index()
		{
			var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id);
			var canEdit = _PermissionsAccessor.IsPermitted(GetUser(), x => x.ManagingOrganization(GetUser().Organization.Id));
			return View(periods.Select(x => new PeriodVM(x) { CanEdit = canEdit }));
        }

        // GET: Period
	    [Access(AccessLevel.Manager)]
	    public ActionResult Modal(long id)
	    {
		    if (id == 0){
			    return PartialView(new PeriodVM( new PeriodModel(){
					StartTime = DateTime.UtcNow.Date,
					EndTime = DateTime.UtcNow.Date.AddDays(89),
				    OrganizationId = GetUser().Organization.Id,
					
			    }));
		    }
			return PartialView(new PeriodVM(PeriodAccessor.GetPeriod(GetUser(),id)));

	    }

	    [Access(AccessLevel.Manager)]
	    [HttpPost]
	    public JsonResult Modal(PeriodVM model)
	    {
			var updated = PeriodAccessor.EditPeriod(GetUser(), model.PeriodId, model.OrgId, model.StartTime.AddHours(-1 * model.Offset), model.EndTime.AddHours(-1 * model.Offset), model.Name);
			return Json(ResultObject.Create(updated,"Period has been updated."));
	    }

		[Access(AccessLevel.Manager)]
		public JsonResult Delete(long id)
		{
			PeriodAccessor.Delete(GetUser(), id);
			return Json(ResultObject.Success("Period deleted."));
		}
    }
}
