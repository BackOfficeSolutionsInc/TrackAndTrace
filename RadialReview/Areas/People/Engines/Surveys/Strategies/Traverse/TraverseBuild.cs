using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.Traverse {

    public class TraverseBuild : ISurveyTraverse {
        public ISurveyContainer SurveyContainer { get; set; }

        public void AtSurveyContainer(ISurveyContainer container) {
            //Nothing to do.
            SurveyContainer = container;
        }
        public void SurveyContainerToSurvey(ISurveyContainer parent, ISurvey child) {
            parent.AppendSurvey(child);
        }
        public void SurveyToSection(ISurvey parent, ISection child) {
            parent.AppendSection(child);
        }
        public void SectionToItem(ISection parent, IItemContainer child) {
            parent.AppendItem(child);
        }

		public virtual void OnComplete(ISurveyContainer container) {
			//Nothing to do
		}
	}
}