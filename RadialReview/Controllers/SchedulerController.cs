using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class SchedulerController : BaseController
    {
        //
        // GET: /Scheduler/
        [Access(AccessLevel.Any)]
        public bool Index()
        {
            return true;
        }

        [Access(AccessLevel.Any)]
        public bool Reschedule()
        {
            return ServerUtility.RegisterCacheEntry();
        }
	}
}