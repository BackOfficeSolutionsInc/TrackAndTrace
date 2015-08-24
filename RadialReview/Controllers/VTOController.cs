using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FluentNHibernate.Mapping;
using RadialReview.Accessors;
using RadialReview.Models.VTO;

namespace RadialReview.Controllers
{
    public class VTOController : BaseController
    {
	    public class VTOListingVM
	    {
		    public List<VtoModel> VTOs { get; set; } 
	    }


        // GET: VTO
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
	        var vtos = VtoAccessor.GetAllVTOForOrganization(GetUser(), GetUser().Id);
	        var model = new VTOListingVM(){
		        VTOs =  vtos
	        };
            return View(model);
        }

		[Access(AccessLevel.UserOrganization)]
	    public ActionResult Edit(long id=0)
		{

			VtoModel model;
			if (id == 0){
				 model =VtoAccessor.CreateVTO(GetUser(), GetUser().Organization.Id);
				return RedirectToAction("Edit", new{id = model.Id});
			}else{
				model = VtoAccessor.GetVTO(GetUser(), id);
			}


			return View(model);
	    }

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Data(long id)
		{
			var model = VtoAccessor.GetAngularVTO(GetUser(), id);
			return Json(model,JsonRequestBehavior.AllowGet);
		}

    }
}