using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FluentNHibernate.Mapping;
using RadialReview.Accessors;
using RadialReview.Models.VTO;
using RadialReview.Models.L10;

namespace RadialReview.Controllers
{
	public partial class 
        VTOController : BaseController
    {
	    public class VTOListingVM
	    {
		    public List<VtoModel> VTOs { get; set; } 
	    }

        public class VTOViewModel
        {
            public long Id { get; set; }

            public bool IsPartial { get; set; }

            public bool OnlyCompanyWideRocks { get; set; }
        }


        // GET: VTO
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
	        var vtos = VtoAccessor.GetAllVTOForOrganization(GetUser(), GetUser().Organization.Id);
	        var model = new VTOListingVM(){
		        VTOs =  vtos
	        };
            return View(model);
        }

		[Access(AccessLevel.UserOrganization)]
        public ActionResult Edit(long id = 0, bool noheading = false, bool? vision = null,bool? traction=null)
        {

			VtoModel model;
			if (id == 0){
				model =VtoAccessor.CreateVTO(GetUser(), GetUser().Organization.Id);
				model.Name = "<no name>";
				return RedirectToAction("Edit", new{id = model.Id,noheading=noheading,vision=vision,traction=traction});
			}else{
				model = VtoAccessor.GetVTO(GetUser(), id);
			}

            var defaultVision = false;
            var defaultTraction = true;
            //var onlyCompanyWideRocks = false;

            if (model.L10Recurrence != null)
            {
                var isLeadership =  L10Accessor.GetL10Recurrence(GetUser(), model.L10Recurrence.Value, false).TeamType == L10TeamType.LeadershipTeam;
                defaultVision = isLeadership;
                //onlyCompanyWideRocks = onlyCompanyWideRocks || isLeadership;
            }else{
                defaultVision = true;
            }

            ViewBag.HideVision = !(vision ?? defaultVision);
            ViewBag.HideTraction = !(traction ?? defaultTraction);


			ViewBag.CanEditCoreValues = _PermissionsAccessor.IsPermitted(GetUser(), x =>x.EditCompanyValues(model.Organization.Id));
			ViewBag.CanEditVTO = _PermissionsAccessor.IsPermitted(GetUser(), x => x.EditVTO(model.Id));

			var vm = new VTOViewModel() { Id = model.Id, IsPartial = noheading };
            if (noheading)
                return PartialView(vm);
            return View(vm);
	    }

		[Access(AccessLevel.UserOrganization)]
        [OutputCache(NoStore = true, Duration = 0)]
		public JsonResult Data(long id)
		{
			var model = VtoAccessor.GetAngularVTO(GetUser(), id);
			return Json(model,JsonRequestBehavior.AllowGet);
		}

    }
}