using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Areas.People.Models.Survey;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace RadialReview.Areas.People.Angular.Survey.SurveyAbout {
	public class AngularSurveyAboutContainer : BaseAngular, ISurveyAboutContainer {
		public AngularSurveyAboutContainer() { }
		public AngularSurveyAboutContainer(long id) : base(id) { }
		//public AngularSurveyAboutContainer(ISurveyAboutContainer container) : base(container.Id) {
		//	Name = container.GetName();
		//	Ordering = container.GetOrdering();
		//	Surveys = container.GetSurveys().NotNull(y=>y.Select(x => new AngularSurveyAbout(x)).ToList());
		//}

		public static AngularSurveyAboutContainer ConstructShallow(ISurveyContainer container) {
			return new AngularSurveyAboutContainer(container.Id) {
				Name = container.GetName(),
				Ordering = container.GetOrdering(),
				Help = container.GetHelp(),
				SurveyType = container.GetSurveyType(),				
				Surveys = new List<AngularSurveyAbout>()
			};
		}

		public String Name { get; set; }
		public DateTime? CreateTime { get; set; }


		[JsonConverter(typeof(StringEnumConverter))]
		public SurveyType? SurveyType { get; set; }
		public string Help { get; set; }
		public int? Ordering { get; set; }

		public ICollection<AngularSurveyAbout> Surveys { get; set; }
		//public IEnumerable<ISurvey> GetSurveys() {
		//    return Surveys;
		//}

		//public void AppendSurvey(ISurvey survey) {
		//    Surveys = Surveys ?? new List<AngularSurvey>();
		//    Surveys.Add((AngularSurvey)survey);
		//}

		public string ToPrettyString() {
			return "";
		}

		public SurveyType GetSurveyType() {
			return SurveyType ?? Models.Survey.SurveyType.Invalid;
		}

		public string GetName() {
			return Name;
		}

		public string GetHelp() {
			return Help;
		}

		public int GetOrdering() {
			return Ordering ?? 0;
		}

		public IEnumerable<ISurveyAbout> GetSurveys() {
			return Surveys;
		}

		public void AppendSurvey(ISurveyAbout survey) {
			Surveys = Surveys ?? new List<AngularSurveyAbout>();
			Surveys.Add((AngularSurveyAbout)survey);
		}
	}
}