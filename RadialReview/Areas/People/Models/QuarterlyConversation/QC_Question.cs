using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Models.QuarterlyConversation {
    public class QC_Question : ILongIdentifiable, IHistorical {
        public virtual long Id { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual long OrgId { get; set; }
        public virtual long QCId { get; set; }
        public virtual string Name { get; set; }
        public virtual string Details { get; set; }


        public class Map : ClassMap<QC_Question> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.OrgId);
                Map(x => x.QCId);
                Map(x => x.Name).Length(256);
                Map(x => x.Details).Length(2048);
            }
        }

    }
}