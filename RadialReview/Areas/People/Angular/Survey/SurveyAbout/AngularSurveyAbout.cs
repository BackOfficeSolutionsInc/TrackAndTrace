using System;
using System.Linq;

using System.Collections.Generic;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Interfaces;

namespace RadialReview.Areas.People.Angular.Survey.SurveyAbout {
	public class AngularSurveyAbout : BaseAngular, ISurveyAbout {

		public AngularSurveyAbout() { }
		public AngularSurveyAbout(long id) : base(id) { }
		//public AngularSurveyAbout(ISurveyAbout survey) : base(survey.GetAbout().ModelId) {
		//	Name = survey.GetName();
		//	Ordering = survey.GetOrdering();
		//	Help = survey.GetHelp();
		//	SurveyContainerId = survey.GetSurveyContainerId();
		//	About = survey.GetAbout().ToImpl();
		//	Sections = survey.GetSections().NotNull(y => y.Select(x => new AngularSurveyAboutSection(x)).ToList());
		//	//Bys = new List<ForModel>() { survey.GetBy().ToImpl() } ;
		//}
		public static AngularSurveyAbout ConstructShallow(ISurvey survey) {
			return new AngularSurveyAbout(survey.Id) {
				Name = survey.GetName(),
				Ordering = survey.GetOrdering(),
				Help = survey.GetHelp(),
				SurveyContainerId = survey.GetSurveyContainerId(),
				About = new AngularForModel(survey.GetAbout()),
				Sections = new List<AngularSurveyAboutSection>(),
				IssueDate = survey.GetIssueDate(),
			};			
		}
		public string Help { get; set; }
		public int? Ordering { get; set; }
		public string Name { get; set; }
		public AngularForModel About { get; set; }
		public ICollection<AngularSurveyAboutSection> Sections { get; set; }
		public DateTime IssueDate { get; set; }


		public void AppendSection(ISectionAbout section) {
			Sections = Sections ?? new List<AngularSurveyAboutSection>();
			Sections.Add((AngularSurveyAboutSection)section);
		}

		public IForModel GetAbout() {
			return About;
		}

		public string GetHelp() {
			return Help;
		}

		public string GetName() {
			return Name;
		}

		public int GetOrdering() {
			return Ordering ?? 0;
		}

		public IEnumerable<ISectionAbout> GetSections() {
			return Sections;
		}

		public long GetSurveyContainerId() {
			return SurveyContainerId ?? 0;
		}

		//public void MergeWith(ISurvey survey) {
		//	if (survey.GetAbout().ToKey() != GetAbout().ToKey())
		//		throw new Exception("Cannot merge, About models are different");

		//	foreach (var otherSection in survey.GetSections()) {
		//		var foundSection = Sections.FirstOrDefault(x => x.GetSectionMergerKey() == otherSection.GetSectionMergerKey());
		//		if (foundSection == null) {
		//			AppendSection(new AngularSurveyAboutSection(otherSection));
		//		} else {
		//			foundSection.MergeWith(otherSection);
		//		}
		//	}
		//}

		public string ToPrettyString() {
			return "";
		}

		public DateTime GetIssueDate() {
			return IssueDate;
		}
		

		public long? SurveyContainerId { get; set; }

		//      public ICollection<AngularSurveySection> Sections { get; set; }
		//      public void AppendSection(ISection item) {
		//          Sections = Sections ?? new List<AngularSurveySection>();
		//          Sections.Add((AngularSurveySection)item);
		//      }
		//      public IEnumerable<ISectionAbout> GetSections() {
		//          return Sections;
		//      }
		//public List<ForModel> Bys { get; set; }
		//public ForModel About { get; set; }
		//public string Name { get; set; }
		//      public string Help { get; set; }
		//      public int? Ordering { get; set; }   

		//      public string GetName() {
		//          return Name;
		//      }
		//      public string GetHelp() {
		//          return Help;
		//      }
		//      public int GetOrdering() {
		//          return Ordering ?? 0;
		//      }    

		//      public string ToPrettyString() {
		//          return "";
		//      }

		//      public long GetSurveyContainerId() {
		//          return SurveyContainerId ?? 0;
		//      }

		//      public IForModel GetBy() {
		//          throw new NotImplementedException();
		//      }

		//      public IForModel GetAbout() {
		//	return About;
		//      }

		//public void MergeWith(ISurvey survey) {
		//	Bys.Add(survey.GetBy().ToImpl();
		//	Section

		//}

		//public void AppendSection(ISectionAbout section) {
		//	throw new NotImplementedException();
		//}
	}
}