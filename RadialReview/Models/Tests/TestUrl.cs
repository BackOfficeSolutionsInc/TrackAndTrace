using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Tests
{
	public class TestResults
	{
		public TestUrlBatch Batch { get; set; }
		public List<TestUrlResult> Results { get; set; }

	}

	public class TestUrl : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual bool Active { get; set; }
		public virtual string Url { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual int ExpectedCode { get; set; }
		public virtual long AsUserId { get; set; }

		public TestUrl()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class TestUrlMap : ClassMap<TestUrl>
		{
			public TestUrlMap()
			{
				Id(x => x.Id);
				Map(x => x.Active);
				Map(x => x.Url);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.ExpectedCode);
				Map(x => x.AsUserId);
			}
		}
	}

	public class TestUrlBatch : ILongIdentifiable
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? CompleteTime { get; set; }

		public virtual int? Passed { get; set; }
		public virtual int? Failed { get; set; }

		public TestUrlBatch()
		{
			CreateTime = DateTime.UtcNow;
		}
		public class TestUrlBatchMap : ClassMap<TestUrlBatch>
		{
			public TestUrlBatchMap()
			{
				Id(x => x.Id);
				Map(x => x.Passed);
				Map(x => x.Failed);
				Map(x => x.CreateTime);
				Map(x => x.CompleteTime);
			}
		}
	}

	public class TestUrlResult : ILongIdentifiable
	{
		public virtual long Id { get; set; }

		public virtual DateTime StartTime { get; set; }
		public virtual DateTime? EndTime { get; set; }

		public virtual TestUrlBatch Batch { get; set; }
		public virtual TestUrl TestUrl { get; set; }

		public virtual int HttpCode { get; set; }
		public virtual bool Passed { get; set; }

		public virtual List<TestUrlResult> _History { get; set; } 
		public virtual string Error { get; set; }
		public virtual double DurationMs { get; set; }

		public TestUrlResult()
		{
			StartTime = DateTime.UtcNow;
		}

		public class TestUrlMap : ClassMap<TestUrlResult>
		{
			public TestUrlMap()
			{
				Id(x => x.Id);
				Map(x => x.StartTime);
				Map(x => x.EndTime);
				Map(x => x.HttpCode);
				Map(x => x.Passed);
				Map(x => x.DurationMs);
				References(x => x.Batch);
				References(x => x.TestUrl);
			}
		}

	}
}