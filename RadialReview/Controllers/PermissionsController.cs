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
using RadialReview.Exceptions;

namespace RadialReview.Controllers {
	public class PermissionsController : BaseController {

		public class PermissionsVM {
			public long Id { get; set; }
			public PermissionType PermissionType { get; set; }
			public long ForUser { get; set; }
			public long CopyFrom { get; set; }

			public List<UserOrganizationModel> PossibleUsers { get; set; }
		}

		//[HttpPost]
		//[Access(AccessLevel.UserOrganization)]
		//public PartialViewResult Dropdown(PermissionDropdownVM model) {
		//	PermissionsAccessor.EditPermItems(GetUser(), model);

		//	return Dropdown(model.ResId, model.ResType);
		//}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult BlankDropdownRow(string q, long resource, PermItem.ResourceType type) {
			var num = q.TryParseLong();
            ViewBag.Heading = PermissionsHeading.GetHeading(type);

            if (num != null) {

				var rgm = _ResponsibilitiesAccessor.GetResponsibilityGroup(GetUser(), num.Value);

				var isAdmin = _PermissionsAccessor.IsPermitted(GetUser(), x => x.CanAdmin(type, resource));

				ViewBag.CanEdit_View = isAdmin;
				ViewBag.CanEdit_Edit = isAdmin;
				ViewBag.CanEdit_Admin = isAdmin;
				ViewBag.CanEdit_Delete = isAdmin;

				var piVM = new PermItemVM() {
					AccessorId = num.Value,
					AccessorType = PermItem.AccessType.RGM,
					CanView = true,
					CanEdit = false,
					CanAdmin = false,
					Title = rgm.GetName(),
					ImageUrl = rgm.GetImageUrl(),
					Edited = false
				};

				var vm = PermissionsAccessor.CreatePermItem(GetUser(), piVM, type, resource);
				return PartialView("PermItemRow", vm);
			} else if (q != null && Emailer.IsValid(q)) {
				var tp = PermTiny.Email(q);
				var isAdmin = _PermissionsAccessor.IsPermitted(GetUser(), x => x.CanAdmin(type, resource));

				ViewBag.CanEdit_View = isAdmin;
				ViewBag.CanEdit_Edit = isAdmin;
				ViewBag.CanEdit_Admin = isAdmin;
				ViewBag.CanEdit_Delete = isAdmin;

				PermissionsAccessor.CreatePermItems(GetUser(), type, resource, tp);
				var piVM = new PermItemVM() {
					AccessorId = tp.PermItem.AccessorId,
					AccessorType = PermItem.AccessType.Email,
					CanView = tp.PermItem.CanView,
					CanEdit = tp.PermItem.CanEdit,
					CanAdmin = tp.PermItem.CanAdmin,
					Title = q,
					//ImageUrl = rgm.GetImageUrl(),
					Edited = false,
					Id = tp.PermItem.Id
				};
				return PartialView("PermItemRow", piVM);
			}
			throw new Exception("Could not add permission.");
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult DeletePermItem(long id) {
			PermissionsAccessor.DeletePermItem(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdatePerm(long id, bool? view = null, bool? edit = null, bool? admin = null) {
			PermissionsAccessor.EditPermItem(GetUser(), id, view, edit, admin);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Dropdown(long id, PermItem.ResourceType type, string buttonClass = null) {
			ViewBag.ButtonClass = buttonClass;
            
            ViewBag.Heading = PermissionsHeading.GetHeading(type);

            try {
				var model = PermissionsAccessor.GetPermItems(GetUser(), id, type);
				return PartialView(model);
			} catch (PermissionsException ) {
				return PartialView(PermissionDropdownVM.NotPermitted);
			}

			
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Modal(long id = 0) {
			var orgId = GetUser().Organization.Id;

			var perm = PermissionsAccessor.GetPermission(GetUser(), id);

			var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), orgId, false, false);

			var p = new PermissionsVM() {
				CopyFrom = perm.AsUser.NotNull(x => x.Id),
				ForUser = perm.ForUser.NotNull(x => x.Id),
				PermissionType = perm.Permissions,
				PossibleUsers = allUsers,
				Id = id,
			};

			return PartialView(p);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Modal(PermissionsVM model) {
			var orgId = GetUser().Organization.Id;
			ValidateValues(model, x => x.Id);

			if (model.CopyFrom == model.ForUser)
				ModelState.AddModelError("Permissions", "Selected user must be different from the one being copied.");

			if (ModelState.IsValid) {
				PermissionsAccessor.EditPermission(GetUser(), model.Id, model.ForUser, model.PermissionType, model.CopyFrom);
				return Json(ResultObject.SilentSuccess());
			}

			model.PossibleUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), orgId, false, false);


			return PartialView(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Edit(long id) {
			var orgId = id;
			var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), orgId, false, false);
			var ps = _PermissionsAccessor.AllPermissionsAtOrganization(GetUser(), orgId).Select(x => new PermissionsVM() {
				CopyFrom = x.AsUser.Id,
				ForUser = x.ForUser.Id,
				PermissionType = x.Permissions,
				PossibleUsers = allUsers,
				Id = x.Id
			}).ToList();

			return View(ps);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Delete(long id) {
			var found = PermissionsAccessor.GetPermission(GetUser(), id);
			PermissionsAccessor.EditPermission(GetUser(), id, found.ForUser.Id, found.Permissions, found.AsUser.Id, DateTime.UtcNow);
			return Json(ResultObject.SilentSuccess(id));
		}

	}
}