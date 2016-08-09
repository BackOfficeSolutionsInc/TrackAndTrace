using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.L10;
using NHibernate.Criterion;
using RadialReview.Models.Interfaces;
using System.Linq.Expressions;

namespace RadialReview.Utilities.DataTypes
{
	public class DateRange
	{

		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }

		public DateRange(DateTime start, DateTime end)
		{
			StartTime = start;
			EndTime = end;
		}

		public DateRange()
		{
			StartTime = DateTime.MinValue;
			EndTime = DateTime.MaxValue;
		}


	}
	public static class DateRangeExtensions
	{
		public static Expression<Func<T, bool>> Filter<T>(this DateRange range) where T : IHistorical
		{
			if (range == null) {
				return x => true;
			}
			return (T x) => x.CreateTime <= range.EndTime && (x.DeleteTime == null || x.DeleteTime >= range.StartTime);
			
		}
	}
}
