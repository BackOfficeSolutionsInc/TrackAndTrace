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
	public class AngularSurveyItemContainer : BaseAngular, IItemContainer {

		public AngularSurveyItemContainer() { }
		public AngularSurveyItemContainer(long id) : base(id) { }

		public AngularSurveyItemContainer(IItemContainer container) : base(container.Id) {
			Name = container.GetName();
			Ordering = container.GetOrdering();
			Help = container.GetHelp();
			ItemMergerKey = container.GetItemMergerKey();
			if (container.GetItem() != null)
				Item = new AngularSurveyItem(container.GetItem());
			if (container.GetResponse() != null)
				Response = new AngularSurveyResponse(container.GetResponse());
			if (container.GetFormat() != null)
				ItemFormat = new AngularSurveyItemFormat(container.GetFormat());
		}

		public string Name { get; set; }
		public string Help { get; set; }
		public string ItemMergerKey { get; set; }
		public int? Ordering { get; set; }

		public AngularSurveyItem Item { get; set; }
		public AngularSurveyResponse Response { get; set; }
		public AngularSurveyItemFormat ItemFormat { get; set; }


		public IItem GetItem() {
			return Item;
		}

		public IResponse GetResponse() {
			return Response;
		}

		public IItemFormat GetFormat() {
			return ItemFormat;
		}

		public bool HasResponse() {
			return Response != null;
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

		public string GetItemMergerKey() {
			return ItemMergerKey;
		}
	}

	public class AngularSurveyItemFormat : BaseAngular, IItemFormat {

		public const string DEFAULT_TEMPLATE_MODIFIER = "bs";

		public AngularSurveyItemFormat() { }
		public AngularSurveyItemFormat(long id) : base(id) { }

		public AngularSurveyItemFormat(IItemFormat itemFormat) {
			Name = itemFormat.GetName();
			Help = itemFormat.GetHelp();
			ItemType = itemFormat.GetItemType();
			TemplateModifier = DEFAULT_TEMPLATE_MODIFIER;
			Ordering = itemFormat.GetOrdering();
			Settings = itemFormat.GetSettings();
			QuestionIdentifier = itemFormat.GetQuestionIdentifier();
		}

		public string Name { get; set; }
		public string Help { get; set; }
		public int? Ordering { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public SurveyItemType? ItemType { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public SurveyQuestionIdentifier? QuestionIdentifier { get; set; }
		
		public string TemplateModifier { get; set; }

		public IDictionary<string, object> Settings { get; set; }

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

		public IItemFormat AddSetting(string key, object value) {
			Settings = Settings ?? new Dictionary<string, object>();
			Settings[key] = value;
			return this;
		}
		public SurveyItemType GetItemType() {
			return ItemType ?? SurveyItemType.Invalid;
		}
		public IDictionary<string, object> GetSettings() {
			return Settings;
		}


		public T GetSetting<T>(string key) {
			var found = Settings[key];
			if (found is T)
				return (T)found;
			return default(T);
		}

		public SurveyQuestionIdentifier GetQuestionIdentifier() {
			return QuestionIdentifier ?? SurveyQuestionIdentifier.None;
		}
	}

	public class AngularSurveyResponse : BaseAngular, IResponse {

		public AngularSurveyResponse() { }
		public AngularSurveyResponse(long id) : base(id) { }

		public AngularSurveyResponse(IResponse response) : base(response.Id) {
			Name = response.GetName();
			Help = response.GetHelp();
			ItemId = response.GetItemId();
			Answer = response.GetAnswer();
			Ordering = response.GetOrdering();
			ItemFormatId = response.GetItemFormatId();
			var byAbout = response.GetByAbout();
			By = new AngularForModel(byAbout.GetBy());
			About = new AngularForModel(byAbout.GetAbout());
			
		}

		public string Name { get; set; }
		public string Help { get; set; }
		public int? Ordering { get; set; }
		public long? ItemFormatId { get; set; }
		public long? ItemId { get; set; }

		public AngularForModel By { get; set; }
		public AngularForModel About { get; set; }

		public string Answer { get; set; }

		public long GetItemFormatId() {
			return ItemFormatId ?? 0;
		}
		public long GetItemId() {
			return ItemId ?? 0;
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
		public string GetAnswer() {
			return Answer;
		}

		public IByAbout GetByAbout() {
			return new ByAbout(By, About);
		}
	}

	public class AngularSurveyItem : BaseAngular, IItem {


		public AngularSurveyItem() { }
		public AngularSurveyItem(long id) : base(id) { }

		public AngularSurveyItem(IItem item) : base(item.Id) {
			Name = item.GetName();
			Help = item.GetHelp();
			Ordering = item.GetOrdering();
			ItemFormatId = item.GetItemFormatId();
			SectionId = item.GetSectionId();
			Source = new AngularForModel(item.GetSource());
			ItemMergerKey = item.GetItemMergerKey();
		}

		public string Name { get; set; }
		public string Help { get; set; }
		public int? Ordering { get; set; }
		public long? ItemFormatId { get; set; }
		public long? SectionId { get; set; }

		public AngularForModel Source { get; set; }
		public string ItemMergerKey { get; set; }

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

		public long GetItemFormatId() {
			return ItemFormatId ?? 0;
		}

		public long GetSectionId() {
			return SectionId ?? 0;
		}

		public IForModel GetSource() {
			return Source;
		}

		public string GetItemMergerKey() {
			return ItemMergerKey;
		}
	}
}