using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Interfaces;
using RadialReview.Areas.People.Models.Survey;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {
	public class GeneralCommentsSection : ISectionInitializer {
		public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
			yield break;
		}

		public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
			yield return new InputItemIntializer("Comments", SurveyQuestionIdentifier.GeneralComment);
		}

		public ISection InitializeSection(ISectionInitializerData data) {
			return new SurveySection(data, "General Comments/Next Steps", SurveySectionType.GeneralComments, "mk-gencomments");
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do.
		}
	}
}