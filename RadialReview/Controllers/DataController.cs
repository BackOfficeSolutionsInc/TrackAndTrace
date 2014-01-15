using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class DataController : BaseController
    {

        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
        protected static PermissionsAccessor _PermissionsAccessor = new PermissionsAccessor();


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

        [Access(AccessLevel.UserOrganization)]
        public FileContentResult OrganizationReviewData(long id, long reviewsId)
        {
            _PermissionsAccessor.Permitted(GetUser(), x => x.EditUserOrganization(id));

            var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id);

            var reviewAnswers = _ReviewAccessor.GetReviewContainerAnswers(GetUser(), reviewsId);
            var completedSliders = reviewAnswers.Where(x => x.Askable.GetQuestionType() == QuestionType.Slider && x.Complete).Cast<SliderAnswer>();

            var categoryIds = categories.Select(x=>x.Id).ToList();

            StringBuilder sb=new StringBuilder();

            //Header row
            sb.AppendLine("about,"+String.Join(",",categoryIds));

            var sbMiddle = new StringBuilder();
            var sbEnd = new StringBuilder();
            
            foreach(var c in completedSliders.GroupBy(x=>x.AboutUserId)) //answers about each user
            {
                var dictionary = new Multimap<long,decimal>();

                foreach(var answer in c.ToList())
                    dictionary.AddNTimes(answer.Askable.Category.Id, answer.Percentage.Value * 200 - 100,(int)answer.Askable.Weight);

                var cols = new String[categoryIds.Count];

                for(int i=0;i<categoryIds.Count;i++)
                {
                    var datapts = dictionary.Get(categoryIds[i]);
                    var average = 0m;
                    if(datapts.Count>0)
                        average=datapts.Average();
                    cols[i] = "" + average;
                }
                var row=String.Join(",",cols);

                sb.AppendLine("Employee,"+row);

                if(c.First().AboutUser.IsManager())
                    sbMiddle.AppendLine("Management," + row);

                if (c.First().AboutUserId == id)
                    sbEnd.AppendLine("You," + row);
            }
            var managers=sbMiddle.ToString();
            var you = sbEnd.ToString();

            if (!String.IsNullOrWhiteSpace(managers))
                sb.Append(managers);
            if (!String.IsNullOrWhiteSpace(you))
                sb.Append(you);

        

            /*
            
            if (!String.IsNullOrWhiteSpace(managers))
                sb.AppendLine(managers);
            if (!String.IsNullOrWhiteSpace(you))
                sb.AppendLine(you);*/
            var csv = sb.ToString();

            return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", "Report.csv");

        }

	}
}