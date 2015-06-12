using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Utilities;
using TrelloNet;

namespace RadialReview.Controllers
{
    public class EmployeeController : BaseController
    {
        // GET: Employee
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Handbook()
		{
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, user).ViewOrganization(user.Organization.Id);
					return View(OrganizationAccessor.GetAllUserOrganizations(s,perms,user.Organization.Id));
				}
			}
		}
    }
}