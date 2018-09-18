using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Json;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class SupportController : BaseController {


		// GET: Support
		[HttpPost]
		[Access(AccessLevel.Any)]
		public async Task<JsonResult> Email(SupportData model) {
            try {
                UserModel user;
                String email = "";
                String name = null;
                try {
                    user = GetUserModel();
                    email = user.UserName;
                    model.Email = email;
                    name = user.FirstName + " " + user.LastName;
                } catch (Exception) {
                    user = null;
                    email = model.Email;
                }

                model.UserAgent = Request.UserAgent;

                SupportAccessor.Add(model);

                StringBuilder builder = new StringBuilder();
                builder.Append(model.Body);

                builder.Append("<br/><br/><span style='color:#aaaaaa;'>Ticket: <a style='text-decoration:none;color:#aaaaaa;cursor:default;' href='" + Config.BaseUrl(null) + "Support/Details/" + model.Lookup + "'>" + model.Lookup + "</a></span>");

                builder.Append("<img src='" + Config.BaseUrl(null) + "t/mark/" + model.Lookup + "?a=true'/>");
				//builder.Append("<br/><br/><div style='color:#aaa'>#####################################");

				//builder.Append("<table>");
				//builder.Append("<tr><th>Email</th><td >" + email + "</td></tr>");
				//builder.Append("<tr><th>User</th><td >" + model.User + "</td></tr>");
				//builder.Append("<tr><th>Org</th><td >" + model.Org + "</td></tr>");
				//builder.Append("<tr><th>PageTitle</th><td >" + model.PageTitle + "</td></tr>");
				//builder.Append("<tr><th>Url</th><td >" + model.Url + "</td></tr>");
				//builder.Append("<tr><th>Console</th><td >" + model.Console + "</td></tr>");            
				//builder.Append("</table></div>");
				var test = false;
				if (test) {
					throw new Exception();
				}


                var emailAddress = ProductStrings.SupportEmail;
                if (model.Status == SupportStatus.JavascriptError)
                    emailAddress = ProductStrings.EngineeringEmail;


                var mail = Mail.To(EmailTypes.CustomerSupport, emailAddress)
                .SubjectPlainText(model.Subject ?? "Customer Service")
                .BodyPlainText(builder.ToString());
                mail.ReplyToAddress = email;
                mail.ReplyToName = name;

                if (!Config.IsLocal()) {
                    await Emailer.SendEmail(mail);
                }

                var result = ResultObject.Success("A message has been sent to support. We'll be contacting you shortly.");

				if (model.Status == SupportStatus.JavascriptError) {
					result.ForceSilent();
					result.ForceNoErrorReport();
				}

                return Json(result);
            }catch(Exception e) {
                var result = ResultObject.Success("There was a problem sending your error report.");
				result.Status = StatusType.Warning;
				if (model.Status == SupportStatus.JavascriptError) {
					result.ForceSilent();
					result.ForceNoErrorReport();
				}
				return Json(result);
            }
		}

		[Access(AccessLevel.Radial)]
		public JsonResult Status(string id, SupportStatus status) {
			SupportAccessor.SetStatus(id, status);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.Radial)]
		public ActionResult List(bool open = true, bool closed = false, bool backlog = false, bool nofix = false, bool all = false, bool js = false) {
			return View(SupportAccessor.List(open || all, closed || all, backlog || all, nofix || all, js || all));
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Details(string id = null) {
			if (GetUserModel().IsRadialAdmin) {
				if (string.IsNullOrWhiteSpace(id))
					return RedirectToAction("List");

				TrackingAccessor.MarkSeen(id, GetUser(), Tracker.TrackerSource.Website);
				var model = SupportAccessor.Get(id);
				return View(model);
			}


			return RedirectToAction("Index", "Home");
		}
	}
}