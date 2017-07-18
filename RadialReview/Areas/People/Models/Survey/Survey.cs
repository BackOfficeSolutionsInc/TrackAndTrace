using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using FluentNHibernate.Mapping;

namespace RadialReview.Areas.People.Models.Survey {
    public class Survey : ILongIdentifiable, IHistorical, ISurvey {
        public virtual long Id { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        public virtual string Name { get; set; }
        public virtual string Help { get; set; }
        public virtual int Ordering { get; set; }

		public virtual SurveyType SurveyType { get; set; }

        public virtual long SurveyContainerId { get; set; }
        public virtual long OrgId { get; set; }

        public virtual ForModel By { get; set; }
        public virtual ForModel About { get; set; }

        public virtual ICollection<ISection> _Sections { get; set; }
		public virtual DateTime DueDate { get; set; }

		[Obsolete("Use other constructor")]
        public Survey() {
            CreateTime = DateTime.UtcNow;
            _Sections = new List<ISection>();
        }

#pragma warning disable CS0618 // Type or member is obsolete
		public Survey(string name,DateTime dueDate, ISurveyInitializerData data) : this() {
#pragma warning restore CS0618 // Type or member is obsolete
			Name = name;
            By = ForModel.From(data.By);
            About = ForModel.From(data.About);
            OrgId = data.OrgId;
            SurveyContainerId = data.SurveyContainer.Id;
			SurveyType = data.SurveyContainer.GetSurveyType();
			CreateTime = data.Now;
			DueDate = dueDate;
        }

        public virtual IEnumerable<ISection> GetSections() {
            return _Sections;
        }

        public virtual void AppendSection(ISection section) {
            _Sections.Add(section);
        }

        public virtual IForModel GetBy() {
            return By;
        }

        public virtual IForModel GetAbout() {
            return About;
        }

        public virtual string ToPrettyString() {
            return "Survey: " + Name + " [" + By.ModelId + "," + About.ModelId + "]";
        }

        public virtual long GetSurveyContainerId() {
            return SurveyContainerId;
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

		public virtual DateTime GetIssueDate() {
			return CreateTime;
		}
		public virtual DateTime? GetDueDate() {
			return DueDate;
		}

		public class Map : ClassMap<Survey> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.Name).Length(512);
                Map(x => x.Help).Length(2000);
                Map(x => x.Ordering);
				Map(x => x.SurveyContainerId);
				Map(x => x.DueDate);
				Map(x => x.SurveyType);
				Map(x => x.OrgId);
                Component(x => x.By).ColumnPrefix("By_");
                Component(x => x.About).ColumnPrefix("About_");
            }
        }

        //public IEnumerator<IComponent> GetEnumerator() {
        //    yield return this;
        //    if (_Sections != null) {
        //        foreach (var section in _Sections)
        //            foreach (var e in section)
        //                yield return e;
        //    }
        //}

        //IEnumerator IEnumerable.GetEnumerator() {
        //    return this.GetEnumerator();
        //}
    }
}