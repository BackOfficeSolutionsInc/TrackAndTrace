using System;
using System.Collections.Generic;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Angular.Base;
using System.Linq;
using RadialReview.Models.Interfaces;
using RadialReview.Areas.People.Angular.Survey;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace RadialReview.Areas.People.Angular.Survey {
	public class AngularSurveyItemContainerAbout : BaseAngular, IItemContainerAbout {

		public AngularSurveyItemContainerAbout() { }
		public AngularSurveyItemContainerAbout(long id) : base(id) { }

		//public AngularSurveyItemContainerAbout(IItemContainerAbout container) : base(container.Id) {
		//	Name = container.GetName();
		//	Ordering = container.GetOrdering();
		//	Help = container.GetHelp();
		//	ItemMergerKey = container.GetItemMergerKey();
		//	if (container.GetItem() != null)
		//		Item = new AngularSurveyItem(container.GetItem());
		//	if (container.GetResponses() != null) {
		//		Responses = new List<AngularSurveyResponse>();
		//		foreach (var r in container.GetResponses())
		//			Responses.Add(new AngularSurveyResponse(r));
		//	}
		//	if (container.GetFormat() != null)
		//		ItemFormat = new AngularSurveyItemFormat(container.GetFormat());
		//}


		public static AngularSurveyItemContainerAbout ConstructShallow(IItemContainer container) {
			return new AngularSurveyItemContainerAbout(container.Id) {
				Name = container.GetName(),
				Ordering = container.GetOrdering(),
				Help = container.GetHelp(),
				ItemMergerKey = container.GetItemMergerKey(),
				Item = (container.GetItem() == null) ? null : new AngularSurveyItem(container.GetItem()),
				ItemFormat = (container.GetFormat() == null) ? null : new AngularSurveyItemFormat(container.GetFormat()),
				Responses = new List<AngularSurveyResponse>()
			};
		}


		public string Name { get; set; }
		public string Help { get; set; }
		public int? Ordering { get; set; }

		public AngularSurveyItem Item { get; set; }
		public ICollection<AngularSurveyResponse> Responses { get; set; }
		public AngularSurveyItemFormat ItemFormat { get; set; }
		public string ItemMergerKey { get; set; }


		public IItem GetItem() {
			return Item;
		}

		public IItemFormat GetFormat() {
			return ItemFormat;
		}

		public bool HasResponse() {
			return Responses != null && Responses.Any();
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

		public string ToPrettyString() {
			return "";
		}

		public IEnumerable<IResponse> GetResponses() {
			return Responses;
		}

		public string GetItemMergerKey() {
			return ItemMergerKey;
		}
	}

}