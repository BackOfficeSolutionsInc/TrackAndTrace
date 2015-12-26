using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Json;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{

    public class EmailController : BaseController
    {

        [Access(AccessLevel.Manager)]
        public async Task<JsonResult> RemindAboutReview(long id)
        {
	        throw new PermissionsException("todo");
           /* var reviewContainterId = id;
            var allReviews= _ReviewAccessor.GetReviewsForReviewContainer(GetUser(), id,true);
            var incompleteReviews = allReviews.Where(x=>!x.Complete).Select(x=>x.ForUser).ToList();

            var organization=GetUser().Organization.GetName();

            var result= await Emailer.SendEmails(incompleteReviews.Select(x =>
                MailModel.To(x.GetEmail())
                .Subject(EmailStrings.ReminderReview_Subject, organization)
				.Body(EmailStrings.RemindReview_Body, x.GetFirstName(),
				x.DueDate.AddDays(-1).ToShortDateString(),
					url,
					url,
					ProductStrings.ProductName)
               ));

            return Json(ResultObject.Create(result), JsonRequestBehavior.AllowGet);*/
        }

	    public class ReminderVM
	    {
			public string UserIds { get; set; }
			public long ReviewId { get; set; }
			public int Count { get; set; }

	    }


		[Access(AccessLevel.Manager)]
		public ActionResult Remind(long reviewId,string userIds)
		{
			if (userIds == null || reviewId == 0)
				throw new PermissionsException("Invalid arguments.");
			var ids = userIds.Split(',').Select(long.Parse);

			var allReviews = _ReviewAccessor.GetReviewsForReviewContainer(GetUser(), reviewId, true);
			var uniqueUsers = allReviews.Where(x => ids.Contains(x.ForUserId));
			return PartialView(new ReminderVM{
				UserIds = string.Join(",",uniqueUsers.Select(x => x.Id)),
				ReviewId = reviewId,
				Count = uniqueUsers.Count()
			});
		}


	    [Access(AccessLevel.Manager)]
		[HttpPost]
	    public async Task<JsonResult> Remind(ReminderVM model)
	    {
		    var review = _ReviewAccessor.GetReviewContainer(GetUser(), model.ReviewId, false, false, false);

			var allReviews = _ReviewAccessor.GetReviewsForReviewContainer(GetUser(), model.ReviewId, true);
		    var ids = model.UserIds.Split(',').Select(long.Parse);

			var uniqueReviews = allReviews
				.Where(x => ids.Contains(x.ForUserId))
				.OrderByDescending(x=>x.DueDate);

		    var url = Config.BaseUrl(review.ForOrganization) + "Tasks";

		    //var users = _UserAccessor.GetUsersByIds(model.UserIds);
			var organization = GetUser().Organization.GetName();
			
			var result = await Emailer.SendEmails(uniqueReviews.Select(x =>
				Mail.To(EmailTypes.ReviewReminder,x.ForUser.GetEmail())
				.Subject(EmailStrings.ReminderReview_Subject, organization)
				.Body(EmailStrings.RemindReview_Body,
					x.ForUser.GetFirstName(),
					review.DueDate.AddDays(-1).ToShortDateString(),
					url,
					url,
					Config.ProductName(review.ForOrganization))
			   ));

			return Json(ResultObject.Create(result), JsonRequestBehavior.AllowGet);
	    }
    }
}