using RadialReview.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ErrorController : BaseController
    {
        //
        // GET: /Error/
        [Access(AccessLevel.Any)]
        public ActionResult Index(String message = null, String redirectUrl = null) {
            ViewBag.Message = message;
            ViewBag.RedirectUrl = redirectUrl;
           // var ex = HttpException.CreateFromLastError("error");            
            return View(/*ex*/);
        }

        [Access(AccessLevel.Any)]
        public ActionResult NotFound() {
            return Index("404 - Not Found");
        }

        [Access(AccessLevel.Any)]
        public ActionResult Modal(Exception e)
        {
            return PartialView("ModalError",e);
        }

		[Access(AccessLevel.Any)]
		public JsonResult TestSync() {
			throw new SyncException(null);
		}
	}
}