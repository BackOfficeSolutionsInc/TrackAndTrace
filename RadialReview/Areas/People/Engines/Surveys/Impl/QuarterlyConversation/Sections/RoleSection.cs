using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models;
using RadialReview.Accessors;
using static RadialReview.Accessors.RoleAccessor;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Accountability;
using static RadialReview.Accessors.RoleAccessor.RoleLinksQuery;
using RadialReview.Models.Enums;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {
	public class RoleSection : ISectionInitializer {

		public static String RoleCommentHeading = "5 Roles/GWC Comments";

		public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
#pragma warning disable CS0618 // Type or member is obsolete
			yield return new RoleListItem();
			yield return new RoleResponseItem();
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
			//only ask if they are not our manager
			if (data.SurveyContainer.GetSurveyType() == SurveyType.QuarterlyConversation && data.About.Is<SurveyUserNode>()) {

				if (data.SurveyContainer.GetCreator().ToKey() == ((SurveyUserNode)data.About).User.ToKey())
					return new List<IItemInitializer>();

				if ((data.About as SurveyUserNode)._Relationship[data.By.ToKey()] == AboutType.Manager) 
					return new List<IItemInitializer>();
				
			}

			var modelType = data.Survey.GetAbout().ModelType;

			var genComments = new TextAreaItemIntializer(RoleCommentHeading, SurveyQuestionIdentifier.GeneralComment);

			if (modelType == ForModel.GetModelType<UserOrganizationModel>()) {
				var query = data.Lookup.Get<RoleLinksQuery>("RoleQuery");
				if (query != null) {
					var roles = query.GetRoleDetailsForUser(data.Survey.GetAbout().ModelId);

					var roleItems = roles.Select(x => (IItemInitializer)new RoleListItem(x));

					if (roleItems.Any()) {
						var roleReponses = new[] {
							new RoleResponseItem("Gets it",             "get"   ),
							new RoleResponseItem("Wants it",            "want"  ),
							new RoleResponseItem("Capacity to do it",   "cap"   ),
						};
						return roleItems.Union(roleReponses).Union(genComments.AsList());
					}
				}
			} else if (modelType == ForModel.GetModelType<AccountabilityNode>()) {
				var query = data.Lookup.Get<RoleLinksQuery>("RoleQuery");
				var nodes = data.Lookup.Get<IEnumerable<AccountabilityNode>>("Nodes").ToDefaultDictionary(x => x.Id, x => x, x => null);
				if (query != null) {
					var node = nodes[data.Survey.GetAbout().ModelId];
					if (node != null) {
						var roles = query.GetRoleDetailsForNode(node);
						var roleItems = roles.Select(x => (IItemInitializer)new RoleListItem(x));

						if (roleItems.Any()) {
							var roleReponses = new[] {
								new RoleResponseItem("Gets it",             "get"   ),
								new RoleResponseItem("Wants it",            "want"  ),
								new RoleResponseItem("Capacity to do it",   "cap"   ),
							};
							return roleItems.Union(roleReponses).Union(genComments.AsList());
						}
					}
				}
			} else if (modelType == ForModel.GetModelType<SurveyUserNode>()) {
				var query = data.Lookup.Get<RoleLinksQuery>("RoleQuery");
				var surveyUserNodes = data.Lookup.GetList<SurveyUserNode>().ToDefaultDictionary(x => x.Id, x => x, x => null);
				if (query != null) {
					var suNode = surveyUserNodes[data.Survey.GetAbout().ModelId];
					if (suNode != null) {
						IEnumerable<RoleDetails> roles;
						if (suNode.AccountabilityNode != null) {
							roles = query.GetRoleDetailsForNode(suNode.AccountabilityNode);
						} else if (suNode.User != null) {
							roles = query.GetRoleDetailsForUser(suNode.UserOrganizationId);
						} else {
							throw new Exception("unhandled");
						}
						var roleItems = roles.Select(x => (IItemInitializer)new RoleListItem(x));

						if (roleItems.Any()) {
							var roleReponses = new[] {
								new RoleResponseItem("Gets it",             "get"   ),
								new RoleResponseItem("Wants it",            "want"  ),
								new RoleResponseItem("Capacity to do it",   "cap"   ),
							};
							return roleItems.Union(roleReponses).Union(genComments.AsList());
						}
					}
				}
			} else {
				throw new ArgumentOutOfRangeException(modelType);
			}

			return new List<IItemInitializer>() {
				new TextItemIntializer("No roles.",true),
				genComments
			};
		}

		public ISection InitializeSection(ISectionInitializerData data) {
			return new SurveySection(data, "Roles", SurveySectionType.Roles,"mk-roles");
		}

		public void Prelookup(IInitializerLookupData data) {
			data.Lookup.Add("RoleQuery", RoleAccessor.GetRolesForOrganization_Unsafe(data.Session, data.OrgId));

			var nodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<AccountabilityNode>()).Select(x => x.ModelId).ToArray();
			if (nodeIds.Any()) {
				data.Lookup.Add("Nodes", data.Session.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(nodeIds).Future());
			}

			var surveyUserNodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<SurveyUserNode>()).Select(x => x.ModelId).ToArray();
			if (surveyUserNodeIds.Any()) {
				data.Lookup.AddList(
					data.Session.QueryOver<SurveyUserNode>()
						.WhereRestrictionOn(x => x.Id).IsIn(surveyUserNodeIds)
						.Fetch(x => x.AccountabilityNode).Eager
						.Fetch(x => x.User).Eager
						.Future()
				);
			}
		}
	}

	public class RoleResponseItem : IItemInitializer {
		public String Title { get; set; }
		public String Value { get; set; }

		public RoleResponseItem(string title, string value) {
			Title = title;
			Value = value;
		}
		[Obsolete("Use other constructor.")]
		public RoleResponseItem() {
		}

		public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx data) {
			var options = new Dictionary<string, string>() {
				{"yes", "Yes" },
				{"no",  "No" },
			};

			return data.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(data,SurveyQuestionIdentifier.GWC, options, new KV("gwc",Value)), Value);
		}

		public bool HasResponse(IResponseInitializerCtx data) {
			return true;
		}

		public IItem InitializeItem(IItemInitializerData data) {
			return new SurveyItem(data, Title, null,"role-"+Value);
		}

		public IResponse InitializeResponse(IResponseInitializerCtx ctx, IItemFormat format) {
			return new SurveyResponse(ctx, format);
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do
		}
	}

	public class RoleListItem : IItemInitializer {
		private RoleLinksQuery.RoleDetails RoleDetails;

		[Obsolete("Use other constructor")]
		public RoleListItem() {
		}

		public RoleListItem(RoleLinksQuery.RoleDetails x) {
			RoleDetails = x;
		}

		public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
			return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateText(ctx,SurveyQuestionIdentifier.Role));
		}

		public bool HasResponse(IResponseInitializerCtx data) {
			return false;
		}

		public IItem InitializeItem(IItemInitializerData data) {
			var forModel = ForModel.Create(RoleDetails.Role);
			return new SurveyItem(data, RoleDetails.Role.Role, forModel, forModel.ToKey());
		}

		public IResponse InitializeResponse(IResponseInitializerCtx data, IItemFormat format) {
			throw new NotImplementedException();
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do
		}
	}
}