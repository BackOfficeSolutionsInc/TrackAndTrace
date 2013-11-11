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
        public ActionResult Index(String message=null,String redirectUrl=null)
        {
            ViewBag.Message = message;
            ViewBag.RedirectUrl = redirectUrl;
            return View();
        }

        public ActionResult Modal(Exception e)
        {
            return PartialView("ModalError",e);
        }

	}
}