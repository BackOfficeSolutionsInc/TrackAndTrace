using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Controllers;
using RadialReview.Engines.Surveys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;

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
            var output = SurveyAccessor.GetAngularSurveyContainer(GetUser(), 6,null);
            var output2 = output.Surveys.First().Sections.First().Items.First().ItemFormat.Settings;


            return Json(output2, JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Radial)]
        public JsonResult GenFakeSurvey() {

            var byAbouts = new[] {
                new ByAbout(GetUser(),GetUser()),
            };

            var output = SurveyAccessor.GenerateSurvey_Unsafe(GetUser(), "test" + (int)(DateTime.UtcNow.Ticks / 100000), byAbouts);

            return Json(output, JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult Data(long surveyContainerId, long? surveyId) {
            var output =SurveyAccessor.GetAngularSurveyContainer(GetUser(), surveyContainerId, surveyId);
            return Json(output, JsonRequestBehavior.AllowGet);
        }
    }
}