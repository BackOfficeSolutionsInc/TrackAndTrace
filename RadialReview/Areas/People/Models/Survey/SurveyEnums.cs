using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Models.Survey {
    public enum SurveyType {
        Invalid = 0,
        QuarterlyConversation=1,
        AnnualReview = 2
    }

    public enum SurveySectionType {
        Invalid = 0,
        Rocks = 1,
        Roles = 2,
        Values = 3,
		GeneralComments = 4,
	}

	public enum SurveyItemType {
		Invalid = 0,
		TextArea = 1,
		TextBox = 2,
		Radio = 3,
		Text = 4,
	}
	public enum SurveyQuestionIdentifier {
		None = 0,

		Value = 1,
		GWC = 2,
		Rock = 3,
		Role = 4,

		GeneralComment = 100,
		
	}
}