using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.People.Angular;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Engines.Surveys;
using RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Events;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Transformers;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Accessors {
	public class QuarterlyConversationAccessor {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

#pragma warning disable CS0618 // Type or member is obsolete
		public static IEnumerable<IByAbout> AvailableByAbouts(UserOrganizationModel caller, bool includeSelf = false) {
			var nodes = AccountabilityAccessor.GetNodesForUser(caller, caller.Id);
			var possible = new List<IByAbout>();
			foreach (var node in nodes) {
				var reports = DeepAccessor.GetDirectReportsAndSelf(caller, node.Id);
				foreach (var report in reports) {
					if (report.User != null) {
						possible.Add(new ByAbout(caller, report));
						if (includeSelf) {
							possible.Add(new ByAbout(report, report));
						}
					}
				}
			}
			return possible;
		}
#pragma warning restore CS0618 // Type or member is obsolete

		/// <summary>
		/// Converts the By's to UserOrganizationModels
		///  
		/// </summary>
		/// <param name="s"></param>
		/// <param name="byAbout"></param>
		/// <returns></returns>
		//private static IEnumerable<IByAbout> TransformByAbouts(ISession s, IEnumerable<IByAbout> byAbout) {

		//}

		public static long GenerateQuarterlyConversation(UserOrganizationModel caller, string name, IEnumerable<IByAbout> byAbout) {

			var possible = AvailableByAbouts(caller, true);
			var invalid = byAbout.Where(selected => possible.All(avail => avail.ToKey() != selected.ToKey()));
			if (invalid.Any()) {
				Console.WriteLine("Invalid");
				foreach (var i in invalid) {
					Console.WriteLine("\tby:" + i.GetBy().ToKey() + "  about:" + i.GetAbout().ToKey());
				}
				throw new PermissionsException("Could not create Quarterly Conversation. You cannot view these items.");
			}

			long containerId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CreateQuarterlyConversation(caller.Organization.Id);

					containerId = GenerateQuarterlyConversation_Unsafe(s, perms, name, byAbout);

					tx.Commit();
					s.Flush();
					return containerId;
				}
			}
			//return GetAngularSurveyContainerBy(caller, caller, containerId);
		}

		public static long GenerateQuarterlyConversation_Unsafe(ISession s, PermissionsUtility perms, string name, IEnumerable<IByAbout> byAbout) {
			var caller = perms.GetCaller();
			var engine = new SurveyBuilderEngine(
				new QuarterlyConversationInitializer(caller, name, caller.Organization.Id),
				new SurveyBuilderEventsSaveStrategy(s),
				new TransformAboutAccountabilityNodes(s)
			);

			//byAbout = TransformByAbouts(s, byAbout);

			var container = engine.BuildSurveyContainer(byAbout);
			var containerId = container.Id;
			var permItems = new[] {
				PermTiny.Creator(),
				PermTiny.Admins(),
				PermTiny.Members(true, true, false)
			};
			PermissionsAccessor.CreatePermItems(s, caller, PermItem.ResourceType.SurveyContainer, containerId, permItems);
			return containerId;
		}


		public static AngularPeopleAnalyzer GetPeopleAnalyzer(UserOrganizationModel caller, long userId, DateRange range = null) {
			//Determine if self should be included.
			var includeSelf = caller.ManagingOrganization;
			if (includeSelf == false) {
				try {
					var rootId = AccountabilityAccessor.GetRoot(caller, caller.Organization.AccountabilityChartId).Id;
					includeSelf = includeSelf || AccountabilityAccessor.GetNodesForUser(caller, userId).Any(x => x.ParentNodeId == rootId);
				} catch (Exception e) {
					log.Error(e);
				}
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);
					var nodes = AccountabilityAccessor.GetNodesForUser(s, perms, userId);
#pragma warning disable CS0618 // Type or member is obsolete
					var childrens = nodes.SelectMany(node => DeepAccessor.GetChildrenAndSelf(s, caller, node.Id));
					if (!includeSelf) {
						childrens = childrens.Where(x => !nodes.Any(y => y.Id == x));
					}
#pragma warning restore CS0618 // Type or member is obsolete

					SurveyItem item = null;
					var accountabiliyNodeResults = s.QueryOver<SurveyResponse>()
						.Where(x => x.SurveyType == SurveyType.QuarterlyConversation && x.OrgId == caller.Organization.Id && x.About.ModelType == ForModel.GetModelType<AccountabilityNode>() && x.DeleteTime==null && x.Answer!=null)
						.Where(range.Filter<SurveyResponse>())
						.WhereRestrictionOn(x => x.About.ModelId).IsIn(childrens.ToArray())
						.List().ToList();
					var users = s.QueryOver<AccountabilityNode>().Where(x=>x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(childrens.Distinct().ToArray()).Fetch(x => x.User).Eager.Future();
					

					var formats = s.QueryOver<SurveyItemFormat>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemFormatId).Distinct().ToArray()).Future();
					var items = s.QueryOver<SurveyItem>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemId).Distinct().ToArray()).Future();
					var surveys = s.QueryOver<Survey>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.SurveyId).Distinct().ToArray()).Future();
					var surveyContainers = s.QueryOver<SurveyContainer>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.SurveyContainerId).Distinct().ToArray()).Future();
					//var users = s.QueryOver<Angular>().WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemId).Distinct().ToArray()).Future();

					var formatsList = formats.ToList();

					var formatsLu = formatsList.ToDefaultDictionary(x => x.Id, x => x, x => null);
					var itemsLu = items.ToDefaultDictionary(x => x.Id, x => x, x => null);
					var userLu = users.ToDefaultDictionary(x => ForModel.From(x), x => x.User.NotNull(y => y.GetName()), x => "n/a");

					foreach (var t in items) {
						if (t.Source.NotNull(x => x.Is<CompanyValueModel>())) {
							t.Source._PrettyString = t.GetName();
						}
					}


					foreach (var result in accountabiliyNodeResults) {
						result._Item = itemsLu[result.ItemId];
						result._ItemFormat = formatsLu[result.ItemFormatId];
					}

					var analyzer = new AngularPeopleAnalyzer() { };
					var rows = new List<AngularPeopleAnalyzerRow>();
					var allValueIds = new List<long>();

					foreach (var row in accountabiliyNodeResults.GroupBy(x => x.About)) {
						var answersAbout = row.OrderByDescending(x => x.CompleteTime ?? DateTime.MinValue);

						var get = answersAbout.Where(x => x._ItemFormat.GetSetting<string>("gwc") == "get").FirstOrDefault();
						var want = answersAbout.Where(x => x._ItemFormat.GetSetting<string>("gwc") == "want").FirstOrDefault();
						var capacity = answersAbout.Where(x => x._ItemFormat.GetSetting<string>("gwc") == "cap").FirstOrDefault();

						var yesNo = new Func<string, string>(x => {
							switch (x) {
								case "yes":
									return "Y";
								case "no":
									return "N";
								default:
									return null;
							}
						});
						var plusMinus = new Func<string, string>(x => {
							switch (x) {
								case "often":
									return "+";
								case "sometimes":
									return "+/–";
								case "not-often":
									return "–";
								default:
									return null;
							}
						});
						row.Key._PrettyString = userLu[row.Key];
						var arow = new AngularPeopleAnalyzerRow(row.Key,!nodes.Any(x=>x.Id ==row.Key.ModelId));						
						rows.Add(arow);
					}

					var overridePriority = new DefaultDictionary<string, int>(x => 0) {
						{ "often", 1 },{ "sometimes", 2 },  { "not-often", 3 },
						{ "done", 1 }, { "not-done", 2 },
						{ "yes", 1 }, { "no", 2 }
					};

					var rewrite = new DefaultDictionary<string, string>(x => x) {
						{ "often", "+" },{ "sometimes", "+/–" },  { "not-often","–" },
						{ "done", "done" }, { "not-done", "not done" },
						{ "yes", "Y" }, { "no", "N" }
					};



					var surveyIssueDateLookup = surveys.ToDictionary(x => x.Id, x => x.GetIssueDate());
					var surveyItemLookup = items.ToDictionary(x => x.Id, x => x);
					var surveyItemFormatLookup = formats.ToDictionary(x => x.Id, x => x);
					
					var responses = new List<AngularPeopleAnalyzerResponse>();
					foreach (var result in accountabiliyNodeResults) {
						if (!surveyContainers.Any(x => x.Id == result.SurveyContainerId))
							continue;
						if (!surveys.Any(x => x.Id == result.SurveyId))
							continue;

						var answerDate = result.CompleteTime;

						var issueDate = surveyIssueDateLookup[result.SurveyId];
						var questionSource = surveyItemLookup[result.ItemId].GetSource();

						if (answerDate != null) {
							var byUser = result.By;							
							var aboutUser = result.About;
							var answerFormatted = rewrite[result.Answer];
							var overrideAnswer = overridePriority[result.Answer];
							var format = surveyItemFormatLookup[result.ItemFormatId];
							var gwc = format.GetSetting<string>("gwc");
							var surveyContainerId = result.SurveyContainerId;

							if ( gwc != null) {
								questionSource = new ForModel() {
									ModelId =-1,
									ModelType = gwc
								};
							}

							var response = new AngularPeopleAnalyzerResponse(
												new ByAbout(byUser,aboutUser),
												issueDate,
												answerDate.Value,
												questionSource,
												answerFormatted,
												result.Answer,
												overrideAnswer,
												surveyContainerId
											);
							responses.Add(response);
						}
					}


					var values = accountabiliyNodeResults.Where(x => x._Item.NotNull(y => y.GetSource().ModelType) == ForModel.GetModelType<CompanyValueModel>());//new Dictionary<long, string>();
					

					//foreach (var row in accountabiliyNodeResults.Where(x => x._Item.NotNull(y => y.GetSource().ModelType) == ForModel.GetModelType<CompanyValueModel>())) {
					//	values.GetOrAddDefault(row._Item.GetSource().ModelId, x => row._Item.GetName());
					//}

					analyzer.Rows = rows;
					analyzer.Responses = responses;
					analyzer.Values = values.Select(x => new PeopleAnalyzerValue(surveyItemLookup[x.GetItemId()].GetSource()));

					analyzer.SurveyContainers = surveyContainers.Select(x=>new AngularSurveyContainer(x));

					var issueDates = responses.Select(x => x.IssueDate.Value).ToList();

					var dateRange = new DateRange();
					if (issueDates.Any()) {
						dateRange = new DateRange(issueDates.Min(), issueDates.Max());
					}

					analyzer.DateRange = new AngularDateRange(dateRange);

					return analyzer;

				}
			}
		}
	}
}
