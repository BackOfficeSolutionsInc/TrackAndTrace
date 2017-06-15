using log4net;
using NHibernate;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Interfaces;
using RadialReview.Reflection;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace RadialReview.Accessors {
	public class BaseAccessor {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected static Dictionary<string, object> CacheLookup = new Dictionary<string, object>();

		protected class Reordering {
			private Reordering() { }
			public static Reordering<T> Create<T>(IEnumerable<T> items, long TId, long? recurrenceId, int oldOrder, int newOrder, Expression<Func<T, int>> orderVariable, Expression<Func<T, long>> idVariable) where T : ILongIdentifiable {
				var ordered = items.OrderBy(orderVariable.Compile()).ToList();
				return new Reordering<T> {
					Items = ordered,
					OldOrder = oldOrder,
					NewOrder = newOrder,
					SelectedId = TId,
					RecurrenceId = recurrenceId,
					OrderVariable = orderVariable,
					IdVariable = idVariable,
				};
			}
		}
		protected class Reordering<T> {
			public List<T> Items { get; set; }
			public Expression<Func<T, long>> IdVariable { get; set; }
			public Expression<Func<T, int>> OrderVariable { get; set; }
			public int OldOrder { get; set; }
			public int NewOrder { get; set; }
			public long SelectedId { get; set; }
			public long? RecurrenceId { get; set; }

			public bool ApplyReorder(ISession s) {
				return ApplyReorder(null, s, null);
			}
			/// </summary>
			/// <param name="rt"></param>
			/// <param name="s"></param>
			/// <param name="ConstructAngularObject"> [Id,Order, new AngularItem(Id){Ordering=order}]</param>
			/// <returns></returns>
			public bool ApplyReorder(RealTimeUtility rt, ISession s, Func<long, int, T, IAngularItem> constructAngularObject) {

				var allItems = Items.OrderBy(OrderVariable.Compile()).ToList();

				var found = allItems.ElementAtOrDefault(OldOrder);
				bool doReorder = true;
				if (found != null && found.Get(IdVariable) == SelectedId) {
					allItems.RemoveAt(OldOrder);
					allItems.Insert(Math.Min(allItems.Count, NewOrder), found);
				} else {
					//fallback
					var located = allItems.Select((x, i) => new { Item = x, Index = i })
						.FirstOrDefault(x => x.Item.Get(IdVariable) == SelectedId);

					if (located != null) {
						allItems.RemoveAt(located.Index);
						allItems.Insert(Math.Min(allItems.Count, NewOrder), located.Item);
					} else {
						doReorder = false;
					}
				}

				if (doReorder) {
					var updater = RecurrenceId == null || rt == null ? null : rt.UpdateRecurrences(RecurrenceId.Value);
					var index = 0;
					var anyMoved = false;
					foreach (var rm in allItems) {
						if (rm.Get(OrderVariable) != index) {
							anyMoved = true;
							rm.Set(OrderVariable, index);
							s.Update(rm);
							if (updater != null) {
								try {
									updater.Update(constructAngularObject(rm.Get(IdVariable), index, rm));
								} catch (Exception) {
								}
							}
						}
						index++;
					}

					if (!anyMoved && updater != null) {
						index = 0;
						//resend all indicies
						foreach (var rm in allItems) {
							try {
								updater.Update(constructAngularObject(rm.Get(IdVariable), index, rm));
							} catch (Exception) {
							}
						}
						index++;
					}
				}
				return doReorder;
			}
		}
	}
}
