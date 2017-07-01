using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Impl {
	public class TextAreaItemIntializer : IItemInitializer {
		public string Name { get; set; }
		public SurveyQuestionIdentifier QuestionIdentifier { get; set; }
		public TextAreaItemIntializer(string name, SurveyQuestionIdentifier questionIdentifier) {
			Name = name;
			QuestionIdentifier = questionIdentifier;
		}

		public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
			return ctx.RegistrationItemFormat(false, () => new SurveyItemFormat(ctx, QuestionIdentifier, SurveyItemType.TextArea));
		}

		public bool HasResponse(IResponseInitializerCtx ctx) {
			return true;
		}

		public IItem InitializeItem(IItemInitializerData data) {
			return new SurveyItem(data, Name, null);
		}

		public IResponse InitializeResponse(IResponseInitializerCtx ctx, IItemFormat format) {
			return new SurveyResponse(ctx, format);		
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do
		}
	}
	

}