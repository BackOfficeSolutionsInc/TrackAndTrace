using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Models.Json;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;

namespace RadialReview.Controllers
{
    public class RolesController : BaseController
    {
		public class RoleVM
		{
			public long TemplateId { get; set; }
			public long UserId { get; set; }
			public List<RoleModel> Roles { get; set; }
			public List<UserTemplate.UT_Role> TemplateRoles { get; set; }
			public bool UpdateOutstandingReviews { get; set; }

			public DateTime CurrentTime = DateTime.UtcNow;
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult Modal(long id) {
			var roles = _RoleAccessor.GetRoles(GetUser(), id);
			return PartialView(new RoleVM { Roles = roles, UserId = id });
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankEditorRow() {
			return PartialView("_RoleRow", new RoleModel());
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult Modal(RoleVM model) {
			

			foreach (var r in model.Roles){
				r.ForUserId = model.UserId;
			}
			_RoleAccessor.EditRoles(GetUser(), model.UserId, model.Roles,model.UpdateOutstandingReviews);
			return Json(ResultObject.SilentSuccess());
		}

		#region Template 
		[Access(AccessLevel.Manager)]
		public PartialViewResult TemplateModal(long id)
		{
			var template= UserTemplateAccessor.GetUserTemplate(GetUser(), id,loadRoles:true);
			return PartialView(new RoleVM { TemplateRoles = template._Roles, TemplateId = id });
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankTemplateEditorRow(long id)
		{
			var templateId = id;
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewTemplate(templateId));
			return PartialView("_TemplateRoleRow", new UserTemplate.UT_Role()
			{
				TemplateId = templateId
			});
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult TemplateModal(RoleVM model)
		{
			foreach (var r in model.TemplateRoles)
			{
				if (r.Id == 0){
					if (r.DeleteTime==null)
						UserTemplateAccessor.AddRoleToTemplate(GetUser(), model.TemplateId, r.Role);
				}
				else
					UserTemplateAccessor.UpdateRoleTemplate(GetUser(), r.Id, r.Role, r.DeleteTime);
			}
			return Json(ResultObject.SilentSuccess());
		}
		#endregion
	}
}