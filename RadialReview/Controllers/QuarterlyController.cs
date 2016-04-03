using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Amazon.IdentityManagement.Model;
using RadialReview.Accessors;

namespace RadialReview.Controllers
{
    public class QuarterlyController : BaseController
    {
        // GET: Quarterly
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
            return View();
        }

		[Access(AccessLevel.UserOrganization)]
        public ActionResult Printout(long id, bool issues = true, bool todos = true, bool scorecard = true, bool rocks = true, bool vto = true, bool print = false)
	    {
            var recur = L10Accessor.GetAngularRecurrence(GetUser(), id);
            var doc = PdfAccessor.CreateDoc(GetUser(), "Quarterly Printout");

            if (vto && recur.VtoId.HasValue && recur.VtoId>0) {
                var vtoModel=VtoAccessor.GetAngularVTO(GetUser(), recur.VtoId.Value);
                PdfAccessor.AddVTO(doc, vtoModel);
            }

            if (scorecard)  PdfAccessor.AddScorecard(doc, recur);
            if (rocks)      PdfAccessor.AddRocks(GetUser(), doc, recur);
            if (todos)      PdfAccessor.AddTodos(GetUser(), doc, recur);
            if (issues)     PdfAccessor.AddIssues(GetUser(), doc, recur, todos);

            var now = DateTime.UtcNow.ToJavascriptMilliseconds() + "";

            return Pdf(doc,now+"_"+recur.Name+"_QuarterlyPrintout.pdf", true);
	    }
    }
}