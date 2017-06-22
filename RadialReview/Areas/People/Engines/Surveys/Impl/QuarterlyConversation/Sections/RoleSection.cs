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

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {
	public class RoleSection : ISectionInitializer {
		public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
			yield return new RoleListItem();
			yield return new RoleResponseItem();
		}

		public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
			var modelType = data.Survey.GetAbout().ModelType;

			var genComments = new TextAreaItemIntializer("General Comments");

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
			}

			return new List<IItemInitializer>() {
				new TextItemIntializer("No roles.",true),
				genComments
			};
		}

		public ISection InitializeSection(ISectionInitializerData data) {
			return new SurveySection(data, "Roles", SurveySectionType.Roles);
		}

		public void Prelookup(IInitializerLookupData data) {
			data.Lookup.Add("RoleQuery", RoleAccessor.GetRolesForOrganization_Unsafe(data.Session, data.OrgId));
			var nodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<AccountabilityNode>()).Select(x => x.ModelId).ToArray();
			if (nodeIds.Any()) {
				data.Lookup.Add("Nodes", data.Session.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(nodeIds).Future());
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

			return data.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(data, options, new KV("gwc",Value.ToLower())), Value);
		}

		public bool HasResponse(IResponseInitializerCtx data) {
			return true;
		}

		public IItem InitializeItem(IItemInitializerData data) {
			return new SurveyItem(data, Title, null);
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
			return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateText(ctx));
		}

		public bool HasResponse(IResponseInitializerCtx data) {
			return false;
		}

		public IItem InitializeItem(IItemInitializerData data) {
			return new SurveyItem(data, RoleDetails.Role.Role, ForModel.Create(RoleDetails.Role));
		}

		public IResponse InitializeResponse(IResponseInitializerCtx data, IItemFormat format) {
			throw new NotImplementedException();
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do
		}
	}
}