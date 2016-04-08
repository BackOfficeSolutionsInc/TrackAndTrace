using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Amazon.IdentityManagement.Model;
using RadialReview.Accessors;

namespace RadialReview.Controllers {
    public class QuarterlyController : BaseController {
        // GET: Quarterly
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
            return View();
        }

        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult Modal()
        {
            return PartialView();
        }

        public class PrintoutOptions {
            public bool issues { get; set; }
            public bool todos { get; set; }
            public bool scorecard { get; set; }
            public bool rocks { get; set; }
            public bool vto { get; set; }
            public bool l10 { get; set; }
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public ActionResult Printout(long id, FormCollection model)
        {
            return Printout(id,
                model["issues"].ToBooleanJS(),
                model["todos"].ToBooleanJS(),
                model["scorecard"].ToBooleanJS(),
                model["rocks"].ToBooleanJS(),
                model["vto"].ToBooleanJS(),
                model["l10"].ToBooleanJS()
            );
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpGet]
        public ActionResult Printout(long id, bool issues = false, bool todos = false, bool scorecard = true, bool rocks = true, bool vto = true, bool l10 = true, bool print = false)
        {
            var recur = L10Accessor.GetAngularRecurrence(GetUser(), id);
            var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout");

            if (vto && recur.VtoId.HasValue && recur.VtoId > 0) {
                var vtoModel = VtoAccessor.GetAngularVTO(GetUser(), recur.VtoId.Value);
                PdfAccessor.AddVTO(doc, vtoModel);
            }
            if (l10) PdfAccessor.AddL10(doc, recur,L10Accessor.GetLastMeetingEndTime(GetUser(),id));
            if (todos) PdfAccessor.AddTodos(GetUser(), doc, recur);
            if (issues) PdfAccessor.AddIssues(GetUser(), doc, recur, todos);
            if (scorecard) PdfAccessor.AddScorecard(doc, recur);
            if (rocks) PdfAccessor.AddRocks(GetUser(), doc, recur);
            var now = DateTime.UtcNow.ToJavascriptMilliseconds() + "";

            return Pdf(doc, now + "_" + recur.Name + "_QuarterlyPrintout.pdf", true);
        }
    }
}