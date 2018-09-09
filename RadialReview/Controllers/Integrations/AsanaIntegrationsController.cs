using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Office365.OutlookServices;
using RadialReview.Models.Json;
using RadialReview.Accessors.VideoConferenceProviders;
using RadialReview.Crosscutting.Integrations.Asana;
using RadialReview.Utilities;
using RadialReview.Exceptions;
using RadialReview.Models.Integrations;

namespace RadialReview.Controllers {
	public partial class IntegrationsController : BaseController {
		

		public class AsanaTokenVM {
			public AsanaTokenVM(long id) {
				Id = id;
			}
			public long Id { get; set; }

		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Asana() {

			var token = await AsanaAccessor.GetTokenForUser(GetUser(), GetUser().Id);
			if (token == null) {
				ViewBag.ClientId = Config.Asana().ClientId;
				ViewBag.RedirectUrl = AsanaAccessor.GetRedirectUrl();
				return View("AsanaNoConnection");
			}

			var actions = await AsanaAccessor.GetUsersActions(GetUser(), GetUser().Id);
			return View(actions.Select(x=>new AsanaActionVM(x)).ToList());
		}

		public class AsanaActionVM {
			public long Id { get; set; }
			public string Description { get; set; }
			public bool SyncMyTodos { get; set; }
			public long Workspace { get; set; }
			
			public AsanaActionVM() {}

			public AsanaActionVM(AsanaAction action) {
				Id = action.Id;
				SyncMyTodos = action.ActionType.HasFlag(AsanaActionType.SyncMyTodos);
				Description = action.GetDescription();
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> AsanaAddAction(long id) {
			var workspaces = (await AsanaAccessor.GetAvailableWorkspaces(GetUser(), GetUser().Id))
				.Select(x => new SelectListItem() {
					Text = x.Name,
					Value= ""+x.Id,
				}).ToList();

			if (!workspaces.Any()) {
				throw new PermissionsException("Your Asana account has no workspaces");
			}
			ViewBag.Workspaces = workspaces;
			return PartialView("AsanaActionModal",  new AsanaActionVM());
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> AsanaEditAction(long id) {
			var action = await AsanaAccessor.GetAction(GetUser(), id);
			var model = new AsanaActionVM(action);
			return PartialView("AsanaActionModal",model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AsanaEditAction(AsanaActionVM model) {
			await AsanaAccessor.UpdateAction(GetUser(), model.Id, model.Workspace, model.SyncMyTodos);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AsanaRemoveAction(long id) {
			await AsanaAccessor.DeleteAction(GetUser(), id);
			return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> AsanaRedirect(string code, string state) {
			var token = await AsanaAccessor.Register(GetUser(), GetUser().Id, code);
			return RedirectToAction("AsanaActions",new { id = token.Id });
		}
	}
}