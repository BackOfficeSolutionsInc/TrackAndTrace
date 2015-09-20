using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FluentNHibernate.Utils;
using RadialReview.Accessors;
using RadialReview.Models.Json;
using RadialReview.Models.Permissions;
using RadialReview.Models;

namespace RadialReview.Controllers
{
    public class PermissionsController : BaseController
    {

	    public class PermissionsVM
		{
			public long Id { get; set; }
			public PermissionType PermissionType { get; set; }
			public long ForUser { get; set; }
			public long CopyFrom { get; set; }

			public List<UserOrganizationModel> PossibleUsers { get; set; }
			
			
	    }

		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult Modal(long id = 0)
		{
			var orgId = GetUser().Organization.Id;

			var perm = PermissionsAccessor.GetPermission(GetUser(), id);

			var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), orgId, false, false);

			var p = new PermissionsVM()
			{
				CopyFrom = perm.AsUser.NotNull(x=>x.Id),
				ForUser = perm.ForUser.NotNull(x => x.Id),
				PermissionType = perm.Permissions,
				PossibleUsers = allUsers,
				Id = id,
			};

			return PartialView(p);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Modal(PermissionsVM model)
		{
			var orgId = GetUser().Organization.Id;
			ValidateValues(model,x=>x.Id);

			if (model.CopyFrom==model.ForUser)
				ModelState.AddModelError("Permissions","Selected user must be different from the one being copied.");

			if (ModelState.IsValid){
				PermissionsAccessor.EditPermission(GetUser(), model.Id, model.ForUser, model.PermissionType, model.CopyFrom);
				return Json(ResultObject.SilentSuccess());
			}

			model.PossibleUsers= _OrganizationAccessor.GetOrganizationMembers(GetUser(), orgId, false, false);

		
			return PartialView(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Edit(long id)
		{
			var orgId = id;
			var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), orgId, false, false);
			var ps = _PermissionsAccessor.AllPermissionsAtOrganization(GetUser(), orgId).Select(x=>new PermissionsVM(){
				CopyFrom = x.AsUser.Id,
				ForUser =  x.ForUser.Id,
				PermissionType = x.Permissions,
				PossibleUsers = allUsers,
				Id = x.Id
			}).ToList();

			return View(ps);
		}

	    [Access(AccessLevel.UserOrganization)]
	    public JsonResult Delete(long id)
	    {
		    var found = PermissionsAccessor.GetPermission(GetUser(), id);
			PermissionsAccessor.EditPermission(GetUser(),id,found.ForUser.Id,found.Permissions,found.AsUser.Id,DateTime.UtcNow);
		    return Json(ResultObject.SilentSuccess(id));
	    }

    }
}