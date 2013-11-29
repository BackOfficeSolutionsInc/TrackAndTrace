using RadialReview.Accessors;
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
        protected static PositionAccessor _PositionAccessor = new PositionAccessor();
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();

        //
        // GET: /Position/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Modal()
        {
            var positions = _PositionAccessor.AllPositions().OrderBy(x=>x.Name).ToList();
            var model = new PositionViewModel() { Positions = positions };
            return PartialView(model);
        }

        [HttpPost]
        public JsonResult Modal(PositionViewModel model)
        {
            var caller=GetUser();
            _OrganizationAccessor.AddOrganizationPosition(caller, caller.Organization.Id, model.Position.Value,model.PositionName);
            return Json(JsonObject.Success);
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