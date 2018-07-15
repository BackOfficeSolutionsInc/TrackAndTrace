using Hangfire;
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
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Hooks;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.RealTime;
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
			var nodes = AvailableByAboutsForMe(caller, true, false);
			//if (removeManager) {
			//	foreach (var n in nodes.Where(x => (x.About.UserOrganizationId == caller.Id && x.By.UserOrganizationId == caller.Id))) {
			//		n._Hidden = true;
			//	}
			//}

			return nodes.Select(x => {
				x.About._Hidden = x.About.UserOrganizationId == caller.Id;
				return x.About;
			}).Distinct(x => x._Hidden + "_" + x.ToViewModelKey());
		}

		public static IEnumerable<ByAboutSurveyUserNode> AvailableByAboutsFiltered(UserOrganizationModel caller, IEnumerable<SurveyUserNode> abouts, bool includeSelf, bool supervisorLMA) {
			var allAvailable = AvailableByAboutsForMe(caller, includeSelf, supervisorLMA);
			return allAvailable
						.Where(aa => abouts.Any(about => about.ToViewModelKey() == aa.About.ToViewModelKey()));
						//.Where(aa => abouts.Any(about => about.ToViewModelKey() == aa.By.ToViewModelKey()));
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
			log.Info("\tStart\tAvailableByAboutsForMe- " + DateTime.UtcNow.ToJsMs());

			var allModels = new List<SurveyUserNode>();
			var sunDict = new Dictionary<string, SurveyUserNode>();
			var nodes = AccountabilityAccessor.GetNodesForUser(caller, caller.Id);
			var possible = new List<ByAboutSurveyUserNode>();
			foreach (var node in nodes) {
				var reports = DeepAccessor.GetDirectReportsAndSelf(caller, node.Id);
				if (!includeSelf) {
					reports = reports.Where(x => x.Id != node.Id).ToList();
				}
				var callerUN = SunGetter(sunDict, node);

				allModels.Add(callerUN);

				foreach (var report in reports) {
					if (report.User != null) {
						var reportUN = SunGetter(sunDict, report);
						allModels.Add(reportUN);
						possible.Add(new ByAboutSurveyUserNode(callerUN, reportUN, AboutType.Subordinate));

						if (includeSelf) {
							possible.Add(new ByAboutSurveyUserNode(reportUN, reportUN, AboutType.Self));
						}
						if (supervisorLMA) {
							possible.Add(new ByAboutSurveyUserNode(reportUN, callerUN, AboutType.Manager));
						}
					}
				}
			}

			var combined = possible.GroupBy(x => x.Key).Select(ba => {
				var about = AboutType.NoRelationship;
				foreach (var x in ba) {
					if (x.AboutIsThe.HasValue)
						about = about | x.AboutIsThe.Value;
				}
				return new ByAboutSurveyUserNode(ba.First().By, ba.First().About, about);
			});
			log.Info("\t End  \tAvailableByAboutsForMe- " + DateTime.UtcNow.ToJsMs());

			return combined.OrderBy(x => x.GetBy().ToPrettyString());
		}

		public static async Task<int> RemindAllIncompleteSurveys(UserOrganizationModel caller, long surveyContainerId) {
			var output = SurveyAccessor.GetForModelsWithIncompleteSurveysForSurveyContainers(caller, surveyContainerId);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var userIds = output.Where(x => x.ModelType == ForModel.GetModelType<UserOrganizationModel>()).Select(x => x.ModelId).Distinct().ToList();
					var users = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(userIds).List().ToList();

					var availUsers = users.Where(x => {
						try {
							perms.ViewUserOrganization(x.Id, false);
							return true;
						} catch (PermissionsException e) {
							return false;
						}
						return false;
					}).ToList();

					var tasks = availUsers.Select(u => SendReminderUnsafe(s, u, surveyContainerId)).ToList();
					await Task.WhenAll(tasks);

					return tasks.Count;
					//tx.Commit();
					//s.Flush();
				}
			}
		}

		public static async Task SendReminderUnsafe(ISession s, UserOrganizationModel user, long surveyContainerId) {
			var email = user.GetEmail();
			var linkUrl = Config.BaseUrl(null, "/People/QuarterlyConversation/");
			var sc = s.Get<SurveyContainer>(surveyContainerId);
			await Emailer.SendEmail(
				Mail.To(EmailTypes.QuarterlyConversationReminder, email)
				.SubjectPlainText("[Reminder] Please complete your Quarterly Conversation")
				.Body(EmailStrings.QuarterlyConversationReminder_Body, user.GetFirstName(), sc.DueDate.NotNull(x => x.Value.ToShortDateString()), linkUrl, linkUrl, Config.ProductName()));

		}

		public static void LockinSurvey(UserOrganizationModel caller, long surveyContainerId) {
			var output = SurveyAccessor.GetAngularSurveyContainerBy(caller, caller, surveyContainerId);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					var surveys = output.GetSurveys();
					foreach (var survey in surveys) {
						var surveyModel = s.Load<Survey>(survey.Id);

						perms.Self(surveyModel.By);

						surveyModel.LockedIn = true;
						s.Update(surveyModel);
					}


					tx.Commit();
					s.Flush();
				}
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		/// <summary>
		/// Converts the By's to UserOrganizationModels
		///  
		/// </summary>
		/// <param name="s"></param>
		/// <param name="byAbout"></param>
		/// <returns></returns>


		public class QuarterlyConversationGeneration {
			public long SurveyContainerId { get; set; }
			public IEnumerable<Mail> UnsentEmail { get; set; }
			public List<String> Errors { get; set; }

			public QuarterlyConversationGeneration() {
				Errors = new List<string>();
				UnsentEmail = new List<Mail>();
			}
		}

		[Queue(HangfireQueues.Immediate.GENERATE_QC)]/*Queues must be lowecase alphanumeric. You must add queues to BackgroundJobServerOptions in Startup.auth.cs*/
		public static async Task<long> GenerateQuarterlyConversation(long callerId, string name, IEnumerable<ByAboutSurveyUserNode> byAbout, DateRange quarterRange, DateTime dueDate, bool sendEmails) {
			UserOrganizationModel caller;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					caller = s.Get<UserOrganizationModel>(callerId);
				}
			}
			return await GenerateQuarterlyConversation(caller, name, byAbout, quarterRange, dueDate, sendEmails);
		}

		public static async Task<long> GenerateQuarterlyConversation(UserOrganizationModel caller, string name, IEnumerable<ByAboutSurveyUserNode> byAbout, DateRange quarterRange, DateTime dueDate, bool sendEmails) {

			log.Info("Start\tQC Generator- " + DateTime.UtcNow.ToJsMs());
			try {


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
						qcResult = await GenerateAndEmail(s, perms, name, populatedByAbouts, quarterRange, dueDate, sendEmails);

						tx.Commit();
						s.Flush();
					}
				}


				/*qcResult = GenerateQuarterlyConversation_Unsafe(caller, caller.Organization.Id, name, populatedByAbouts, quarterRange, dueDate, sendEmails);
					try {
						if (sendEmails) {
							return Emailer.SendEmails(qcResult.UnsentEmail);
						}
					} catch (Exception e) {
					}
					return Task.Delay(1);*/


				log.Info("End  \tQC Generator- " + DateTime.UtcNow.ToJsMs());

				return qcResult.SurveyContainerId;
			} catch (Exception e) {
				throw e;
			}
		}

		public static async Task<QuarterlyConversationGeneration> GenerateAndEmail(ISession s, PermissionsUtility perms, string name, IEnumerable<ByAboutSurveyUserNode> byAbout, DateRange quarterRange, DateTime dueDate, bool sendEmails) {
			var qcResult = GenerateQuarterlyConversation_Unsafe(s, perms, name, byAbout, quarterRange, dueDate, sendEmails);


			if (qcResult.Errors.Any()) {
				await HooksRegistry.Each<IQuarterlyConversationHook>(s, (ses, x) => x.QuarterlyConversationError(ses, perms.GetCaller(), QuarterlyConversationErrorType.CreationFailed, qcResult.Errors));
			} else {
				await HooksRegistry.Each<IQuarterlyConversationHook>(s, (ses, x) => x.QuarterlyConversationCreated(ses, qcResult.SurveyContainerId));
				try {
					if (sendEmails) {
						await Emailer.SendEmails(qcResult.UnsentEmail);
						await HooksRegistry.Each<IQuarterlyConversationHook>(s, (ses, x) => x.QuarterlyConversationEmailsSent(ses, qcResult.SurveyContainerId));
					}
				} catch (Exception e) {
					await HooksRegistry.Each<IQuarterlyConversationHook>(s, (ses, x) => x.QuarterlyConversationError(ses, perms.GetCaller(), QuarterlyConversationErrorType.EmailsFailed, new List<string>() {
						"Quarterly Conversation successfully created, but some email notifications failed to send."
					}));
				}
			}
			return qcResult;
		}

		public static QuarterlyConversationGeneration GenerateQuarterlyConversation_Unsafe(ISession s, PermissionsUtility perms, string name, IEnumerable<ByAboutSurveyUserNode> byAbout, DateRange quarterRange, DateTime dueDate, bool generateEmails) {
			log.Info("\tStart\tGenerateQuarterlyConversation_Unsafe- " + DateTime.UtcNow.ToJsMs());
			//var creator = creator_userOrganizationModel;
			var creator = perms.GetCaller();

			var reconstructed = byAbout.GroupBy(x => x.By.UserOrganizationId + "~" + x.About.ToViewModelKey())
				.OrderBy(x => x.First().AboutIsThe.NotNull(y => y.Value.Order()))
				.Select(ba => {
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
					//int a = 0;//already saved. Should usually get here.
				}
			}

			var engine = new SurveyBuilderEngine(
				new QuarterlyConversationInitializer(creator, name, creator.Organization.Id, quarterRange, dueDate),
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

			PermissionsAccessor.CreatePermItems(s, creator, PermItem.ResourceType.SurveyContainer, containerId, permItems);

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
			log.Info("\tEnd  \tGenerateQuarterlyConversation_Unsafe- " + DateTime.UtcNow.ToJsMs());

			return result;

		}

		public static DefaultDictionary<string, string> TransformValueAnswer = new DefaultDictionary<string, string>(x => x);

		static QuarterlyConversationAccessor() {
			TransformValueAnswer["often"] = "+";
			TransformValueAnswer["sometimes"] = "+/–";
			TransformValueAnswer["not-often"] = "–";
		}
		
		public static List<long> GetUsersWhosePeopleAnalyzersICanSee(ISession s, PermissionsUtility perms, long myUserId, long? recurrenceId = null) {
			perms.Self(myUserId);
			var callerMeetings = L10Accessor.GetVisibleL10Meetings_Tiny(s, perms, myUserId, onlyPersonallyAttending: true)
											.Select(x => x.Id)
											.ToArray();

			var shareingIdsQ = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
				.Where(x => x.DeleteTime == null && x.SharePeopleAnalyzer == L10Recurrence.SharePeopleAnalyzer.Yes && x.User.Id != myUserId);

			if (recurrenceId == null) {
				shareingIdsQ = shareingIdsQ.WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(callerMeetings);
			} else {
				if (!callerMeetings.Contains(recurrenceId.Value)) {
					//Hey. This permission is correct.
					throw new PermissionsException("Not an attendee.");
				}
				shareingIdsQ = shareingIdsQ.Where(x => x.L10Recurrence.Id == recurrenceId);
			}

			var shareingIds = shareingIdsQ.Select(x => x.User.Id).List<long>().Distinct().ToList();
			return (new[] { myUserId }).Union(shareingIds).Distinct().ToList();
		}

		public static AngularPeopleAnalyzer GetVisiblePeopleAnalyzers(UserOrganizationModel caller, long userId, long? recurrenceId=null,DateRange range = null) {
			List<long> allVisibleUsers = new List<long>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);
					allVisibleUsers = GetUsersWhosePeopleAnalyzersICanSee(s, perms, userId, recurrenceId);
				}
			}

			var pas = allVisibleUsers.Select(id => GetPeopleAnalyzer(caller, id, range)).ToList();

			var flat = new AngularPeopleAnalyzer();
			var rows= new List<AngularPeopleAnalyzerRow>();
			var response= new List<AngularPeopleAnalyzerResponse>();
			var values= new List<PeopleAnalyzerValue>();
			var containers= new List<AngularSurveyContainer>();
			var lockins= new List<AngularLockedIn>();

			foreach (var p in pas) {
				flat.DateRange = p.DateRange;
				rows.AddRange(p.Rows);
				response.AddRange(p.Responses);
				values.AddRange(p.Values);
				values = values.Distinct(x=>x.Key).ToList();
				containers.AddRange(p.SurveyContainers);
				containers = containers.Distinct(x => x.Key).ToList();
				lockins.AddRange(p.LockedIn);
			}
			flat.Rows = rows.OrderBy(x => x.About.PrettyString).ToList();
			flat.Responses = response.OrderBy(x => x.IssueDate).ToList();
			flat.Values = values;
			flat.SurveyContainers = containers.OrderBy(x=>x.IssueDate).ToList();
			flat.LockedIn = lockins;

			return flat;
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
					perms.ViewPeopleAnalyzer(userId);
					var myNodes = AccountabilityAccessor.GetNodesForUser(s, perms, userId);
