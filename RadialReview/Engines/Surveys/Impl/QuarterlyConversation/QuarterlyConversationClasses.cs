using RadialReview.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Interfaces;
using RadialReview.Engines.Surveys.Impl.QuarterlyConversation.Sections;

namespace RadialReview.Engines.Surveys.Impl.QuarterlyConversation {
    public class QuarterlyConversationInitializer : ISurveyInitializer {

        public IForModel CreatedBy { get; set; }
        public String Name { get; set; }
        public long OrgId { get; set; }

        public QuarterlyConversationInitializer(IForModel createdBy, string name, long orgId) {
            CreatedBy = createdBy;
            Name = name;
            OrgId = orgId;
        }

        private IEnumerable<ISectionInitializer> _sectionBuilders() {
            yield return new ValueSection();
            yield return new RoleSection();
            yield return new RockSection();
        }

        #region Standard Customization
        public ISurveyContainer BuildSurveyContainer() {
            return new SurveyContainer(CreatedBy, Name, OrgId, SurveyType.QuarterlyConversation, null);
        }

        public IEnumerable<ISectionInitializer> GetAllPossibleSectionBuilders(IEnumerable<IByAbout> byAbouts) {
            return _sectionBuilders();
        }
        
        public void Prelookup(IInitializerLookupData data) {
            //nothing to do.
        }

        public ISurvey InitializeSurvey(ISurveyInitializerData data) {
            return new Survey(Name, data);
        }

        public IEnumerable<ISectionInitializer> GetSectionBuilders(ISectionInitializerData data) {
            return _sectionBuilders();
        }
        #endregion
    }
    
}