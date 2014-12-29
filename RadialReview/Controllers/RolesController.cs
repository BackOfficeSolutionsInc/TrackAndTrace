using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Models.Json;
using RadialReview.Models.UserModels;

namespace RadialReview.Controllers
{
    public class RolesController : BaseController
    {
		public class RoleVM {
			public long UserId { get; set; }
			public List<RoleModel> Roles { get; set; }
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
			_RoleAccessor.EditRoles(GetUser(), model.UserId, model.Roles);
			return Json(ResultObject.SilentSuccess());
		}
    }
}