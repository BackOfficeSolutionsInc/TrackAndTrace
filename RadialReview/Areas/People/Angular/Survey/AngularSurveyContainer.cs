using RadialReview.Engines.Surveys.Interfaces;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Areas.People.Models.Survey;

namespace RadialReview.Areas.People.Angular.Survey {
    public class AngularSurveyContainer : BaseAngular, ISurveyContainer {
        public AngularSurveyContainer() { }
        public AngularSurveyContainer(long id) : base(id) { }

        public String Name { get; set; }
        public DateTime? CreateTime { get; set; }
        public SurveyType? SurveyType { get; set; }
        public string Help { get; set; }
        public int? Ordering { get; set; }

        public ICollection<ISurvey> Surveys { get; set; }
        public IEnumerable<ISurvey> GetSurveys() {
            return Surveys;
        }

        public void AppendSurvey(ISurvey survey) {
            Surveys = Surveys ?? new List<ISurvey>();
            Surveys.Add(survey);
        }

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
            return Ordering??0;
        }
    }
}