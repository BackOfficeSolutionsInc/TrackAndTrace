using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular;
/*
namespace RadialReview.Models.Angular
{
	public class AngularList<T> : Dictionary<string,bool> where T : Angular
	{
		private List<T> _Backing =new List<T>();
		private Angular _Parent;

		public AngularList(Angular parent){
			if (parent==null)
				throw new NullReferenceException();
			_Parent = parent;
		}

		public void Add(T item)
		{
			_Backing.Add(item);
			Add(item.Key,true);
			_Parent.AddItem(item);
		}
		
		public void Remove(T item)
		{
			Add(item.Key, false);
			_Backing.RemoveAll(x => x.Key == item.Key);
			_Parent.RemoveItem(item);
		}

	}
}

namespace RadialReview
{
	public static class AngularListExtensions
	{
		public static AngularList<T> ToAngularList<T>(this IEnumerable<T> self,Angular parent) where T : Angular
		{
			var result = new AngularList<T>(parent);
			foreach (var i in self){
				result.Add(i);
			}
			return result;
		}

	}

}*/