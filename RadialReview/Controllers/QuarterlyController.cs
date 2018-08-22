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

			//if (model["root"] != null && root == null)
			//    root = Newtonsoft.Json.JsonConvert.DeserializeObject<PdfAccessor.AccNodeJs>(model["root"]);

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

			// root:root
			);
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
		public async Task<ActionResult> Printout(long id, bool issues = false, bool todos = false, bool scorecard = true, bool rocks = true, bool headlines = true, bool vto = true, bool l10 = true, bool acc = true, bool print = false, bool quarterly = true/*, PdfAccessor.AccNodeJs root = null*/, bool pa = false) {

			var recur = L10Accessor.GetL10Recurrence(GetUser(), id, LoadMeeting.False());

			var angRecur = await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), id, forceIncludeTodoCompletion: recur.IncludeAggregateTodoCompletionOnPrintout);
			var merger = new DocumentMerger();

			//
			var anyPages = false;
			AngularVTO vtoModel = VtoAccessor.GetAngularVTO(GetUser(), angRecur.VtoId.Value);
			var settings  = new PdfSettings(GetUser().Organization.Settings);

			if (rocks) {
				var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout6");
				PdfAccessor.AddRocks(GetUser(), doc, settings, quarterly, angRecur, vtoModel, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
			if (headlines && angRecur.Headlines.Any()) {
				var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout6");
				PdfAccessor.AddHeadLines(GetUser(), doc, settings, quarterly, angRecur, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
			if (vto && angRecur.VtoId.HasValue && angRecur.VtoId > 0) {
				//vtoModel 
				var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout1");

				var vtoSettings = new VtoPdfSettings(GetUser().GetOrganizationSettings());

				await PdfAccessor.AddVTO(doc, vtoModel, GetUser().GetOrganizationSettings().GetDateFormat(), vtoSettings);
				anyPages = true;
				merger.AddDoc(doc);
			}
			if (acc) {
				try {
					var tree = AccountabilityAccessor.GetTree(GetUser(), GetUser().Organization.AccountabilityChartId, expandAll: true);

					var nodes = new List<AngularAccountabilityNode>();
					var topNodes = tree.Root.GetDirectChildren();

					//Add nodes from the tree.
					tree.Dive(x => {
						if (x.User != null && angRecur.Attendees.Any(a => a.Id == x.User.Id))
							nodes.Add(x);
					});

					//Setup if has parents
					foreach (var n in nodes) {
						n._hasParent = topNodes.All(x => x.Id != n.Id);
					}
					var acSettings = new AccountabilityChartPDF.AccountabilityChartSettings(GetUser().Organization.Settings);
					var doc = AccountabilityChartPDF.GenerateAccountabilityChartSingleLevelsMultiDocumentsPerPage(nodes, XUnit.FromInch(11), XUnit.FromInch(8.5),acSettings , true);

				
					merger.AddDoc(doc);
					anyPages = true;
				} catch (Exception e) {
					int a = 0;
				}
			}
			if (l10) {
				var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printou2t");
				PdfAccessor.AddL10(doc, angRecur, settings, L10Accessor.GetLastMeetingEndTime(GetUser(), id), addPageNumber: false);
				anyPages = true;
				merger.AddDoc(doc);
			}
			if (scorecard) {
				var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printou5t");
				var addedSC = PdfAccessor.AddScorecard(doc, angRecur, settings, addPageNumber: false);
				anyPages = anyPages || addedSC;
				if (addedSC) {
					merger.AddDoc(doc);
				}
			}
			if (todos) {
				var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout3");
				await PdfAccessor.AddTodos(GetUser(), doc,  angRecur,settings, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
			if (issues) {
				var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout4");
				PdfAccessor.AddIssues(GetUser(), doc, angRecur, settings, todos, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
			if (pa) {
                try {
                    var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout5");
                    var peopleAnalyzer = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(GetUser(), GetUser().Id, id);
                    var renderer = PeopleAnalyzerPdf.AppendPeopleAnalyzer(GetUser(), doc, peopleAnalyzer, settings, DateTime.MaxValue);
                    merger.AddDoc(renderer);
                    anyPages = true;
                }catch(Exception e) {
                    //eat it..
                }
			}


			var now = DateTime.UtcNow.ToJavascriptMilliseconds() + "";
			if (!anyPages)
				return Content("No pages to print.");

			var doc1 = merger.Flatten("Quarterly Printout", true, true, GetUser().Organization.Settings.GetDateFormat(), recur.Name);


			return Pdf(doc1, now + "_" + angRecur.Basics.Name + "_QuarterlyPrintout.pdf", true);
		}
	}
}