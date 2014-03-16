using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Tasks
{
    public class ScheduledTask : IDeletable, ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual String Url { get; set; }
        public virtual DateTime Fire { get; set; }
        public virtual DateTime? Executed { get; set; }
        public virtual int ExceptionCount { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual DateTime? Started { get; set; }

    }

    public class ScheduledTaskMap : ClassMap<ScheduledTask>
    {
        public ScheduledTaskMap()
        {
            Id(x => x.Id);
            Map(x => x.Url);
            Map(x => x.Fire);
            Map(x => x.Started);
            Map(x => x.Executed);
            Map(x => x.DeleteTime);
            Map(x => x.ExceptionCount);
        }
    }
}