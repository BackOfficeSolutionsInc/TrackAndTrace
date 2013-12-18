using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class UploadController : BaseController
    {
        private static ImageAccessor _ImageAccessor = new ImageAccessor();
        //
        // GET: /Upload/
        [Access(AccessLevel.User)]
        public ActionResult Index()
        {
            return View();
        }

       /* [HttpPost]
        [Access(AccessLevel.User)]
        public ActionResult Image(HttpPostedFileBase file, String forType)
        {
            var user=GetUserModel();
            if (user == null)
                throw new PermissionsException();

            //you can put your existing save code here
            if (file != null && file.ContentLength > 0)
            { 
                // extract only the fielname
                UploadType uploadType=forType.Parse<UploadType>();
                _ImageAccessor.UploadImage(user, Server, file, uploadType);
                return Redirect(Request.UrlReferrer.ToString());
            }
            ViewBag.AlertMessage = ExceptionStrings.SomethingWentWrong;
            return Redirect(Request.UrlReferrer.ToString());            
        }*/

        [HttpPost]
        [Access(AccessLevel.User)]
        public JsonResult Image(HttpPostedFileBase file, String forType)
        {
            var user = GetUserModel();
            if (user == null)
                throw new PermissionsException();

            //you can put your existing save code here
            if (file != null && file.ContentLength > 0)
            {
                // extract only the fielname
                UploadType uploadType = forType.Parse<UploadType>();
                var url=_ImageAccessor.UploadImage(user, Server, file, uploadType);
                return Json(ResultObject.Create(url));
            }
            return Json(new ResultObject(true,ExceptionStrings.SomethingWentWrong));            
        }
	}
}