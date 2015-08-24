using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Survey
{

	public class SurveyResultVM
	{
		public SurveyContainerModel Container { get; set; }
		public List<SurveyResultItemVM> Questions { get; set; }

		public int TotalRequestedRespondents { get; set; }
		public int TotalStartedRespondents { get; set; }
	}
	public class SurveyResultItemVM
	{
		public String Question { get; set; }
		public String PartialView { get; set; }

	}

	public class SurveyResultItem_RadioVM : SurveyResultItemVM
	{
		public int[] Answers { get; set; }
	}

	public class SurveyResultItem_FeedbackVM : SurveyResultItemVM
	{
		public String[] Answers { get; set; }
	}

}