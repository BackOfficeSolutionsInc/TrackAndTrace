using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Accountability;
using RadialReview.Models.Enums;
using RadialReview.Utilities.Extensions;
using RadialReview.Utilities.DataTypes;
using NHibernate.Envers;
using NHibernate.Envers.Query;
using RadialReview.Utilities;
using NHibernate.Envers.Query.Criteria;
using RadialReview.Utilities.NHibernate;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {

	public class RockSection : ISectionInitializer {
		//private IEnumerable<RockModel> rockLookup = new List<RockModel>();
		public static String RockCommentHeading = "Rock Quality/Comments";
		public DateRange SearchRange { get; set; }


		public const string AUDIT_ROCKS = "AuditRocks";

		public class LookupMethods {
			public LookupRocks LookupRocks { get; set; }
			public LookupAuditRocks LookupAuditRocks { get; set; }
			public LookupAccountabilityNodes LookupAccountabilityNodes { get; set; }
			public LookupSurveyUserNodes LookupSurveyUserNodes { get; set; }
		}


		/// <summary>
		/// Pass in the full range of the quarter. It automatically trims the range.
		/// 
		/// 
		/// It uses an inner range to prevent things deleted shortly after the beginning of the quarter
		/// 
		///     start = 7 days after qtr start
		///     end = 21 days before qtr end
		///     
		///                 +==============QTR==============+
		///                 |                               |
		/// -----------*-*-*-*-*----|=====Range=====|----------------------
		///            ^ Rocks Created
		/// 
		/// 
		///    -------------Created-----------------+
		///                                         |
		///                         o=====Range=====o
		///                         |
		///                         +-----------------------Deleted--------------------------->
		///                                         
		/// </summary>
		public RockSection(DateRange qtrRange, double startPaddingPercent = .1, double endPaddingPercent = .23, LookupMethods replacementLookupMethods =null) {

			//var replacementLookupMethods = new LookupMethods();
			replacementLookupMethods = replacementLookupMethods ?? new LookupMethods();
			replacementLookupMethods.LookupRocks = replacementLookupMethods.LookupRocks ?? DB_LookupRocks;
			replacementLookupMethods.LookupAuditRocks = replacementLookupMethods.LookupAuditRocks ?? DB_LookupAuditRocks;
			replacementLookupMethods.LookupAccountabilityNodes = replacementLookupMethods.LookupAccountabilityNodes ?? DB_LookupAccountabilityNodes;
			replacementLookupMethods.LookupSurveyUserNodes = replacementLookupMethods.LookupSurveyUserNodes ?? DB_LookupSurveyUserNodes;
			lookupMethods = replacementLookupMethods;

			var ts = qtrRange.ToTimespan();
			var start = qtrRange.StartTime.AddDays(ts.TotalDays * startPaddingPercent);
			var end = qtrRange.EndTime.AddDays(-ts.TotalDays * endPaddingPercent);

			SearchRange = new DateRange(start, end);
		}

		public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
			return new[] { new RockItems(null, null) };
		}

		public delegate IEnumerable<RockModel> LookupRocks(IInitializerLookupData data);
		public delegate IEnumerable<RockModel> LookupAuditRocks(IInitializerLookupData data);
		public delegate IEnumerable<AccountabilityNode> LookupAccountabilityNodes(IInitializerLookupData data, long[] nodeIds);
		public delegate IEnumerable<SurveyUserNode> LookupSurveyUserNodes(IInitializerLookupData data, long[] surveyUserNodeIds);

		private LookupMethods lookupMethods;

		public void Prelookup(IInitializerLookupData data) {
			//All rocks
			IEnumerable<RockModel> rocks = lookupMethods.LookupRocks(data);
			IEnumerable<RockModel> auditRocks = lookupMethods.LookupAuditRocks(data);
			data.Lookup.Add(AUDIT_ROCKS, auditRocks.ToList());
			data.Lookup.AddList(rocks);

			//All nodes
			var nodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() })
									.Where(x => x.Is<AccountabilityNode>())
									.Select(x => x.ModelId)
									.ToArray();
			if (nodeIds.Any()) {
				data.Lookup.AddList(lookupMethods.LookupAccountabilityNodes(data, nodeIds));
			}

			//All survey user-nodes
			var surveyUserNodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() })
												.Where(x => x.Is<SurveyUserNode>())
												.Select(x => x.ModelId)
												.ToArray();
			if (surveyUserNodeIds.Any()) {
				data.Lookup.AddList(lookupMethods.LookupSurveyUserNodes(data, surveyUserNodeIds));
			}
		}

		#region Db Lookup Methods
		private static IEnumerable<SurveyUserNode> DB_LookupSurveyUserNodes(IInitializerLookupData data, long[] surveyUserNodeIds) {
			return data.Session.QueryOver<SurveyUserNode>()
									.WhereRestrictionOn(x => x.Id).IsIn(surveyUserNodeIds)
									.Fetch(x => x.AccountabilityNode).Eager
									.Fetch(x => x.User).Eager
									.Future();
		}

		private static IEnumerable<AccountabilityNode> DB_LookupAccountabilityNodes(IInitializerLookupData data, long[] nodeIds) {
			return data.Session.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(nodeIds).Future();
		}
		private IEnumerable<RockModel> DB_LookupAuditRocks(IInitializerLookupData data) {
			var audit = data.Session.AuditReader();
			var orgProp = AuditEntity.Property(HibernateSession.Names.ColumnName<RockModel>(x => x.OrganizationId));
			//var auditRocks = audit.CreateQuery()//.ForEntitiesAtRevision<RockModel>(audit.GetRevisionNumberForDate(SearchRange.EndTime))
			//					.ForRevisionsOfEntity(typeof(RockModel), true, false)
			//					.Add(SearchRange.FilterAudit<RockModel>())
			//					.Add(orgProp.Eq(data.OrgId))
			//					.GetResultList<RockModel>()
			//					.ToList();

			
			try {
				var revisionId = audit.GetRevisionNumberForDate(SearchRange.EndTime);
				var auditRocks = audit.CreateQuery().ForEntitiesAtRevision<RockModel>(revisionId)								
								.Add(SearchRange.FilterAudit<RockModel>())
								.Add(orgProp.Eq(data.OrgId))
								.Results().ToList();
				return auditRocks;
			} catch (Exception e) {
				return new List<RockModel>();
			}

		}
		private IEnumerable<RockModel> DB_LookupRocks(IInitializerLookupData data) {
			var rocks = data.Session.QueryOver<RockModel>()
							//.Where(x=>x.CreateTime>=SearchRange.StartTime && x.CreateTime<=SearchRange.EndTime)
							.Where(SearchRange.Filter<RockModel>())
							.Where(x => x.OrganizationId == data.OrgId)
							.Future();
			return rocks;
		}
		#endregion

		public ISection InitializeSection(ISectionInitializerData data) {
			return new SurveySection(data, "Rocks", SurveySectionType.Rocks, "mk-rocks");
		}

		private List<IItemInitializer> GetRocksForAccountabilityNode(IItemInitializerData data, AccountabilityNode about) {
			var accNodeLookup = data.Lookup.GetList<AccountabilityNode>();

			var items = new List<IItemInitializer>();
			var node = accNodeLookup.FirstOrDefault(x => x.Id == about.ModelId);
			if (node != null) {
				items.AddRange(GetRockForUserId(data, node.UserId));
			}
			return items;
		}

		private static IEnumerable<IItemInitializer> GetRockForUserId(IItemInitializerData data, long? userId) {
			var rockLookup = data.Lookup.GetList<RockModel>();
			var audits = data.Lookup.GetOrAdd(AUDIT_ROCKS, x => new List<RockModel>());
			return rockLookup.Where(x => x.ForUserId == userId).Select(x => new RockItems(x, audits.FirstOrDefault(y => y.Id == x.Id)));
		}

		public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {

			//var dict = data.Lookup.GetOrAdd("RockSectionAlreadyGenerated", (_str) => new DefaultDictionary<string, bool>(x => false));
			//var byAboutKey = data.By.ToKey() + "-" + data.About.ToKey();
			//if (data.About.Is<SurveyUserNode>()) {
			//    byAboutKey = data.By.ToKey() + "-" + ((SurveyUserNode)data.About).User.ToKey();
			//}
			//var alreadyGenerated = dict[byAboutKey];

			if (data.FirstSeenByAbout()) {
				//dict[byAboutKey] = true;

				//only ask if they are not our manager
				if (data.SurveyContainer.GetSurveyType() == SurveyType.QuarterlyConversation && data.About.Is<SurveyUserNode>()) {

					if (data.SurveyContainer.GetCreator().ToKey() == ((SurveyUserNode)data.About).User.ToKey())
						return new List<IItemInitializer>();

					if ((data.About as SurveyUserNode)._Relationship[data.By.ToKey()] == AboutType.Manager)
						return new List<IItemInitializer>();

				}

				var items = new List<IItemInitializer>();
				var about = data.Survey.GetAbout();
				if (about.ModelType == ForModel.GetModelType<UserOrganizationModel>()) {
					//var rockLookup = data.Lookup.GetList<RockModel>();
					//var rocks = rockLookup.Where(x => x.ForUserId == about.ModelId).Select(x => new RockItems(x,));
					var rocks = GetRockForUserId(data, about.ModelId);
					items.AddRange(rocks);
				} else if (about.ModelType == ForModel.GetModelType<AccountabilityNode>()) {
					items.AddRange(GetRocksForAccountabilityNode(data, (AccountabilityNode)about));
				} else if (about.ModelType == ForModel.GetModelType<SurveyUserNode>()) {
					var node = data.Lookup.GetList<SurveyUserNode>().First(x => x.Id == about.ModelId).AccountabilityNode;
					items.AddRange(GetRockForUserId(data, node.UserId));
				}
				if (!items.Any())
					items.Add(new TextItemIntializer("No rocks.", true));
				items.Add(new InputItemIntializer(RockCommentHeading, SurveyQuestionIdentifier.GeneralComment));
				return items;
			}
			return new IItemInitializer[] { };
		}
	}

	public class RockItems : IItemInitializer {
		private RockModel Rock;
		private RockModel Audit;

		public RockItems(RockModel rock, RockModel audit) {
			Rock = rock;
			Audit = audit;
		}

		public IItem InitializeItem(IItemInitializerData data) {
			var forModel = ForModel.Create(Rock);

			var name = Rock.Rock;
			try {
				var newName = Audit.NotNull(x => x.Name);
				if (name != newName && newName != null)
					name = newName;
			} catch (Exception e) {
				//no revision available
				int a = 0;
			}
			return new SurveyItem(data, name, forModel, forModel.ToKey());
		}

		public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
			var options = new Dictionary<string, string>() {
					{ "done","Done" },
					{ "not-done","Not Done" },
			};
			return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(ctx, SurveyQuestionIdentifier.Rock, options));
		}

		public bool HasResponse(IResponseInitializerCtx data) {
			return true;
		}

		public IResponse InitializeResponse(IResponseInitializerCtx data, IItemFormat format) {
			return new SurveyResponse(data, format);
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do
		}
	}
}
