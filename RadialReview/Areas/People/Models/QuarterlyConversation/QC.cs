using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Models.QuarterlyConversation {
    public class QC : ILongIdentifiable, IHistorical {
        public virtual long Id { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual long OrgId { get; set; }
        public virtual long CreatedBy { get; set; }
        public virtual string Name { get; set; }
        public QC() {
            CreateTime = DateTime.UtcNow;
        }

        public class Map : ClassMap<QC> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.CreatedBy);
                Map(x => x.OrgId);
                Map(x => x.Name);
            }
        }
    }
}