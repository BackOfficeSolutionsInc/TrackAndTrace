using RadialReview.Accessors;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class PositionController : BaseController
    {

        //
        // GET: /Position/
        [Access(AccessLevel.Any)]
        public ActionResult Index()
        {
            return View();
        }
        /*
        public ActionResult Responsiblities(long id)
        {
            var responsibilities = _ResponsibilitiesAccessor.GetResponsibilities(GetUser(), id);
            return View(responsibilities);
        }

        public ActionResult Edit(long id)
        {
            var positionId = id;
            var position=_OrganizationAccessor.GetOrganizationPosition(GetUser(), id);
        }*/

        [Access(AccessLevel.Manager)]
        public PartialViewResult Modal(long id = 0)
        {
            var positions = _PositionAccessor.AllPositions().ToList();

            PositionViewModel model = new PositionViewModel() { Positions = positions.OrderBy(x => x.Name.Translate()).ToList(),Id=id };
            if (id != 0)
            {
                var found= _OrganizationAccessor.GetOrganizationPosition(GetUser(), id);
                model.PositionName = found.CustomName;
               /* model.Position = found.Position.Id;*/
            }

            return PartialView(model);
        }

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult Modal(PositionViewModel model) {
			var caller = GetUser();
			_OrganizationAccessor.EditOrganizationPosition(caller, model.Id, caller.Organization.Id, /*model.Position.Value,*/model.PositionName);
			return Json(ResultObject.Success("Updated position.").ForceRefresh());
		}

		
		[Access(AccessLevel.Manager)]
		public JsonResult Delete(long id) {
			var caller = GetUser();
			PositionAccessor.DeletePosition(caller, id);
			return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
		}
		/*
public ActionResult EditAccountabilities(long positionId)
{
	var caller = GetUser();
	return View();
}*/

		/*
        public JsonResult Search(string search)
        {
            var positions=PositionAccessor.AllPositions();
            return Json(JsonObject.Create(positions));
        }*/
	}
}