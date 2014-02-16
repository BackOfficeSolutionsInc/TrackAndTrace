using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Charts;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using RadialReview.Engines;

namespace RadialReview.Controllers
{
    public class DataController : BaseController
    {

        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
        protected static PermissionsAccessor _PermissionsAccessor = new PermissionsAccessor();
        protected static TeamAccessor _TeamAccessor = new TeamAccessor();
        protected static ChartsEngine _ChartsEngine = new ChartsEngine();
        //
        // GET: /Data/
        /*[Access(AccessLevel.Any)]
        public JsonResult Scatter()
        {
            var data = GenerateData();

            return Json(ResultObject.Create(data), JsonRequestBehavior.AllowGet);
        }


        private ScatterPlot GenerateData()
        {
            var points = new List<ScatterData>();
            Random r = new Random(12345);

            var dimMax = 4;

            var min = -100;
            var max = 100;
            var delta = 20;

            var c = 0;

            var dimensions = new List<ScatterDimension>();


            for (int dimNum = 0; dimNum < dimMax; dimNum++)
            {
                var dimName = "Dim-" + dimNum;

                dimensions.Add(new ScatterDimension()
                {
                    Id = "dim-id-" + dimNum,
                    Min = min * r.NextDouble() * 10,
                    Max = max * r.NextDouble() * 10,
                    Name = dimName,
                });
            }
            var dimLookup = dimensions.ToDictionary(x => x.Id, x => x);

            var peopleType = new string[] { "self", "manager", "teammate", "peer", "subordinate" };
            var teamType = new string[] { "team-creative", "team-management", "", "", "", "", "", "", "", "", "", "" };

            var filters = new List<ScatterFilter>();

            filters.Add(new ScatterFilter("Self", "self"));
            filters.Add(new ScatterFilter("Manager", "manager"));
            filters.Add(new ScatterFilter("Creative", "team-creative"));
            filters.Add(new ScatterFilter("Management", "team-management"));

            var groups = new List<ScatterGroup>();
            groups.Add(new ScatterGroup("Self", "self"));
            groups.Add(new ScatterGroup("Manager", "manager"));
            groups.Add(new ScatterGroup("Creative", "team-creative"));
            groups.Add(new ScatterGroup("Subordinate", "subordinate"));

            int id = 99;

            var startDate = new DateTime(1990, 11, 7);
            var now = startDate;
            var dates = new List<DateTime>();

            for (int dateId = 0; dateId < 10; dateId++)
            {
                dates.Add(now);
                now = now.AddDays(r.NextDouble() * 365 * 10);
            }

            for (int personId = 0; personId < 20; personId++)
            {
                var curDims = new List<ScatterDatum>();

                foreach (var dim in dimensions)
                {
                    curDims.Add(new ScatterDatum()
                    {
                        Class = "dim-point-" + dim.Name,
                        DimensionId = dim.Id,
                        Value = (dim.Max - dim.Min) * r.NextDouble() + dim.Min,
                    });
                }

                int? prevId = null;

                var reviewId = 0;

                foreach (var date in dates)
                {
                    reviewId++;
                    points.Add(new ScatterData()
                    {
                        Date = date,
                        Class = "point " + peopleType[personId % peopleType.Length] + " " + teamType[personId % teamType.Length],
                        Dimensions = curDims.ToDictionary(x => x.DimensionId, x => x),
                        Id = id,
                        SliceId = reviewId
                       // PreviousId = prevId,
                    });
                    prevId = id;
                    id += 1;

                    for (int i = 0; i < curDims.Count; i++)
                    {
                        var dim = dimLookup[curDims[i].DimensionId];
                        var curDim = curDims[i];
                        var range = (dim.Max - dim.Min) / 5;

                        var newDim = new ScatterDatum()
                        {
                            Class = curDim.Class,
                            DimensionId = curDim.DimensionId,
                            Value = Math.Min(dim.Max, Math.Max(dim.Min, curDim.Value + range * (r.NextDouble() - .5)))
                        };
                        curDims[i] = newDim;
                    }
                }
            }

            var data = new ScatterPlot()
            {
                Class = "scatter-data",
                Points = points,
                Dimensions = dimensions.ToDictionary(x => x.Id, x => x),
                InitialXDimension = "dim-id-0",
                InitialYDimension = "dim-id-1",
                Filters = filters,
                Groups = groups
            };
            return data;

            /*


            for (int dateNum = 0; dateNum < 10; dateNum++)
            {
                points.Add(new ScatterDataContainer()
                {
                    Id= c,
                    Date = now,
                    Class = "point point-" + dateNum+" "+choices[

                    Dimensions = curDims.ToDictionary(x=>x.DimensionId,x=>x)
                });
                c++;

                now = now.AddDays(r.NextDouble() * 365 * 10);
                var newDims = new List<ScatterDataDimension>();
                for (int dimNum = 0; dimNum < dimMax; dimNum++)
                {
                    newDims.Add(new ScatterDataDimension()
                    {
                        DimensionId="dim-id-"+dimNum,                        
                        Class = curDims[dimNum].Class,
                        Value = Math.Max(min, Math.Min(max, curDims[dimNum].Value + r.NextDouble() * delta))
                    });
                }
                curDims = newDims;
            }

            
            var data = new ScatterData()
            {
                Class = "scatter-data",
                Points = points,
                Width = 200,
                Height = 200,
                Dimensions=dimensions.ToDictionary(x=>x.Id,x=>x),
                InitialXDimension = "dim-id-0",
                InitialYDimension = "dim-id-1",
            };
            return data;*/
        /*}
        
        [Access(AccessLevel.UserOrganization)]
        public JsonResult ReviewScatterTest(long id, long reviewsId,string filters)
        {
            var options=new ChartOptions(){
                ForUserId=id,
                Options=""+reviewsId,
                Filters=filters??"",
                Source=ChartDataSource.Review,
                DimensionIds="category-1,category-2"
            };
            var newScatter=_ChartsEngine.ScatterFromOptions(GetUser(), options);

            return Json(ResultObject.Create(newScatter), JsonRequestBehavior.AllowGet);
        }
        */

