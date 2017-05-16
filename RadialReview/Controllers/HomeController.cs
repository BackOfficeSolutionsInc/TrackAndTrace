using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.ViewModels;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Models;
using Microsoft.AspNet.Identity;

namespace RadialReview.Controllers {
	public class BackendViewModel {
		public UserViewModel User { get; set; }
		public List<OutstandingReviewViewModel> OutstandingReview { get; set; }
		public bool IncludeTodos { get; set; }
		public bool IncludeScorecard { get; set; }
		public bool IncludeRocks { get; set; }

		public BackendViewModel() {
			OutstandingReview = new List<OutstandingReviewViewModel>();
		}
	}

	public class OutstandingReviewViewModel {
		public String Name { get; set; }
		public long ReviewContainerId { get; set; }

	}


	public class HomeController : BaseController {

		[Access(AccessLevel.Any)]
		public ActionResult Index() {
			if (IsLoggedIn()) {
				return RedirectToAction("Index", "Dashboard");
				//var model = new BackendViewModel();

				//try
				//{
				//	var user = GetUser();
				//	model.User = new UserViewModel() { User = user.User };
				//	if (user.IsManager())
				//	{
				//		model.OutstandingReview = _ReviewAccessor.GetMostRecentReviewContainer(GetUser(), GetUser().Id).Select(recentReview => new OutstandingReviewViewModel()
				//		{
				//			Name = recentReview.ReviewName,
				//			ReviewContainerId = recentReview.Id,
				//		}).ToList();
				//	}
				//	model.IncludeTodos = GetUser().Organization.Settings.EnableL10;
				//	model.IncludeScorecard = GetUser().Organization.Settings.EnableL10;
				//	model.IncludeRocks = GetUser().Organization.Settings.EnableL10;
				//	ViewBag.AnyL10s = L10Accessor.GetVisibleL10Meetings(GetUser(), GetUser().Id, false).Any();

				//}
				//catch (Exception)
				//{
				//	model.User = new UserViewModel() { User = GetUserModel() };
				//}



				//return View("Backend", model);
			}
			return RedirectToAction("Login", "Account");
		}




		[Access(AccessLevel.Any)]
		public ActionResult About() {
			ViewBag.Message = "Your application description page.";

			return View();
		}

		[Access(AccessLevel.Any)]
		public ActionResult Contact() {
			ViewBag.Message = "Your contact page.";

			return View();
		}

		[Access(AccessLevel.Any)]
		[HttpPost]
		public async Task<ActionResult> Submit() {
			// Create an event with action 'event1' and additional data
			await this.NotifyAsync("event1", new { P1 = "p1" });

			return new EmptyResult();
		}

		[Access(AccessLevel.Any)]
		[HttpPost]
		public ActionResult AddEventSubscription(WebhookEventsSubscription model) {
			WebhooksAccessor acc = new WebhooksAccessor();
			model.WebhookId = acc.GetAllWebHook().FirstOrDefault().Id;

			var list = acc.GetWebhookEventSubscriptions(User.Identity.GetUserId(), model.WebhookId);

			// fill viewModel
			WebhooksEventSubscriptionViewModel webhooksEventSubscriptionViewModel = new WebhooksEventSubscriptionViewModel();
			webhooksEventSubscriptionViewModel.Id = list.Id;
			webhooksEventSubscriptionViewModel.Email = list.Email;
			webhooksEventSubscriptionViewModel.UserId = list.UserId;
			webhooksEventSubscriptionViewModel.angularUser = new Models.Angular.Users.AngularUser() {
				Name = list.User.Name()
			};
			webhooksEventSubscriptionViewModel.ProtectedData = list.ProtectedData;

			return Json(webhooksEventSubscriptionViewModel, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Any)]
		public ActionResult CreateWebhookEvents() {
			WebhooksAccessor acc = new WebhooksAccessor();
			acc.CreateWebhookEvents(new WebhookEvents() { Name = "Create To Do", Description = "To Do" });
			return new EmptyResult();
		}

	}
}