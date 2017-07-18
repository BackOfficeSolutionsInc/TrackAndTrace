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

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation {
    public class QuarterlyConversationInitializer : ISurveyInitializer {

		public IForModel CreatedBy { get; set; }
		//public IForModel By { get; set; }
		public String Name { get; set; }
		public DateTime DueDate { get; set; }
		public long OrgId { get; set; }

        public QuarterlyConversationInitializer(IForModel createdBy/*, IForModel by*/, string name, long orgId,DateTime dueDate) {
			CreatedBy = createdBy;
			//By = by;
			DueDate = dueDate;
            Name = name;
            OrgId = orgId;
        }

        private IEnumerable<ISectionInitializer> _sectionBuilders() {
            yield return new ValueSection();
            yield return new RoleSection();
			yield return new RockSection();
			yield return new GeneralCommentsSection();
		}

        #region Standard Customization
        public ISurveyContainer BuildSurveyContainer() {
            return new SurveyContainer(CreatedBy, Name, OrgId, SurveyType.QuarterlyConversation, null,DueDate);
        }

        public IEnumerable<ISectionInitializer> GetAllPossibleSectionBuilders(IEnumerable<IByAbout> byAbouts) {
            return _sectionBuilders();
        }
        
        public void Prelookup(IInitializerLookupData data) {
			//nothing to do.
			
			var nodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<AccountabilityNode>()).Select(x => x.ModelId).ToArray();
			if (nodeIds.Any()) {
				data.Lookup.AddList(data.Session.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(nodeIds).Future());
			}

		}

        public ISurvey InitializeSurvey(ISurveyInitializerData data) {
			var name = data.About.ToPrettyString();
			if (name == null && data.About.ModelType == ForModel.GetModelType<AccountabilityNode>()) {
				name = data.Lookup.GetList<AccountabilityNode>().FirstOrDefault(x => x.Id == data.About.ModelId).NotNull(x => x.User.GetName());
			}
            return new Survey(name, DueDate, data);
        }

        public IEnumerable<ISectionInitializer> GetSectionBuilders(ISectionInitializerData data) {
            return _sectionBuilders();
        }
        #endregion
    }
    
}