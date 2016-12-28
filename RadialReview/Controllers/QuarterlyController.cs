using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Amazon.IdentityManagement.Model;
using RadialReview.Accessors;
using RadialReview.Models.Angular.VTO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace RadialReview.Controllers {
    public class QuarterlyController : BaseController {
        // GET: Quarterly
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index() {
            return View();
        }

        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult Modal(long id) {
            return PartialView(id);
        }

        public class PrintoutOptions {
            public bool issues { get; set; }
            public bool todos { get; set; }
            public bool scorecard { get; set; }
            public bool rocks { get; set; }
            public bool vto { get; set; }
            public bool l10 { get; set; }
            public bool acc { get; set; }
        }



        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public ActionResult Printout(long id, FormCollection model, PdfAccessor.AccNodeJs root = null) {

            if (model["root"] != null && root == null)
                root = Newtonsoft.Json.JsonConvert.DeserializeObject<PdfAccessor.AccNodeJs>(model["root"]);

            return Printout(id,
                model["issues"].ToBooleanJS(),
                model["todos"].ToBooleanJS(),
                model["scorecard"].ToBooleanJS(),
                model["rocks"].ToBooleanJS(),
                model["vto"].ToBooleanJS(),
                model["l10"].ToBooleanJS(),
                model["acc"].ToBooleanJS(),
                root:root
            );
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpGet]
        public ActionResult PrintVTO(long id) {
            var vto = VtoAccessor.GetAngularVTO(GetUser(), id);
            var doc = PdfAccessor.CreateDoc(GetUser(), vto.Name + " Vision/Traction Organizer");

            PdfAccessor.AddVTO(doc, vto, GetUser().GetOrganizationSettings().GetDateFormat());
            var now = DateTime.UtcNow.ToJavascriptMilliseconds() + "";
            return Pdf(doc, now + "_" + vto.Name + "_VTO.pdf", true);
        }
        [Access(AccessLevel.UserOrganization)]
        [HttpGet]
        public ActionResult PrintPages(long id, bool issues = false, bool todos = false, bool scorecard = false, bool rocks = false, bool vto = false, bool l10 = false, bool acc = false, bool print = false) {
            return Printout(id, issues, todos, scorecard, rocks, vto, l10, acc, print);
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpGet]
        public ActionResult Printout(long id, bool issues = false, bool todos = false, bool scorecard = true, bool rocks = true, bool vto = true, bool l10 = true, bool acc = true, bool print = false, PdfAccessor.AccNodeJs root = null) {
            var recur = L10Accessor.GetAngularRecurrence(GetUser(), id);
            var merger = new DocumentMerger();
			
            //
            var anyPages = false;
            AngularVTO vtoModel = VtoAccessor.GetAngularVTO(GetUser(), recur.VtoId.Value);
            if (vto && recur.VtoId.HasValue && recur.VtoId > 0) {
                //vtoModel 
                var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout1");
                PdfAccessor.AddVTO(doc, vtoModel, GetUser().GetOrganizationSettings().GetDateFormat());
                anyPages = true;
                merger.AddDoc(doc);
            }
            if (l10) {
                var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printou2t");
                PdfAccessor.AddL10(doc, recur, L10Accessor.GetLastMeetingEndTime(GetUser(), id), addPageNumber: false);
				anyPages = true;
                merger.AddDoc(doc);
            }

            if (todos) {
                var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout3");
				PdfAccessor.AddTodos(GetUser(), doc, recur, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
            if (issues) {
				var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout4");
				PdfAccessor.AddIssues(GetUser(), doc, recur, todos, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
            if (scorecard) {
				var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printou5t");
				var addedSC = PdfAccessor.AddScorecard(doc, recur, addPageNumber: false);
				anyPages = anyPages || addedSC;
				if (addedSC) {
					merger.AddDoc(doc);
				}
			}
            if (rocks) {
				var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout6");
				PdfAccessor.AddRocks(GetUser(), doc, recur, vtoModel, addPageNumber: false);
				merger.AddDoc(doc);
				anyPages = true;
			}
            if (acc && root != null && root.children!=null) {
				//var doc = new PdfDocument();
				//PdfAccessor.AddAccountabilityChart(GetUser(), doc, root);				
				merger.AddDoc(PdfAccessor.GenerateAccountabilityChart(root,11,8.5,false));
				anyPages = true;
			}
            var now = DateTime.UtcNow.ToJavascriptMilliseconds() + "";
            if (!anyPages)
                return Content("No pages to print.");

			var doc1 = merger.Flatten("Quarterly Printout",true);


			return Pdf(doc1, now + "_" + recur.Basics.Name + "_QuarterlyPrintout.pdf", true);
        }
    }
}