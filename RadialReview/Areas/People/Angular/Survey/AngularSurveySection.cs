using System;
using System.Collections.Generic;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Angular.Base;
using System.Linq;

namespace RadialReview.Areas.People.Angular.Survey {
    public class AngularSurveySection : BaseAngular, ISection {

		public AngularSurveySection() { }
        public AngularSurveySection(long id) : base(id) { }

		public AngularSurveySection(ISection section) : base(section.Id) {
			Name = section.GetName();
			Ordering = section.GetOrdering();
			Help= section.GetHelp();
			SectionType = section.GetSectionType();
			SurveyId = section.GetSurveyId();
			Items = section.GetItemContainers().NotNull(y => y.Select(x => new AngularSurveyItemContainer(x)).ToList());

		}

		public string Name { get; set; }
        public string Help { get; set; }
        public int? Ordering { get; set; }

		public string SectionType { get; set; }
		public long? SurveyId { get;  set; }
		public string SectionMergerKey { get; set; }

        public ICollection<AngularSurveyItemContainer> Items { get; set; }
        public void AppendItem(IItemContainer item) {
            Items = Items ?? new List<AngularSurveyItemContainer>();
            Items.Add((AngularSurveyItemContainer)item);
        }
        public IEnumerable<IItemContainer> GetItemContainers() {
            return Items;
        }
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
	}
}