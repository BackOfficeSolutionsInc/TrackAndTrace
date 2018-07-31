using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using RadialReview.Accessors;
using RadialReview.Accessors.TodoIntegrations;
using TrelloNet;

namespace RadialReview.Controllers {
	public class CallbackController : BaseController {
		public class SelectTrelloList {
			public string ServiceName { get; set; }
			public string RecurrenceName { get; set; }
			public string UserName { get; set; }
			public long recurrence { get; set; }
			public long user { get; set; }
			public string token { get; set; }
			public string apiid { get; set; }
			public List<SelectListItem> PossibleItems { get; set; }
			public String Selected { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Trello(long recurrence, long user, string token = null) {
			var username = UserAccessor.GetUserOrganization(GetUser(), user, false, false).GetName();
			var recurName = L10Accessor.GetL10Recurrence(GetUser(), recurrence, false);

			var model = new SelectTrelloList() {
				ServiceName = "Trello",
				RecurrenceName = recurName.Name,
				UserName = username,
				recurrence = recurrence,
				user = user,
				token = token,
				PossibleItems = token.NotNull(z => TrelloAccessor.GetLists(GetUser(), token).ToSelectList(x => x.Name, x => x.ListId))
			};
			return View("PickList", model);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Trello(SelectTrelloList model) {
			ValidateValues(model, x => x.recurrence, x => x.apiid, x => x.token, x => x.user);
			TrelloAccessor.AttachToTrello(GetUser(), model.token, model.recurrence, model.user, model.Selected);
			return RedirectToAction("External", "L10", new { id = model.recurrence });
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Basecamp(string state, string code = null) {
			var args = state.Split('_');
			var user = args[0].ToLong();
			var recurrence = args[1].ToLong();

			var username = UserAccessor.GetUserOrganization(GetUser(), user, false, false).GetName();
			var recurName = L10Accessor.GetL10Recurrence(GetUser(), recurrence, false);

			var auth = BaseCampAccessor.Authorize(GetUser(), code, recurrence, user);

			var model = new SelectTrelloList() {
				ServiceName = "Basecamp",
				RecurrenceName = recurName.Name,
				UserName = username,
				recurrence = recurrence,
				user = user,
				apiid = auth.UID,
				PossibleItems = code.NotNull(z => BaseCampAccessor.GetLists(GetUser(), auth.ApiId).ToSelectList(x => x.Name, x => x.ListId))
			};
			return View("PickList", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Basecamp(SelectTrelloList model) {
			ValidateValues(model, x => x.recurrence, x => x.apiid, x => x.token, x => x.user);
			//var auth = BaseCampAccessor.Authorize(model.token);

			BaseCampAccessor.AttachToBasecamp(GetUser(), model.apiid, model.recurrence, model.user, model.Selected);
			return RedirectToAction("External", "L10", new { id = model.recurrence });
		}
	}
}