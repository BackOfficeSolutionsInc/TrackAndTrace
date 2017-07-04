using System;
using System.Collections.Generic;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Angular.Base;
using System.Linq;
using RadialReview.Models.Interfaces;

namespace RadialReview.Areas.People.Angular.Survey {
    public class AngularSurvey : BaseAngular, ISurvey {

		public AngularSurvey() {  }
        public AngularSurvey(long id) : base(id) {  }
		public AngularSurvey(ISurvey survey) :base(survey.Id){
			Name = survey.GetName();
			Ordering = survey.GetOrdering();
			Help = survey.GetHelp();
			By = survey.GetBy();
			About = survey.GetAbout();
			SurveyContainerId = survey.GetSurveyContainerId();
			Sections = survey.GetSections().NotNull(y => y.Select(x => new AngularSurveySection(x)).ToList());
		}

		public long? SurveyContainerId { get; set; }

        public ICollection<AngularSurveySection> Sections { get; set; }
        public void AppendSection(ISection item) {
            Sections = Sections ?? new List<AngularSurveySection>();
            Sections.Add((AngularSurveySection)item);
        }
        public IEnumerable<ISection> GetSections() {
            return Sections;
        }        

        public string Name { get; set; }
        public string Help { get; set; }
        public int? Ordering { get; set; }   
				   
        public string GetName() {
            return Name;
        }
        public string GetHelp() {
            return Help;
        }
        public int GetOrdering() {
            return Ordering ?? 0;
        }    

        public string ToPrettyString() {
            return "";
        }

        public long GetSurveyContainerId() {
            return SurveyContainerId ?? 0;
        }

		protected IForModel By;
		protected IForModel About;


		public IForModel GetBy() {
			return By;
        }

        public IForModel GetAbout() {
			return About;
        }
    }
}