using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Angular {

	public class AngularPeopleAnalyzer : BaseAngular {

		public AngularPeopleAnalyzer() { }
		public AngularPeopleAnalyzer(long id) : base(id) {
		}

		public IEnumerable<AngularPeopleAnalyzerRow> Rows { get; set; }
		public IEnumerable<AngularPeopleAnalyzerResponse> Responses { get; set; }
		public IEnumerable<PeopleAnalyzerValue> Values { get; set; }
		public AngularDateRange DateRange { get; set; }
		public IEnumerable<AngularSurveyContainer> SurveyContainers { get; set; }
	}

	public class AngularPeopleAnalyzerRow : BaseStringAngular {
		public AngularPeopleAnalyzerRow() { }
		public AngularPeopleAnalyzerRow(IForModel user,bool canPrint) : base(user.ToKey()) {
			About = new AngularForModel(user);
			CanPrint = canPrint;
		}

		public bool? CanPrint { get; set; }
		public AngularForModel About { get; set; }
	}

	public class AngularPeopleAnalyzerResponse : BaseStringAngular {
		public AngularPeopleAnalyzerResponse() { }

		public static string ToUID(IByAbout byAbout, IForModel source, DateTime issueDate) {
			return byAbout.ToKey() + "__" + source.ToKey() + "__" + issueDate.ToJavascriptMilliseconds();
		}

		public AngularPeopleAnalyzerResponse(IByAbout byAbout, DateTime issueDate, DateTime answerDate, IForModel source, string answerFormatted, string answer, int overridePriority, long surveyContainerId) : base(ToUID(byAbout, source, issueDate)) {
			Answer = answer;
			AnswerFormatted = answerFormatted;
			IssueDate = issueDate;
			AnswerDate = answerDate;
			Override = overridePriority;
			By = new AngularForModel(byAbout.GetBy());
			About = new AngularForModel(byAbout.GetAbout());
			Source = new AngularForModel(source);
			SurveyContainerId = surveyContainerId;
		}
		public AngularForModel About { get; set; }
		public AngularForModel By { get; set; }
		public AngularForModel Source { get; set; }
		public string Answer { get; set; }
		public string AnswerFormatted { get; set; }

		public DateTime? IssueDate { get; set; }
		public DateTime? AnswerDate { get; set; }
		public int? Override { get; set; }
		public long? SurveyContainerId { get; set; }
	}

	public class PeopleAnalyzerValue : BaseStringAngular {

		public PeopleAnalyzerValue(IForModel source) : base(source.ToKey()) {
			Source = new AngularForModel(source);

		}
		public AngularForModel Source { get; set; }
	}
}