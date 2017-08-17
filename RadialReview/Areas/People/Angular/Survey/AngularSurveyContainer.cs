using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Areas.People.Models.Survey;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Interfaces;

namespace RadialReview.Areas.People.Angular.Survey {
    public class AngularSurveyContainer : BaseAngular,ISurveyContainer {
        public AngularSurveyContainer() { }
		public AngularSurveyContainer(long id) : base(id) { }
		public AngularSurveyContainer(ISurveyContainer container,bool locked,AngularUser issuedBy) : base(container.Id) {
			Name = container.GetName();
			Ordering = container.GetOrdering();
			IssueDate = container.GetIssueDate();
			Surveys = container.GetSurveys().NotNull(y=>y.Select(x => new AngularSurvey(x)).ToList());
			Locked = locked;
			DueDate = container.GetDueDate();
			IssuedBy = issuedBy;
			Creator = container.GetCreator();

		}

		public String Name { get; set; }
        public DateTime? CreateTime { get; set; }

		public bool? Locked { get; set; }


        [JsonConverter(typeof(StringEnumConverter))]
        public SurveyType? SurveyType { get; set; }
        public string Help { get; set; }
        public int? Ordering { get; set; }

        public ICollection<AngularSurvey> Surveys { get; set; }
		public DateTime? IssueDate { get; set; }
		public AngularUser IssuedBy { get; set; }
		public DateTime? DueDate { get; set; }
		public IForModel Creator { get; set; }

		public IEnumerable<ISurvey> GetSurveys() {
            return Surveys;
        }

        public void AppendSurvey(ISurvey survey) {
            Surveys = Surveys ?? new List<AngularSurvey>();
            Surveys.Add((AngularSurvey)survey);
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

		public DateTime GetIssueDate() {
			return IssueDate ?? DateTime.MinValue;
		}

		public DateTime? GetDueDate() {
			return DueDate;
		}

		public IForModel GetCreator() {
			return Creator;
		}
	}
}