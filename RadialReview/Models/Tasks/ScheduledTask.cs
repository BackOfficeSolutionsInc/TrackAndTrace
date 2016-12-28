﻿using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Tasks
{
    public class ScheduledTask : IDeletable, ILongIdentifiable
    {

		public const string MonthlyPaymentPlan = "MONTHLY_PAYMENT_PLAN";
		public const string Prereview = "CREATE_REVIEW";

		public virtual long Id { get; set; }
		public virtual long? CreatedFromTaskId { get; set; }
		public virtual long OriginalTaskId { get; set; }
		public virtual String Url { get; set; }
		public virtual DateTime? FirstFire { get; set; }
		public virtual DateTime Fire { get; set; }
        public virtual DateTime? Executed { get; set; }
        public virtual int ExceptionCount { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual DateTime? Started { get; set; }
		public virtual TimeSpan? NextSchedule { get; set; }
		public virtual bool EmailOnException { get; set; }

		public virtual DateTime CreateTime { get; set; }

		public virtual String TaskName { get; set; }
	    public virtual int? MaxException { get; set; }

        public ScheduledTask()
        {
			CreateTime = DateTime.UtcNow;
        }

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
			Map(x => x.TaskName);
			Map(x => x.FirstFire);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
			Map(x => x.ExceptionCount);
			Map(x => x.NextSchedule);
			Map(x => x.MaxException);
			Map(x => x.EmailOnException);
			Map(x => x.CreatedFromTaskId);
			Map(x => x.OriginalTaskId);
		}
    }
}