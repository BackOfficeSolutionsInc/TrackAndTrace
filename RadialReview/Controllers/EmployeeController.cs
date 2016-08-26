﻿using System;
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
        public ActionResult Listing()
		{
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, user).ViewOrganization(user.Organization.Id);
#pragma warning disable CS0618 // Type or member is obsolete
					return View(OrganizationAccessor.GetAllUserOrganizations(s,perms,user.Organization.Id));
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
		}
    }
}