using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Application {
    public class Tracker {

        public enum TrackerSource : int {
            Email = 1,
            Website = 2,
        }

        public virtual long Id { get; set; }
        public virtual string ResGuid { get; set; }
        public virtual DateTime ViewedAt { get; set; }
        public virtual long? ViewedBy { get; set; }
        public virtual TrackerSource? Source { get; set; }
        public Tracker()
        {
            ViewedAt = DateTime.UtcNow;
        }
        public class Map : ClassMap<Tracker> {
            public Map()
            {
                Id(x => x.Id);
                Map(x => x.ResGuid);
                Map(x => x.ViewedAt);
                Map(x => x.ViewedBy);
                Map(x => x.Source).CustomType<TrackerSource>();
            }
        }
    }
}