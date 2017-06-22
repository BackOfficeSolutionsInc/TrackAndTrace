using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Controllers;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using RadialReview.Extensions;
using RadialReview.Exceptions;

namespace RadialReview.Areas.People.Controllers {
	public class QuarterlyConversationController : BaseController {
		// GET: People/QuarterlyConversation
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			var containers = SurveyAccessor.GetSurveyContainersBy(GetUser(), GetUser(), SurveyType.QuarterlyConversation);
			return View(containers);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Questions(long id) {
			return View(id);
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Issue() {
			var possible = SurveyAccessor.AvailableByAbouts(GetUser());
			return View(possible);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public ActionResult Issue(FormCollection form) {

			var arr = form["selected"].NotNull(x => x.Split(',').ToList()) ?? new List<string>();
			var byAbouts = arr.Select(x => x.ByAboutFromKey()).ToList();


			var evalSelf = form["self"].ToBooleanJS();

			if (evalSelf) {
				byAbouts.AddRange(byAbouts.Select(x => new ByAbout(x.GetAbout(), x.GetAbout())).ToList());
			}

			byAbouts = byAbouts.Distinct().ToList();

			SurveyAccessor.GenerateSurveyContainer(GetUser(), form["name"], byAbouts);


			return RedirectToAction("Index");
		}
	}
}