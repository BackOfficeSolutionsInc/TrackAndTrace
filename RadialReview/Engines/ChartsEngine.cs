using Amazon.IdentityManagement.Model;
using NHibernate.Criterion;
using NHibernate.Linq;
using RadialReview.Accessors;
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

		public Scatter AggregateReviewScatter(UserOrganizationModel caller, long reviewsId)
		{
			var allAnswers = _ReviewAccessor.GetReviewContainerAnswers(caller, reviewsId);
			var reviewContainer = _ReviewAccessor.GetReviewContainer(caller, reviewsId, false, false, false);

			QuestionCategoryModel companyValuesCategory;
			QuestionCategoryModel rolesCategory;

			
			var teammemberLookup = new Multimap<long, OrganizationTeamModel>();
			Dictionary<long, OrganizationTeamModel> teamLookup=null;
			var teamMembers = _TeamAccessor.GetTeamMembersAtOrganization(caller, reviewContainer.ForOrganizationId);

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					companyValuesCategory = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.COMPANY_VALUES);
					rolesCategory = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.ROLES);
					
					var teams = s.QueryOver<OrganizationTeamModel>().Where(x => x.DeleteTime == null && x.Organization.Id == reviewContainer.ForOrganizationId).List().ToList();
					teamLookup = teams.ToDictionary(x => x.Id, x => x);
					
					foreach (var t in teamMembers){
						var teamId = t.TeamId;
						var userId = t.UserId;
						if (teamLookup.ContainsKey(teamId)){
							var team = teamLookup[teamId];
							teammemberLookup.Add(userId, team);
						}
					}
				}
			}
			
			var scatterDataPoints = new List<ScatterData>();
			var groups = new HashSet<ScatterGroup>(new EqualityComparer<ScatterGroup>((x, y) => x.Class.Equals(y.Class), x => x.Class.GetHashCode()));
			var remapper = RandomUtility.CreateRemapper();
			var remapperUser = RandomUtility.CreateRemapper();
			foreach (var revieweeAnswers in allAnswers.GroupBy(x => x.AboutUserId)){
				var datums = new Dictionary<string, ScatterDatum>();
				var scorer = new ScatterScorer(datums, groups, companyValuesCategory, rolesCategory);
				foreach(var answer in revieweeAnswers)
					scorer.Add(answer);

				var title = "<span class='aboutType hoverTitle'></span> <span class='reviewName hoverTitle'>" + revieweeAnswers.First().AboutUser.GetName()+ "</span>";

				var uniqueId = remapper.Remap(revieweeAnswers.First().Id);

				var safeUserIdMap = remapperUser.Remap(revieweeAnswers.Min(x => x.Id)); //We'll use the Min-Answer Id because its unique and not traceable
				var userClassStr = "user-" + safeUserIdMap;
				var point = new ScatterData(){
					Class = "userDataPoint " + userClassStr,
					Date = revieweeAnswers.Max(x => x.CompleteTime) ?? new DateTime(2014, 1, 1),
					Dimensions = datums,
					SliceId = reviewsId,
					Title = title,
					Subtext = "",
					Id = uniqueId,
				};
				point.OtherData.AboutUser = revieweeAnswers.First().AboutUser;
				scatterDataPoints.Add(point);
			}
			var dimensions = new List<ScatterDimension>();

			dimensions.Insert(0, new ScatterDimension() { Max = 100, Min = -100, Id = "category-" + companyValuesCategory.Id, Name = "Company Values" });
			dimensions.Insert(1, new ScatterDimension() { Max = 100, Min = -100, Id = "category-" + rolesCategory.Id, Name = "Roles" });

			var xDimId = dimensions.FirstOrDefault().NotNull(x => x.Id);
			var yDimId = dimensions.Skip(1).FirstOrDefault().NotNull(x => x.Id);

			var data = new List<Scatter.ScatterPoint>();

			
			var xAxis = dimensions.FirstOrDefault(x => x.Id == xDimId).NotNull(x => x.Name);
			var yAxis = dimensions.FirstOrDefault(x => x.Id == yDimId).NotNull(x => x.Name);

			var subgroupLookup = new DefaultDictionary<long,Checktree.Subtree>(teamId=>new Checktree.Subtree(){
				id = "team_"+teamId,
				title = teamLookup[teamId].GetName()
			});

			foreach (var pt in scatterDataPoints){

				var xVal = 0m;
				var yVal = 0m;

				var zeroDen = 0;

				if (pt.Dimensions.ContainsKey(xDimId)){
					var dx = pt.Dimensions[xDimId];
					xVal = dx.Denominator == 0.0 ? 0m : (decimal) (dx.Value/dx.Denominator);
					if (dx.Denominator == 0)
						zeroDen++;
				}else{
					zeroDen++;
				}
				if (pt.Dimensions.ContainsKey(yDimId)){
					var dy = pt.Dimensions[yDimId];
					yVal = dy.Denominator == 0.0 ? 0m : (decimal)(dy.Value / dy.Denominator);
					if (dy.Denominator == 0)
						zeroDen++;
				}else{
					zeroDen++;
				}

				if (zeroDen != 2){
					var user = ((UserOrganizationModel) pt.OtherData.AboutUser);
					data.Add(new Scatter.ScatterPoint(){
						cx = xVal,
						cy = yVal,
						date = pt.Date,
						imageUrl = user.ImageUrl(),
						subtitle = pt.Date.ToShortDateString(),
						title = user.GetName(),
						xAxis = xAxis,
						yAxis = yAxis,
						@class = "user_"+user.Id,
						id = "user_" + user.Id,
					});
					var teams = teammemberLookup.Get(user.Id);
					foreach (var team in teams){
						subgroupLookup[team.Id].subgroups.Add(new Checktree.Subtree(){
							id = "user_" + user.Id,
							title = user.GetName()
						});
					}
				}
			}

			var checktree = new Checktree();
			checktree.Data.title = reviewContainer.ReviewName;

			foreach (var s in subgroupLookup){
				checktree.Data.subgroups.Add(s.Value);
			}

			var valueCoef = 1;
			var roleCoef  = 5;

			var ordered = data.OrderByDescending(x =>{
				var xx = (x.cx + 100);
				var yy = (x.cy + 100);
				return valueCoef*xx*xx + roleCoef*yy*yy;
			}).ToList();


			return new Scatter(){
				Points = data,
				xAxis = xAxis,
				yAxis = yAxis,
				title = "Aggregate Results",
				FilterTree = checktree,
				OrderedPoints = ordered,
			};

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
					rolesCategory		  = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.ROLES);
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
					var scorer = new ScatterScorer(datums, groups,companyValuesCategory,rolesCategory);
					foreach (var answer in userReviewAnswers){
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
						if (aboutTypes!=AboutType.NoRelationship && aboutType==AboutType.NoRelationship)
							continue;

						var aboutClass = "about-" + aboutType;

						legend.Add(new ScatterLegendItem(aboutType.ToString(), aboutClass));

						var reviewsClass = "reviews-" + reviewContainerId;
						var teamClassesStr = String.Join(" ", teamClasses);
						var userClassStr = "user-" + safeUserIdMap;

						//The first answer id should be unique for all this stuff, so lets just use it for convenience
						var uniqueId = remapper.Remap(userReviewAnswers.First().Id);

						String title, subtext = "";
						if (sensitive){
							var user = userReviewAnswers.First().ByUser;
							title = "<span class='nameAndTitle hoverTitle'>" + user.GetNameAndTitle() + "</span> <span class='aboutType hoverTitle'>" + aboutType + "</span> <span class='reviewName hoverTitle'>" + reviewContainer.ReviewName + "</span>";
						}
						else{
							title = "<span class='aboutType hoverTitle'>" + aboutType.ToString() + "</span> <span class='reviewName hoverTitle'>" + reviewContainer.ReviewName + "</span>";
						}

						scatterDataPoints.Add(new ScatterData(){
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

			dimensions.Insert(0,new ScatterDimension() { Max = 100, Min = -100, Id = "category-"+rolesCategory.Id, Name = "Roles" });
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

			public void Add(AnswerModel answer)
			{
				var cat = answer.Askable.Category;
				switch (answer.Askable.GetQuestionType())
				{
					case QuestionType.Thumbs:
						break;
					case QuestionType.Feedback:
						break;
					case QuestionType.Rock:
						break;
					case QuestionType.Slider:
						AddScatterScore(cat.Id, cat.Category.Translate(), (double)(((SliderAnswer)answer).Percentage ?? .5m), (double)answer.Askable.Weight);
						break;
					case QuestionType.GWC:
						{
							var a = (GetWantCapacityAnswer)answer;
							var count = 0.0;
							var weight = 0.0;
							count += a.GetIt == Tristate.True ? 1 : 0;
							count += a.WantIt == Tristate.True ? 1 : 0;
							count += a.HasCapacity == Tristate.True ? 1 : 0;

							weight += a.GetIt != Tristate.Indeterminate ? 1 : 0;
							weight += a.WantIt != Tristate.Indeterminate ? 1 : 0;
							weight += a.HasCapacity != Tristate.Indeterminate ? 1 : 0;


							AddScatterScore(rolesCategory.Id, "Roles", count / 3.0, weight/3.0);
						}
						break;
					case QuestionType.CompanyValue:
						{
							var a = (CompanyValueAnswer)answer;
							var count = 0.0;
							var weight = 1;
							switch (a.Exhibits)
							{
								case PositiveNegativeNeutral.Indeterminate:
									weight = 0;
									break;
								case PositiveNegativeNeutral.Negative:
									count = 0;
									break;
								case PositiveNegativeNeutral.Neutral:
									count = 1;
									break;
								case PositiveNegativeNeutral.Positive:
									count = 2;
									break;
								default:
									throw new Exception("Unhandled PositiveNegativeNeutral: "+a.Exhibits);
							}
							AddScatterScore(companyValueCategory.Id, "Values", count / 2.0, weight);
						}
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
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