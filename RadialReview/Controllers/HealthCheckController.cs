using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class HealthCheckController : Controller
    {
        // GET: HealthCheck
        public bool Index()
        {
            if (Config.GetEnv() != Env.production)
                throw new Exception("Env is not production (found " + Config.GetEnv() + ")");
            return true;
        }
    }
}