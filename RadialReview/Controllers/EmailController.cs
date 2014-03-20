using RadialReview.Accessors;
using RadialReview.Models.Application;
using RadialReview.Models.Json;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{

    public class EmailController : BaseController
    {

        [Access(AccessLevel.Manager)]
        public async Task<JsonResult> RemindAboutReview(long id)
        {
            var reviewContainterId = id;
            var allReviews= _ReviewAccessor.GetReviewsForReviewContainer(GetUser(), id);
            var incompleteReviews = allReviews.Where(x=>!x.Complete).Select(x=>x.ForUser).ToList();

            var organization=GetUser().Organization.GetName();

            var result= await Emailer.SendEmails(incompleteReviews.Select(x =>
                MailModel.To(x.GetEmail())
                .Subject(EmailStrings.ReminderReview_Subject, organization)
                .Body(EmailStrings.RemindReview_Body, x.GetFirstName())
               ));

            return Json(ResultObject.Create(result), JsonRequestBehavior.AllowGet);
        }
    }
}