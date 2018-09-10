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
using RadialReview.Utilities.DataTypes;

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
			public long Project { get; set; }
			public long TokenId { get; set; }
			public long WorkspaceId { get; set; }

			public AsanaActionVM() {}

			public AsanaActionVM(AsanaAction action) {
				Id = action.Id;
				SyncMyTodos = action.ActionType.HasFlag(AsanaActionType.SyncMyTodos);
				Description = action.GetDescription();
				TokenId = action.AsanaTokenId;
				WorkspaceId = action.WorkspaceId;
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> AsanaAddAction() {
			await SetupActionViewBag();
			return PartialView("AsanaActionModal", new AsanaActionVM() {
				TokenId = (await AsanaAccessor.GetTokenForUser(GetUser(), GetUser().Id)).Id,
				SyncMyTodos = true,
			});
		}

		private async Task<List<AsanaProject>> SetupActionViewBag() {
			var groups = new DefaultDictionary<string, SelectListGroup>(x => new SelectListGroup() { Name = x });
			var availableProjects = await AsanaAccessor.GetAvailableProjects(GetUser(), GetUser().Id);
			var selectList = availableProjects.Select(x => new SelectListItem() {
								Group = groups[x.Workspace],
								Text = x.Name,
								Value = "" + x.Id,
							}).ToList();

			if (!selectList.Any()) {
				throw new PermissionsException("Your Asana account has no workspaces");
			}
			ViewBag.Projects = selectList;
			return availableProjects;
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> AsanaEditAction(long id) {
			var action = await AsanaAccessor.GetAction(GetUser(), id);
			await SetupActionViewBag();
			var model = new AsanaActionVM(action);
			return PartialView("AsanaActionModal",model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AsanaEditAction(AsanaActionVM model) {
			AsanaAction res;
			var projects = (await SetupActionViewBag()).ToDefaultDictionary(x=>x.Id,x=>x,x=>null);
			var project = projects[model.Project];
			if (model.Id == 0) {
				res = await AsanaAccessor.CreateAction(GetUser(), model.TokenId, model.Project, model.SyncMyTodos, project.NotNull(x => x.Workspace), project.NotNull(x => x.Name), project.NotNull(x => x.WorkspaceId));
			} else {
				res = await AsanaAccessor.UpdateAction(GetUser(), model.Id, model.Project, model.SyncMyTodos, project.NotNull(x => x.Workspace), project.NotNull(x => x.Name),project.NotNull(x=>x.WorkspaceId));
			}
			return Json(ResultObject.SilentSuccess(new AsanaActionVM(res)));
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AsanaRemoveAction(long id) {
			await AsanaAccessor.DeleteAction(GetUser(), id);
			return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> AsanaRedirect(string code, string state) {
			var token = await AsanaAccessor.Register(GetUser(), GetUser().Id, code);
			return RedirectToAction("Asana");
		}
	}
}