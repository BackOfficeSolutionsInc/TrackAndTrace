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
	    public ActionResult Printout(long id)
	    {
		    var printout = PdfAccessor.GetScorecard(GetUser(), id);
			return Pdf(printout,"QuarterlyPrintout.pdf");
	    }
    }
}