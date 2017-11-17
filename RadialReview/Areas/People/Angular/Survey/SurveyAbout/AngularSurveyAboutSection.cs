using System;
using System.Collections.Generic;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Angular.Base;
using System.Linq;

namespace RadialReview.Areas.People.Angular.Survey.SurveyAbout {
	public class AngularSurveyAboutSection : BaseAngular, ISectionAbout {

		public AngularSurveyAboutSection() { }
		public AngularSurveyAboutSection(long id) : base(id) { }

		//public AngularSurveyAboutSection(ISectionAbout section) : base(section.Id) {
		//	Name = section.GetName();
		//	Ordering = section.GetOrdering();
		//	Help = section.GetHelp();
		//	SectionType = section.GetSectionType();
		//	SurveyId = section.GetSurveyId();
		//	Items = section.GetItemContainers().NotNull(y => y.Select(x => new AngularSurveyItemContainerAbout(x)).ToList());
		//}

		public static AngularSurveyAboutSection ConstructShallow(ISection section) {
			return new AngularSurveyAboutSection(section.Id) {
				Name = section.GetName(),
				Ordering = section.GetOrdering(),
				Help = section.GetHelp(),
				SectionType = section.GetSectionType(),
				SurveyId = section.GetSurveyId(),
				Items = new List<AngularSurveyItemContainerAbout>()
			};
			//Items = section.GetItemContainers().NotNull(y => y.Select(x => new AngularSurveyItemContainer(x)).ToList());
		}

		public string Name { get; set; }
		public string Help { get; set; }
		public int? Ordering { get; set; }

		public string SectionType { get; set; }
		public long? SurveyId { get; set; }
		public string SectionMergerKey { get; set; }

		public ICollection<AngularSurveyItemContainerAbout> Items { get; set; }

		public IEnumerable<IItem> GetItems() {
			return Items.Select(x => x.GetItem());
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
		public string GetSectionType() {
			return SectionType;
		}
		public long GetSurveyId() {
			return SurveyId ?? 0;
		}
		public string ToPrettyString() {
			return "";
		}

		public string GetSectionMergerKey() {
			return SectionMergerKey;
		}

		public IEnumerable<IItemContainerAbout> GetItemContainers() {
			return Items;
		}

		public void AppendItem(IItemContainerAbout item) {
			Items = Items ?? new List<AngularSurveyItemContainerAbout>();
			Items.Add((AngularSurveyItemContainerAbout)item);
		}

		//public void MergeWith(ISection survey) {
		//	if (survey.GetSectionMergerKey() != GetSectionMergerKey())
		//		throw new Exception("Cannot merge, Merger Keys are different");

		//	foreach (var otherItem in survey.GetItemContainers()) {
		//		var foundSection = Items.FirstOrDefault(x => x.GetItemMergerKey() == otherItem.GetItemMergerKey());
		//		if (foundSection == null) {
		//			AppendSection(new AngularSurveyAboutSection(otherItems));
		//		} else {
		//			foundSection.MergeWith(otherSection);
		//		}
		//	}
		//}
	}
}