using RadialReview.Accessors;
using RadialReview.Models.Json;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ResponsibilitiesController : BaseController
    {

        protected ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();

        //
        // GET: /Responsibilities/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Modal(long id=0)
        {
            var caller=GetUser();

            var responsiblity=new ResponsibilityModel();
            if (id!=0)
                responsiblity=_ResponsibilitiesAccessor.GetResponsibility(caller,id);

            return View(responsiblity);
        }

        [HttpPost]
        public JsonResult Add(long responsibilityGroupId, ResponsibilityModel model)
        {
            var caller = GetUser();
            _ResponsibilitiesAccessor.AddRespnsibility(caller, responsibilityGroupId, model);
            return Json(JsonObject.Success);
        }
	}
}