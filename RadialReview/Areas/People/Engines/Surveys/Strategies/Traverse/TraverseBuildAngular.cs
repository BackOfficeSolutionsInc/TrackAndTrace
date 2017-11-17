using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.Traverse {
	public class TraverseBuildAngular : ISurveyTraverse {
		private AngularSurveyContainer SurveyContainer { get; set; }
		private Dictionary<long, AngularSurvey> Surveys = new Dictionary<long, AngularSurvey>();
		private Dictionary<long, AngularSurveySection> Sections = new Dictionary<long, AngularSurveySection>();
		//private Dictionary<long, AngularSurveyItem> Items = new Dictionary<long, AngularSurveyItem>();
		public string TemplateModifier { get; set; }


		public TraverseBuildAngular(AngularSurveyContainer qc, string templateModifier = AngularSurveyItemFormat.DEFAULT_TEMPLATE_MODIFIER) {
			SurveyContainer = qc;
			TemplateModifier = templateModifier;
		}
		public void AtSurveyContainer(ISurveyContainer child) {
			SurveyContainer.Id = child.Id;
			SurveyContainer.Name = child.GetName();
			SurveyContainer.Ordering = child.GetOrdering();
			SurveyContainer.SurveyType = child.GetSurveyType();
			SurveyContainer.Help = child.GetHelp();
		}

		public void SurveyContainerToSurvey(ISurveyContainer parent, ISurvey child) {
			//var survey = new AngularSurvey() {
			//	Id = child.Id,
			//	Name = child.GetName(),
			//	Ordering = child.GetOrdering(),
			//	SurveyContainerId = parent.Id,
			//	Help = child.GetHelp(),
			//	Sections = new List<AngularSurveySection>()
			//};

			var survey = new AngularSurvey(child);

			SurveyContainer.AppendSurvey(survey);
			Surveys[child.Id] = survey;
		}

		public void SurveyToSection(ISurvey parent, ISection child) {
			var survey = Surveys[parent.Id];
			//var section = new AngularSurveySection() {
			//	Id = child.Id,
			//	Name = child.GetName(),
			//	Ordering = child.GetOrdering(),
			//	Help = child.GetHelp(),
			//	Items = new List<AngularSurveyItemContainer>(),
			//	SectionType = child.GetSectionType(),
			//	SurveyId = survey.Id,
			//};

			var section = new AngularSurveySection(child);
			survey.AppendSection(section);
			Sections[child.Id] = section;
		}

		public void SectionToItem(ISection parent, IItemContainer child) {
			/*var cItem = child.GetItem();
			 var item = new AngularSurveyItem(child.get) {
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
				 TemplateModifier = TemplateModifier,
				 QuestionIdentifier = cFormat.GetQuestionIdentifier()
			 };

			 var cResponse = child.GetResponse();
			 AngularSurveyResponse response = null;
			 if (child.HasResponse()) {
				 response = new AngularSurveyResponse(cResponse); {
					 Id = cResponse.Id,
					 Name = cResponse.GetName(),
					 Ordering = cResponse.GetOrdering(),
					 Help = cResponse.GetHelp(),
					 ItemFormatId = cResponse.GetItemFormatId(),
					 ItemId = cResponse.GetItemId(),           
					 Answer = cResponse.GetAnswer(),				    
				 };
			 }

			 var itemContainer = new AngularSurveyItemContainer(child); {
				 Id = child.GetItem().Id,
				 Name = child.GetName(),
				 Ordering = child.GetOrdering(),
				 Help = child.GetHelp(),
				 Item = item,
				 Response = response,
				 ItemFormat = format,
			 };*/

			var itemContainer = new AngularSurveyItemContainer(child);
			var section = Sections[parent.Id];
			section.AppendItem(itemContainer);
		}

		public void OnComplete(ISurveyContainer container) {
			//Nothing to do
		}
	}


}