#pragma warning disable CS0618 // Type or member is obsolete
					var acNodeChildrenModels = myNodes.SelectMany(node => DeepAccessor.GetChildrenAndSelfModels(s, caller, node.Id, allowAnyFromSameOrg:true)).ToList();
					if (!includeSelf) {
						acNodeChildrenModels = acNodeChildrenModels.Where(x => !myNodes.Any(y => y.Id == x.Id)).ToList();
					}
#pragma warning restore CS0618 // Type or member is obsolete

					SurveyItem item = null;
					var allSurveyNodeItems = s.QueryOver<SurveyUserNode>().Where(x => x.DeleteTime == null)
												.WhereRestrictionOn(x => x.User.Id)
												.IsIn(acNodeChildrenModels.Select(x => x.UserId).Distinct().Where(x => x != null).ToArray())
												.WhereRestrictionOn(x => x.AccountabilityNodeId)
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

					//Should be more of these which produce more accNodeResults...
					var availableSurveyNodes = allSurveyNodeItems.Where(n => acNodeChildrenModels.Any(x => x.Id == n.AccountabilityNodeId && x.UserId == n.UserOrganizationId)).ToList();
					var surveyNodeIds = availableSurveyNodes.Select(x => x.ModelId).ToList();

					var accountabiliyNodeResults = s.QueryOver<SurveyResponse>()
						.Where(x => x.SurveyType == SurveyType.QuarterlyConversation && x.OrgId == caller.Organization.Id && x.About.ModelType == ForModel.GetModelType<SurveyUserNode>() && x.DeleteTime == null && x.Answer != null)
						.Where(range.Filter<SurveyResponse>())
						.WhereRestrictionOn(x => x.About.ModelId).IsIn(surveyNodeIds.ToArray())
						.List().ToList();

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
					var discriminatorToSun = availableSurveyNodes.ToDefaultDictionary(x => x.AccountabilityNodeId + "_" + x.UserOrganizationId, x => x, x => null);

					var sunToAccNode = availableSurveyNodes.ToDefaultDictionary(x => x.ToKey, x => x.AccountabilityNode, x => null);

					//Build response rows
					var allowedQuestionIdentifiers = new[] {
						SurveyQuestionIdentifier.GWC,
						SurveyQuestionIdentifier.Value
					};
					var groupByAbout_filtered = accountabiliyNodeResults
													.Where(x => x._ItemFormat != null && allowedQuestionIdentifiers.Contains(x._ItemFormat.GetQuestionIdentifier()))
													.GroupBy(x => sunToDiscriminator[x.About.ToKey()]);

					foreach (var row in groupByAbout_filtered) {
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
						//var plusMinus = new Func<string, string>(x => {
						//	switch (x) {
						//		case "often":
						//			return "+";
						//		case "sometimes":
						//			return "+/–";
						//		case "not-often":
						//			return "–";
						//		default:
						//			return null;
						//	}
						//});
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
						{ "often", TransformValueAnswer["often"] },{ "sometimes", TransformValueAnswer["sometimes"] },  { "not-often",TransformValueAnswer["not-often"] },
						{ "done", "done" }, { "not-done", "not done" },
						{ "yes", "Y" }, { "no", "N" }
					};



					var surveyIssueDateLookup = surveys.ToDictionary(x => x.Id, x => x.GetIssueDate());
					var surveyLookup = surveys.ToDictionary(x => x.Id, x => x);
					var surveyItemLookup = items.ToDictionary(x => x.Id, x => x);
					var surveyItemFormatLookup = formats.ToDictionary(x => x.Id, x => x);

					var responses = new List<AngularPeopleAnalyzerResponse>();
					var accountabilityNodeResults_filtered = accountabiliyNodeResults.Where(x => allowedQuestionIdentifiers.Contains(x._ItemFormat.GetQuestionIdentifier()));

					foreach (var result in accountabilityNodeResults_filtered) {
						if (!surveyContainers.Any(x => x.Id == result.SurveyContainerId))
							continue;
						if (!surveys.Any(x => x.Id == result.SurveyId))
							continue;

						var answerDate = result.CompleteTime;

						var survey = surveyLookup[result.SurveyId];
						var issueDate = survey.GetIssueDate(); //surveyIssueDateLookup[result.SurveyId];
															   //var lockedIn = survey.LockedIn;
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
					var allLockedIn = new List<AngularLockedIn>();
					var userOrgSurveyLockinLookup = new DefaultDictionary<string, bool>(x => false);
					foreach (var survey in surveys) {
						userOrgSurveyLockinLookup[survey.By.ToKey()] = survey.LockedIn;
					}

					var userToNodes = new DefaultDictionary<long, List<ForModel>>(x => new List<ForModel>());

					foreach (var sun in allSurveyNodeItems) {
						userToNodes[sun.UserOrganizationId].Add(ForModel.Create<AccountabilityNode>(sun.AccountabilityNodeId));
						userToNodes[sun.UserOrganizationId] = userToNodes[sun.UserOrganizationId].Distinct(x => x.ToKey()).ToList();
					}

					foreach (var survey in surveys) {
						if (survey.By.Is<UserOrganizationModel>()) {

							var byUserOrgId = survey.By.ModelId;
							var lockedIn = survey.LockedIn;  //userOrgSurveyLockinLookup[ForModel.Create<UserOrganizationModel>(sun.UserOrganizationId).ToKey()];
							var surveyContainerId = survey.SurveyContainerId;

							foreach (var node in userToNodes[byUserOrgId]) {
								allLockedIn.Add(new AngularLockedIn() {
									By = new AngularForModel(node),
									LockedIn = lockedIn,
									SurveyContainerId = surveyContainerId,
									IssueDate = survey.GetIssueDate()
								});

							}
						}
					}
					//var allLockedIn = surveys.Select(x => new AngularLockedIn() {
					//	By = new AngularForModel(x.By),
					//	LockedIn = x.LockedIn,
					//	SurveyContainerId = x.SurveyContainerId,
					//});


					analyzer.Rows = rows;
					analyzer.Responses = responses;
					analyzer.Values = values.Select(x => new PeopleAnalyzerValue(surveyItemLookup[x.GetItemId()].GetSource()));
					analyzer.LockedIn = allLockedIn;

					analyzer.SurveyContainers = surveyContainers.Select(x => new AngularSurveyContainer(x, false, null));

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
