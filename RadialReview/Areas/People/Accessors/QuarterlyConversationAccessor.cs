using RadialReview.Accessors;
using RadialReview.Areas.People.Angular;
using RadialReview.Areas.People.Models.Survey;
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

		//    public static List<QC> GetMyQuarterlyConversations(UserOrganizationModel caller, long userId) {
		//        throw new NotImplementedException();
		//    }


		//    public static AngularQuarterlyConversation GetQuarterlyConversation(UserOrganizationModel caller, long qcId,bool populateAnsers=false) {
		//        throw new NotImplementedException();
		//    }

		//    public static List<QC> GetQuestions(UserOrganizationModel caller, long qcId) {
		//        throw new NotImplementedException();
		//    }

		//    public static List<QC> GetQuestionsByUser(UserOrganizationModel caller, long qcId, long byUserId) {
		//        throw new NotImplementedException();
		//    }

		//    public static List<QC> GetQuestionsAboutUser(UserOrganizationModel caller, long qcId, long aboutUserId) {
		//        throw new NotImplementedException();
		//    }

		public static AngularPeopleAnalyzer GetPeopleAnalyzer(UserOrganizationModel caller, long userId,DateRange range = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);
					var nodes = AccountabilityAccessor.GetNodesForUser(s, perms, userId);
					var childrens =nodes.SelectMany(node => DeepAccessor.GetChildrenAndSelf(s, caller, node.Id));

					SurveyItem item=null;

					var accountabiliyNodeResults = s.QueryOver<SurveyResponse>()
						.Where(x => x.SurveyType == SurveyType.QuarterlyConversation && x.OrgId == caller.Organization.Id && x.About.ModelType == ForModel.GetModelType<AccountabilityNode>())
						.Where(range.Filter<SurveyResponse>())
						.WhereRestrictionOn(x=>x.About.ModelId).IsIn(childrens.ToArray())
						.List().ToList();

					var formats = s.QueryOver<SurveyItemFormat>().WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemFormatId).Distinct().ToArray()).Future();
					var items = s.QueryOver<SurveyItem>().WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemId).Distinct().ToArray()).Future();
					var users = s.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(childrens.Distinct().ToArray()).Fetch(x=>x.User).Eager.Future();
					//var users = s.QueryOver<Angular>().WhereRestrictionOn(x => x.Id).IsIn(accountabiliyNodeResults.Select(x => x.ItemId).Distinct().ToArray()).Future();

					var formatsLu = formats.ToDefaultDictionary(x => x.Id, x => x, x => null);
					var itemsLu = items.ToDefaultDictionary(x => x.Id, x => x, x => null);
					var userLu = users.ToDefaultDictionary(x => ForModel.From(x), x => x.User.NotNull(y=>y.GetName()), x => "n/a");

					foreach (var result in accountabiliyNodeResults) {
						result._Item = itemsLu[result.ItemId];
						result._ItemFormat = formatsLu[result.ItemFormatId];
					}

					var analyzer =new AngularPeopleAnalyzer() {};
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

						arow.Get = yesNo(get.NotNull(x=>x.Answer));
						arow.Want = yesNo(want.NotNull(x => x.Answer));
						arow.Capacity = yesNo(capacity.NotNull(x => x.Answer));

						var values = answersAbout.Where(x => x._Item.NotNull(y=>y.GetSource().ModelType) == ForModel.GetModelType<CompanyValueModel>());
						var valueIds = values.GroupBy(x => x._Item.NotNull(y => y.GetSource().ModelId));
						
						var avalues = new List<PeopleAnalyzerValue>();

						foreach (var value in valueIds) {
							var v =value.OrderByDescending(x => x.CompleteTime ?? DateTime.MinValue).FirstOrDefault();
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
					analyzer.Values = dict.Select(x =>new PeopleAnalyzerValue() {ValueId = x.Key,Value = x.Value});

					return analyzer;

				}
			}
		}
	}
}
