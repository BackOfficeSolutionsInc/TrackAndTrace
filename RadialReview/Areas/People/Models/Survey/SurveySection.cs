using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using FluentNHibernate.Mapping;

namespace RadialReview.Areas.People.Models.Survey {
    public class SurveySection : ILongIdentifiable, IHistorical, ISection {
        public virtual long Id { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        public virtual string Name { get; set; }
        public virtual string Help { get; set; }
        public virtual int Ordering { get; set; }

        public virtual string SectionType { get; set; }

        public virtual long OrgId { get; set; }
        public virtual long SurveyContainerId { get; set; }
        public virtual long SurveyId { get; set; }
		public virtual long? SectionTemplateId { get; set; }
		public virtual string SectionMergerKey { get; set; }

		public virtual ISurveyContainer _SurveyContainer { get; set; }
        public virtual ISurvey _Survey { get; set; }
        public virtual ICollection<IItemContainer> _Items { get; set; }

		#region Constructor
#pragma warning disable CS0618 // Type or member is obsolete
		public SurveySection(ISectionInitializerData data, string name, SurveySectionType sectionType,string mergerKey) : this() {
#pragma warning restore CS0618 // Type or member is obsolete
			Name = name;
            OrgId = data.OrgId;
            SectionType = "" + sectionType;
            SurveyContainerId = data.SurveyContainer.Id;
            SurveyId = data.Survey.Id;
            SectionTemplateId = null;
			SectionMergerKey = mergerKey;

            _SurveyContainer = data.SurveyContainer;
            _Survey = data.Survey;
        }

        [Obsolete("Use other constructor")]
        public SurveySection() {
            CreateTime = DateTime.UtcNow;
            _Items = new List<IItemContainer>();
        }
        #endregion

        public virtual IEnumerable<IItemContainer> GetItemContainers() {
            return _Items;
        }
        public virtual IEnumerable<IItem> GetItems() {
            return _Items.NotNull(x => x.Select(y => y.GetItem()));
        }
        public virtual void AppendItem(IItemContainer item) {
            _Items.Add(item);
        }
        public virtual string ToPrettyString() {
            return "Section: " + Name;
        }

        public virtual long GetSurveyId() {
            return SurveyId;
        }

        public virtual string GetSectionType() {
            return SectionType;
       }

        public virtual string GetName() {
            return Name;
        }

        public virtual string GetHelp() {
            return Help;
        }

        public virtual int GetOrdering() {
            return Ordering;
        }

		public virtual string GetSectionMergerKey() {
			return SectionMergerKey;
		}

		public class Map : ClassMap<SurveySection> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.Name).Length(512);
                Map(x => x.Help).Length(2000);
                Map(x => x.Ordering);
                Map(x => x.SectionType);
                Map(x => x.OrgId);
                Map(x => x.SurveyContainerId);
                Map(x => x.SurveyId);
				Map(x => x.SectionTemplateId);
				Map(x => x.SectionMergerKey);
			}
        }
        //public IEnumerator<IComponent> GetEnumerator() {
        //    yield return this;
        //    if (_Items != null) {
        //        foreach (var item in _Items)
        //            yield return item;
        //    }
        //}
        //IEnumerator IEnumerable.GetEnumerator() {
        //    return this.GetEnumerator();
        //}
    }
}