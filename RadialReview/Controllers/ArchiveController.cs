using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ArchiveController : BaseController
    {
        
        [Access(AccessLevel.Manager)]
        public ActionResult Users()
        {
            var user = GetUser();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    //tx.Commit();
                    //s.Flush();

                    var users = s.QueryOver<UserOrganizationModel>()
                        .Where(x => x.DeleteTime != null && x.Organization.Id == user.Organization.Id)
                        .List().ToList();

                    return View(users);

                }
            }
        }
    }
}