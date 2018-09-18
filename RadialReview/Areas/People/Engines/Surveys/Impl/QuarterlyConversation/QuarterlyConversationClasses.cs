using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Interfaces;
using RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections;
using RadialReview.Models.Accountability;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation {
	public class QuarterlyConversationInitializer : ISurveyInitializer {

		public IForModel CreatedBy { get; set; }
		public String Name { get; set; }
		public DateTime DueDate { get; set; }
		public DateRange QuarterRange { get; set; }
		public long OrgId { get; set; }

		public QuarterlyConversationInitializer(IForModel createdBy, string name, long orgId, DateRange quarterRange, DateTime dueDate) {
			CreatedBy = createdBy;
			DueDate = dueDate;
			Name = name;
			OrgId = orgId;
			QuarterRange = quarterRange;
		}

		private IEnumerable<ISectionInitializer> _sectionBuilders() {
			yield return new ValueSection();
			yield return new RoleSection();
			yield return new RockSection(QuarterRange);// new DateRange(QuarterRange.AddDays(-7),QuarterRange.AddDays(65)));
			//yield return new RockCompletionSection();
			yield return new LeadershipAssessmentSection(false);
			yield return new ManagementAssessmentSection(false);
			yield return new GeneralCommentsSection();
		}

		#region Standard Customization
		public ISurveyContainer BuildSurveyContainer() {
			return new SurveyContainer(CreatedBy, Name, OrgId, SurveyType.QuarterlyConversation, null, DueDate);
		}

		public IEnumerable<ISectionInitializer> GetAllPossibleSectionBuilders(IEnumerable<IByAbout> byAbouts) {
			return _sectionBuilders();
		}

		public IEnumerable<ISectionInitializer> GetSectionBuilders(ISectionInitializerData data) {
			return _sectionBuilders();
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do.

			var nodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<AccountabilityNode>()).Select(x => x.ModelId).ToArray();
			if (nodeIds.Any()) {
				data.Lookup.AddList(data.Session.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(nodeIds).Future());
			}

			var surveyUserIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<SurveyUserNode>()).Select(x => x.ModelId).ToArray();
			if (surveyUserIds.Any()) {
				data.Lookup.AddList(data.Session.QueryOver<SurveyUserNode>().WhereRestrictionOn(x => x.Id).IsIn(surveyUserIds).Future());
			}

		}

		public ISurvey InitializeSurvey(ISurveyInitializerData data) {
			var name = data.About.ToPrettyString();
			if (name == null && data.About.ModelType == ForModel.GetModelType<AccountabilityNode>()) {
				name = data.Lookup.GetList<AccountabilityNode>().FirstOrDefault(x => x.Id == data.About.ModelId).NotNull(x => x.User.GetNameAndTitle());
			} else if (name == null && data.About.ModelType == ForModel.GetModelType<SurveyUserNode>()) {
				name = data.Lookup.GetList<SurveyUserNode>().FirstOrDefault(x => x.Id == data.About.ModelId).NotNull(x => x.ToPrettyString());
			} // This is only for the name, name can be null
			  /* else if (name==null) {
				  throw new ArgumentOutOfRangeException(data.About.ModelType);
			  }*/
			return new Survey(name, DueDate, data);
		}

		#endregion
	}

}
