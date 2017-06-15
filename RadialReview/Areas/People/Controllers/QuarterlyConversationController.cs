using RadialReview.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Areas.People.Controllers
{
    public class QuarterlyConversationController : BaseController
    {
        // GET: People/QuarterlyConversation
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {

            return View();
        }
    }
}