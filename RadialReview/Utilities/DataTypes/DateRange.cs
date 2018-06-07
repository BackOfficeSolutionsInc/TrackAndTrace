using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.L10;
using NHibernate.Criterion;
using RadialReview.Models.Interfaces;
using System.Linq.Expressions;
using NHibernate.Envers.Query.Criteria;
using NHibernate.Envers.Query;

namespace RadialReview.Utilities.DataTypes {
	public class DateRange {

		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public bool IncludeCurrent { get; set; }


		public DateRange(long? start, long? end) {
			StartTime = (start == null) ? DateTime.MinValue : start.Value.ToDateTime();
			EndTime = (end == null) ? DateTime.MaxValue : end.Value.ToDateTime();
		}

		public DateRange(DateTime start, DateTime end) {
			StartTime = start;
			EndTime = end;
		}

		public DateRange() {
			StartTime = DateTime.MinValue;
			EndTime = DateTime.MaxValue;
		}

		public static DateRange Instant(DateTime time) {
			return new DateRange(time, time);
		}

		public TimeSpan ToTimespan() {
			return EndTime - StartTime;
		}

		public static DateRange Full() {
			return new DateRange(DateTime.MinValue, DateTime.MaxValue);
		}

	}
	public static class DateRangeExtensions {

		/// <summary>
		/// Filters IDeletable. Use Filter.Compile() for Linq
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="range"></param>
		/// <returns></returns>
		public static Expression<Func<T, bool>> FilterRestricted<T>(this DateRange range) where T : IDeletable {
			if (range == null) {
				return (T x) => x.DeleteTime == null; /// x => true
			}
			return (T x) => (x.DeleteTime == null || x.DeleteTime >= range.StartTime);

		}

		/// <summary>
		/// Filters IHistoricals. Use Filter.Compile() for Linq. Was alive any time during this range
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="range"></param>
		/// <returns></returns>
		public static Expression<Func<T, bool>> Filter<T>(this DateRange range) where T : IHistorical {
			if (range == null) {
				return (T x) => x.DeleteTime == null; /// x => true
			}
			return (T x) => x.CreateTime <= range.EndTime && (x.DeleteTime == null || x.DeleteTime >= range.StartTime);
		}

		public static IAuditCriterion FilterAudit<T>(this DateRange range) where T : class, IHistorical, new() {

			var createProp = AuditEntity.Property(HibernateSession.Names.ColumnName<T>(x => x.CreateTime));
			var deleteProp = AuditEntity.Property(HibernateSession.Names.ColumnName<T>(x => x.DeleteTime));

			var ands = new AuditConjunction();
			ands.Add(createProp.Le(range.EndTime));//x.CreateTime <= range.EndTime
			var ors = new AuditDisjunction();
			ors.Add(deleteProp.IsNull());
			ors.Add(deleteProp.Ge(range.StartTime));// (x.DeleteTime == null || x.DeleteTime >= range.StartTime)
			ands.Add(ors);

			return ands;
		}

		public static bool AliveAt(this IHistorical historical, DateTime time) {
			return DateRange.Instant(time).Filter<IHistorical>().Compile()(historical);
		}

		public static Expression<Func<T, bool>> Filter<T>(this DateRange range, Func<T, DateTime> transform) {
			if (range == null) {
				return x => true; /// x => true
			}
			return (T x) => transform(x) <= range.EndTime && transform(x) >= range.StartTime;
		}

		public static Expression<Func<T, bool>> Filter<T>(this DateRange range, bool allowNull, Func<T, DateTime?> transform) {
			if (range == null) {
				if (allowNull)
					return x => true; /// x => true
				return (T x) => transform(x) != null;
			}
			if (allowNull)
				return (T x) => transform(x) == null || (transform(x) <= range.EndTime && transform(x) >= range.StartTime);
			return (T x) => transform(x) != null && transform(x) <= range.EndTime && transform(x) >= range.StartTime;

		}
	}
}
