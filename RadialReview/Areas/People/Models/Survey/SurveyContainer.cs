using RadialReview.Engines.Surveys.Interfaces;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using FluentNHibernate.Mapping;

namespace RadialReview.Areas.People.Models.Survey {
    public class SurveyContainer : ILongIdentifiable, IHistorical, ISurveyContainer {
        public virtual long Id { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        public virtual string Name { get; set; }
        public virtual string Help { get; set; }
        public virtual int Ordering { get; set; }
        public virtual long OrgId { get; set; }

        public virtual ForModel CreatedBy { get; set; }
        public virtual SurveyType SurveyType { get; set; }
        
        public virtual ICollection<ISurvey> _Surveys { get; set; }

        public SurveyContainer(IForModel createdBy,string name, long orgid, SurveyType type, string help) : this() {
            CreatedBy = ForModel.From(createdBy);
            Name = name;
            SurveyType = type;
            Help = help;
            OrgId = orgid;
        }

        [Obsolete("Use other constructor")]
        public SurveyContainer() {
            CreateTime = DateTime.UtcNow;
            Help = "";
            _Surveys = new List<ISurvey>();
        }

        public virtual IEnumerable<ISurvey> GetSurveys() {
            return _Surveys;
        }

        public virtual void AppendSurvey(ISurvey survey) {
            _Surveys.Add(survey);
        }

        public virtual string ToPrettyString() {
            return "SurveyContainer: " + Name;
        }

        public virtual SurveyType GetSurveyType() {
            return SurveyType;
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

        public class Map : ClassMap<SurveyContainer> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.Name).Length(512);
                Map(x => x.Help).Length(2000);
                Map(x => x.Ordering);
                Map(x => x.OrgId);
                Map(x => x.SurveyType);
                Component(x => x.CreatedBy).ColumnPrefix("CreatedBy_");
            }
        }

        //public IEnumerator<IComponent> GetEnumerator() {
        //    yield return this;
        //    if (_Surveys != null) {
        //        foreach (var survey in _Surveys)
        //            foreach (var e in survey)
        //                yield return e;
        //    }
        //}
        //IEnumerator IEnumerable.GetEnumerator() {
        //    return this.GetEnumerator();
        //}
    }
}