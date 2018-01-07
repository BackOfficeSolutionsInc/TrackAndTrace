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
using RadialReview.Models.Enums;
using NHibernate;
using RadialReview.Utilities;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Properties;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Areas.People.Controllers {
	public class QuarterlyConversationController : BaseController {
		// GET: People/QuarterlyConversation
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			ViewBag.CanCreate = new PermissionsAccessor().IsPermitted(GetUser(), x => x.CreateQuarterlyConversation(GetUser().Organization.Id));
			var containers = SurveyAccessor.GetSurveyContainersBy(GetUser(), GetUser(), SurveyType.QuarterlyConversation).OrderByDescending(x => x.IssueDate);
			return View(containers);
		}

		public class IssueViewModel {
			public IEnumerable<SurveyUserNode> AvailableUsers { get; set; }
            public DateTime DueDate { get; set; }
            public DateTime QuarterStart { get; set; }
            public bool Email { get; set; }
			public bool EvalSelf { get; set; }
			public string Name { get; set; }
			public bool EvalManager { get; internal set; }

			public IssueViewModel() {
				Email = true;
				EvalSelf = true;
                QuarterStart = DateTime.UtcNow.AddDays(-90);
			}
		}
		private IEnumerable<SurveyUserNode> Possible() {
			return QuarterlyConversationAccessor.AvailableAboutsForMe(GetUser());
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Issue() {

			var vm = new IssueViewModel() {
				AvailableUsers = Possible(),
				DueDate = GetUser().GetTimeSettings().ConvertFromServerTime(DateTime.UtcNow.AddDays(7)),
			};
			return View(vm);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<ActionResult> Issue(FormCollection form) {
			var name = form["Name"];
            var qtrStart = (form["QuarterStart"] ?? "").ToDateTime("MM-dd-yyyy HH:mm:ss");
            var dueDate = (form["DueDate"] ?? "").ToDateTime("MM-dd-yyyy HH:mm:ss");

			if (string.IsNullOrWhiteSpace(name))
				ModelState.AddModelError("name", "Name is required");
			if (ModelState.IsValid) {
				var arr = form["selected"].NotNull(x => x.Split(',').ToList()) ?? new List<string>();
				var byAbouts = arr.Select(x => SurveyUserNode.FromViewModelKey(x)).ToList();
				var evalSelf = form["EvalSelf"].ToBooleanJS();
				var evalManager = form["EvalManager"].ToBooleanJS();
				var email = (evalSelf || evalManager) && form["Email"].ToBooleanJS(); //Sending email requires that EvaluateSelf is true

				var filtered = QuarterlyConversationAccessor.AvailableByAboutsFiltered(GetUser(), byAbouts, evalSelf, evalManager);

                //if (evalManager) {
                //	byAbouts.AddRange(byAbouts.Select(x => new ByAboutSurveyUserNode(x.About, x.By,AboutType.Subordinate)).ToList());
                //}
                //if (evalSelf) {
                //	byAbouts.AddRange(byAbouts.Select(x => new ByAboutSurveyUserNode(x.About, x.About, AboutType.Self)).ToList());
                //}
                //byAbouts = byAbouts.Distinct().ToList();
                var quarterRange = new DateRange(qtrStart, qtrStart.AddDays(90));


                var id = await QuarterlyConversationAccessor.GenerateQuarterlyConversation(GetUser(), name, filtered, quarterRange, dueDate, email);
				return RedirectToAction("Questions",new { id = id });
			}

			return View(new IssueViewModel() {
				AvailableUsers = Possible(),
				DueDate = dueDate,
                QuarterStart = qtrStart, 
				Name = form["Name"],
				EvalSelf = (form["EvalSelf"] ?? "true").ToBooleanJS(),
				Email = (form["Email"] ?? "true").ToBooleanJS(),
				EvalManager = (form["EvalManager"] ?? "true").ToBooleanJS(),                
			});
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Questions(long id) {
			return View(id);
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Print(long surveyContainerId, long sunId, bool print = true) {
			var surveyContainer = SurveyAccessor.GetSurveyContainerAbout(GetUser(), ForModel.Create<SurveyUserNode>(sunId), surveyContainerId);
			var doc = SurveyPdfAccessor.CreateDoc(GetUser(), "Quarterly Conversation");
			foreach (var survey in surveyContainer.GetSurveys()) {
				SurveyPdfAccessor.AppendSurveyAbout(doc, surveyContainer.GetName(), DateTime.UtcNow, survey);
			}
			return Pdf(doc, "Quarterly Conversation", !print);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Remind(long surveyContainerId, long sunId) {
			var surveyContainer = SurveyAccessor.GetSurveyContainerAbout(GetUser(), ForModel.Create<SurveyUserNode>(sunId), surveyContainerId);

			var caller = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var sun = s.Get<SurveyUserNode>(sunId);
					perms.ViewUserOrganization(sun.UserOrganizationId, false);

                    var user = s.Get<UserOrganizationModel>(sun.UserOrganizationId);                    
                    await QuarterlyConversationAccessor.SendReminderUnsafe(s,user,surveyContainerId);

				}
			}
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult PrintAll(long surveyContainerId, bool print = true) {

			var doc = SurveyPdfAccessor.CreateDoc(GetUser(), "All Quarterly Conversations");

			var allAbout = QuarterlyConversationAccessor.GetPeopleAnalyzer(GetUser(), GetUser().Id).Responses
				.Where(x => x.SurveyContainerId == surveyContainerId && x.SunId.HasValue)
				.Select(x => ForModel.Create<SurveyUserNode>(x.SunId.Value))
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
		public ActionResult LockIn(long id) {
			QuarterlyConversationAccessor.LockinSurvey(GetUser(), id);// QuarterlyConversationAccessor
			return RedirectToAction("Index", "Home", new { area = "" });
		}

        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> RemindAll(long id) {
            var count = await QuarterlyConversationAccessor.RemindAllIncompleteSurveys(GetUser(), id);
            var txt = "All Quarterly Conversations completed.";
            if (count == 1) {
                txt = "Reminder sent";
            }else if (count > 1) {
                txt = "Reminders sent";
            }
            return Json(ResultObject.Success(txt), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
		public JsonResult Remove(long id) {
			SurveyAccessor.RemoveSurveyContainer(GetUser(), id);// QuarterlyConversationAccessor
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

	}
}