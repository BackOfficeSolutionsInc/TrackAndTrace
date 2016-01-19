using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages.Razor.Configuration;
using RadialReview.Models;
using RadialReview.Models.Json;
using RadialReview.Models.Log;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
    public class LogController : BaseController
    {
        // GET: Log
		[Access(AccessLevel.Radial)]
        public ActionResult Index(DateTime? after=null)
		{
			var afterTime = after ?? DateTime.UtcNow.AddDays(-14);
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var logs = s.QueryOver<InteractionLogItem>().Where(x => x.DeleteTime == null && x.CreateTime > afterTime).List().ToList();
					return View(logs);
				}
			}

        }

		[Access(AccessLevel.Radial)]
	    public PartialViewResult Row(InteractionLogItem row)
	    {
		    return PartialView("Row",row);
	    }


		[Access(AccessLevel.Radial)]
		public void Delete(long id)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var f =s.Get<InteractionLogItem>(id);
					f.DeleteTime = DateTime.UtcNow;
					s.Update(f);
					tx.Commit();
					s.Flush();
				}
			}
		}

		[Access(AccessLevel.Radial)]
		[HttpPost]
	    public PartialViewResult Create(InteractionLogItem model)
	    {
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){

					if (model.UserId != null){
						model.AccountType=s.Get<UserOrganizationModel>(model.UserId).Organization.AccountType;
					}
					
					s.Save(model);


					tx.Commit();
					s.Flush();
				}
			}
			return Row(model);
	    }



    }
}