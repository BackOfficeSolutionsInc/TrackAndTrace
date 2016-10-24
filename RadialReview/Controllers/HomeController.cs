using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.ViewModels;
using System.Text;

namespace RadialReview.Controllers
{
	public class BackendViewModel
	{
		public UserViewModel User { get; set; }
		public List<OutstandingReviewViewModel> OutstandingReview { get; set; }
		public bool IncludeTodos { get; set; }
		public bool IncludeScorecard { get; set; }
		public bool IncludeRocks { get; set; }

		public BackendViewModel()
		{
			OutstandingReview=new List<OutstandingReviewViewModel>();
		}
	}

	public class OutstandingReviewViewModel
	{
		public String Name { get; set; }
		public long ReviewContainerId { get; set; }

	}


	public class HomeController : BaseController
	{
	
		[Access(AccessLevel.Any)]
		public ActionResult Index()
		{
			if (IsLoggedIn()){
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
		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		[Access(AccessLevel.Any)]
		public ActionResult Contact()
		{
			ViewBag.Message = "Your contact page.";

			return View();
		}
	}
}