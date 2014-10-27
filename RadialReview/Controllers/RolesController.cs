﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
			var roles = _UserAccessor.GetRoles(GetUser(), id);
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
			_UserAccessor.EditRoles(GetUser(), model.UserId, model.Roles);
			return Json(ResultObject.SilentSuccess());
		}
    }
}