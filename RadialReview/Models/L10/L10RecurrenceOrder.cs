using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.L10 {
    public class L10RecurrenceOrder : ILongIdentifiable {
        public virtual long Id { get; set; }
        public virtual long RecurrenceId { get; set; }
        public virtual long UserId { get; set; }
        public virtual int? Ordering { get; set; }

        public L10RecurrenceOrder() {

        }


        public class Map : ClassMap<L10RecurrenceOrder> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.RecurrenceId);
                Map(x => x.UserId);
                Map(x => x.Ordering);
            }
        }        
    }
}