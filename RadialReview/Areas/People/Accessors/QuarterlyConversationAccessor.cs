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
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Areas.People.Accessors {
	public class QuarterlyConversationAccessor {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

#pragma warning disable CS0618 // Type or member is obsolete

		public static IEnumerable<SurveyUserNode> AvailableAboutsForMe(UserOrganizationModel caller) {
			var nodes = AvailableByAboutsForMe(caller, false, false);
			return nodes.Select(x => x.About).Distinct(x => x.ToViewModelKey());
		}

		public static IEnumerable<ByAboutSurveyUserNode> AvailableByAboutsFiltered(UserOrganizationModel caller, IEnumerable<SurveyUserNode> abouts, bool includeSelf, bool supervisorLMA) {
			var allAvailable = AvailableByAboutsForMe(caller, includeSelf, supervisorLMA);

			return allAvailable.Where(aa => abouts.Any(about => about.ToViewModelKey() == aa.About.ToViewModelKey()));


		}

		private static SurveyUserNode SunGetter(Dictionary<string, SurveyUserNode> existingItems, AccountabilityNode toAdd) {
			var k = toAdd.ToKey();
			if (!existingItems.ContainsKey(k))
				existingItems[k] = new SurveyUserNode() {
					AccountabilityNodeId = toAdd.Id,
					User = toAdd.User,
					AccountabilityNode = toAdd,
					UserOrganizationId = toAdd.UserId.Value,
					UsersName = toAdd.User.GetName(),
					PositionName = toAdd.AccountabilityRolesGroup.NotNull(x => x.Position.GetName())
				};
			return existingItems[k];
		}

		public static IEnumerable<ByAboutSurveyUserNode> AvailableByAboutsForMe(UserOrganizationModel caller, bool includeSelf = false, bool supervisorLMA = false) {

			var allModels = new List<SurveyUserNode>();

			var sunDict = new Dictionary<string, SurveyUserNode>();

			var nodes = AccountabilityAccessor.GetNodesForUser(caller, caller.Id);
			var possible = new List<ByAboutSurveyUserNode>();
			foreach (var node in nodes) {
				var reports = DeepAccessor.GetDirectReportsAndSelf(caller, node.Id);
				var callerUN = SunGetter(sunDict, node);//new SurveyUserNode() {
				//	AccountabilityNodeId = node.Id,
				//	User = node.User,
				//	AccountabilityNode = node,
				//	UserOrganizationId = node.UserId.Value,
				//	UsersName = caller.GetName(),
				//	PositionName = node.AccountabilityRolesGroup.NotNull(x => x.Position.GetName())
				//};
				allModels.Add(callerUN);

				foreach (var report in reports) {
					if (report.User != null) {
						var reportUN = SunGetter(sunDict, report);//new SurveyUserNode() {
						//	AccountabilityNodeId = report.Id,
						//	UserOrganizationId = report.UserId.Value,
						//	User = report.User,
						//	AccountabilityNode = report,
						//	UsersName = report.User.GetName(),
						//	PositionName = report.AccountabilityRolesGroup.NotNull(x => x.Position.GetName())
						//};
						allModels.Add(reportUN);
						possible.Add(new ByAboutSurveyUserNode(callerUN, reportUN, AboutType.Subordinate));

						//if (reportUN.ToKey() == callerUN.ToKey()) {
						//	var about = AboutType.NoRelationship;
						//	if (includeSelf)
						//		about = about | AboutType.Self;

						//	if (supervisorLMA)
						//		about = about | AboutType.Manager;

						//	if (includeSelf || supervisorLMA)
						//		possible.Add(new ByAboutSurveyUserNode(reportUN, callerUN, about));

						//} else {
						if (includeSelf) {
							possible.Add(new ByAboutSurveyUserNode(reportUN, reportUN, AboutType.Self));
						}
						if (supervisorLMA) {
							possible.Add(new ByAboutSurveyUserNode(reportUN, callerUN, AboutType.Manager));
						}
						//}
					}
				}
			}
			//if (save) {
			//	using (var s = HibernateSession.GetCurrentSession()) {
			//		using (var tx = s.BeginTransaction()) {
			//			foreach (var m in allModels) {
			//				s.Save(m);
			//			}
			//			tx.Commit();
			//			s.Flush();
			//		}
			//	}
			//}


			var combined = possible.GroupBy(x => x.Key).Select(ba => {
				var about = AboutType.NoRelationship;
				foreach (var x in ba) {
					if (x.AboutIsThe.HasValue)
						about = about | x.AboutIsThe.Value;
				}
				return new ByAboutSurveyUserNode(ba.First().By, ba.First().About, about);
			});


			 return combined.OrderBy(x => x.GetBy().ToPrettyString());
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

		public class QuarterlyConversationGeneration {
			public long SurveyContainerId { get; set; }
			public IEnumerable<Mail> UnsentEmail { get; set; }
			public List<String> Errors { get; set; }

			public QuarterlyConversationGeneration() {
				Errors = new List<string>();
				UnsentEmail = new List<Mail>();
			}
		}

		public static async Task<long> GenerateQuarterlyConversation(UserOrganizationModel caller, string name, IEnumerable<ByAboutSurveyUserNode> byAbout, DateTime dueDate, bool sendEmails) {

			var possible = AvailableByAboutsForMe(caller, true, true);
			var invalid = byAbout.Where(selected => possible.All(avail => avail.GetViewModelKey() != selected.GetViewModelKey()));
			if (invalid.Any()) {
				Console.WriteLine("Invalid");
				foreach (var i in invalid) {
					Console.WriteLine("\tby:" + i.GetBy().ToKey() + "  about:" + i.GetAbout().ToKey());
				}
				throw new PermissionsException("Could not create Quarterly Conversation. You cannot view these items.");
			}

			var populatedByAbouts = byAbout.Select(x => possible.First(y => x.GetViewModelKey() == y.GetViewModelKey())).ToList();

			QuarterlyConversationGeneration qcResult;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CreateQuarterlyConversation(caller.Organization.Id);
					qcResult = GenerateQuarterlyConversation_Unsafe(s, perms, name, populatedByAbouts, dueDate, sendEmails);
					tx.Commit();
					s.Flush();
				}
			}
			if (sendEmails) {
				await Emailer.SendEmails(qcResult.UnsentEmail);
			}
			return qcResult.SurveyContainerId;
		}

		public static QuarterlyConversationGeneration GenerateQuarterlyConversation_Unsafe(ISession s, PermissionsUtility perms, string name, IEnumerable<ByAboutSurveyUserNode> byAbout, DateTime dueDate, bool generateEmails) {
			var caller = perms.GetCaller();

			var reconstructed = byAbout.GroupBy(x => x.By.UserOrganizationId + "~" + x.About.ToViewModelKey()).Select(ba => {
				//var reconstructedRelationship = AboutType.NoRelationship;
				//foreach (var x in ba) {
				//	if (x.AboutIsThe.HasValue)
				//		reconstructedRelationship = reconstructedRelationship | x.AboutIsThe.Value;
				//}

				//var reconstructedAbout = SurveyUserNode.Clone(ba.First().About,true);
				//reconstructedAbout.Relationship = reconstructedRelationship;
				//return new ByAbout(ba.First().By.User, reconstructedAbout);

				return new ByAbout(ba.First().By.User, ba.First().About);
			}).ToList();

			foreach (var ba in reconstructed) {
				//	if (ba.By.Id == 0) {
				//		ba.By.AccountabilityNode = s.Load<AccountabilityNode>(ba.By.AccountabilityNodeId);
				//		ba.By.User = s.Load<UserOrganizationModel>(ba.By.UserOrganizationId);
				//		s.Save(ba.By);
				//	} else {
				//		int a = 0;//already saved. Should usually get here.
				//	}
				var about = (SurveyUserNode)ba.About;
				if (about.Id == 0) {
					about.AccountabilityNode = s.Load<AccountabilityNode>(about.AccountabilityNodeId);
					about.User = s.Load<UserOrganizationModel>(about.UserOrganizationId);
					s.Save(about);
				} else {
					int a = 0;//already saved. Should usually get here.
				}
			}

		//	var reconstructed = byAbout.Select(x => new ByAbout(x.By.User, x.About)).ToList();






			var engine = new SurveyBuilderEngine(
				new QuarterlyConversationInitializer(caller, name, caller.Organization.Id, dueDate),
				new SurveyBuilderEventsSaveStrategy(s),
				new TransformAboutAccountabilityNodes(s)
			);

			var container = engine.BuildSurveyContainer(reconstructed);
			var containerId = container.Id;
			var permItems = new[] {
				PermTiny.Creator(),
				PermTiny.Admins(),
				PermTiny.Members(true, true, false)
			};
			PermissionsAccessor.CreatePermItems(s, caller, PermItem.ResourceType.SurveyContainer, containerId, permItems);

			var emails = new List<Mail>();
			var allBys = container.GetSurveys()
							.Select(x => x.GetBy())
							.Distinct(x => x.ToKey());

			var linkUrl = Config.BaseUrl(null, "/People/QuarterlyConversation/");

			var result = new QuarterlyConversationGeneration() {
				SurveyContainerId = containerId,
			};
			if (generateEmails) {
				foreach (var byUser in allBys) {
					var user = ForModelAccessor.GetTinyUser_Unsafe(s, byUser);
					if (user != null) {
						if (user.Email != null) {
							var email = Mail.To(EmailTypes.QuarterlyConversationIssued, user.Email)
								.SubjectPlainText("You have a Quarterly Conversation to complete")
								.Body(EmailStrings.QuarterlyConversation_Body, user.FirstName, dueDate.ToShortDateString(), linkUrl, linkUrl, Config.ProductName());
							emails.Add(email);
						} else {
							result.Errors.Add("By (" + byUser.ToKey() + ") did not have an email.");
						}
					} else {
						result.Errors.Add("By (" + byUser.ToKey() + ") was not a user.");
					}
				}
				result.UnsentEmail = emails;
			}

			return result;
		}


		public static AngularPeopleAnalyzer GetPeopleAnalyzer(UserOrganizationModel caller, long userId, DateRange range = null) {
			//Determine if self should be included.
			var includeSelf = caller.IsManager();
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
					var myNodes = AccountabilityAccessor.GetNodesForUser(s, perms, userId);
#pragma warning disable CS0618 // Type or member is obsolete
					var acNodeChildrenModels = myNodes.SelectMany(node => DeepAccessor.GetChildrenAndSelfModels(s, caller, node.Id)).ToList();
					if (!includeSelf) {
						acNodeChildrenModels = acNodeChildrenModels.Where(x => !myNodes.Any(y => y.Id == x.Id)).ToList();
					}
#pragma warning restore CS0618 // Type or member is obsolete

					SurveyItem item = null;
					var allSurveyNodeItems = s.QueryOver<SurveyUserNode>().Where(x => x.DeleteTime == null)
												.WhereRestrictionOn(x => x.User.Id)
												.IsIn(acNodeChildrenModels.Select(x => x.UserId).Distinct().Where(x => x != null).ToArray())
												.WhereRestrictionOn(x=>x.AccountabilityNodeId)
												.IsIn(acNodeChildrenModels.Select(x => x.Id).Distinct().ToArray())
												.Select(x => x.Id, x => x.AccountabilityNodeId, x => x.UserOrganizationId, x => x.UsersName, x => x.PositionName)
												.List<object[]>()
												.Select(x => {
													var prettyString = (((string)x[3] ?? "") + ((string)x[4].NotNull(y => " (" + y + ")") ?? "")).Trim();
													var acNode = ForModel.Create<AccountabilityNode>((long)x[1]);
													acNode._PrettyString = prettyString;

													return new {
														Id = (long)x[0],
														ModelId = (long)x[0],
														AccountabilityNodeId = (long)x[1],
														AccountabilityNode = acNode,
														UserOrganizationId = (long)x[2],
														ToKey = ForModel.GetModelType<SurveyUserNode>() + "_" + (long)x[0],
														//PrettyString = prettyString
													};
												}).ToList();
					//var acNodeChildrenIds = acNodeChildrenModels.Where(x => allSurveyNodeItems.Any(n => n.AccountabilityNodeId == x.Id && n.UserOrganizationId == x.UserId)).Select(x => x.Id);


					//Should be more of these which produce more accNodeResults...
					var availableSurveyNodes = allSurveyNodeItems.Where(n => acNodeChildrenModels.Any(x => x.Id == n.AccountabilityNodeId && x.UserId == n.UserOrganizationId)).ToList();
					var surveyNodeIds = availableSurveyNodes.Select(x => x.ModelId).ToList();
					//System.Diagnostics.Debug.WriteLine("-----");
					//foreach (var sni in allSurveyNodeItems) {						
					//	System.Diagnostics.Debug.WriteLine(sni.Id+" "+sni.UserOrganizationId + " " + sni.AccountabilityNodeId);
					//}
					//System.Diagnostics.Debug.WriteLine("-----");

					//foreach (var sni in acNodeChildrenModels) {
					//	System.Diagnostics.Debug.WriteLine(sni.UserId + " " + sni.Id);
					//}


					var accountabiliyNodeResults = s.QueryOver<SurveyResponse>()
						.Where(x => x.SurveyType == SurveyType.QuarterlyConversation && x.OrgId == caller.Organization.Id && x.About.ModelType == ForModel.GetModelType<SurveyUserNode>() && x.DeleteTime == null && x.Answer != null)
						.Where(range.Filter<SurveyResponse>())
						.WhereRestrictionOn(x => x.About.ModelId).IsIn(surveyNodeIds.ToArray())
						.List().ToList();
					//var users = s.QueryOver<AccountabilityNode>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(acNodeChildrenIds.Distinct().ToArray()).Fetch(x => x.User).Eager.Future();


					var formats = s.QueryOver<SurveyItemFormat>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemFormatId).Distinct().ToArray()).Future();
					var items = s.QueryOver<SurveyItem>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemId).Distinct().ToArray()).Future();
					var surveys = s.QueryOver<Survey>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.SurveyId).Distinct().ToArray()).Future();
					var surveyContainers = s.QueryOver<SurveyContainer>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.SurveyContainerId).Distinct().ToArray()).Future();
					//var users = s.QueryOver<Angular>().WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemId).Distinct().ToArray()).Future();

					var formatsList = formats.ToList();

					var formatsLu = formatsList.ToDefaultDictionary(x => x.Id, x => x, x => null);
					var itemsLu = items.ToDefaultDictionary(x => x.Id, x => x, x => null);
				//	var userLu = allSurveyNodeItems.ToDefaultDictionary(x => x.ToKey(), x => x.ToPrettyString(), x => "n/a");// .NotNull(y => y.GetName()), x => "n/a");
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

					var sunToDiscriminator = availableSurveyNodes.ToDefaultDictionary(x => x.ToKey, x => x.AccountabilityNodeId + "_" + x.UserOrganizationId, x => "unknown SUN");
					var discriminatorToSun = availableSurveyNodes.ToDefaultDictionary(x => x.AccountabilityNodeId + "_" + x.UserOrganizationId, x => x, x =>null);

					var sunToAccNode = availableSurveyNodes.ToDefaultDictionary(x => x.ToKey, x => x.AccountabilityNode, x => null);

					foreach (var row in accountabiliyNodeResults.GroupBy(x => sunToDiscriminator[x.About.ToKey()] )) {
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
						//row.Key._PrettyString = userLu[row.Key/*.ToKey()*/];
						var sun = discriminatorToSun[row.Key];

						var arow = new AngularPeopleAnalyzerRow(sun.AccountabilityNode, !myNodes.Any(x => x.Id == sun.AccountabilityNodeId));
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
							var aboutUser = sunToAccNode[result.About.ToKey()];
							var answerFormatted = rewrite[result.Answer];
							var overrideAnswer = overridePriority[result.Answer];
							var format = surveyItemFormatLookup[result.ItemFormatId];
							var gwc = format.GetSetting<string>("gwc");
							var surveyContainerId = result.SurveyContainerId;

							if (gwc != null) {
								questionSource = new ForModel() {
									ModelId = -1,
									ModelType = gwc
								};
							}

							var response = new AngularPeopleAnalyzerResponse(
												new ByAbout(byUser, aboutUser),
												issueDate,
												answerDate.Value,
												questionSource,
												answerFormatted,
												result.Answer,
												overrideAnswer,
												surveyContainerId,
												result.About.ModelId
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

					analyzer.SurveyContainers = surveyContainers.Select(x => new AngularSurveyContainer(x, false));

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
