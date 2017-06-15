using System;
using System.Collections.Generic;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Engines.Surveys.Interfaces;
using RadialReview.Models.Angular.Base;
using System.Linq;
using RadialReview.Models.Interfaces;
using RadialReview.Areas.People.Angular.Survey;

namespace RadialReview.Areas.People.Angular.Survey {
    public class AngularSurveyItemContainer : BaseAngular, IItemContainer {

        public AngularSurveyItemContainer() { }
        public AngularSurveyItemContainer(long id) : base(id) { }

        public string Name { get; set; }
        public string Help { get; set; }
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
    }

    public class AngularSurveyItemFormat : BaseAngular, IItemFormat {

        public AngularSurveyItemFormat() { }
        public AngularSurveyItemFormat(long id) : base(id) { }

        public string Name { get; set; }
        public string Help { get; set; }
        public int? Ordering { get; set; }

        public SurveyItemType? ItemType { get; set; }

        public IDictionary<string,object> Settings { get; set; }

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
        public IDictionary<string,object> GetSettings() {
            return Settings;
        }


        public T GetSetting<T>(string key) {
            var found = Settings[key];
            if (found is T)
                return (T)found;
            return default(T);
        }

    }

    public class AngularSurveyResponse : BaseAngular, IResponse {

        public AngularSurveyResponse() { }
        public AngularSurveyResponse(long id) : base(id) { }

        public string Name { get; set; }
        public string Help { get; set; }
        public int? Ordering { get; set; }
        public long? ItemFormatId { get; set; }
        public long? ItemId { get; set; }


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
    }

    public class AngularSurveyItem : BaseAngular, IItem {
        public AngularSurveyItem() { }
        public AngularSurveyItem(long id) : base(id) { }

        public string Name { get; set; }
        public string Help { get; set; }
        public int? Ordering { get; set; }
        public long? ItemFormatId { get; set; }
        public long? SectionId { get; set; }

        public AngularForModel Source { get; set; }

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

    }
}