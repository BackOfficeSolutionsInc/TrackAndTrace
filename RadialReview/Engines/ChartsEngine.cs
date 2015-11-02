using Amazon.ElasticTranscoder.Model;
using Amazon.IdentityManagement.Model;
using FluentNHibernate.Utils;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Mapping;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Charts;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Chart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using RadialReview.Utilities.DataTypes;
using System.Security.Cryptography;
using TimeSpan = System.TimeSpan;

namespace RadialReview.Engines
{
	public class ChartsEngine
	{
		protected static DeepSubordianteAccessor _DeepSubordianteAccessor = new DeepSubordianteAccessor();
		protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
		protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
		protected static PermissionsAccessor _PermissionsAccessor = new PermissionsAccessor();
		protected static ScorecardAccessor _ScorecardAccessor = new ScorecardAccessor();
		protected static TeamAccessor _TeamAccessor = new TeamAccessor();
		protected static UserAccessor _UserAccessor = new UserAccessor();

		public String GetChartTitle(UserOrganizationModel caller, long chartTupleId)
		{

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Tuple
					var tuple = s.Get<LongTuple>(chartTupleId);

					if (tuple.Title != null)
						return tuple.Title;

					//Filters
					var filters = ChartClassMatcher.CreateMatchers(tuple.Filters);
					var filterStrs = new List<String>();
					foreach (var f in filters)
					{
						foreach (var r in f.Requirements)
						{
							var split = r.Split('-');
							switch (split[0].ToLower())
							{
								case "team":
									{
										if (split[1] == "*")
											filterStrs.Add("Teams");
										else
											filterStrs.Add("?" + r + "?");
										break;
									}
								case "reviews":
									{
										try
										{
											filterStrs.Add(s.Get<ReviewsModel>(split[1].ToLong()).ReviewName);
										}
										catch
										{
											filterStrs.Add("?" + r + "?");
										}
									} break;
								default:
									{
										filterStrs.Add("?" + r + "?");
										break;
									}
							}
						}
					}
					var groupsStrs = new List<String>();

					//Groups
					var groups = ChartClassMatcher.CreateMatchers(tuple.Groups);
					if (!groups.Any())
					{
						groupsStrs.Add("Review");
					}

					foreach (var g in groups)
					{
						foreach (var r in g.Requirements)
						{
							var split = r.Split('-');
							switch (split[0].ToLower())
							{
								case "about":
									{
										if (split[1] == "*")
											groupsStrs.Add("Relationship");
										else
											groupsStrs.Add("?" + r + "?");
										break;
									}
								case "user":
									{
										groupsStrs.Add("User");
										break;
									}
								default:
									{
										filterStrs.Add("?" + r + "?");
										break;
									}
							}
						}
					}

					var cat1 = s.Get<QuestionCategoryModel>(tuple.Item1);
					var cat2 = s.Get<QuestionCategoryModel>(tuple.Item2);

					var description = "";

					if (filterStrs.Count > 0)
					{
						description += " (" + String.Join(",", filterStrs) + ")";
					}
					if (groupsStrs.Count > 0)
					{
						description += " By " + String.Join(",", groupsStrs);
					}


					return String.Format("{0} vs {1}{2}", cat2.NotNull(x => x.Category.Translate()) ?? "?", cat1.NotNull(x => x.Category.Translate()) ?? "?", description);
				}
			}
		}

		public ScatterPlot ScatterFromOptions(UserOrganizationModel caller, ChartOptions options, bool sensitive)
		{
			switch (options.Source)
			{
				case ChartDataSource.Review: return ReviewScatterFromOptions(caller, options, sensitive);
				default: throw new ArgumentException("Unknown ChartDataSource");
			}
		}

		protected ScatterPlot ReviewScatterFromOptions(UserOrganizationModel caller, ChartOptions options, bool sensitive)
		{
			var reviewsId = long.Parse(options.Options.Split(',')[0]);
			var unfilteredPlot = ReviewScatter(caller, options.ForUserId, reviewsId, sensitive);

			var filterPack = (options.Filters ?? "").Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries);

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
			var dimensionFilters = ChartDimensionFilter.Create(options.DimensionIds);

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

			var legendType = "";
			if (options.GroupBy == "")
			{
				legendType = "Review";
			}
			else
			{
				legendType = "All";
			}

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
				OtherData = new { Title = title },
				Legend = unfilteredPlot.Legend,
				LegendType = legendType
			};

			return filteredPlot;
		}
		


		public Scatter AggregateReviewScatter(UserOrganizationModel caller, long reviewsId, bool admin)
		{
			long orgId;
			ReviewsModel reviewContainer;
			List<FastReviewQueries.UserReviewRoleValues> roleValues;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					roleValues	= FastReviewQueries.GetAllRoleValues(s, reviewsId);
					reviewContainer = s.Get<ReviewsModel>(reviewsId);
					orgId = reviewContainer.ForOrganizationId; 
					tx.Commit();
					s.Flush();
				}
			}

			//var allAnswers = _ReviewAccessor.GetReviewContainerAnswers(caller, reviewsId);
			//var reviewContainer = _ReviewAccessor.GetReviewContainer(caller, reviewsId, false, false, false);

			QuestionCategoryModel companyValuesCategory;
			QuestionCategoryModel rolesCategory;


			var teammemberLookup = new Multimap<long, OrganizationTeamModel>();
			Dictionary<long, OrganizationTeamModel> teamLookup = null;
			var teamMembers = _TeamAccessor.GetTeamMembersAtOrganization(caller, orgId);
			if (admin){
				_PermissionsAccessor.Permitted(caller, x => x.ManagingOrganization(caller.Organization.Id).Or(y=>y.EditReviewContainer(reviewsId)));
			}else{
				var subordinateIds =_DeepSubordianteAccessor.GetSubordinatesAndSelf(caller, caller.Id);
				teamMembers=teamMembers.Where(x => subordinateIds.Contains(x.UserId)).ToList();
			}

			var managingTeamIds = new List<long>();

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//companyValuesCategory = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.COMPANY_VALUES);
					//rolesCategory = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.ROLES);

					var teams = s.QueryOver<OrganizationTeamModel>().Where(x => x.DeleteTime == null && x.Organization.Id == orgId).List().ToList();
					teamLookup = teams.ToDictionary(x => x.Id, x => x);

					managingTeamIds.AddRange(teams.Where(x => x.ManagedBy == caller.Id).Select(t => t.Id));

					foreach (var t in teamMembers)
					{
						var teamId = t.TeamId;
						var userId = t.UserId;
						if (teamLookup.ContainsKey(teamId))
						{
							var team = teamLookup[teamId];
							teammemberLookup.Add(userId, team);
						}
					}
				}
			}

			var subgroupLookup = new DefaultDictionary<long, Checktree.Subtree>(teamId => new Checktree.Subtree()
			{
				id = "team_" + teamId,
				title = teamLookup[teamId].GetName()
			});

			var xAxis = "Company Values";
			var yAxis = "Roles";

			/*var reportLookup = new DefaultDictionary<long, long?>(x => null);
			foreach (var person in allAnswers.GroupBy(x => x.ByUserId)){
				reportLookup[person.Key] = person.First().ForReviewId;
			}*/

			var userLookup = teamMembers.Distinct(x => x.UserId).ToDictionary(x => x.UserId,x=>x.User);
			var data = new List<Scatter.ScatterPoint>();

			foreach (var person in roleValues.Where(x => teamMembers.Any(y => y.UserId == x.UserId)))
			{
				var agg = Aggregate(person, reviewsId).ToList();
				//Should be either 1 or none.
				var person1 = person;
				agg.ForEach(p =>
				{
					var user = userLookup[person1.UserId];
					p.imageUrl = user.ImageUrl(true);
					p.subtitle = user.GetTitles();
					p.title = user.GetName();
					p.xAxis = xAxis;
					p.yAxis = yAxis;
					p.@class = "user_" + user.Id;
					p.id = "user_" + user.Id;
					p.link = "/Reports/Details/" + reviewsId + "?userId=" + person1.UserId;

					var teams = teammemberLookup.Get(user.Id);
					foreach (var team in teams)
					{
						subgroupLookup[team.Id].subgroups.Add(new Checktree.Subtree()
						{
							id = "user_" + user.Id,
							title = user.GetName(),
						});
					}
				});


				data.AddRange(agg);


			}

			/*
			foreach (var person in allAnswers.GroupBy(x => x.AboutUserId).Where(x=>teamMembers.Any(y=>y.UserId==x.Key)))
			{
				var agg = Aggregate(person, reviewsId).ToList();
				//Should be either 1 or none.
				agg.ForEach(p =>{
					var user = person.First().AboutUser;
					p.imageUrl = user.ImageUrl(true);
					p.subtitle = user.GetTitles();
					p.title = user.GetName();
					p.xAxis = xAxis;
					p.yAxis = yAxis;
					p.@class = "user_" + user.Id;
					p.id = "user_" + user.Id;
					p.link = "/Review/Details/" + reportLookup[person.First().AboutUserId];
					
					var teams = teammemberLookup.Get(user.Id);
					foreach (var team in teams){
						subgroupLookup[team.Id].subgroups.Add(new Checktree.Subtree(){
							id = "user_" + user.Id,
							title = user.GetName()
						});
					}
				});

				
				data.AddRange(agg);
			}*/

			foreach (var tId in managingTeamIds){
				if (subgroupLookup.Backing.ContainsKey(tId)){
					subgroupLookup[tId].hidden = false;
				}
			}

			//var scatterDataPoints = new List<ScatterData>();
			//var groups = new HashSet<ScatterGroup>(new EqualityComparer<ScatterGroup>((x, y) => x.Class.Equals(y.Class), x => x.Class.GetHashCode()));
			//var remapper = RandomUtility.CreateRemapper();
			//var remapperUser = RandomUtility.CreateRemapper();
			//foreach (var revieweeAnswers in allAnswers.GroupBy(x => x.AboutUserId)){
			//	var datums = new Dictionary<string, ScatterDatum>();
			//	var scorer = new ScatterScorer(datums, groups, companyValuesCategory, rolesCategory);
			//	foreach(var answer in revieweeAnswers)
			//		scorer.Add(answer);

			//	var title = "<span class='aboutType hoverTitle'></span> <span class='reviewName hoverTitle'>" + revieweeAnswers.First().AboutUser.GetName()+ "</span>";

			//	var uniqueId = remapper.Remap(revieweeAnswers.First().Id);

			//	var safeUserIdMap = remapperUser.Remap(revieweeAnswers.Min(x => x.Id)); //We'll use the Min-Answer Id because its unique and not traceable
			//	var userClassStr = "user-" + safeUserIdMap;
			//	var point = new ScatterData(){
			//		Class = "userDataPoint " + userClassStr,
			//		Date = revieweeAnswers.Max(x => x.CompleteTime) ?? new DateTime(2014, 1, 1),
			//		Dimensions = datums,
			//		SliceId = reviewsId,
			//		Title = title,
			//		Subtext = "",
			//		Id = uniqueId,
			//	};
			//	point.OtherData.AboutUser = revieweeAnswers.First().AboutUser;
			//	scatterDataPoints.Add(point);
			//}
			//var dimensions = new List<ScatterDimension>();

			//dimensions.Insert(0, new ScatterDimension() { Max = 100, Min = -100, Id = "category-" + companyValuesCategory.Id, Name = "Company Values" });
			//dimensions.Insert(1, new ScatterDimension() { Max = 100, Min = -100, Id = "category-" + rolesCategory.Id, Name = "Roles" });

			//var xDimId = dimensions.FirstOrDefault().NotNull(x => x.Id);
			//var yDimId = dimensions.Skip(1).FirstOrDefault().NotNull(x => x.Id);

			//var data = new List<Scatter.ScatterPoint>();


			//var xAxis = dimensions.FirstOrDefault(x => x.Id == xDimId).NotNull(x => x.Name);
			//var yAxis = dimensions.FirstOrDefault(x => x.Id == yDimId).NotNull(x => x.Name);


			//foreach (var pt in scatterDataPoints){

			//	var xVal = 0m;
			//	var yVal = 0m;

			//	var zeroDen = 0;

			//	if (pt.Dimensions.ContainsKey(xDimId)){
			//		var dx = pt.Dimensions[xDimId];
			//		xVal = dx.Denominator == 0.0 ? 0m : (decimal) (dx.Value/dx.Denominator);
			//		if (dx.Denominator == 0)
			//			zeroDen++;
			//	}else{
			//		zeroDen++;
			//	}
			//	if (pt.Dimensions.ContainsKey(yDimId)){
			//		var dy = pt.Dimensions[yDimId];
			//		yVal = dy.Denominator == 0.0 ? 0m : (decimal)(dy.Value / dy.Denominator);
			//		if (dy.Denominator == 0)
			//			zeroDen++;
			//	}else{
			//		zeroDen++;
			//	}

			//	if (zeroDen != 2){
			//		var user = ((UserOrganizationModel) pt.OtherData.AboutUser);
			//		data.Add(new Scatter.ScatterPoint(){
			//			cx = xVal,
			//			cy = yVal,
			//			date = pt.Date,
			//			imageUrl = user.ImageUrl(true),
			//			subtitle = pt.Date.ToShortDateString(),
			//			title = user.GetName(),
			//			xAxis = xAxis,
			//			yAxis = yAxis,
			//			@class = "user_"+user.Id,
			//			id = "user_" + user.Id,
			//		});
			//		var teams = teammemberLookup.Get(user.Id);
			//		foreach (var team in teams){
			//			subgroupLookup[team.Id].subgroups.Add(new Checktree.Subtree(){
			//				id = "user_" + user.Id,
			//				title = user.GetName()
			//			});
			//		}
			//	}
			//}

			var checktree = new Checktree();
			checktree.Data.title = reviewContainer.ReviewName;

			foreach (var s in subgroupLookup)
			{
				checktree.Data.subgroups.Add(s.Value);
			}

			var valueCoef = 3;
			var roleCoef = 1;

			var ordered = data.OrderByDescending(x =>
			{
				var xx = (x.cx + 100);
				var yy = (x.cy + 100);
				return valueCoef * xx * xx + roleCoef * yy * yy;
			}).ToList();

			var top = ordered.Take(10).Where(x => x.cx > 0 && x.cy > 0).ToList();

			checktree.Data.subgroups.Insert(0, new Checktree.Subtree()
			{
				id = "team-top",
				title = "Top Performers",
				hidden = !admin,
				subgroups = top.Select(x => new Checktree.Subtree()
				{
					id = x.id,
					title = x.title
				}).ToList()
			});

			var remaining = ordered.Where(x => top.All(y => y.id != x.id)).ToList();

			checktree.Data.subgroups.Insert(1, new Checktree.Subtree()
			{
				id = "team-bottom",
				title = "At Risk",
				hidden = !admin,
				subgroups = Enumerable.Reverse(remaining).Take(10).Select(x => new Checktree.Subtree()
				{
					id = x.id,
					title = x.title
				}).ToList()
			});

			checktree.Data.subgroups.Add(new Checktree.Subtree()
			{
				id = "team-all",
				title = "Everyone",
				subgroups = ordered.Select(x => new Checktree.Subtree()
				{
					id = x.id,
					title = x.title
				}).ToList()
			});


			return new Scatter()
			{
				Points = data,
				xAxis = xAxis,
				yAxis = yAxis,
				title = "Aggregate Results",
				FilterTree = checktree,
				OrderedPoints = ordered,
			};
		}

		private static IEnumerable<Scatter.ScatterPoint> Aggregate(IEnumerable<AnswerModel> reviewAnswers, long reviewsId,string encryptionKey)
		{
			var lookup = new DefaultDictionary<string, Ratio>(x => new Ratio());
			foreach (var a in reviewAnswers)
			{
				Ratio ratio;
				String category;
				if (ScatterScorer.ScoreFunction(a, out ratio, out category))
					lookup[category].Merge(ratio);
			}

			if (lookup.Backing.ContainsKey("Roles") || lookup.Backing.ContainsKey("Values"))
			{
				var o = new Scatter.ScatterPoint()
				{
					@class = "review-" + reviewsId,
					cx = ScatterScorer.ShiftRatio(lookup["Values"]),
					cy = ScatterScorer.ShiftRatio(lookup["Roles"]),
					xAxis = "Values",
					yAxis = "Roles",
					id = Hash(encryptionKey, "Review")
				};
				yield return o;
			}
		}

		private static IEnumerable<Scatter.ScatterPoint> Aggregate(FastReviewQueries.UserReviewRoleValues roleValues, long reviewsId)
		{
			Ratio roles;
			Ratio values;

			if (ScatterScorer.ScoreFunction(roleValues, out roles, out values)){

				var o = new Scatter.ScatterPoint()
				{
					@class = "review-" + reviewsId,
					cx = ScatterScorer.ShiftRatio(values),
					cy = ScatterScorer.ShiftRatio(roles),
					xAxis = "Values",
					yAxis = "Roles"
				};
				yield return o;
			}
		}

		public List<Scatter.ScatterPoint> GenerateLegend()
		{
			var legend = Enum.GetValues(typeof(AboutType))
				.Cast<AboutType>()
				.Select(aboutType => new Scatter.ScatterPoint()
				{
					@class = "about-" + aboutType + " point " + aboutType.GetBestShape(),
					title = aboutType.GetBestTitle()
				}).Reverse()
				.ToList();
			return legend;
		}
		/*
		public List<> GenerateAnimosity(UserOrganizationModel caller, long reviewsId)
		{
			using(var s = HibernateSession.GetCurrentSession())
			{
				using(var tx=s.BeginTransaction())
				{
					
				}
			}
		}*/

		public List<Scatter> ReviewScatterFromTo(UserOrganizationModel caller, long forUserId, DateTime startTime,DateTime endTime, string groupBy, bool sensitive)
		{
			List<ReviewsModel> foundReviews;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					foundReviews = s.QueryOver<ReviewsModel>().Where(x => x.DateCreated <= endTime && startTime <= x.DueDate).List().ToList();
				}
			}

			var output=new List<Scatter>();

			foreach (var r in foundReviews){
				try{
					var scatter=ReviewScatter2(caller, forUserId, r.Id, groupBy, sensitive, true);
					output.Add(scatter);
				}
				catch (Exception){
					//Just don't add it..
				}
			}
			return output;
		}

		public Scatter ReviewScatter2(UserOrganizationModel caller, long forUserId, long reviewsId, string groupBy, bool sensitive,bool includePrevious)
		{
			
			if (sensitive){
				new PermissionsAccessor().Permitted(caller, x => x.ManagesUserOrganization(forUserId, true));
			}
			else{
				using (var s = HibernateSession.GetCurrentSession())
				{
					using (var tx = s.BeginTransaction())
					{
						var p=PermissionsUtility.Create(s, caller);//.ManagesUserOrganization(forUserId, false);
						//p.Permitted(caller, x => x.ManagesUserOrganization(forUserId, false));
						var review = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewsId && x.ForUserId == forUserId).Take(1).SingleOrDefault();
						var managingOrg = p.IsPermitted(x => x.ManagingOrganization(review.ForReviewContainer.ForOrganizationId));
						if (forUserId == caller.Id && ((!review.ClientReview.Visible && !managingOrg) || !review.ClientReview.IncludeScatterChart))
							throw new PermissionsException();
						if (forUserId != caller.Id && !managingOrg)
							p.ManagesUserOrganization(forUserId, false);
						
						includePrevious = review.ClientReview.ScatterChart.IncludePrevious;
						groupBy = review.ClientReview.ScatterChart.Groups;
					}
				}
			}
			long? previousReview=null;
			if (includePrevious){
				using (var s = HibernateSession.GetCurrentSession()){
					using (var tx = s.BeginTransaction())
					{
						var review = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForUserId == forUserId && x.ForReviewsId<reviewsId)
							.OrderBy(x=>x.DueDate).Desc
							.Take(1).SingleOrDefault();
						previousReview = review.NotNull(x => x.ForReviewsId);

					}
				}
			}
			var reviewAnswers = _ReviewAccessor.GetAnswersForUserReview(caller, forUserId, reviewsId);



			List<Scatter.ScatterPoint> points;
			String title = null;
			var legend = GenerateLegend();

			string groupByStandard = null;

			var encryptKey = Guid.NewGuid().ToString();

			points = GenScatterPoints(reviewsId, groupBy, sensitive, reviewAnswers,encryptKey, ref title, ref groupByStandard);

			if (previousReview != null){
				string temp = null;
				var previousReviewAnswers = _ReviewAccessor.GetAnswersForUserReview(caller, forUserId, previousReview.Value);
				var previous = GenScatterPoints(previousReview.Value, groupBy, sensitive, previousReviewAnswers, encryptKey, ref temp, ref temp);
				points.ForEach(x => {
					var p = previous.FirstOrDefault(y => y.id == x.id);
					if (p != null){
						x.ox = p.cx;
						x.oy = p.cy;
					}
				});
			}

			var pointsNoTeammates = points.Where(x => !x.@class.Contains("about-" + AboutType.Teammate)).ToList();
			var legendNoTeammates = legend.Where(x => !x.@class.Contains("about-" + AboutType.Teammate)).ToList();


			var scatter = new Scatter()
			{
				Points = pointsNoTeammates,
				Legend = legendNoTeammates,
				title  = title,
				xAxis  = "Company Values",
				yAxis  = "Roles",
				xMin   = -100,
				xMax   = 100,
				yMin   = -100,
				yMax   = 100,
				groupBy = groupByStandard

			};
			return scatter;
		}

		private static string Hash(string privateKey, string encrypt)
		{
			SHA256 shaM = new SHA256Managed();
			return shaM.ComputeHash((privateKey+encrypt).GetBytes()).GetString();
		}

		private static List<Scatter.ScatterPoint> GenScatterPoints(long reviewsId, string groupBy, bool sensitive, List<AnswerModel> reviewAnswers,string encryptionKey, ref string title, ref string groupByStandard)
		{
			List<Scatter.ScatterPoint> points;
			switch(groupBy){
				case "about-*":{
					//lookup[AboutType][Category]=score
					var lookup = new DefaultDictionary<String, DefaultDictionary<string, Ratio>>(x => new DefaultDictionary<string, Ratio>(y => new Ratio()));
					var bestType = new DefaultDictionary<string, string>(x => "");

					foreach (var flag in Enum.GetNames(typeof (AboutType))){
						var aboutType = ((AboutType) Enum.Parse(typeof (AboutType), flag));
						foreach (var a in reviewAnswers){
							var aboutTypes = a.AboutType.Invert();
							if (aboutTypes != AboutType.NoRelationship && aboutType == AboutType.NoRelationship)
								continue;

							if (!aboutTypes.HasFlag(aboutType))
								continue;

							Ratio ratio;
							String category;
							if (ScatterScorer.ScoreFunction(a, out ratio, out category)){
								var fmod=flag.Replace("Teammate", "NoRelationship");
								lookup[fmod][category].Merge(ratio);
							}
						}
						bestType[flag] = aboutType.GetBestShape();
					}
					points = lookup.SelectMany(x =>{
						var cx = x.Value["Values"];
						var cy = x.Value["Roles"];
						if (!cx.IsValid() && !cy.IsValid())
							return new List<Scatter.ScatterPoint>();

						return new Scatter.ScatterPoint(){
							@class = "about-" + x.Key + " " + ((AboutType) Enum.Parse(typeof (AboutType), x.Key)).GetBestShape(),
							cx = ScatterScorer.ShiftRatio(cx),
							cy = ScatterScorer.ShiftRatio(cy),
							xAxis = "Values",
							yAxis = "Roles",
							id= Hash(encryptionKey,x.Key)
						}.AsList();
					}).ToList();
					title = "Evaluations grouped by Relationship";
					groupByStandard = "about-*";
				}
					break;
				case "user-*":{
					points = reviewAnswers.GroupBy(x => x.ByUserId).Select(answers =>{
						//lookup[user][Category]=score
						var lookup = new DefaultDictionary<string, Ratio>(x => new Ratio());
						var aboutType = AboutType.NoRelationship;

						foreach (var a in answers){
							Ratio ratio;
							String category;
							if (ScatterScorer.ScoreFunction(a, out ratio, out category))
								lookup[category].Merge(ratio);
							aboutType = aboutType | a.AboutType.Invert();
						}
						aboutType = aboutType.GetBestAboutType();

						var remapperUser = RandomUtility.CreateRemapper();
						if (!lookup.Backing.ContainsKey("Roles") && !lookup.Backing.ContainsKey("Values"))
							return null;

						var o = new Scatter.ScatterPoint(){
							@class = "user-" + remapperUser.Remap(answers.First().ByUserId) + " about-" + aboutType + " " + aboutType.GetBestShape(),
							cx = ScatterScorer.ShiftRatio(lookup["Values"]),
							cy = ScatterScorer.ShiftRatio(lookup["Roles"]),
							xAxis = "Values",
							yAxis = "Roles",
							id = Hash(encryptionKey, answers.First().ByUserId.ToString())
						};
						if (sensitive){
							var u = answers.First().ByUser;
							o.imageUrl = u.ImageUrl(true);
							o.title = u.GetName();
							o.subtitle = u.GetTitles();
						}
						return o;
					}).Where(x => x != null).ToList();
					title = "Evaluations grouped by User";
					groupByStandard = "user-*";
				}
					break;
				case "undefined":
					goto case "review-*";
				case "":
					goto case "review-*";
				case null:
					goto case "review-*";
				case "review-*":{
					points = Aggregate(reviewAnswers, reviewsId,encryptionKey).ToList();
					title = "Aggregate Evaluation";
					groupByStandard = "review-*";
				}
					break;
				default:
					throw new PermissionsException("Unrecognized group");
			}
			return points;
		}

		public ScatterPlot ReviewScatter(UserOrganizationModel caller, long forUserId, long reviewsId, bool sensitive)
		{
			if (sensitive)
			{
				new PermissionsAccessor().Permitted(caller, x => x.ManagesUserOrganization(forUserId, true));
			}

			var categories = _OrganizationAccessor.GetOrganizationCategories(caller, caller.Organization.Id);
			var review = _ReviewAccessor.GetAnswersForUserReview(caller, forUserId, reviewsId);
			var history = new List<AnswerModel>();
			if (true)//includeHistory)
			{
				history = _ReviewAccessor.GetAnswersAboutUser(caller, forUserId);
			}

			var completeSliders = review.UnionBy(x => x.Id, history).Where(x => /*x.Askable.GetQuestionType() == QuestionType.Slider &&*/ x.Complete)/*.Cast<SliderAnswer>()*/.ToListAlive();
			var groupedByUsers = completeSliders.GroupBy(x => x.ByUserId);

			/*var dimensions = completeSliders.Distinct(x => x.Askable.Category.Id).Select(x => new ScatterDimension()
			{
				Max = 100,
				Min = -100,
				Id = "category-" + x.Askable.Category.Id,
				Name = x.Askable.Category.Category.Translate()
			}).ToList();*/

			QuestionCategoryModel companyValuesCategory;
			QuestionCategoryModel rolesCategory;

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					companyValuesCategory = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.COMPANY_VALUES);
					rolesCategory = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.ROLES);
				}
			}


			var teamMembers = _TeamAccessor.GetAllTeammembersAssociatedWithUser(caller, forUserId);
			//var teamLookup = teamMembers.Distinct(x => x.TeamId).ToDictionary(x => x.TeamId, x => x.Team);

			var scatterDataPoints = new List<ScatterData>();
			var filters = new HashSet<ScatterFilter>(new EqualityComparer<ScatterFilter>((x, y) => x.Class.Equals(y.Class), x => x.Class.GetHashCode()));
			var groups = new HashSet<ScatterGroup>(new EqualityComparer<ScatterGroup>((x, y) => x.Class.Equals(y.Class), x => x.Class.GetHashCode()));

			var legend = new HashSet<ScatterLegendItem>(new EqualityComparer<ScatterLegendItem>((x, y) => x.Class.Equals(y.Class), x => x.Class.GetHashCode()));

			var remapper = RandomUtility.CreateRemapper();
			var remapperUser = RandomUtility.CreateRemapper();




			foreach (var userAnswers in groupedByUsers) //<UserId>
			{
				var byReviewOrdered = userAnswers.GroupBy(x => x.ForReviewContainerId).OrderBy(x => x.Max(y => y.CompleteTime)).ToList();
				long? prevId = null;
				var safeUserIdMap = remapperUser.Remap(userAnswers.Min(x => x.Id)); //We'll use the Min-Answer Id because its unique and not traceable


				foreach (var userReviewAnswers in byReviewOrdered)
				{
					//Each review container
					var userId = userReviewAnswers.First().ByUserId;
					var reviewContainerId = userReviewAnswers.First().ForReviewContainerId;
					var reviewContainer = _ReviewAccessor.GetReviewContainer(caller, reviewContainerId, false, false, false);

					// userReviewAnswers.First().ForReviewContainer;
					var datums = new Dictionary<String, ScatterDatum>();
					var scorer = new ScatterScorer(datums, groups, companyValuesCategory, rolesCategory);
					foreach (var answer in userReviewAnswers)
					{
						//Heavy lifting
						scorer.Add(answer);
					}

					filters.Add(new ScatterFilter(reviewContainer.ReviewName, "reviews-" + reviewContainerId, on: reviewsId == reviewContainerId));

					var teams = teamMembers.Where(x => x.UserId == userId).Distinct(x => x.TeamId);
					var teamClasses = new List<String>();

					foreach (var team in teams)
					{
						var teamClass = "team-" + team.TeamId;

						filters.Add(new ScatterFilter(team.Team.Name, teamClass));
						groups.Add(new ScatterGroup(team.Team.Name, teamClass));

						teamClasses.Add(teamClass);
					}

					var aboutTypes = userReviewAnswers.First().AboutType.Invert();
					foreach (AboutType aboutType in aboutTypes.GetFlags())
					{
						if (aboutTypes != AboutType.NoRelationship && aboutType == AboutType.NoRelationship)
							continue;

						var aboutClass = "about-" + aboutType;

						legend.Add(new ScatterLegendItem(aboutType.ToString(), aboutClass));

						var reviewsClass = "reviews-" + reviewContainerId;
						var teamClassesStr = String.Join(" ", teamClasses);
						var userClassStr = "user-" + safeUserIdMap;

						//The first answer id should be unique for all this stuff, so lets just use it for convenience
						var uniqueId = remapper.Remap(userReviewAnswers.First().Id);

						String title, subtext = "";
						if (sensitive)
						{
							var user = userReviewAnswers.First().ByUser;
							title = "<span class='nameAndTitle hoverTitle title'>" + user.GetNameAndTitle() + "</span> <span class='aboutType hoverTitle title'>" + aboutType + "</span> <span class='reviewName hoverTitle title'>" + reviewContainer.ReviewName + "</span>";
						}
						else
						{
							title = "<span class='aboutType hoverTitle title'>" + aboutType.ToString() + "</span> <span class='reviewName hoverTitle title'>" + reviewContainer.ReviewName + "</span>";
						}

						scatterDataPoints.Add(new ScatterData()
						{
							Class = String.Join(" ", aboutClass, reviewsClass, teamClassesStr, userClassStr),
							Date = userReviewAnswers.Max(x => x.CompleteTime) ?? new DateTime(2014, 1, 1),
							Dimensions = datums,
							SliceId = reviewContainerId,
							//PreviousId = prevId,
							Title = title,
							Subtext = subtext,
							Id = uniqueId
						});

						prevId = uniqueId;
					}
				}
			}
			var dimensions = completeSliders.Distinct(x => x.Askable.Category.Id).Select(x => new ScatterDimension()
			{
				Max = 100,
				Min = -100,
				Id = "category-" + x.Askable.Category.Id,
				Name = x.Askable.Category.Category.Translate()
			}).ToList();

			dimensions.Insert(0, new ScatterDimension() { Max = 100, Min = -100, Id = "category-" + rolesCategory.Id, Name = "Roles" });
			dimensions.Insert(0, new ScatterDimension() { Max = 100, Min = -100, Id = "category-" + companyValuesCategory.Id, Name = "Values" });

			var xDimId = dimensions.FirstOrDefault().NotNull(x => x.Id);
			var yDimId = dimensions.Skip(1).FirstOrDefault().NotNull(x => x.Id);

			var dates = scatterDataPoints.Select(x => x.Date).ToList();
			if (!dates.Any())
				dates.Add(DateTime.UtcNow);

			var scatter = new ScatterPlot()
			{
				Dimensions = dimensions.ToDictionary(x => x.Id, x => x),
				Filters = filters.OrderByDescending(x => x.On).ToList(),
				Groups = groups.ToList(),
				InitialXDimension = xDimId,
				InitialYDimension = yDimId ?? xDimId,
				Points = scatterDataPoints,
				MinDate = dates.Min(),
				MaxDate = dates.Max(),
				Legend = legend.ToList(),
			};

			return scatter;
		}

		public Line ScorecardChart(UserOrganizationModel caller, long measureableId)
		{
			var scores = ScorecardAccessor.GetMeasurableScores(caller, measureableId);

			var o = new Line(){
				marginLeft = 0,
				marginRight = 0,
				marginTop = 0,
				marginBottom = 0,
			};
			if (scores.Any()){
				var values = scores.OrderBy(x => x.DateDue);
				var chart = new Line.LineChart(){
					points = values.Select(x => new Line.LinePoint(){time = x.DateDue.ToJavascriptMilliseconds(),value = x.Measured}).ToList(),
					displayName = scores.First().Measurable.Title,
					name = scores.First().Measurable.Title,
				};
				o.charts.Add(chart);
				o.start = values.First().DateDue.ToJavascriptMilliseconds();
				o.end = values.Last().DateDue.ToJavascriptMilliseconds();
			}

			return o;
		}


		public class ScatterScorer
		{
			private Dictionary<string, ScatterDatum> datums;
			private HashSet<ScatterGroup> groups;
			private QuestionCategoryModel companyValueCategory;
			private QuestionCategoryModel rolesCategory;

			public ScatterScorer(Dictionary<string, ScatterDatum> datums, HashSet<ScatterGroup> groups, QuestionCategoryModel companyValueCategory, QuestionCategoryModel rolesCategory)
			{
				this.datums = datums;
				this.groups = groups;
				this.companyValueCategory = companyValueCategory;
				this.rolesCategory = rolesCategory;
			}

			public static decimal ShiftRatio(Ratio ratio)
			{
				return ratio.GetValue(.5m) * 200 - 100;
			}

			public static bool ScoreFunction(FastReviewQueries.UserReviewRoleValues roleValue, out Ratio roles, out Ratio values)
			{
				roles = ScoreRole(roleValue.GetIt, roleValue.WantIt, roleValue.HasCapacity);
				values = ScoreValue(roleValue);
				return (roles.IsValid() || values.IsValid());
			}

			public static Ratio ScoreRole(Ratio getIt,Ratio wantIt,Ratio hasCapacity)
			{
				var r = new Ratio();
				r.Merge(getIt);
				r.Merge(wantIt);
				r.Merge(hasCapacity);
				return r;
				
			}

			protected static Ratio ScoreValue(FastReviewQueries.UserReviewRoleValues roleValues)
			{
				var r = new Ratio();
				for (var i = 0; i < roleValues.ValuePositive; i++)
					r.Merge(ScoreValue(PositiveNegativeNeutral.Positive));
				for (var i = 0; i < roleValues.ValueNeutral; i++)
					r.Merge(ScoreValue(PositiveNegativeNeutral.Neutral));
				for (var i = 0; i < roleValues.ValueNegative; i++)
					r.Merge(ScoreValue(PositiveNegativeNeutral.Negative));
				return r;
			}

			protected static Ratio ScoreValue(PositiveNegativeNeutral exhibits)
			{
				var num = 0m;
				var denom = 1m;
				switch (exhibits)
				{
					case PositiveNegativeNeutral.Indeterminate:
						denom = 0;
						break;
					case PositiveNegativeNeutral.Negative:
						num = 0;
						break;
					case PositiveNegativeNeutral.Neutral:
						num = .25m;
						break;
					case PositiveNegativeNeutral.Positive:
						num = 1;
						break;
					default:
						throw new Exception("Unhandled PositiveNegativeNeutral: " + exhibits);
				}

				return new Ratio(num, denom);//, (decimal)a.Askable.Weight);
			}

			public static bool ScoreFunction(AnswerModel answer, out Ratio score, out string category)
			{
				var cat = answer.Askable.Category;
				switch (answer.Askable.GetQuestionType())
				{
					case QuestionType.Thumbs:
						{
							score = new Ratio();
							category = null;
							return false;
						}
					case QuestionType.Feedback:
						{
							score = new Ratio();
							category = null;
							return false;
						}
					case QuestionType.Rock:
						{
							score = new Ratio();
							category = null;
							return false;
						}
					case QuestionType.Slider:
						score = new Ratio((decimal)(((SliderAnswer)answer).Percentage ?? .5m) * (decimal)answer.Askable.Weight, (decimal)answer.Askable.Weight);
						category = cat.Category.Translate();
						//AddScatterScore(cat.Id, cat.Category.Translate(), );
						return true;
					case QuestionType.GWC:
						{
							var a = (GetWantCapacityAnswer)answer;
							var num = 0.0m;
							var denom = 0.0m;
							
							/*
							num += a.GetIt == Tristate.True ? 1 : 0;
							num += a.WantIt == Tristate.True ? 1 : 0;
							num += a.HasCapacity == Tristate.True ? 1 : 0;

							denom += a.GetIt != Tristate.Indeterminate ? 1 : 0;
							denom += a.WantIt != Tristate.Indeterminate ? 1 : 0;
							denom += a.HasCapacity != Tristate.Indeterminate ? 1 : 0;*/

							//score = new Ratio(num/3.0m, denom/3.0m, 1);//(decimal)a.Askable.Weight);
							
							score = ScoreRole(a.GetItRatio, a.WantItRatio, a.HasCapacityRatio);
							category = "Roles";

							return (score.Denominator != 0);
							//AddScatterScore(rolesCategory.Id, "Roles", count / 3.0, weight / 3.0);
						}
						break;
					case QuestionType.CompanyValue:
						{
							var a = (CompanyValueAnswer)answer;
							var num = 0.0m;
							var denom = 1m;

							score = ScoreValue(a.Exhibits);

							category = "Values";

							return (score.Denominator != 0);
							//AddScatterScore(companyValueCategory.Id, "Values", count / 2.0, weight);
						}
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			public void Add(AnswerModel answer)
			{
				Ratio score;
				String cat;
				if (ScoreFunction(answer, out score, out cat))
					AddScatterScore(answer.Askable.Category.Id, cat, (double)score.Numerator, (double)score.Denominator);
			}


			private void AddScatterScore(long categoryId, string category, double percent, double weight)
			{
				var catClass = "category-" + categoryId;
				if (!datums.ContainsKey(catClass))
				{
					datums[catClass] = new ScatterDatum()
					{
						DimensionId = "category-" + categoryId,
						Class = String.Join(" ", catClass),
					};
				}
				datums[catClass].Denominator += weight;
				datums[catClass].Value += ((double)(percent) * 200.0 - 100.0) * weight;
				groups.Add(new ScatterGroup(category, catClass));

				/*datums[catClass].Denominator += (double) answer.Askable.Weight;
				datums[catClass].Value += ((double) (answer.Percentage.Value)*200 - 100)*(double) answer.Askable.Weight;
				//filters.Add(new ScatterFilter(answer.Askable.Category.Category.Translate(), catClass));
				groups.Add(new ScatterGroup(answer.Askable.Category.Category.Translate(), catClass));*/
			}
		}

	}
}