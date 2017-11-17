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
using System.Threading.Tasks;

namespace RadialReview.Controllers
{
    public class RolesController : BaseController
    {
		public class RoleVM
		{
			public long TemplateId { get; set; }
			public long UserId { get; set; }
			public List<RoleModel> Roles { get; set; }
			//public List<UserTemplate.UT_Role> TemplateRoles { get; set; }
			public bool UpdateOutstandingReviews { get; set; }

			public DateTime CurrentTime = DateTime.UtcNow;
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Modal(long id)
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.EditQuestionForUser(id));
			var roles = _RoleAccessor.GetRoles(GetUser(), id);
			return PartialView(new RoleVM { Roles = roles, UserId = id });
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult BlankEditorRow() {
			return PartialView("_RoleRow", new RoleModel());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Modal(RoleVM model) {


			//foreach (var r in model.Roles){
			//	r.ForUserId = model.UserId;
			//}
			//try {
				await _RoleAccessor.EditRoles(GetUser(), model.UserId, model.Roles, model.UpdateOutstandingReviews);
			//} catch (Exception) {

			//}
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.Radial)]
		public JsonResult Undelete(long id) {
			RoleAccessor.UndeleteRole(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		#region Template
		[Access(AccessLevel.Manager)]
		public PartialViewResult TemplateModal(long id)
		{
			var template= UserTemplateAccessor.GetUserTemplate(GetUser(), id,loadRoles:true);
			return PartialView(new RoleVM { Roles = template._Roles, TemplateId = id });
		}

		[Access(AccessLevel.UserOrganization)]
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
		public async Task<JsonResult> TemplateModal(RoleVM model)
		{
			foreach (var r in model.Roles)
			{
				if (r.Id == 0){
					if (r.DeleteTime==null)
						await UserTemplateAccessor.AddRoleToTemplate(GetUser(), model.TemplateId, r.Role);
				}
				else
					await RoleAccessor.EditRole(GetUser(), r.Id, r.Role, r.DeleteTime);
			}
			return Json(ResultObject.SilentSuccess());
		}
		#endregion
	}
}