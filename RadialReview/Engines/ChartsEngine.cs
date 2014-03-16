using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Charts;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Reviews;
using RadialReview.Utilities;
using RadialReview.Utilities.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RadialReview.Engines
{
    public class ChartsEngine
    {
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
        protected static PermissionsAccessor _PermissionsAccessor = new PermissionsAccessor();
        protected static TeamAccessor _TeamAccessor = new TeamAccessor();

        public String GetChartTitle(UserOrganizationModel caller, long chartTupleId)
        {

            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    //Tuple
                    var tuple= s.Get<LongTuple>(chartTupleId);

                    //Filters
                    var filters = ChartClassMatcher.CreateMatchers(tuple.Filters);
                    var filterStrs=new List<String>();
                    foreach(var f in filters)
                    {
                        foreach(var r in f.Requirements)
                        {
                            var split=r.Split('-');
                            switch(split[0].ToLower()){
                                case "team":{
                                    if(split[1]=="*")
                                        filterStrs.Add("Teams");
                                    else
                                        filterStrs.Add("?"+r+"?");
                                    break;
                                }
                                default:{
                                    filterStrs.Add("?"+r+"?");
                                    break;
                                }
                            }
                        }
                    }
                    //Groups
                    var groups = ChartClassMatcher.CreateMatchers(tuple.Groups);
                    foreach(var g in groups)
                    {
                        foreach(var r in g.Requirements)
                        {
                            var split=r.Split('-');
                            switch(split[0].ToLower()){
                                case "about":{
                                    if(split[1]=="*")
                                        filterStrs.Add("Relationship");
                                    else
                                        filterStrs.Add("?"+r+"?");
                                    break;
                                }
                                case "user":
                                    {
                                        filterStrs.Add("User");
                                        break;
                                    }
                                default:{
                                    filterStrs.Add("?"+r+"?");
                                    break;
                                }
                            }
                        }
                    }

                    var cat1=s.Get<QuestionCategoryModel>(tuple.Item1);
                    var cat2=s.Get<QuestionCategoryModel>(tuple.Item2);

                    var filterStr="";

                    if(filterStrs.Count>0)
                    {
                        filterStr=" (By "+String.Join(",",filterStrs)+")";
                    }

                    return String.Format("{0} vs {1}{2}", cat2.Category.Translate(), cat1.Category.Translate(), filterStr);
                }
            }
        }

        public ScatterPlot ScatterFromOptions(UserOrganizationModel caller, ChartOptions options)
        {
            switch (options.Source)
            {
                case ChartDataSource.Review: return ReviewScatterFromOptions(caller, options);
                default: throw new ArgumentException("Unknown ChartDataSource");
            }
        }

        protected ScatterPlot ReviewScatterFromOptions(UserOrganizationModel caller, ChartOptions options)
        {
            var reviewsId = long.Parse(options.Options.Split(',')[0]);
            var unfilteredPlot = ReviewScatter(caller, options.ForUserId, reviewsId);

            var filterPack = options.Filters.Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            var allowableDimensions = options.DimensionIds.Split(',').Select(x => x.Trim().ToLower()).ToList();
            var filteredDimensions = unfilteredPlot.Dimensions.Where(x => allowableDimensions.Any(y => y.Equals(x.Key.Trim().ToLower()))).ToDictionary(x => x.Key, x => x.Value);

            String initialXDim = null;
            if (filteredDimensions.Any(x => x.Key.ToLower().Trim().Equals(unfilteredPlot.InitialXDimension.ToLower().Trim())))
                initialXDim = unfilteredPlot.InitialXDimension;
            String initialYDim = null;
            if (filteredDimensions.Any(x => x.Key.ToLower().Trim().Equals(unfilteredPlot.InitialYDimension.ToLower().Trim())))
                initialYDim = unfilteredPlot.InitialYDimension;

            if (initialXDim == null)
                initialXDim = filteredDimensions.Keys.FirstOrDefault();
            if (initialYDim == null)
                initialYDim = filteredDimensions.Keys.Skip(1).FirstOrDefault() ?? filteredDimensions.Keys.FirstOrDefault();

            var groupMatchers = ChartClassMatcher.CreateMatchers(options.GroupBy);
            var filterMatchers = ChartClassMatcher.CreateMatchers(options.Filters);
            var dimensionFilters=ChartDimensionFilter.Create(options.DimensionIds);

            var filteredPoints = ChartUtility.Filter(unfilteredPlot.Points, filterMatchers, dimensionFilters);

            var minDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
            var maxDate = DateTime.UtcNow.Add(TimeSpan.FromDays(1));

            var groupedPoints = ChartUtility.Group(filteredPoints, groupMatchers);

            //about-* not matching correctly aka fix for *

            if (groupedPoints.Count > 0)
            {
                minDate = groupedPoints.Min(x => x.Date);
                maxDate = groupedPoints.Max(x => x.Date);
            }

            var title = GetChartTitle(caller, options.Id);

            var filteredPlot = new ScatterPlot()
            {
                Class = unfilteredPlot.Class,
                Dimensions = filteredDimensions,
                InitialXDimension = initialXDim,
                InitialYDimension = initialYDim,
                Points = groupedPoints,
                Groups = new List<ScatterGroup>(),//groupMatchers.Select(x=>x.Requirements.ToArray()).ToArray(),
                Filters = new List<ScatterFilter>(),
                MinDate = minDate,
                MaxDate = maxDate,
                OtherData = new { Title= title },
                Legend = unfilteredPlot.Legend
            };

            return filteredPlot;
        }

        public ScatterPlot ReviewScatter(UserOrganizationModel caller, long forUserId, long reviewsId)
        {
            var categories = _OrganizationAccessor.GetOrganizationCategories(caller, caller.Organization.Id);
            var review = _ReviewAccessor.GetAnswersForUserReview(caller, forUserId, reviewsId);
            var history = new List<AnswerModel>();
            if (true)//includeHistory)
            {
                history = _ReviewAccessor.GetAnswersAboutUser(caller, forUserId);
            }

            var completeSliders = review.UnionBy(x => x.Id, history).Where(x => x.Askable.GetQuestionType() == QuestionType.Slider && x.Complete).Cast<SliderAnswer>().ToListAlive();

            var groupedByUsers = completeSliders.GroupBy(x => x.ByUserId);

            var dimensions = completeSliders.Distinct(x => x.Askable.Category.Id).Select(x => new ScatterDimension()
            {
                Max = 100,
                Min = -100,
                Id = "category-" + x.Askable.Category.Id,
                Name = x.Askable.Category.Category.Translate()
            }).ToList();

            var teamMembers = _TeamAccessor.GetAllTeammembersAssociatedWithUser(caller, forUserId);
            //var teamLookup = teamMembers.Distinct(x => x.TeamId).ToDictionary(x => x.TeamId, x => x.Team);

            var scatterDataPoints = new List<ScatterData>();
            var filters = new HashSet<ScatterFilter>(new EqualityComparer<ScatterFilter>((x, y) => x.Class.Equals(y.Class), x => x.Class.GetHashCode()));
            var groups = new HashSet<ScatterGroup>(new EqualityComparer<ScatterGroup>((x, y) => x.Class.Equals(y.Class), x => x.Class.GetHashCode()));

            var legend = new HashSet<ScatterLegendItem>(new EqualityComparer<ScatterLegendItem>((x,y)=>x.Class.Equals(y.Class),x=>x.Class.GetHashCode()));
            
            foreach (var userAnswers in groupedByUsers) //<UserId>
            {
                var byReviewOrdered = userAnswers.GroupBy(x => x.ForReviewContainerId).OrderBy(x => x.Max(y => y.CompleteTime)).ToList();
                long? prevId = null;
                var safeUserIdMap = userAnswers.Min(x => x.Id); //We'll use the Min-Answer Id because its unique and not traceable


                foreach (var userReviewAnswers in byReviewOrdered)
                {
                    //Each review container
                    var userId = userReviewAnswers.First().ByUserId;
                    var reviewContainerId = userReviewAnswers.First().ForReviewContainerId;

                    var datums = new Dictionary<String,ScatterDatum>();

                    foreach (var answer in userReviewAnswers)
                    {
                        var catClass = "category-" + answer.Askable.Category.Id;
                        if (!datums.ContainsKey(catClass))
                        {
                            datums[catClass]=new ScatterDatum(){
                                DimensionId = "category-" + answer.Askable.Category.Id,
                                Class = String.Join(" ", catClass),
                            };
                        }

                        datums[catClass].Denominator += (double)answer.Askable.Weight;
                        datums[catClass].Value += ((double)(answer.Percentage.Value ) * 200 - 100)* (double)answer.Askable.Weight;

                        
                        //filters.Add(new ScatterFilter(answer.Askable.Category.Category.Translate(), catClass));
                        groups.Add(new ScatterGroup(answer.Askable.Category.Category.Translate(), catClass));
                    }

                    var teams = teamMembers.Where(x => x.UserId == userId).Distinct(x => x.TeamId);

                    var teamClasses = new List<String>();

                    foreach (var team in teams)
                    {
                        var teamClass = "team-" + team.TeamId;

                        filters.Add(new ScatterFilter(team.Team.Name, teamClass));
                        groups.Add(new ScatterGroup(team.Team.Name, teamClass));

                        teamClasses.Add(teamClass);
                    }

                    var aboutType=userReviewAnswers.First().AboutType.Invert();
                    var aboutClass = "about-" + aboutType;
                                        
                    legend.Add(new ScatterLegendItem(aboutType.ToString(),aboutClass));
                    
                    var reviewsClass = "reviews-" + reviewContainerId;
                    var teamClassesStr = String.Join(" ", teamClasses);
                    var userClassStr = "user-" + safeUserIdMap;

                    //The first answer id should be unique for all this stuff, so lets just use it for convenience
                    var uniqueId = userReviewAnswers.First().Id;
                    scatterDataPoints.Add(new ScatterData()
                    {
                        Class = String.Join(" ", aboutClass, reviewsClass, teamClassesStr, userClassStr),
                        Date = userReviewAnswers.Max(x => x.CompleteTime)??new DateTime(2014,1,1),
                        Dimensions = datums,
                        SliceId=reviewContainerId,
                        //PreviousId = prevId,
                        Id = uniqueId
                    });

                    prevId = uniqueId;
                }
            }

            var xDimId = dimensions.FirstOrDefault().NotNull(x => x.Id);
            var yDimId = dimensions.Skip(1).FirstOrDefault().NotNull(x => x.Id);

            var dates=scatterDataPoints.Select(x=>x.Date).ToList();
            if (!dates.Any())
                dates.Add(DateTime.UtcNow);

            var scatter = new ScatterPlot()
            {
                Dimensions = dimensions.ToDictionary(x => x.Id, x => x),
                Filters = filters.ToList(),
                Groups = groups.ToList(),
                InitialXDimension = xDimId,
                InitialYDimension = yDimId ?? xDimId,
                Points = scatterDataPoints,
                MinDate = dates.Min(),
                MaxDate = dates.Max(),
                Legend=legend.ToList(),
            };

            return scatter;
        }

    }
}