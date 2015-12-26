using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Customize;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
    public class CustomizeController : BaseController
    {
        
		[Access(AccessLevel.Manager)]
        public ActionResult Index()
		{
			var all = CustomizeAccessor.GetCustomizations(GetUser(), GetUser().Organization.Id);
            return View(all);
        }

		[Access(AccessLevel.Manager)]
		public PartialViewResult EditModal(long id=0)
		{
			CustomText text;
			text = id != 0 ? CustomizeAccessor.GetCustomization(GetUser(), id) : new CustomText(){
				OrgId = GetUser().Organization.Id
			};
			text._PossibleItems = CustomizeAccessor.AllProperties();

			return PartialView(text);
		}

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public JsonResult EditModal(CustomText model)
		{
			CustomizeAccessor.EditCustomizeProperty(GetUser(),model);
			return Json(ResultObject.SilentSuccess().ForceRefresh());
		}

		[Access(AccessLevel.Manager)]
		public ActionResult Delete(long id)
		{
			var found = CustomizeAccessor.GetCustomization(GetUser(), id);
			found.DeleteTime = DateTime.UtcNow;
			CustomizeAccessor.EditCustomizeProperty(GetUser(),found);
			return RedirectToAction("Index");
		}
    }
}