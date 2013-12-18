using RadialReview.Accessors;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class TemplateController : BaseController
    {
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        [Access(AccessLevel.Manager)]
        public ActionResult Create()
        {
            var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(),GetUser().Organization.Id);
            var model = new TemplateViewModel();

            return View("Edit",model);
        }
	}
}