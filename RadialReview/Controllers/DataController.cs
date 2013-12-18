using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class DataController : BaseController
    {

        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();

        [Access(AccessLevel.UserOrganization)]
        public JsonResult OrganizationHierarchy(long id)
        {
            var tree = _OrganizationAccessor.GetOrganizationTree(GetUser(), id);

            return Json(tree, JsonRequestBehavior.AllowGet);
        }

        public class Merger
        {
            public Dictionary<long, List<decimal>> dictionary { get; set; }

            public AboutType About { get; set; }

            public Merger(List<SliderAnswer> merger)
            {
                dictionary = new Dictionary<long, List<decimal>>();
                foreach (var m in merger)
                {
                    var catId=m.Askable.Category.Id;
                    var found = new List<decimal>();
                    if (dictionary.ContainsKey(catId))
                        found = dictionary[catId];
                    for (int i = 0; i < (int)m.Askable.Weight; i++)
                    {
                        found.Add(m.Percentage.Value * 200 - 100);
                    }
                    dictionary[catId] = found;
                    About = m.AboutType;
                }
            }

            public String ToCsv(List<QuestionCategoryModel> categories)
            {
                var list = categories.Select(c =>{
                    var catId=c.Id;
                    if (dictionary.ContainsKey(catId))
                        return "" + dictionary[catId].Average();
                    return "0";
                }).ToList();

                var about = About.GetFlags().OrderBy(x => x).LastOrDefault();
                
                list.Insert(0, Convert(about));

                return String.Join(",", list);
            }

            private String Convert(Enum e)
            {
                var str = e.ToString();

                if (str == AboutType.Subordinate.ToString())
                    return AboutType.Manager.ToString();

                if (str == AboutType.Manager.ToString())
                    return AboutType.Subordinate.ToString();

                return e.ToString();
            }

        }

        [Access(AccessLevel.UserOrganization)]

        public FileContentResult ReviewData(long id,long reviewsId)
        {
            var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id);

            var review=_ReviewAccessor.GetAnswersForUserReview(GetUser(), id,reviewsId);
            var completeSliders = review.Where(x => x.Askable.GetQuestionType() == QuestionType.Slider && x.Complete).Cast<SliderAnswer>();

            var titles = categories.Select(x => "" + x.Id).ToList();
            titles.Insert(0, "about");

            var lines = completeSliders.GroupBy(x => x.ByUserId).Select(x => new Merger(x.ToList()).ToCsv(categories)).ToList();
            lines.Insert(0, String.Join(",",titles));


            var csv = String.Join("\n",lines);
            return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", "Report.csv");
        }

	}
}