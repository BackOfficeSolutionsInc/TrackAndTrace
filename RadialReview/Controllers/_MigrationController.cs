﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Models.Responsibilities;

namespace RadialReview.Controllers
{
	public class MigrationController : BaseController {
        // GET: Migration
		[Access(AccessLevel.Radial)]
        public int M11_8_2014()
		{
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx=s.BeginTransaction()){
					foreach (var a in s.QueryOver<Askable>().List()){
						if (a.OnlyAsk == AboutType.NoRelationship){
							a.OnlyAsk = (AboutType) long.MaxValue;
							s.Update(a);
							count++;
						}
					}

					foreach (var r in s.QueryOver<RoleModel>().List()){
						if (r.OrganizationId == 0){
							r.OrganizationId = s.Get<UserOrganizationModel>(r.ForUserId).Organization.Id;
							s.Update(r);
							count++;
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
			return count;
        }
    }
}