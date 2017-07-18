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
using RadialReview.Models.Accountability;
using RadialReview.Areas.People.Accessors.PDF;
using RadialReview.Models.Json;
using System.Threading.Tasks;

namespace RadialReview.Areas.People.Controllers {
	public class QuarterlyConversationController : BaseController {
		// GET: People/QuarterlyConversation
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			ViewBag.CanCreate = new PermissionsAccessor().IsPermitted(GetUser(), x => x.CreateQuarterlyConversation(GetUser().Organization.Id));
			var containers = SurveyAccessor.GetSurveyContainersBy(GetUser(), GetUser(), SurveyType.QuarterlyConversation).OrderByDescending(x => x.IssueDate);
			return View(containers);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Questions(long id) {
			return View(id);
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Print(long surveyContainerId, long nodeId, bool print = true) {
			var surveyContainer = SurveyAccessor.GetSurveyContainerAbout(GetUser(), ForModel.Create<AccountabilityNode>(nodeId), surveyContainerId);
			var doc = SurveyPdfAccessor.CreateDoc(GetUser(), "Quarterly Conversation");
			foreach (var survey in surveyContainer.GetSurveys()) {
				SurveyPdfAccessor.AppendSurveyAbout(doc, surveyContainer.GetName(), DateTime.UtcNow, survey);
			}
			return Pdf(doc, "Quarterly Conversation", !print);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult PrintAll(long surveyContainerId, bool print = true) {

			var doc = SurveyPdfAccessor.CreateDoc(GetUser(), "All Quarterly Conversations");

			var allAbout = QuarterlyConversationAccessor.GetPeopleAnalyzer(GetUser(), GetUser().Id).Responses
				.Where(x => x.SurveyContainerId == surveyContainerId)
				.Select(x => x.About)
				.Distinct(x => x.ToKey())
				.ToList();
			foreach (var about in allAbout) {
				var surveyContainer = SurveyAccessor.GetSurveyContainerAbout(GetUser(), about, surveyContainerId);
				foreach (var survey in surveyContainer.GetSurveys()) {
					SurveyPdfAccessor.AppendSurveyAbout(doc, surveyContainer.GetName(), DateTime.UtcNow, survey);
				}
			}
			return Pdf(doc, "All Quarterly Conversations", !print);
		}


		[Access(AccessLevel.UserOrganization)]
		public JsonResult Remove(long id) {
			SurveyAccessor.RemoveSurveyContainer(GetUser(), id);// QuarterlyConversationAccessor
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		public class IssueViewModel {
			public IEnumerable<IByAbout> AvailableUsers { get; set; }
			public DateTime DueDate { get; set; }
			public bool Email { get; set; }
			public bool EvalSelf { get; set; }
			public string Name { get; set; }
			public IssueViewModel() {
				Email = true;
				EvalSelf = true;
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Issue() {
			var possible = QuarterlyConversationAccessor.AvailableByAbouts(GetUser());
			var vm = new IssueViewModel() {
				AvailableUsers = possible,
				DueDate = GetUser().GetTimeSettings().ConvertFromServerTime(DateTime.UtcNow.AddDays(7)),
			};
			return View(vm);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<ActionResult> Issue(FormCollection form) {
			var name = form["Name"];
			var dueDate = (form["DueDate"]??"").ToDateTime("MM-dd-yyyy HH:mm:ss");

			if (string.IsNullOrWhiteSpace(name))
				ModelState.AddModelError("name", "Name is required");
			if (ModelState.IsValid) {
				var arr = form["selected"].NotNull(x => x.Split(',').ToList()) ?? new List<string>();
				var byAbouts = arr.Select(x => x.ByAboutFromKey()).ToList();
				var evalSelf = form["EvalSelf"].ToBooleanJS();
				var email = evalSelf && form["Email"].ToBooleanJS(); //Sending email requires that EvaluateSelf is true
				if (evalSelf) {
					byAbouts.AddRange(byAbouts.Select(x => new ByAbout(x.GetAbout(), x.GetAbout())).ToList());
				}
				byAbouts = byAbouts.Distinct().ToList();
				await QuarterlyConversationAccessor.GenerateQuarterlyConversation(GetUser(), name, byAbouts, dueDate, email);
				return RedirectToAction("Index");
			}
			var possible = QuarterlyConversationAccessor.AvailableByAbouts(GetUser());
			return View(new IssueViewModel() {
				AvailableUsers = possible,
				DueDate = dueDate,
				Name = form["Name"],
				EvalSelf = (form["EvalSelf"] ?? "true").ToBooleanJS(),
				Email = (form["Email"] ?? "true").ToBooleanJS(),
			});
		}
	}
}