        [Access(AccessLevel.UserOrganization)]
        public JsonResult Scatter(long id,long reviewId)
        {
            var chartTuple = _ReviewAccessor.GetChartTuple(GetUser(),reviewId, id);

            var reviewContainer = _ReviewAccessor.GetReviewContainerByReviewId(GetUser(), reviewId);
            var review =_ReviewAccessor.GetReview(GetUser(),reviewId);

            var title = _ChartsEngine.GetChartTitle(GetUser(),id);

            var options = new ChartOptions()
            {
                Id=id,
                ChartName = title,
                DeleteTime = chartTuple.DeleteTime,
                DimensionIds = "category-"+chartTuple.Item1+",category-"+chartTuple.Item2,
                Filters=chartTuple.Filters,
                ForUserId = review.ForUserId,
                GroupBy=chartTuple.Groups,
                Options=""+reviewContainer.Id,
                Source = ChartDataSource.Review,
            };
            var scatter = _ChartsEngine.ScatterFromOptions(GetUser(), options);
            return Json(ResultObject.Create(scatter), JsonRequestBehavior.AllowGet);
        }


        [Access(AccessLevel.UserOrganization)]
        public JsonResult ReviewScatter(long id, long reviewsId)
        {

            var newScatter = _ChartsEngine.ReviewScatter(GetUser(), id, reviewsId);
            return Json(ResultObject.Create(newScatter), JsonRequestBehavior.AllowGet);

            //return null;
            /*var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id);
            var review = _ReviewAccessor.GetAnswersForUserReview(GetUser(), id, reviewsId);
            var history = new List<AnswerModel>();
            if (includeHistory)
            {
                history = _ReviewAccessor.GetAnswersAboutUser(GetUser(), id);
            }

            var completeSliders = review.UnionBy(x => x.Id, history).Where(x => x.Askable.GetQuestionType() == QuestionType.Slider && x.Complete).Cast<SliderAnswer>().ToListAlive();

            var groupedByUsers = completeSliders.GroupBy(x => x.ByUserId);

            var dimensions = completeSliders.Distinct(x => x.Askable.Category.Id).Select(x => new ScatterDimension()
            {
                Max = 100,
                Min = -100,
                Id = "" + x.Askable.Category.Id,
                Name = x.Askable.Category.Category.Translate()
            }).ToList();

            var teamMembers = _TeamAccessor.GetAllTeammembersAssociatedWithUser(GetUser(), id);
            //var teamLookup = teamMembers.Distinct(x => x.TeamId).ToDictionary(x => x.TeamId, x => x.Team);
            
            var scatterDataPoints = new List<ScatterData>();
            var filters = new HashSet<ScatterFilter>(new EqualityComparer<ScatterFilter>((x,y)=>x.Class.Equals(y.Class),x=>x.Class.GetHashCode()));
            var groups  = new HashSet<ScatterGroup>(new EqualityComparer<ScatterGroup>((x,y)=>x.Class.Equals(y.Class),x=>x.Class.GetHashCode()));

            


            foreach (var userAnswers in groupedByUsers) //<UserId>
            {
                var safeUserIdMap = userAnswers.Min(x => x.Id); //We'll use the Min-Answer Id because its unique and not traceable
                
                var byReviewOrdered = userAnswers.GroupBy(x => x.ForReviewContainerId).OrderBy(x => x.Max(y => y.CompleteTime)).ToList();
                long? prevId = null;



                foreach (var userReviewAnswers in byReviewOrdered)
                {
                    //Each review container
                    var userId = userReviewAnswers.First().ByUserId;
                    var reviewContainerId = userReviewAnswers.First().ForReviewContainerId;

                    var datums = new List<ScatterDatum>();

                    foreach (var answer in userReviewAnswers)
                    {
                        var catClass="category-" + answer.Askable.Category.Id;
                        datums.Add(new ScatterDatum()
                        {
                            DimensionId = "" + answer.Askable.Category.Id,
                            Class = String.Join(" ",catClass),
                            Denominator = (double)answer.Askable.Weight,
                            Value = (double)(answer.Percentage.Value * (decimal)answer.Askable.Weight) * 200 - 100,
                        });
                        filters.Add(new ScatterFilter(answer.Askable.Category.Category.Translate(),catClass));
                        groups.Add(new ScatterGroup(answer.Askable.Category.Category.Translate(),catClass));
                    }

                    var teams = teamMembers.Where(x => x.UserId == userId).Distinct(x=>x.TeamId);

                    var teamClasses= new List<String>();

                    foreach(var team in teams){
                        var teamClass="team-"+team.TeamId;

                        filters.Add(new ScatterFilter(team.Team.Name,teamClass));
                        groups.Add(new ScatterGroup(team.Team.Name,teamClass));

                        teamClasses.Add(teamClass);
                    }
                    
                    var aboutClass="about-"+userReviewAnswers.First().AboutType.Invert().ToString().ToLower();
                    var reviewsClass="reviews-"+reviewContainerId;
                    var teamClassesStr= String.Join(" ",teamClasses);
                    
                    //The first answer id should be unique for all this stuff, so lets just use it for convenience
                    var uniqueId = userReviewAnswers.First().Id;
                    scatterDataPoints.Add(new ScatterData()
                    {
                        Class = String.Join(" ", aboutClass, reviewsClass, teamClasses, userClassStr),
                        Date = userReviewAnswers.Max(x => x.CompleteTime).Value,
                        Dimensions = datums.ToDictionary(x => x.DimensionId, x => x),
                        SliceId=reviewContainerId,
                        //PreviousId=prevId,
                        Id=uniqueId
                    });

                    prevId = id;
                }
            }

            var xDimId=dimensions.FirstOrDefault().NotNull(x=>x.Id);
            var yDimId=dimensions.Skip(1).FirstOrDefault().NotNull(x=>x.Id);
            
            var scatter = new ScatterPlot()
            {
                Dimensions=dimensions.ToDictionary(x=>x.Id,x=>x),
                Filters = filters.ToList(),
                Groups = groups.ToList(),
                InitialXDimension =  xDimId,
                InitialYDimension =  yDimId??xDimId,
                Points=scatterDataPoints,
                MinDate = scatterDataPoints.Min(x => x.Date),
                MaxDate = scatterDataPoints.Max(x => x.Date)               

            };


            return Json(null, JsonRequestBehavior.AllowGet);*/
        }


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
                    var catId = m.Askable.Category.Id;
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
                var list = categories.Select(c =>
                {
                    var catId = c.Id;
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
        public FileContentResult ReviewData(long id, long reviewsId)
        {
            var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id);

            var review = _ReviewAccessor.GetAnswersForUserReview(GetUser(), id, reviewsId);
            var completeSliders = review.Where(x => x.Askable.GetQuestionType() == QuestionType.Slider && x.Complete).Cast<SliderAnswer>();

            var titles = categories.Select(x => "" + x.Id).ToList();
            titles.Insert(0, "about");

            var lines = completeSliders.GroupBy(x => x.ByUserId).Select(x => new Merger(x.ToList()).ToCsv(categories)).ToList();
            lines.Insert(0, String.Join(",", titles));


            var csv = String.Join("\n", lines);
            return File(new System.Text.UTF8Encoding().GetBytes(csv), "text/csv", "Report.csv");
        }

        [Access(AccessLevel.UserOrganization)]
        public FileContentResult OrganizationReviewData(long id, long reviewsId)
        {
            _PermissionsAccessor.Permitted(GetUser(), x => x.EditUserOrganization(id));

            var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id);

            var reviewAnswers = _ReviewAccessor.GetReviewContainerAnswers(GetUser(), reviewsId);
            var completedSliders = reviewAnswers.Where(x => x.Askable.GetQuestionType() == QuestionType.Slider && x.Complete).Cast<SliderAnswer>();

            var categoryIds = categories.Select(x => x.Id).ToList();

            StringBuilder sb = new StringBuilder();

            //Header row
            sb.AppendLine("about," + String.Join(",", categoryIds));

            var sbMiddle = new StringBuilder();
            var sbEnd = new StringBuilder();

            foreach (var c in completedSliders.GroupBy(x => x.AboutUserId)) //answers about each user
            {
                var dictionary = new Multimap<long, decimal>();

                foreach (var answer in c.ToList())
                    dictionary.AddNTimes(answer.Askable.Category.Id, answer.Percentage.Value * 200 - 100, (int)answer.Askable.Weight);

                var cols = new String[categoryIds.Count];

                for (int i = 0; i < categoryIds.Count; i++)
                {
                    var datapts = dictionary.Get(categoryIds[i]);
                    var average = 0m;
                    if (datapts.Count > 0)
                        average = datapts.Average();
                    cols[i] = "" + average;
                }
                var row = String.Join(",", cols);

                sb.AppendLine("Employee," + row);

                if (c.First().AboutUser.IsManager())
                    sbMiddle.AppendLine("Management," + row);

                if (c.First().AboutUserId == id)
                    sbEnd.AppendLine("You," + row);
            }
            var managers = sbMiddle.ToString();
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