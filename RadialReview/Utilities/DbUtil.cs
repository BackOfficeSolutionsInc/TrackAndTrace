using System.Collections;
using NHibernate;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
	public static class DbUtil
	{
		public static void UpdateList<T>(this ISession s, IEnumerable<T> oldValues, IEnumerable<T> newValues, DateTime? now=null) where T : IOneToMany
		{
			now = now ?? DateTime.UtcNow;
			var update = SetUtility.AddRemove(oldValues, newValues, x => ((ILongIdentifiable)x).Id);
			foreach (var u in update.RemovedValues)
			{
				((IDeletable)u).DeleteTime = now;
				s.Update(u);
			}
			foreach (var a in update.AddedValues){
				s.Save(a);
			}
		}
		/*
		public static void UpdateLists<T>(this ISession self, T newValue, DateTime now, params Func<T, IEnumerable>[] selectors) where T : ILongIdentifiable
		{
			var id = newValue.Id;
			if (id != 0)
			{
				var old = (T)self.Get(typeof(T), id);
				foreach (var s in selectors)
				{
					var oldList = s(old);
					var newList = s(newValue);
					UpdateList(oldList, newList, now);

				}
			}
		}*/
		/*
		public static void UpdateLists<T>(this ISession self, T newValue,DateTime? now=null) where T : class, ILongIdentifiable
		{
			if (newValue.Id == 0){
				self.Save(newValue);
			}

			now = now ?? DateTime.UtcNow;
			var toUpdate = new List<object>();
			var toSave = new List<object>();

			T oldValue = null;
			foreach (var p in newValue.GetType().GetProperties()){
				var isEnumerable = p.PropertyType.GetInterfaces().Any(y => y == typeof (IEnumerable));
				if (isEnumerable)
				{
					if (p.PropertyType.GetGenericArguments().Any()){
						var isOneMany = p.PropertyType.GetGenericArguments()[0].GetInterfaces().Any(y => y == typeof (IOneToMany));
						if (isOneMany){
							if (oldValue == null){
								oldValue = self.Get<T>(newValue.Id);
							}

							var oldList = ((IEnumerable<IOneToMany>)p.GetValue(oldValue));
							var newList = (IEnumerable<IOneToMany>)p.GetValue(newValue);

							if (oldList != null && newList != null){
								
								oldList=oldList.Where(x=>x.DeleteTime==null);
								var update = SetUtility.AddRemove(oldList, newList, x => x.UniqueKey());
								foreach (var u in update.RemovedValues){
									u.DeleteTime = now;
									//self.Update(u);
									toUpdate.Add(u);
								}
								foreach (var u in update.AddedValues){
									toSave.Add(u);
									//self.Save(u);
								}
							}

							p.SetValue(newValue, null);//update.AddedValues.Select(x => Convert.ChangeType(x, p.PropertyType.GetGenericArguments()[0])));
						}
					}
				}
			}

			if (oldValue != null){
				//
				foreach (var u in toUpdate)
					self.Update(u);
				self.Evict(oldValue);
				foreach (var s in toSave){
					self.Save(s);
				self.Update(newValue);
				}
			}
		}*/

	}
}