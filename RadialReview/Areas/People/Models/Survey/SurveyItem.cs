using FluentNHibernate.Mapping;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Models.Survey {
    public class SurveyItem : ILongIdentifiable, IHistorical, IItem {
        public virtual long Id { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        public virtual string Name { get; set; }
        public virtual string Help { get; set; }
        public virtual int Ordering { get; set; }

        public virtual ForModel Source { get; set; }

        public virtual long ItemFormatId { get; set; }

        public virtual long SurveyContainerId { get; set; }
        public virtual long SurveyId { get; set; }
        public virtual long SectionId { get; set; }

        public virtual long OrgId { get; set; }


        [Obsolete("Use other constructor")]
        public SurveyItem() {
            CreateTime = DateTime.UtcNow;
        }

        public SurveyItem(IItemInitializerData data, string name, IForModel source) : this() {
            Name = name;
            OrgId = data.OrgId;
            SurveyContainerId = data.SurveyContainer.Id;
            SurveyId = data.Survey.Id;
            SectionId = data.Section.Id;

            ItemFormatId = data.ItemFormat.Id;

            if (source != null) {
                Source = ForModel.From(source);
            }
        }
        public virtual IForModel GetSource() {
            return Source;
        }
        public virtual string ToPrettyString() {
            return "Item: " + Name;
        }

        public virtual long GetSectionId() {
            return SectionId;
        }

        public virtual long GetItemFormatId() {
            return ItemFormatId;
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

        public class Map : ClassMap<SurveyItem> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.Name).Length(512);
                Map(x => x.Help).Length(2000);
                Map(x => x.Ordering);
                Map(x => x.OrgId);
                Map(x => x.SurveyContainerId);
                Map(x => x.SurveyId);
                Map(x => x.ItemFormatId);
                Map(x => x.SectionId);
                Component(x => x.Source).ColumnPrefix("Source_");
            }
        }
    }
}