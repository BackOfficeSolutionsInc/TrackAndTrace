using FluentNHibernate.Mapping;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Models.QuarterlyConversation {
    public class QC_Answer : ILongIdentifiable, IHistorical {
        public virtual long Id { get; set; }

        public virtual ForModel ForModel { get; set; }

        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual long OrgId { get; set; }
        public virtual long QCId { get; set; }

        public virtual long ByUserId { get; set; }
        public virtual long AboutUserId { get; set; }

        public virtual string Answer { get; set; }
        public virtual long SectionId { get; set; }
        public virtual DateTime? CompleteTime { get; set; }


        public class Map : ClassMap<QC_Answer> {
            public Map() {
                Id(x => x.Id);
                Component(x => x.ForModel);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.OrgId);
                Map(x => x.QCId);
                Map(x => x.SectionId);
                Map(x => x.ByUserId);
                Map(x => x.AboutUserId);
                Map(x => x.Answer).Length(256);

            }
        }

    }
}