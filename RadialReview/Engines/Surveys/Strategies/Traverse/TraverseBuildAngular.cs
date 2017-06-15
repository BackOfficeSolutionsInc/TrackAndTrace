using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Engines.Surveys.Interfaces;
using RadialReview.Models.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Engines.Surveys.Strategies.Traverse {
    public class TraverseBuildAngular : ISurveyTraverse {
        private AngularSurveyContainer SurveyContainer { get; set; }
        private Dictionary<long, AngularSurvey> Surveys = new Dictionary<long, AngularSurvey>();
        private Dictionary<long, AngularSurveySection> Sections = new Dictionary<long, AngularSurveySection>();
        //private Dictionary<long, AngularSurveyItem> Items = new Dictionary<long, AngularSurveyItem>();

        public TraverseBuildAngular(AngularSurveyContainer qc) {
            SurveyContainer = qc;
        }
        public void AtSurveyContainer(ISurveyContainer child) {
            SurveyContainer.Id = child.Id;
            SurveyContainer.Name = child.GetName();
            SurveyContainer.Ordering = child.GetOrdering();
            SurveyContainer.SurveyType = child.GetSurveyType();
            SurveyContainer.Help = child.GetHelp();
        }

        public void SurveyContainerToSurvey(ISurveyContainer parent, ISurvey child) {
            var survey = new AngularSurvey() {
                Id = child.Id,
                Name = child.GetName(),
                Ordering = child.GetOrdering(),
                SurveyContainerId = parent.Id,
                Help = child.GetHelp(),
                Sections = new List<ISection>()
            };

            SurveyContainer.AppendSurvey(survey);
            Surveys[child.Id] = survey;
        }

        public void SurveyToSection(ISurvey parent, ISection child) {
            var survey = Surveys[parent.Id];
            var section = new AngularSurveySection() {
                Id = child.Id,
                Name = child.GetName(),
                Ordering = child.GetOrdering(),
                Help = child.GetHelp(),
                Items = new List<IItemContainer>(),
                SectionType = child.GetSectionType(),
                SurveyId = survey.Id,
            };

            survey.AppendSection(section);
            Sections[child.Id] = section;
        }

        public void SectionToItem(ISection parent, IItemContainer child) {

            var cItem = child.GetItem();
            var item = new AngularSurveyItem() {
                Id = cItem.Id,
                Name = cItem.GetName(),
                Ordering = cItem.GetOrdering(),
                Help = cItem.GetHelp(),
                ItemFormatId = cItem.GetItemFormatId(),
                SectionId = cItem.GetSectionId(),
                Source = new AngularForModel(cItem.GetSource())                
            };

            var cFormat = child.GetFormat();
            var format = new AngularSurveyItemFormat() {
                Id = cFormat.Id,
                Name = cFormat.GetName(),
                Ordering = cFormat.GetOrdering(),
                Help = cFormat.GetHelp(),
                ItemType = cFormat.GetItemType(),
                Settings = cFormat.GetSettings(),
            };

            var cResponse = child.GetResponse();
            AngularSurveyResponse response = null;
            if (child.HasResponse()) {
                response = new AngularSurveyResponse() {
                    Id = cResponse.Id,
                    Name = cResponse.GetName(),
                    Ordering = cResponse.GetOrdering(),
                    Help = cResponse.GetHelp(),
                    ItemFormatId = cResponse.GetItemFormatId(),
                    ItemId = cResponse.GetItemId(),                    
                };
            }
            
            var itemContainer = new AngularSurveyItemContainer() {
                Id = child.Id,
                Name = child.GetName(),
                Ordering = child.GetOrdering(),
                Help = child.GetHelp(),
                Item = item,
                Response = response,
                ItemFormat = format,
            };

            var section = Sections[parent.Id];
            section.AppendItem(itemContainer);
            //Items[child.Id] = section;
        }

    }


}