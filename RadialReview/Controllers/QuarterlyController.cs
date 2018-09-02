using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Angular.VTO;
using RadialReview.Accessors.PDF;
using RadialReview.Models.Angular.Accountability;
using System.Threading.Tasks;
using static RadialReview.Accessors.PdfAccessor;
using RadialReview.Areas.People.Accessors.PDF;
using RadialReview.Areas.People.Accessors;
using RadialReview.Utilities.Pdf;
using PdfSharp.Drawing;
using RadialReview.Models.Json;

namespace RadialReview.Controllers {
    public class QuarterlyController : BaseController {
		// GET: Quarterly
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			return View();
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Modal(long id) {
			ViewBag.IncludePeople = GetUser().Organization.Settings.EnablePeople;
			return PartialView(id);
		}

		public class PrintoutOptions {
			public bool issues { get; set; }
			public bool todos { get; set; }
			public bool scorecard { get; set; }
			public bool rocks { get; set; }
			public bool headlines { get; set; }
			public bool vto { get; set; }
			public bool l10 { get; set; }
			public bool acc { get; set; }
			public bool pa { get; set; }
		}



		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<ActionResult> Printout(long id, FormCollection model/*, PdfAccessor.AccNodeJs root = null*/) {
			return await Printout(id,
				model["issues"].ToBooleanJS(),
				model["todos"].ToBooleanJS(),
				model["scorecard"].ToBooleanJS(),
				model["rocks"].ToBooleanJS(),
				model["headlines"].ToBooleanJS(),
				model["vto"].ToBooleanJS(),
				model["l10"].ToBooleanJS(),
				model["acc"].ToBooleanJS(),
				model["print"].ToBooleanJS(),
				model["quarterly"].ToBooleanJS(),
				model["pa"].ToBooleanJS()				
			);
		}

		public class SendQuarterlyVM {

			public class ScheduledEmails {
				public string Email { get; set; }
				public string Date { get; set; }
				public bool Sent { get; set; }
			}

			public string ImplementerEmail { get; set; }
			public DateTime SendDate { get; set; }
			public bool Later { get; set; }
			public long RecurrenceId { get; set; }
			public List<ScheduledEmails> Scheduled { get; set; }
			public SendQuarterlyVM() {
				SendDate = DateTime.UtcNow;
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Send(long id) {
			PermissionsAccessor.EnsurePermitted(GetUser(), x => x.ViewL10Recurrence(id));

			var sched = QuarterlyAccessor.GetScheduledEmails(GetUser(), id);

			var model = new SendQuarterlyVM() {
				ImplementerEmail = GetUser().Organization.ImplementerEmail,
				RecurrenceId = id,
				Scheduled =  sched.Select(x=> new SendQuarterlyVM.ScheduledEmails() {
					Date = x.ScheduledTime.ToString("MM/dd/yyyy"),
					Email = x.Email,
					Sent = x.SentTime!=null
				}).ToList()
			};

			return PartialView(model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult Send(SendQuarterlyVM model) {
			QuarterlyAccessor.ScheduleQuarterlyEmail(GetUser(), model.RecurrenceId, model.ImplementerEmail,model.Later?model.SendDate:DateTime.UtcNow);
			
			return Json(ResultObject.Success((model.Later ? "Email scheduled!":"Email sent!")));
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpGet]
		public async Task<ActionResult> PrintVTO(long id,string fill=null,string border=null,string image=null, string filltext = null, string lighttext = null, string lightborder = null, string textColor = null) {
			var vto = VtoAccessor.GetAngularVTO(GetUser(), id);
			var doc = PdfAccessor.CreateDoc(GetUser(), vto.Name + " Vision/Traction Organizer");

			var settings = new VtoPdfSettings(image, fill,lighttext, lightborder, filltext,textColor,border);
			await PdfAccessor.AddVTO(doc, vto, GetUser().GetOrganizationSettings().GetDateFormat(), settings);
			var now = DateTime.UtcNow.ToJavascriptMilliseconds() + "";

			var merger = new DocumentMerger();
			merger.AddDoc(doc);
			var merged = merger.Flatten(now + "_" + vto.Name + "_VTO.pdf", false, true, GetUser().Organization.Settings.GetDateFormat());



			return Pdf(merged, now + "_" + vto.Name + "_VTO.pdf", true);
		}
		[Access(AccessLevel.UserOrganization)]
		[HttpGet]
		public async Task<ActionResult> PrintPages(long id, bool issues = false, bool todos = false, bool scorecard = false, bool rocks = false, bool vto = false, bool l10 = false, bool acc = false, bool print = false) {
			return await Printout(id, issues, todos, scorecard, rocks, false, vto, l10, acc, print);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpGet]
		public async Task<ActionResult> Printout(long id, bool issues = false, bool todos = false, bool scorecard = true, bool rocks = true, bool headlines = true, bool vto = true, bool l10 = true, bool acc = true, bool print = false, bool quarterly = true/*, PdfAccessor.AccNodeJs root = null*/, bool pa = false,int? maxSec=null) {
			var d = await PdfAccessor.QuarterlyPrintout(GetUser(),id,issues,todos,scorecard,rocks,headlines,vto,l10,acc,print,quarterly,pa,maxSec);
			return Pdf(d.Document, d.CreateTime.ToJsMs() + "_" + d.RecurrenceName + "_QuarterlyPrintout.pdf", true);
		}
	}
}