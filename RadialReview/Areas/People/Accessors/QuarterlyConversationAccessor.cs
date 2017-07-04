using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.People.Angular;
using RadialReview.Areas.People.Engines.Surveys;
using RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Events;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Transformers;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
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
		
#pragma warning disable CS0618 // Type or member is obsolete
		public static IEnumerable<IByAbout> AvailableByAbouts(UserOrganizationModel caller, bool includeSelf = false) {
			var nodes = AccountabilityAccessor.GetNodesForUser(caller, caller.Id);
			var possible = new List<IByAbout>();
			foreach (var node in nodes) {
				var reports = DeepAccessor.GetDirectReportsAndSelf(caller, node.Id);
				foreach (var report in reports) {
					possible.Add(new ByAbout(caller, report));
					if (includeSelf) {
						possible.Add(new ByAbout(report, report));
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
					Console.WriteLine("\tby:"+i.GetBy().ToKey()+"  about:"+i.GetAbout().ToKey());
				}
				throw new PermissionsException("Could not create Quarterly Conversation. You cannot view these items.");
			}

			long containerId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CreateSurveyContainer(caller.Organization.Id);

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
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);
					var nodes = AccountabilityAccessor.GetNodesForUser(s, perms, userId);
#pragma warning disable CS0618 // Type or member is obsolete
					var childrens = nodes.SelectMany(node => DeepAccessor.GetChildrenAndSelf(s, caller, node.Id));
#pragma warning restore CS0618 // Type or member is obsolete

					SurveyItem item = null;
					var accountabiliyNodeResults = s.QueryOver<SurveyResponse>()
						.Where(x => x.SurveyType == SurveyType.QuarterlyConversation && x.OrgId == caller.Organization.Id && x.About.ModelType == ForModel.GetModelType<AccountabilityNode>())
						.Where(range.Filter<SurveyResponse>())
						.WhereRestrictionOn(x => x.About.ModelId).IsIn(childrens.ToArray())
						.List().ToList();

					var formats = s.QueryOver<SurveyItemFormat>().WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemFormatId).Distinct().ToArray()).Future();
					var items = s.QueryOver<SurveyItem>().WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemId).Distinct().ToArray()).Future();
					var users = s.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(childrens.Distinct().ToArray()).Fetch(x => x.User).Eager.Future();
					//var users = s.QueryOver<Angular>().WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemId).Distinct().ToArray()).Future();

					var formatsLu = formats.ToDefaultDictionary(x => x.Id, x => x, x => null);
					var itemsLu = items.ToDefaultDictionary(x => x.Id, x => x, x => null);
					var userLu = users.ToDefaultDictionary(x => ForModel.From(x), x => x.User.NotNull(y => y.GetName()), x => "n/a");

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

						var arow = new AngularPeopleAnalyzerRow(row.Key.ModelId);

						arow.Name = userLu[row.Key];

						arow.Value = new Dictionary<long, string>();

						arow.Get = yesNo(get.NotNull(x => x.Answer));
						arow.Want = yesNo(want.NotNull(x => x.Answer));
						arow.Capacity = yesNo(capacity.NotNull(x => x.Answer));

						var values = answersAbout.Where(x => x._Item.NotNull(y => y.GetSource().ModelType) == ForModel.GetModelType<CompanyValueModel>());
						var valueIds = values.GroupBy(x => x._Item.NotNull(y => y.GetSource().ModelId));

						var avalues = new List<PeopleAnalyzerValue>();

						foreach (var value in valueIds) {
							var v = value.OrderByDescending(x => x.CompleteTime ?? DateTime.MinValue).FirstOrDefault();
							if (v != null) {
								var vid = v._Item.GetSource().ModelId;
								arow.Value[vid] = plusMinus(v.Answer);
								allValueIds.Add(vid);
							}
						}
						rows.Add(arow);
					}

					var dict = new Dictionary<long, string>();
					foreach (var row in accountabiliyNodeResults.Where(x => x._Item.NotNull(y => y.GetSource().ModelType) == ForModel.GetModelType<CompanyValueModel>())) {
						dict.GetOrAddDefault(row._Item.GetSource().ModelId, x => row._Item.GetName());
					}

					analyzer.Rows = rows;
					analyzer.Values = dict.Select(x => new PeopleAnalyzerValue() { ValueId = x.Key, Value = x.Value });

					return analyzer;

				}
			}
		}
	}
}
