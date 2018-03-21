using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Controllers;
using RadialReview.Areas.People.Engines.Surveys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;
using RadialReview.Models.Json;
using RadialReview.Accessors;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Areas.People.Controllers {
	public class SurveyController : BaseController {

		[Access(AccessLevel.Radial)]
		public JsonResult Test() {

			//var output=new AngularSurveyItemFormat() {
			//    ItemType = SurveyItemType.Text
			//};
			//var output1 = new AngularSurveyItemContainer() {
			//    ItemFormat = output
			//};
			//var output2 = new AngularSurveySection() {
			//    Items = new[] { output1 }
			//};
			var output = SurveyAccessor.GetAngularSurveyContainerBy(GetUser(), GetUser(), 6);
			var output2 = output.Surveys.First().Sections.First().Items.First().ItemFormat.Settings;


			return Json(output2, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Radial)]
		public JsonResult GenFakeSurvey() {

			var subs = DeepAccessor.Tiny.GetSubordinatesAndSelf(GetUser(), GetUser().Id);

			var byAbouts = new List<ByAboutSurveyUserNode>();

			foreach (var sub in subs)
				byAbouts.Add(new ByAboutSurveyUserNode(
					new SurveyUserNode() { UserOrganizationId = GetUser().Id },
					new SurveyUserNode() { UserOrganizationId = sub.ModelId },
					AboutType.Subordinate
				));

			var output = QuarterlyConversationAccessor.GenerateQuarterlyConversation(GetUser(), "test" + (int)(DateTime.UtcNow.Ticks / 100000), byAbouts, new DateRange(), DateTime.UtcNow.AddDays(1), false);

			return Json(output, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Data(long surveyContainerId) {
			var output = SurveyAccessor.GetAngularSurveyContainerBy(GetUser(), GetUser(), surveyContainerId);
			return Json(output, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateAngularSurveyResponse(AngularSurveyResponse response, string connectionId = null) {
			var output = SurveyAccessor.UpdateAngularSurveyResponse(GetUser(), response.Id, response.Answer, connectionId);
			return Json(ResultObject.SilentSuccess());
		}


	}
}