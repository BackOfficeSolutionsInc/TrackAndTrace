using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.Events {
    public class SurveyReconstructionEventsNoOp : ISurveyReconstructorEvents {
        public void OnBegin(IOuterLookup outerLookup) {}
    }
}