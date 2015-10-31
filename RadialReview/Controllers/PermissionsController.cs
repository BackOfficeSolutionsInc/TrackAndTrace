using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Amazon.ElasticTranscoder.Model;
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

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Dropdown(PermissionDropdownVM model)
		{
			PermissionsAccessor.EditPermItems(GetUser(), model);

			return Dropdown(model.ResId, model.ResType);
		}

	    [Access(AccessLevel.UserOrganization)]
	    public PartialViewResult BlankDropdownRow(long id,long resource,PermItem.ResourceType type)
	    {
			var rgm = _ResponsibilitiesAccessor.GetResponsibilityGroup(GetUser(), id);

		    var isAdmin = _PermissionsAccessor.IsPermitted(GetUser(), x => x.CanAdmin(type, resource));

		    ViewBag.CanEdit_View = isAdmin;
		    ViewBag.CanEdit_Edit = isAdmin;
		    ViewBag.CanEdit_Admin = isAdmin;

		    var piVM = new PermItemVM(){
				AccessorId = id,
				AccessorType = PermItem.AccessType.RGM,
				CanView = true,
				CanEdit = false,
				CanAdmin = false,
				Title = rgm.GetName(),
				ImageUrl = rgm.GetImageUrl(),
				Edited = false
		    };

		    var vm = PermissionsAccessor.CreatePermItem(GetUser(), piVM, PermItem.ResourceType.L10Recurrence, resource);

			return PartialView("PermItemRow", vm);
	    }

	    [Access(AccessLevel.UserOrganization)]
	    public JsonResult UpdatePerm(long id, bool? view=null,bool? edit=null,bool? admin=null)
	    {
			PermissionsAccessor.EditPermItem(GetUser(), id, view, edit, admin);
		    return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
	    }

	    [Access(AccessLevel.UserOrganization)]
		public PartialViewResult Dropdown(long id,PermItem.ResourceType type)
		{
			var model = PermissionsAccessor.GetPermItems(GetUser(),id, type);

			return PartialView(model);
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