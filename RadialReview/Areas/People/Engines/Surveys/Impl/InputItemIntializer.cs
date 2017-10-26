using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Impl {
	public class InputItemIntializer : IItemInitializer {
        
        public SurveyItemType InputType { get; set; }
		public string Name { get; set; }
		public SurveyQuestionIdentifier QuestionIdentifier { get; set; }
        public KV[] Parameters { get;  set; }

        public InputItemIntializer(string name, SurveyQuestionIdentifier questionIdentifier, SurveyItemType type = SurveyItemType.TextArea,KV[] parameters=null) {
			Name = name;
			QuestionIdentifier = questionIdentifier;
            InputType = type;
            Parameters = parameters ?? new KV[] { };

        }

		public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
			return ctx.RegistrationItemFormat(false, () => new SurveyItemFormat(ctx, QuestionIdentifier, InputType, Parameters));
		}

		public bool HasResponse(IResponseInitializerCtx ctx) {
			return true;
		}

		public IItem InitializeItem(IItemInitializerData data) {
			return new SurveyItem(data, Name, null, Name);
		}

		public IResponse InitializeResponse(IResponseInitializerCtx ctx, IItemFormat format) {
			return new SurveyResponse(ctx, format);		
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do
		}
	}
	

}