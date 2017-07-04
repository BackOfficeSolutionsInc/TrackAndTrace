using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.PostProcesses {
	public class PostProcessorNoOp : IPostProcessor {
		public void Process(ISurveyContainer surveyContainer) {			
		}
	}
}