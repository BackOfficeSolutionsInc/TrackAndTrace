using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.Events {
    public class SurveyBuilderEventsNoOp : ISurveyBuilderEvents {
        public void OnBegin(ISurveyInitializer builder,long orgId, IOuterLookup lookup, IEnumerable<IByAbout> byAbouts) {}

        public void OnEnd(ISurveyContainer container) {}

		public void AfterInitialized(IComponent compontent,bool anyElements) {}

		public void OnInitialize(IComponent compontent) {}
    }
}